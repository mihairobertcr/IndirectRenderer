using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class IndirectRendererConfig
{
    [Header("Rendering")]
    public Camera RenderCamera;
    public Camera DebugCamera;

    [Header("Compute Shaders")]
    public ComputeShader MatricesInitializer;
    public ComputeShader LodBitonicSorter;
    public ComputeShader InstancesCuller;
    public ComputeShader InstancesScanner;
    public ComputeShader GroupSumsScanner;
    public ComputeShader InstancesDataCopier;
    
    [Header("Debug")]
    public bool LogMatrices;
    public bool LogArgumentsBufferAfterReset;
    public bool LogArgumentsAfterOcclusion;
    public bool LogInstancesIsVisibleBuffer;
    public bool LogGroupSumsBuffer;
    public bool LogScannedPredicates;
    public bool LogScannedGroupSumsBuffer;
    public bool LogCulledMatrices;
    public bool LogArgsBufferAfterCopy;
    public bool LogSortingData;
    public bool DebugBounds;
}

[Serializable]
public class IndirectRendererSettings
{
    public bool RunCompute = true;
    public bool DrawInstances = true;
    public bool DrawShadows = true;
    public bool ComputeAsync = true;
    
    [Space]
    public bool EnableFrustumCulling = true;
    public bool EnableOcclusionCulling = true;
    public bool EnableDetailCulling = true;
    public bool EnableLod = true;
    public bool EnableOnlyLod2Shadows = true;
    
    [Range(00.00f, 00.02f)] 
    public float DetailCullingPercentage = 0.005f;
}

[Serializable]
public class TransformDto
{
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;
}

[Serializable]
public class IndirectMesh
{
    public GameObject Prefab;
    public Material Material;

    [Space] 
    [Header("Mesh")]
    public Mesh CombinedMesh;
    public bool RecombineLods;
    public Mesh Lod0Mesh;
    public Mesh Lod1Mesh;
    public Mesh Lod2Mesh;

    public TransformDto Offset;
    public Bounds Bounds;

    [Space] 
    [Header("Instances Transforms")]
    public List<Vector3> Positions;
    public List<Vector3> Rotations;
    public List<Vector3> Scales;
    
    public MaterialPropertyBlock Lod0PropertyBlock;
    public MaterialPropertyBlock Lod1PropertyBlock;
    public MaterialPropertyBlock Lod2PropertyBlock;
    
    public MaterialPropertyBlock ShadowLod0PropertyBlock;
    public MaterialPropertyBlock ShadowLod1PropertyBlock;
    public MaterialPropertyBlock ShadowLod2PropertyBlock;

    public void Initialize()
    {
        InitializeCombinedMesh();
        InitializeMaterialPropertyBlocks();
    }

    private void InitializeCombinedMesh()
    {
        if (RecombineLods) return;
        
        CombinedMesh = new Mesh();
        CombinedMesh.name = Prefab.name;
        var combinedMeshes = new CombineInstance[]
        {
            new() { mesh = Lod0Mesh },
            new() { mesh = Lod1Mesh },
            new() { mesh = Lod2Mesh }
        };
        
        CombinedMesh.CombineMeshes(
            combine: combinedMeshes,
            mergeSubMeshes: false,
            useMatrices: false,
            hasLightmapData: false);
        
        CombinedMesh.RecalculateTangents();
        CombinedMesh.RecalculateNormals();
        
        // ----- DEBUG
        // var gameObject = new GameObject($"{Prefab.name}_Debug");
        // var filter = gameObject.AddComponent<MeshFilter>();
        // gameObject.AddComponent<MeshRenderer>();
        // filter.mesh = CombinedMesh;
        // UnityEditor.Formats.Fbx.Exporter.ModelExporter.ExportObject($"Assets/{gameObject}.fbx", gameObject);
        // -----
    }

    private void InitializeMaterialPropertyBlocks()
    {
        Lod0PropertyBlock = new MaterialPropertyBlock();
        Lod1PropertyBlock = new MaterialPropertyBlock();
        Lod2PropertyBlock = new MaterialPropertyBlock();
        
        ShadowLod0PropertyBlock = new MaterialPropertyBlock();
        ShadowLod1PropertyBlock = new MaterialPropertyBlock();
        ShadowLod2PropertyBlock = new MaterialPropertyBlock();
    }
}

public class IndirectRendererComponent : MonoBehaviour
{
    [SerializeField] private IndirectRendererConfig _config;
    [SerializeField] private IndirectRendererSettings _settings;
    [SerializeField] private HierarchicalDepthMap _hizMap;
    [SerializeField] private IndirectMesh[] _instances;

    private IndirectRenderer _renderer;
    
    List<Vector3> _positions = new List<Vector3>();
    List<Vector3> _scales = new List<Vector3>();
    List<Vector3> _rotations = new List<Vector3>();

    private void Start()
    {
        foreach (var instance in _instances)
        {
            for (var i = 0; i < 128; i++)
            {
                for (var j = 0; j < 128; j++)
                {
                    instance.Positions.Add(new Vector3
                    {
                        x = i,
                        y = .5f,
                        z = j
                    });
                
                    instance.Rotations.Add(new Vector3
                    {
                        x = 0f,
                        y = 0f,
                        z = 0f
                    });
                
                    instance.Scales.Add(new Vector3
                    {
                        x = .75f,
                        y = .75f,
                        z = .75f
                    });
                }
            }
        }

        _renderer = new IndirectRenderer(_instances, _config, _settings);
    }

    private void OnDestroy()
    {
        _renderer.Dispose();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        
        _renderer.DrawGizmos();
    }
}
