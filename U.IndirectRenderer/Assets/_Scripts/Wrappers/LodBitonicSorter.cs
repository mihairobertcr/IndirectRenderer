using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

public class LodBitonicSorter
{
    private const uint BITONIC_BLOCK_SIZE = 256;
    private const uint TRANSPOSE_BLOCK_SIZE = 8;

    public bool ComputeAsync { get; set; }
    
    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;

    public LodBitonicSorter(ComputeShader computeShader, int numberOfInstances)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;

        // Maybe move instantiation of compute buffers to its own class
        ShaderBuffers.InstancesSortingData = new ComputeBuffer(_numberOfInstances, SortingData.Size, ComputeBufferType.Default);
        ShaderBuffers.InstancesSortingDataTemp = new ComputeBuffer(_numberOfInstances, SortingData.Size, ComputeBufferType.Default);
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
            Graphics.ExecuteCommandBufferAsync(ShaderBuffers.SortingCommandBuffer, ComputeQueueType.Background);
        }
        else
        {
            Graphics.ExecuteCommandBuffer(ShaderBuffers.SortingCommandBuffer);
        }
    }
    
    // TODO: #EDITOR
    public void LogSortingData(string prefix = "")
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

    private void InitializeSorterBuffers(List<Vector3> positions, Vector3 cameraPosition)
    {
        var sortingData = new List<SortingData>();
        for (var i = 0; i < _numberOfInstances; i++)
        {
            sortingData.Add(new SortingData
            {
                DrawCallInstanceIndex = (((uint)0 * IndirectRenderer.NUMBER_OF_ARGS_PER_INSTANCE_TYPE) << 16) + ((uint)i), // 0 might be the index of the type in this case
                DistanceToCamera = Vector3.Distance(positions[i], cameraPosition)
            });
        }

        ShaderBuffers.InstancesSortingData.SetData(sortingData);
        ShaderBuffers.InstancesSortingDataTemp.SetData(sortingData);
    }

    private void CreateSortingCommandBuffer()
    {
        // Parameters.
        var elements = (uint)_numberOfInstances;
        var width = BITONIC_BLOCK_SIZE;
        var height = elements / BITONIC_BLOCK_SIZE;

        ShaderBuffers.SortingCommandBuffer = new CommandBuffer { name = "AsyncGPUSorting" };
        ShaderBuffers.SortingCommandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

        // Sort the data
        // First sort the rows for the levels <= to the block size
        for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
        {
            SetGpuSortConstants(level, level, height, width);

            // Sort the row data
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingData);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for (uint l = (BITONIC_BLOCK_SIZE << 1); l <= elements; l <<= 1)
        {
            // Transpose the data from buffer 1 into buffer 2
            var level = (l / BITONIC_BLOCK_SIZE);
            var mask = (l & ~elements) / BITONIC_BLOCK_SIZE;
            SetGpuSortConstants(level, mask, width, height);

            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, ShaderBuffers.InstancesSortingData);
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingDataTemp);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodTransposedSorter, (int)(width / TRANSPOSE_BLOCK_SIZE), (int)(height / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingDataTemp);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGpuSortConstants(BITONIC_BLOCK_SIZE, l, height, width);
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, ShaderBuffers.InstancesSortingDataTemp);
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingData);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodTransposedSorter, (int)(height / TRANSPOSE_BLOCK_SIZE), (int)(width / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            ShaderBuffers.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodSorter, ShaderProperties.Data, ShaderBuffers.InstancesSortingData);
            ShaderBuffers.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }
    }

    private void SetGpuSortConstants(uint level, uint levelMask, uint width, uint height)
    {
        ShaderBuffers.SortingCommandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.Level,     (int)level);
        ShaderBuffers.SortingCommandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.LevelMask, (int)levelMask);
        ShaderBuffers.SortingCommandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.Width,     (int)width);
        ShaderBuffers.SortingCommandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.Height,    (int)height);
    }
}