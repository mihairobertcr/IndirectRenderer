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
    public const int NUMBER_OF_ARGS_PER_INSTANCE_TYPE = NUMBER_OF_DRAW_CALLS * NUMBER_OF_ARGS_PER_DRAW;  // 3draws * 5args = 15args
    
    private const int NUMBER_OF_DRAW_CALLS = 3;                                                           // (LOD00 + LOD01 + LOD02)
    private const int NUMBER_OF_ARGS_PER_DRAW = 5;                                                        // (indexCount, instanceCount, startIndex, baseVertex, startInstance)
    private const int ARGS_BYTE_SIZE_PER_DRAW_CALL = NUMBER_OF_ARGS_PER_DRAW * sizeof(uint); // 5args * 4bytes = 20 bytes
    
    // private const uint BITONIC_BLOCK_SIZE     = 256;
    // private const uint TRANSPOSE_BLOCK_SIZE   = 8;
    
 
    [SerializeField] private IndirectRendererConfig _config;

    private MatricesInitializer _matricesInitializer;
    private LodBitonicSorter _lodBitonicSorter;

    private int _numberOfInstances = 16384;
    private int _numberOfInstanceTypes = 1;
    
    private Vector3 _cameraPosition = Vector3.zero;
    
    private MeshProperties _meshProperties = new();

    private void Start()
    {
        var positions = new List<Vector3>();
        var scales = new List<Vector3>();
        var rotations = new List<Vector3>();
        // var sortingData = new List<SortingData>();

        //TODO: Look into thread allocation
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
        
        // Argument buffer used by DrawMeshInstancedIndirect.
        // var args = new uint[5] { 0, 0, 0, 0, 0 };
        
        // Arguments for drawing Mesh.
        // 0 == number of triangle indices, 1 == population,
         // others are only relevant if drawing submeshes.
        // args[0] = _config.Lod0Mesh.GetIndexCount(0);
        // args[1] = (uint)_numberOfInstances;
        // args[2] = _config.Lod0Mesh.GetIndexStart(0);
        // args[3] = _config.Lod0Mesh.GetBaseVertex(0);
        // var size = args.Length * sizeof(uint);
        
        var args = new uint[NUMBER_OF_ARGS_PER_INSTANCE_TYPE]; //new uint[_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE]
        
        // Initialize Mesh
        _meshProperties.Lod0Vertices = (uint)_config.Lod0Mesh.vertexCount;
        _meshProperties.Lod1Vertices = (uint)_config.Lod1Mesh.vertexCount;
        _meshProperties.Lod2Vertices = (uint)_config.Lod2Mesh.vertexCount;
        
        _meshProperties.Lod0Indices = _config.Lod0Mesh.GetIndexCount(0);
        _meshProperties.Lod1Indices = _config.Lod1Mesh.GetIndexCount(0);
        _meshProperties.Lod2Indices = _config.Lod2Mesh.GetIndexCount(0);
        
        _meshProperties.Mesh = new Mesh();
        _meshProperties.Mesh.name = "Mesh"; // TODO: name it
        _meshProperties.Mesh.CombineMeshes(new CombineInstance[] 
        {
            new() { mesh = _config.Lod0Mesh},
            new() { mesh = _config.Lod1Mesh},
            new() { mesh = _config.Lod2Mesh}
        },
        true,    // Merge Submeshes 
        false,   // Use Matrices
        false);  // Has lightmap data
        
        // Arguments
        // Buffer with arguments has to have five integer numbers
        // Lod 0
        args[0] = _meshProperties.Lod0Indices;      // 0 - index count per instance, 
        args[1] = 1;                    // 1 - instance count
        args[2] = 0;                    // 2 - start index location
        args[3] = 0;                    // 3 - base vertex location
        args[4] = 0;                    // 4 - start instance location
        
        // Lod 1
        args[5] = _meshProperties.Lod1Indices;      // 0 - index count per instance, 
        args[6] = 1;                    // 1 - instance count
        args[7] = args[0] + args[2];    // 2 - start index location
        args[8] = 0;                    // 3 - base vertex location
        args[9] = 0;                    // 4 - start instance location
        
        // Lod 2
        args[10] = _meshProperties.Lod2Indices;     // 0 - index count per instance, 
        args[11] = 1;                   // 1 - instance count
        args[12] = args[5] + args[7];   // 2 - start index location
        args[13] = 0;                   // 3 - base vertex location
        args[14] = 0;                   // 4 - start instance location
        
        // Materials
        _meshProperties.Material = _config.Material;

        // Note: Considering we have multiple types of meshes
        // I don't know how this is working right now but
        // it should preserve the sorting functionality
        var count = NUMBER_OF_ARGS_PER_INSTANCE_TYPE; // * _numberOfInstanceTypes
        ShaderBuffers.InstancesArgsBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.IndirectArguments);
        ShaderBuffers.InstancesArgsBuffer.SetData(args);
        
        ShaderKernels.Initialize(_config);
        
        _matricesInitializer = new MatricesInitializer(_config.MatricesInitializer, _meshProperties, _numberOfInstances);
        _matricesInitializer.Initialize(positions, rotations, scales);
        _matricesInitializer.Dispatch();

        _cameraPosition = _config.RenderCamera.transform.position;

        _lodBitonicSorter = new LodBitonicSorter(_config.LodBitonicSorter, _numberOfInstances);
        _lodBitonicSorter.Initialize(positions, _cameraPosition);

        LogInstanceDrawMatrices();
        
        // TODO: OnPreCull
        _lodBitonicSorter.Dispatch();

        LogSortingData();
    }

    private void Update()
    {
        // On pre cull
        _cameraPosition = _config.RenderCamera.transform.position;

        Graphics.DrawMeshInstancedIndirect(
            mesh: _meshProperties.Mesh,
            submeshIndex: 0,
            material: _meshProperties.Material,
            bounds: new Bounds(Vector3.zero, Vector3.one * 1000),
            bufferWithArgs: ShaderBuffers.InstancesArgsBuffer,
            argsOffset: 0, //ARGS_BYTE_SIZE_PER_DRAW_CALL,
            properties: _meshProperties.Lod2PropertyBlock,
            castShadows: ShadowCastingMode.On,
            receiveShadows: true);
        // camera: Camera.main);   
    }

    private void OnDestroy()
    {
        ShaderBuffers.Dispose();
    }

    #region Logging
    
    private void LogInstanceDrawMatrices(string prefix = "")
    {
        var matrix1 = new Indirect2x2Matrix[_numberOfInstances];
        var matrix2 = new Indirect2x2Matrix[_numberOfInstances];
        var matrix3 = new Indirect2x2Matrix[_numberOfInstances];
        
        ShaderBuffers.InstanceMatrixRows01.GetData(matrix1);
        ShaderBuffers.InstanceMatrixRows23.GetData(matrix2);
        ShaderBuffers.InstanceMatrixRows45.GetData(matrix3);
        
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(prefix))
        {
            stringBuilder.AppendLine(prefix);
        }
        
        for (var i = 0; i < matrix1.Length; i++)
        {
            stringBuilder.AppendLine(
                i + "\n" 
                  + matrix1[i].FirstRow + "\n"
                  + matrix1[i].SecondRow + "\n"
                  + matrix2[i].FirstRow + "\n"
                  + "\n\n"
                  + matrix2[i].SecondRow + "\n"
                  + matrix3[i].FirstRow + "\n"
                  + matrix3[i].SecondRow + "\n"
                  + "\n"
            );
        }
    
        Debug.Log(stringBuilder.ToString());
    }
    
    private void LogSortingData(string prefix = "")
    {
        var sortingData = new SortingData[_numberOfInstances];
        ShaderBuffers.InstancesSortingData.GetData(sortingData);
        
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(prefix))
        {
            stringBuilder.AppendLine(prefix);
        }
        
        uint lastDrawCallIndex = 0;
        for (var i = 0; i < sortingData.Length; i++)
        {
            var drawCallIndex = (sortingData[i].DrawCallInstanceIndex >> 16);
            var instanceIndex = (sortingData[i].DrawCallInstanceIndex) & 0xFFFF;
            if (i == 0)
            {
                lastDrawCallIndex = drawCallIndex;
            }
            
            stringBuilder.AppendLine($"({drawCallIndex}) --> {sortingData[i].DistanceToCamera} instanceIndex:{instanceIndex}");

            if (lastDrawCallIndex == drawCallIndex) continue;
            
            Debug.Log(stringBuilder.ToString());
            stringBuilder = new StringBuilder();
            lastDrawCallIndex = drawCallIndex;
        }

        Debug.Log(stringBuilder.ToString());
    }
    
    #endregion
}
