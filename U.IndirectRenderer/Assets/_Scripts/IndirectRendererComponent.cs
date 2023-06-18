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
public class IndirectRenderingMesh
{
    public Mesh Mesh;
    // public Material Material;
    
    // public MaterialPropertyBlock Lod0PropertyBlock;
    // public MaterialPropertyBlock Lod1PropertyBlock;
    // public MaterialPropertyBlock Lod2PropertyBlock;
    //
    // public MaterialPropertyBlock ShadowLod0PropertyBlock;
    // public MaterialPropertyBlock ShadowLod1PropertyBlock;
    // public MaterialPropertyBlock ShadowLod2PropertyBlock;
    
    public uint Lod0Vertices;
    public uint Lod1Vertices;
    public uint Lod2Vertices;
    
    public uint Lod0Indices;
    public uint Lod1Indices;
    public uint Lod2Indices;
}

public class IndirectRendererComponent : MonoBehaviour
{
    private const int NUMBER_OF_DRAW_CALLS = 3;                                                           // (LOD00 + LOD01 + LOD02)
    private const int NUMBER_OF_ARGS_PER_DRAW = 5;                                                        // (indexCount, instanceCount, startIndex, baseVertex, startInstance)
    private const int NUMBER_OF_ARGS_PER_INSTANCE_TYPE = NUMBER_OF_DRAW_CALLS * NUMBER_OF_ARGS_PER_DRAW;  // 3draws * 5args = 15args

    private const uint BITONIC_BLOCK_SIZE     = 256;
    private const uint TRANSPOSE_BLOCK_SIZE   = 8;
    
 
    [SerializeField] private IndirectRendererConfig _config;

    private MatricesInitializer _matricesInitializer;

    private int _numberOfInstances = 16384;
    private int _numberOfInstanceTypes = 1;
    
    private Vector3 _cameraPosition = Vector3.zero;
    
    IndirectRenderingMesh irm = new IndirectRenderingMesh();

    private void Start()
    {
        var positions = new List<Vector3>();
        var scales = new List<Vector3>();
        var rotations = new List<Vector3>();
        var sortingData = new List<SortingData>();

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
        
        // // Argument buffer used by DrawMeshInstancedIndirect.
        // var args = new uint[5] { 0, 0, 0, 0, 0 };
        //
        // // Arguments for drawing Mesh.
        // // 0 == number of triangle indices, 1 == population,
        // // others are only relevant if drawing submeshes.
        // args[0] = _config.Mesh.GetIndexCount(0);
        // args[1] = (uint)_numberOfInstances;
        // args[2] = _config.Mesh.GetIndexStart(0);
        // args[3] = _config.Mesh.GetBaseVertex(0);
        //
        // var size = args.Length * sizeof(uint);
        
        var args = new uint[NUMBER_OF_ARGS_PER_INSTANCE_TYPE]; //new uint[_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE]

        // Initialize Mesh
        irm.Lod0Vertices = (uint)_config.Lod0Mesh.vertexCount;
        irm.Lod1Vertices = (uint)_config.Lod1Mesh.vertexCount;
        irm.Lod2Vertices = (uint)_config.Lod2Mesh.vertexCount;
        
        irm.Lod0Indices = _config.Lod0Mesh.GetIndexCount(0);
        irm.Lod1Indices = _config.Lod1Mesh.GetIndexCount(0);
        irm.Lod2Indices = _config.Lod2Mesh.GetIndexCount(0);
        
        irm.Mesh = new Mesh();
        irm.Mesh.name = "Mesh"; // TODO: name it
        irm.Mesh.CombineMeshes(new CombineInstance[] 
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
        args[0] = irm.Lod0Indices;      // 0 - index count per instance, 
        args[1] = 0;                    // 1 - instance count
        args[2] = 0;                    // 2 - start index location
        args[3] = 0;                    // 3 - base vertex location
        args[4] = 0;                    // 4 - start instance location
        
        // Lod 1
        args[5] = irm.Lod1Indices;      // 0 - index count per instance, 
        args[6] = 0;                    // 1 - instance count
        args[7] = args[0] + args[2];    // 2 - start index location
        args[8] = 0;                    // 3 - base vertex location
        args[9] = 0;                    // 4 - start instance location
        
        // Lod 2
        args[10] = irm.Lod2Indices;     // 0 - index count per instance, 
        args[11] = 0;                   // 1 - instance count
        args[12] = args[5] + args[7];   // 2 - start index location
        args[13] = 0;                   // 3 - base vertex location
        args[14] = 0;                   // 4 - start instance location
        
        // Materials
        // irm.Material = _config.Material;

        // Note: Considering we have multiple types of meshes
        // I don't know how this is working right now but
        // it should preserve the sorting functionality
        var count = NUMBER_OF_ARGS_PER_INSTANCE_TYPE; // * _numberOfInstanceTypes
        ShaderBuffers.InstancesArgsBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.IndirectArguments);
        ShaderBuffers.InstancesArgsBuffer.SetData(args);
        
        ShaderKernels.Initialize(_config);
        _matricesInitializer = new MatricesInitializer(_config.Material, _config.MatricesInitializer, _numberOfInstances);
        _matricesInitializer.Initialize(positions, rotations, scales);
        _matricesInitializer.Dispatch();

        _cameraPosition = _config.RenderCamera.transform.position;
        
        ShaderBuffers.InstancesSortingData            = new ComputeBuffer(_numberOfInstances, SortingData.Size, ComputeBufferType.Default);
        ShaderBuffers.InstancesSortingDataTemp        = new ComputeBuffer(_numberOfInstances, SortingData.Size, ComputeBufferType.Default);

        for (var i = 0; i < _numberOfInstances; i++)
        {
            sortingData.Add(new SortingData
            {
                DrawCallInstanceIndex = ((((uint)0 * NUMBER_OF_ARGS_PER_INSTANCE_TYPE) << 16) + ((uint) i)), // 0 might be the index of the type in this case
                DistanceToCamera = Vector3.Distance(positions[i], _cameraPosition)
            });
        }

        ShaderBuffers.InstancesSortingData.SetData(sortingData);
        ShaderBuffers.InstancesSortingDataTemp.SetData(sortingData);

        CreateSortingCommandBuffer();

        LogInstanceDrawMatrices();
        
        // TODO: OnPreCull
        // m_lastCamPosition = m_camPosition;
        var AsyncCompute = true;
        if (AsyncCompute)
            Graphics.ExecuteCommandBufferAsync(ShaderBuffers.SortingCommandBuffer, ComputeQueueType.Background);
        else
            Graphics.ExecuteCommandBuffer(ShaderBuffers.SortingCommandBuffer);
        
        LogSortingData();
    }

