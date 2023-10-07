using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using IndirectRendering;

public class LodBitonicSorter : ComputeShaderDispatcher
{
    private const uint BITONIC_BLOCK_SIZE = 256;
    private const uint TRANSPOSE_BLOCK_SIZE = 8;

    private readonly int _sortKernel;
    private readonly int _transposedSortKernel;

    private readonly CommandBuffer _command;
    private readonly ComputeBuffer _dataBuffer;
    private readonly ComputeBuffer _tempBuffer;
    
    private bool _computeAsync;

    public LodBitonicSorter(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _sortKernel = GetKernel("BitonicSort");
        _transposedSortKernel = GetKernel("MatrixTranspose");

        InitializeSortingBuffers(out _command, out _dataBuffer, out _tempBuffer);
    }
    
    ~LodBitonicSorter() => _command.Release();

    public LodBitonicSorter SetSortingData(InstanceProperties[] meshes, Camera camera)
    {
        var cameraPosition = camera.transform.position;
        var sortingData = new List<SortingData>();
        
        var instancesCount = 0u;
        for (var i = 0u; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            foreach (var transform in mesh.Transforms)
            {
                var drawCallIndex = ((i * (uint)Context.Arguments.InstanceArgumentsCount) << 16) + instancesCount;
                sortingData.Add(new SortingData
                {
                    DrawCallInstanceIndex = drawCallIndex,
                    DistanceToCamera = Vector3.Distance(transform.Position, cameraPosition)
                });

                instancesCount++;
            }
        }

        Context.Sorting.Data.SetData(sortingData);
        Context.Sorting.Temp.SetData(sortingData);

        return this;
    }

    public LodBitonicSorter SetupSortingCommand()
    {
        // Parameters
        var elements = (uint)Context.MeshesCount;
        var width = BITONIC_BLOCK_SIZE;
        var height = elements / BITONIC_BLOCK_SIZE;

        _command.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

        // Sort the data
        // First sort the rows for the levels <= to the block size
        for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
        {
            SetGpuSortConstants(level, level, height, width);

            // Sort the row data
            _command.SetComputeBufferParam(ComputeShader, _sortKernel, ShaderProperties.Data, _dataBuffer);
            _command.DispatchCompute(ComputeShader, _sortKernel, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for (uint l = (BITONIC_BLOCK_SIZE << 1); l <= elements; l <<= 1)
        {
            // Transpose the data from buffer 1 into buffer 2
            var level = (l / BITONIC_BLOCK_SIZE);
            var mask = (l & ~elements) / BITONIC_BLOCK_SIZE;
            SetGpuSortConstants(level, mask, width, height);

            _command.SetComputeBufferParam(ComputeShader, _transposedSortKernel, ShaderProperties.Input, _dataBuffer);
            _command.SetComputeBufferParam(ComputeShader, _transposedSortKernel, ShaderProperties.Data, _tempBuffer);
            _command.DispatchCompute(ComputeShader, _transposedSortKernel, (int)(width / TRANSPOSE_BLOCK_SIZE), (int)(height / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            _command.SetComputeBufferParam(ComputeShader, _sortKernel, ShaderProperties.Data, _tempBuffer);
            _command.DispatchCompute(ComputeShader, _sortKernel, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGpuSortConstants(BITONIC_BLOCK_SIZE, l, height, width);
            _command.SetComputeBufferParam(ComputeShader, _transposedSortKernel, ShaderProperties.Input, _tempBuffer);
            _command.SetComputeBufferParam(ComputeShader, _transposedSortKernel, ShaderProperties.Data, _dataBuffer);
            _command.DispatchCompute(ComputeShader, _transposedSortKernel, (int)(height / TRANSPOSE_BLOCK_SIZE), (int)(width / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            _command.SetComputeBufferParam(ComputeShader, _sortKernel, ShaderProperties.Data, _dataBuffer);
            _command.DispatchCompute(ComputeShader, _sortKernel, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }

        return this;
    }

    public void EnabledAsyncComputing(bool enable) => _computeAsync = enable;
    
    public override void Dispatch()
    {
        if (_computeAsync)
        {
            Graphics.ExecuteCommandBufferAsync(_command, ComputeQueueType.Background);
        }
        else
        {
            Graphics.ExecuteCommandBuffer(_command);
        }
    }

    private void InitializeSortingBuffers(out CommandBuffer command, out ComputeBuffer data, out ComputeBuffer temp)
    {
        command = new CommandBuffer { name = "AsyncGPUSorting" };
        data = Context.Sorting.Data;
        temp = Context.Sorting.Temp;
    }

    private void SetGpuSortConstants(uint level, uint levelMask, uint width, uint height)
    {
        _command.SetComputeIntParam(ComputeShader, ShaderProperties.Level,     (int)level);
        _command.SetComputeIntParam(ComputeShader, ShaderProperties.LevelMask, (int)levelMask);
        _command.SetComputeIntParam(ComputeShader, ShaderProperties.Width,     (int)width);
        _command.SetComputeIntParam(ComputeShader, ShaderProperties.Height,    (int)height);
    }
}