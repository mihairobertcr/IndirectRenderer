using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using IndirectRendering;

public class LodBitonicSorter
{
    private const uint BITONIC_BLOCK_SIZE = 256;
    private const uint TRANSPOSE_BLOCK_SIZE = 8;
    
    public bool ComputeAsync { get; set; }
    
    private readonly ComputeShader _computeShader;
    private readonly RendererDataContext _context;
    private readonly CommandBuffer _commandBuffer;

    public LodBitonicSorter(ComputeShader computeShader, RendererDataContext context)
    {
        _computeShader = computeShader;
        _context = context;
        _commandBuffer = new CommandBuffer { name = "AsyncGPUSorting" };
    }
    
    // How do we dispose Handlers ?
    ~LodBitonicSorter()
    {
        _commandBuffer.Release();
        Debug.Log("LodBitonicSorter destroyed!");
    }

    public void Initialize(List<Vector3> positions, Vector3 cameraPosition)
    {
        InitializeSorterBuffers(positions, cameraPosition);
        CreateSortingCommandBuffer();
    }

    public void Dispatch()
    {
        if (ComputeAsync)
        {
            Graphics.ExecuteCommandBufferAsync(_commandBuffer, ComputeQueueType.Background);
        }
        else
        {
            Graphics.ExecuteCommandBuffer(_commandBuffer);
        }
    }
    
    private void InitializeSorterBuffers(List<Vector3> positions, Vector3 cameraPosition)
    {
        var sortingData = new List<SortingData>();
        for (var i = 0; i < _context.MeshesCount; i++)
        {
            sortingData.Add(new SortingData
            {
                DrawCallInstanceIndex = (uint)i,
                DistanceToCamera = Vector3.Distance(positions[i], cameraPosition)
            });
        }

        _context.Sorting.Data.SetData(sortingData);
        _context.Sorting.Temp.SetData(sortingData);
    }

    private void CreateSortingCommandBuffer()
    {
        // Parameters
        var elements = (uint)_context.MeshesCount;
        var width = BITONIC_BLOCK_SIZE;
        var height = elements / BITONIC_BLOCK_SIZE;

        _commandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

        // Sort the data
        // First sort the rows for the levels <= to the block size
        for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
        {
            SetGpuSortConstants(level, level, height, width);

            // Sort the row data
            _commandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodSorter, ShaderProperties.Data, _context.Sorting.Data);
            _commandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for (uint l = (BITONIC_BLOCK_SIZE << 1); l <= elements; l <<= 1)
        {
            // Transpose the data from buffer 1 into buffer 2
            var level = (l / BITONIC_BLOCK_SIZE);
            var mask = (l & ~elements) / BITONIC_BLOCK_SIZE;
            SetGpuSortConstants(level, mask, width, height);

            _commandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, _context.Sorting.Data);
            _commandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, _context.Sorting.Temp);
            _commandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodTransposedSorter, (int)(width / TRANSPOSE_BLOCK_SIZE), (int)(height / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            _commandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodSorter, ShaderProperties.Data, _context.Sorting.Temp);
            _commandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGpuSortConstants(BITONIC_BLOCK_SIZE, l, height, width);
            _commandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, _context.Sorting.Temp);
            _commandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, _context.Sorting.Data);
            _commandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodTransposedSorter, (int)(height / TRANSPOSE_BLOCK_SIZE), (int)(width / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            _commandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodSorter, ShaderProperties.Data, _context.Sorting.Data);
            _commandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }
    }

    private void SetGpuSortConstants(uint level, uint levelMask, uint width, uint height)
    {
        _commandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.Level,     (int)level);
        _commandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.LevelMask, (int)levelMask);
        _commandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.Width,     (int)width);
        _commandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.Height,    (int)height);
    }
}