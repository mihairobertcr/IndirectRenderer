using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[Serializable]
public class IndirectRendererConfig
{
    [Header("Rendering")]
    public Camera RenderCamera;
    public Camera DebugCamera;
    
    public Material Material;
    
    public Mesh Lod0Mesh;
    public Mesh Lod1Mesh;
    public Mesh Lod2Mesh;

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
public class MeshProperties
{
    public Mesh Mesh;
    public Material Material;
    
    public uint Lod0Vertices;
    public uint Lod1Vertices;
    public uint Lod2Vertices;
    
    public uint Lod0Indices;
    public uint Lod1Indices;
    public uint Lod2Indices;
    
    public MaterialPropertyBlock Lod0PropertyBlock;
    public MaterialPropertyBlock Lod1PropertyBlock;
    public MaterialPropertyBlock Lod2PropertyBlock;
    
    public MaterialPropertyBlock ShadowLod0PropertyBlock;
    public MaterialPropertyBlock ShadowLod1PropertyBlock;
    public MaterialPropertyBlock ShadowLod2PropertyBlock;
}

public class IndirectRendererComponent : MonoBehaviour
{
    [SerializeField] private IndirectRendererConfig _config;
    [SerializeField] private IndirectRendererSettings _settings;
    [FormerlySerializedAs("_hizBufferConfig")] [SerializeField] private HierarchicalDepthMap hizMap;

    private IndirectRenderer _renderer;
    
    List<Vector3> _positions = new List<Vector3>();
    List<Vector3> _scales = new List<Vector3>();
    List<Vector3> _rotations = new List<Vector3>();

    private void Start()
    {
        for (var i = 0; i < 128; i++)
        {
            for (var j = 0; j < 128; j++)
            {
                _positions.Add(new Vector3
                {
                    x = i,
                    y = .5f,
                    z = j
                });
                
                _rotations.Add(new Vector3
                {
                    x = 0f,
                    y = 0f,
                    z = 0f
                });
                
                _scales.Add(new Vector3
                {
                    x = .75f,
                    y = .75f,
                    z = .75f
                });
            }
        }
        
        _renderer = new IndirectRenderer(_config, _settings, hizMap, _positions, _rotations, _scales);
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
