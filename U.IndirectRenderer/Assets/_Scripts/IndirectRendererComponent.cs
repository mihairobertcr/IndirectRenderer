using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class IndirectRendererConfig
{
    [Header("Rendering")]
    public Camera Camera;

    [Header("Compute Shaders")]
    public ComputeShader MatricesInitializer;
    public ComputeShader LodBitonicSorter;
    public ComputeShader InstancesCuller;
    public ComputeShader InstancesScanner;
    public ComputeShader GroupSumsScanner;
    public ComputeShader InstancesDataCopier;
}

[Serializable]
public class IndirectRendererSettings
{
    [Header("Execution")]
    public bool RunCompute = true;
    public bool DrawInstances = true;
    public bool DrawShadows = true;
    public bool ComputeAsync = true;
    
    [Space]
    [Header("Features")]
    public bool EnableFrustumCulling = true;
    public bool EnableOcclusionCulling = true;
    public bool EnableDetailCulling = true;
    public bool EnableLod = true;
    public bool EnableOnlyLod2Shadows = true;
    
    [Space]
    [Header("Details")]
    [Range(00.00f, 00.02f)] 
    public float DetailCullingPercentage = 0.005f;
    
    [Space]
    [Header("Debug")]
    public bool DrawBounds;
    public bool LogMatrices;
    public bool LogArgumentsAfterReset;
    public bool LogArgumentsAfterOcclusion;
    public bool LogVisibilityBuffer;
    public bool LogGroupSums;
    public bool LogScannedPredicates;
    public bool LogScannedGroupSums;
    public bool LogCulledMatrices;
    public bool LogArgumentsAfterCopy;
    public bool LogSortingData;
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
    [Header("Rendering")]
    public GameObject Prefab;
    public Material Material;

    [Space] 
    [Header("Mesh")]
    public Mesh CombinedMesh;
    public bool RecombineLods;
    public Mesh Lod0Mesh;
    public Mesh Lod1Mesh;
    public Mesh Lod2Mesh;

    [Space] 
    [Header("Location")]
    public TransformDto Offset;
    public Bounds Bounds;

    [Space] 
    [Header("Instances")]
    public List<TransformDto> Transforms; //TODO: Consider making it an array
    
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
        if (!RecombineLods) return;

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
    [SerializeField] private IndirectMesh[] _instances;

    private IndirectRenderer _renderer;

    private void Start()
    {
        foreach (var instance in _instances)
        {
            for (var i = 0; i < 128; i++)
            {
                for (var j = 0; j < 128; j++)
                {
                    var data = new TransformDto
                    {
                        Position = new Vector3
                        {
                            x = i,
                            y = .5f,
                            z = j
                        },
                        
                        Rotation = new Vector3
                        {
                            x = 0f,
                            y = 0f,
                            z = 0f
                        },
                        
                        Scale = new Vector3
                        {
                            x = .75f,
                            y = .75f,
                            z = .75f
                        }
                    };

                    data.Position += instance.Offset.Position;
                    data.Rotation += instance.Offset.Rotation;
                    data.Scale += instance.Offset.Scale;
                    
                    instance.Transforms.Add(data);
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