    private void Update()
    {
        // On pre cull
        _cameraPosition = _config.RenderCamera.transform.position;

        Graphics.DrawMeshInstancedIndirect(
            mesh: irm.Mesh,
            submeshIndex: 0,
            material: _config.Material,
            bounds: new Bounds(Vector3.zero, Vector3.one * 1000),
            bufferWithArgs: ShaderBuffers.InstancesArgsBuffer,
            castShadows: ShadowCastingMode.On,
            receiveShadows: true);
        // camera: Camera.main);   
    }

    private void OnDestroy()
    {
        ShaderBuffers.Dispose();
    }

    private void CreateSortingCommandBuffer()
    {
        // Parameters.
        var elements = (uint)_numberOfInstances;
        var width    = BITONIC_BLOCK_SIZE;
        var height   = elements / BITONIC_BLOCK_SIZE;

        ShaderBuffers.SortingCommandBuffer = new CommandBuffer {name = "AsyncGPUSorting"};
        ShaderBuffers.SortingCommandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

        // Sort the data
        // First sort the rows for the levels <= to the block size
        for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
        {
            SetGpuSortConstants(level, level, height, width);

            // Sort the row data
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingData);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for (uint l = (BITONIC_BLOCK_SIZE << 1); l <= elements; l <<= 1)
        {
            // Transpose the data from buffer 1 into buffer 2
            var level = (l / BITONIC_BLOCK_SIZE);
            var mask = (l & ~elements) / BITONIC_BLOCK_SIZE;
            SetGpuSortConstants(level, mask, width, height);
            
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, ShaderBuffers.InstancesSortingData);
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingDataTemp);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, (int)(width / TRANSPOSE_BLOCK_SIZE), (int)(height / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingDataTemp);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGpuSortConstants(BITONIC_BLOCK_SIZE, l, height, width);
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, ShaderBuffers.InstancesSortingDataTemp);
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingData);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, (int)(height / TRANSPOSE_BLOCK_SIZE), (int)(width / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingData);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }
    }
    
    private void SetGpuSortConstants(uint level, uint levelMask, uint width, uint height)
    {
        ShaderBuffers.SortingCommandBuffer.SetComputeIntParam(_config.LodBitonicSorter, ShaderProperties.Level,     (int)level);
        ShaderBuffers.SortingCommandBuffer.SetComputeIntParam(_config.LodBitonicSorter, ShaderProperties.LevelMask, (int)levelMask);
        ShaderBuffers.SortingCommandBuffer.SetComputeIntParam(_config.LodBitonicSorter, ShaderProperties.Width,     (int)width);
        ShaderBuffers.SortingCommandBuffer.SetComputeIntParam(_config.LodBitonicSorter, ShaderProperties.Height,    (int)height);
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
