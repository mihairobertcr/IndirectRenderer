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
    [SerializeField] private Mesh _mesh;
    public Mesh Mesh => _mesh;
    
    [SerializeField] private Material _material;
    public Material Material => _material;
    
    [Header("Compute Shaders")]
    [SerializeField] private ComputeShader _matricesInitializer;
    public ComputeShader MatricesInitializer => _matricesInitializer;
    
    [SerializeField] private ComputeShader _lodBitonicSorter;
    public ComputeShader LodBitonicSorter => _lodBitonicSorter;
}

public class IndirectRendererComponent : MonoBehaviour
{
    private const uint BITONIC_BLOCK_SIZE     = 256;
    private const uint TRANSPOSE_BLOCK_SIZE   = 8;

    [SerializeField] private IndirectRendererConfig _config;

    private MatricesInitializer _matricesInitializer;

    // Compute Buffers
    private ComputeBuffer _instancesArgsBuffer;

    private ComputeBuffer _instancesSortingData;
    private ComputeBuffer _instancesSortingDataTemp;
    
    // Command Buffers
    private CommandBuffer _sortingCommandBuffer;
    
    
    private int _numberOfInstances = 16384;
    private int _numberOfInstanceTypes;
    

    private void Start()
    {
        var positions = new List<Vector3>();
        var scales = new List<Vector3>();
        var rotations = new List<Vector3>();
        
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
        var args = new uint[5] { 0, 0, 0, 0, 0 };
        
        // Arguments for drawing Mesh.
        // 0 == number of triangle indices, 1 == population,
        // others are only relevant if drawing submeshes.
        args[0] = _config.Mesh.GetIndexCount(0);
        args[1] = (uint)_numberOfInstances;
        args[2] = _config.Mesh.GetIndexStart(0);
        args[3] = _config.Mesh.GetBaseVertex(0);
        
        var size = args.Length * sizeof(uint);
        
        _instancesArgsBuffer = new ComputeBuffer(_numberOfInstances, size, ComputeBufferType.IndirectArguments);
        _instancesArgsBuffer.SetData(args);
        
        ShaderKernels.Initialize(_config);
        _matricesInitializer = new MatricesInitializer(_config.MatricesInitializer, _numberOfInstances);
        _matricesInitializer.Initialize(_config.Material, positions, rotations, scales);
        _matricesInitializer.Dispatch();

        // CreateSortingCommandBuffer();

        // LogInstanceDrawMatrices();
        // LogSortingData();
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(
            mesh: _config.Mesh,
            submeshIndex: 0,
            material: _config.Material,
            bounds: new Bounds(Vector3.zero, Vector3.one * 1000),
            bufferWithArgs: _instancesArgsBuffer,
            castShadows: ShadowCastingMode.On,
            receiveShadows: true);
        // camera: Camera.main);   
    }

    private void OnDestroy()
    {
        _instancesArgsBuffer?.Release();
    }

    // private bool TryGetKernels() =>
    //     TryGetKernel("CSMain", _matricesInitializer, out MatricesInitializer) &&
    //     TryGetKernel("BitonicSort", _lodBitonicSorter, out LodSorter) &&
    //     TryGetKernel("MatrixTranspose", _lodBitonicSorter, out LodTransposedSorter); //&& 
    // TryGetKernel("CSMain",            _culler,                  out _cullerKernelID) && 
    // TryGetKernel("CSMain",            _instancesScanner,        out _instancesScannerKernelID) && 
    // TryGetKernel("CSMain",            _groupSumsScanner,        out _groupSumsScannerKernelID) && 
    // TryGetKernel("CSMain",            _instanceDataCopier,      out _instanceDataCopierKernelID);
    
