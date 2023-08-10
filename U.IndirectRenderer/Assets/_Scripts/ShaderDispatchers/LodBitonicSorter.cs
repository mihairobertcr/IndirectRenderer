using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using IndirectRendering;

public class LodBitonicSorter : ComputeShaderDispatcher
{
    public bool ComputeAsync { get; set; }

    private const uint BITONIC_BLOCK_SIZE = 256;
    private const uint TRANSPOSE_BLOCK_SIZE = 8;

    private readonly int _sortKernel;
    private readonly int _transposedSortKernel;

    private readonly CommandBuffer _commandBuffer;
    private readonly ComputeBuffer _dataBuffer;
    private readonly ComputeBuffer _tempBuffer;

    public LodBitonicSorter(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _sortKernel = GetKernel("BitonicSort");
        _transposedSortKernel = GetKernel("MatrixTranspose");

        InitializeSortingBuffers(out _commandBuffer, out _dataBuffer, out _tempBuffer);
    }
    
    ~LodBitonicSorter() => _commandBuffer.Release();

    public void Initialize(List<Vector3> positions, Vector3 cameraPosition)
    {
        SetSortingData(positions, cameraPosition);
        SetupSortingCommandBuffer();
    }

    public override void Dispatch()
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

    private void InitializeSortingBuffers(out CommandBuffer command, out ComputeBuffer data, out ComputeBuffer temp)
    {
        command = new CommandBuffer { name = "AsyncGPUSorting" };
        data = Context.Sorting.Data;
        temp = Context.Sorting.Temp;
    }
    
    private void SetSortingData(List<Vector3> positions, Vector3 cameraPosition)
    {
        var sortingData = new List<SortingData>();
        for (var i = 0; i < Context.MeshesCount; i++)
        {
            sortingData.Add(new SortingData
            {
                DrawCallInstanceIndex = (uint)i,
                DistanceToCamera = Vector3.Distance(positions[i], cameraPosition)
            });
        }

        Context.Sorting.Data.SetData(sortingData);
        Context.Sorting.Temp.SetData(sortingData);
    }

    private void SetupSortingCommandBuffer()
    {
        // Parameters
        var elements = (uint)Context.MeshesCount;
        var width = BITONIC_BLOCK_SIZE;
        var height = elements / BITONIC_BLOCK_SIZE;

        _commandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

        // Sort the data
        // First sort the rows for the levels <= to the block size
        for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
        {
            SetGpuSortConstants(level, level, height, width);

            // Sort the row data
            _commandBuffer.SetComputeBufferParam(ComputeShader, _sortKernel, ShaderProperties.Data, _dataBuffer);
            _commandBuffer.DispatchCompute(ComputeShader, _sortKernel, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for (uint l = (BITONIC_BLOCK_SIZE << 1); l <= elements; l <<= 1)
        {
            // Transpose the data from buffer 1 into buffer 2
            var level = (l / BITONIC_BLOCK_SIZE);
            var mask = (l & ~elements) / BITONIC_BLOCK_SIZE;
            SetGpuSortConstants(level, mask, width, height);

            _commandBuffer.SetComputeBufferParam(ComputeShader, _transposedSortKernel, ShaderProperties.Input, _dataBuffer);
            _commandBuffer.SetComputeBufferParam(ComputeShader, _transposedSortKernel, ShaderProperties.Data, _tempBuffer);
            _commandBuffer.DispatchCompute(ComputeShader, _transposedSortKernel, (int)(width / TRANSPOSE_BLOCK_SIZE), (int)(height / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            _commandBuffer.SetComputeBufferParam(ComputeShader, _sortKernel, ShaderProperties.Data, _tempBuffer);
            _commandBuffer.DispatchCompute(ComputeShader, _sortKernel, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGpuSortConstants(BITONIC_BLOCK_SIZE, l, height, width);
            _commandBuffer.SetComputeBufferParam(ComputeShader, _transposedSortKernel, ShaderProperties.Input, _tempBuffer);
            _commandBuffer.SetComputeBufferParam(ComputeShader, _transposedSortKernel, ShaderProperties.Data, _dataBuffer);
            _commandBuffer.DispatchCompute(ComputeShader, _transposedSortKernel, (int)(height / TRANSPOSE_BLOCK_SIZE), (int)(width / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            _commandBuffer.SetComputeBufferParam(ComputeShader, _sortKernel, ShaderProperties.Data, _dataBuffer);
            _commandBuffer.DispatchCompute(ComputeShader, _sortKernel, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }
    }

    private void SetGpuSortConstants(uint level, uint levelMask, uint width, uint height)
    {
        _commandBuffer.SetComputeIntParam(ComputeShader, ShaderProperties.Level,     (int)level);
        _commandBuffer.SetComputeIntParam(ComputeShader, ShaderProperties.LevelMask, (int)levelMask);
        _commandBuffer.SetComputeIntParam(ComputeShader, ShaderProperties.Width,     (int)width);
        _commandBuffer.SetComputeIntParam(ComputeShader, ShaderProperties.Height,    (int)height);
    }
}