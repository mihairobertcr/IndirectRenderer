using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using IndirectRendering;

public class LodsSortingDispatcher : ComputeShaderDispatcher
{
    private const uint BITONIC_BLOCK_SIZE = 256;
    private const uint TRANSPOSE_BLOCK_SIZE = 8;
    
    private static readonly int DataId = Shader.PropertyToID("_Data");
    private static readonly int InputId = Shader.PropertyToID("_Input");

    private static readonly int LevelId = Shader.PropertyToID("_Level");
    private static readonly int LevelMaskId = Shader.PropertyToID("_LevelMask");
    private static readonly int WidthId = Shader.PropertyToID("_Width");
    private static readonly int HeightId = Shader.PropertyToID("_Height");

    private readonly int _sortKernel;
    private readonly int _transposedSortKernel;

    private readonly CommandBuffer _command;
    private readonly ComputeBuffer _dataBuffer;
    private readonly ComputeBuffer _tempBuffer;
    
    public LodsSortingDispatcher(RendererContext context)
        : base(context.Config.LodSorting, context)
    {
        _sortKernel = GetKernel("BitonicSort");
        _transposedSortKernel = GetKernel("MatrixTranspose");

        InitializeSortingBuffers(out _command, out _dataBuffer, out _tempBuffer);
    }
    
    ~LodsSortingDispatcher() => _command.Release();

    public override ComputeShaderDispatcher Initialize()
    {
        SetSortingData();
        SetupSortingCommand();
        
        return this;
    }

    public override void Dispatch()
    {
        if (Context.Config.SortLodsAsync)
        {
            Graphics.ExecuteCommandBufferAsync(_command, ComputeQueueType.Background);
        }
        else
        {
            Graphics.ExecuteCommandBuffer(_command);
        }
    }
    
    private void SetSortingData()
    {
        var cameraPosition = Context.Camera.transform.position;
        var sortingData = new List<SortingData>();
        var meshes = Context.MeshesProperties;
        
        var instancesCount = 0u;
        for (var i = 0; i < meshes.Count; i++)
        {
            var mesh = meshes[i];
            foreach (var transform in mesh.Transforms)
            {
                var drawCallIndex = (((uint)i * (uint)Context.Arguments.InstanceArgumentsCount) << 16) + instancesCount;
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
    }

    public void SetupSortingCommand()
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
            _command.SetComputeBufferParam(ComputeShader, _sortKernel, DataId, _dataBuffer);
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

            _command.SetComputeBufferParam(ComputeShader, _transposedSortKernel, InputId, _dataBuffer);
            _command.SetComputeBufferParam(ComputeShader, _transposedSortKernel, DataId, _tempBuffer);
            _command.DispatchCompute(ComputeShader, _transposedSortKernel, (int)(width / TRANSPOSE_BLOCK_SIZE), (int)(height / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            _command.SetComputeBufferParam(ComputeShader, _sortKernel, DataId, _tempBuffer);
            _command.DispatchCompute(ComputeShader, _sortKernel, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGpuSortConstants(BITONIC_BLOCK_SIZE, l, height, width);
            _command.SetComputeBufferParam(ComputeShader, _transposedSortKernel, InputId, _tempBuffer);
            _command.SetComputeBufferParam(ComputeShader, _transposedSortKernel, DataId, _dataBuffer);
            _command.DispatchCompute(ComputeShader, _transposedSortKernel, (int)(height / TRANSPOSE_BLOCK_SIZE), (int)(width / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            _command.SetComputeBufferParam(ComputeShader, _sortKernel,  DataId, _dataBuffer);
            _command.DispatchCompute(ComputeShader, _sortKernel, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
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
        _command.SetComputeIntParam(ComputeShader, LevelId, (int)level);
        _command.SetComputeIntParam(ComputeShader, LevelMaskId, (int)levelMask);
        _command.SetComputeIntParam(ComputeShader, WidthId, (int)width);
        _command.SetComputeIntParam(ComputeShader, HeightId, (int)height);
    }
}