    private void CreateSortingCommandBuffer()
    {
        // Parameters.
        var elements = (uint)_numberOfInstances;
        var width    = BITONIC_BLOCK_SIZE;
        var height   = elements / BITONIC_BLOCK_SIZE;

        _sortingCommandBuffer = new CommandBuffer {name = "AsyncGPUSorting"};
        _sortingCommandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

        // Sort the data
        // First sort the rows for the levels <= to the block size
        for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
        {
            SetGpuSortConstants(level, level, height, width);

            // Sort the row data
            _sortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodSorter, ShaderProperties.Data, _instancesSortingData);
            _sortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for (uint l = (BITONIC_BLOCK_SIZE << 1); l <= elements; l <<= 1)
        {
            // Transpose the data from buffer 1 into buffer 2
            var level = (l / BITONIC_BLOCK_SIZE);
            var mask = (l & ~elements) / BITONIC_BLOCK_SIZE;
            SetGpuSortConstants(level, mask, width, height);
            
            _sortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, _instancesSortingData);
            _sortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, _instancesSortingDataTemp);
            _sortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, (int)(width / TRANSPOSE_BLOCK_SIZE), (int)(height / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            _sortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodSorter, ShaderProperties.Data, _instancesSortingDataTemp);
            _sortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGpuSortConstants(BITONIC_BLOCK_SIZE, l, height, width);
            _sortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, _instancesSortingDataTemp);
            _sortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, _instancesSortingData);
            _sortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodTransposedSorter, (int)(height / TRANSPOSE_BLOCK_SIZE), (int)(width / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            _sortingCommandBuffer.SetComputeBufferParam(_config.LodBitonicSorter, ShaderKernels.LodSorter, ShaderProperties.Data, _instancesSortingData);
            _sortingCommandBuffer.DispatchCompute(_config.LodBitonicSorter, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }
    }
    
    private void SetGpuSortConstants(uint level, uint levelMask, uint width, uint height)
    {
        _sortingCommandBuffer.SetComputeIntParam(_config.LodBitonicSorter, ShaderProperties.Level,     (int)level);
        _sortingCommandBuffer.SetComputeIntParam(_config.LodBitonicSorter, ShaderProperties.LevelMask, (int)levelMask);
        _sortingCommandBuffer.SetComputeIntParam(_config.LodBitonicSorter, ShaderProperties.Width,     (int)width);
        _sortingCommandBuffer.SetComputeIntParam(_config.LodBitonicSorter, ShaderProperties.Height,    (int)height);
    }
    
    // private static bool TryGetKernel(string kernelName, ComputeShader computeShader, out int kernelId)
    // {
    //     kernelId = default;
    //     if (!computeShader.HasKernel(kernelName))
    //     {
    //         Debug.LogError($"{kernelName} kernel not found in {computeShader.name}!");
    //         return false;
    //     }
    //     
    //     kernelId = computeShader.FindKernel(kernelName);
    //     return true;
    // }

    #region Logging
    
    // private void LogInstanceDrawMatrices(string prefix = "")
    // {
    //     var matrix1 = new Indirect2x2Matrix[_numberOfInstances];
    //     var matrix2 = new Indirect2x2Matrix[_numberOfInstances];
    //     var matrix3 = new Indirect2x2Matrix[_numberOfInstances];
    //     _instanceMatrixRows01.GetData(matrix1);
    //     _instanceMatrixRows23.GetData(matrix2);
    //     _instanceMatrixRows45.GetData(matrix3);
    //     
    //     var stringBuilder = new StringBuilder();
    //     if (!string.IsNullOrEmpty(prefix))
    //     {
    //         stringBuilder.AppendLine(prefix);
    //     }
    //     
    //     for (var i = 0; i < matrix1.Length; i++)
    //     {
    //         stringBuilder.AppendLine(
    //             i + "\n" 
    //               + matrix1[i].FirstRow + "\n"
    //               + matrix1[i].SecondRow + "\n"
    //               + matrix2[i].FirstRow + "\n"
    //               + "\n\n"
    //               + matrix2[i].SecondRow + "\n"
    //               + matrix3[i].FirstRow + "\n"
    //               + matrix3[i].SecondRow + "\n"
    //               + "\n"
    //         );
    //     }
    //
    //     Debug.Log(stringBuilder.ToString());
    // }
    
    private void LogSortingData(string prefix = "")
    {
        var sortingData = new SortingData[_numberOfInstances];
        _instancesSortingData.GetData(sortingData);
        
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
