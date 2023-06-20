using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

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

    private IndirectRenderer _renderer;

    private void Start()
    {
        var positions = new List<Vector3>();
        var scales = new List<Vector3>();
        var rotations = new List<Vector3>();

        for (var i = 0; i < 128; i++)
        {
            for (var j = 0; j < 128; j++)
            {
                positions.Add(new Vector3
                {
                    x = i,
                    y = .5f,
                    z = j
                });
                
                rotations.Add(new Vector3
                {
                    x = 0f,
                    y = 0f,
                    z = 0f
                });
                
                scales.Add(new Vector3
                {
                    x = .75f,
                    y = .75f,
                    z = .75f
                });
            }
        }
        
        _renderer = new IndirectRenderer(_config, positions, rotations, scales);
    }

    // private void Update()
    // {
    //     Graphics.DrawMeshInstancedIndirect(
    //         mesh: _meshProperties.Mesh,
    //         submeshIndex: 0,
    //         material: _meshProperties.Material,
    //         bounds: new Bounds(Vector3.zero, Vector3.one * 1000),
    //         bufferWithArgs: ShaderBuffers.InstancesArgsBuffer,
    //         argsOffset: 0, //ARGS_BYTE_SIZE_PER_DRAW_CALL,
    //         properties: _meshProperties.Lod2PropertyBlock,
    //         castShadows: ShadowCastingMode.On,
    //         receiveShadows: true);
    //     // camera: Camera.main);   
    // }

    private void OnDestroy()
    {
        // ShaderBuffers.Dispose();
    }
}
