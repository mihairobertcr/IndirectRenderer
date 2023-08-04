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

    private readonly RendererDataContext _context;

    public LodBitonicSorter(ComputeShader computeShader, int numberOfInstances, RendererDataContext context)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _context = context;
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
            Graphics.ExecuteCommandBufferAsync(_context.SortingCommandBuffer, ComputeQueueType.Background);
        }
        else
        {
            Graphics.ExecuteCommandBuffer(_context.SortingCommandBuffer);
        }
    }
    
    // TODO: #EDITOR
    public void LogSortingData(string prefix = "")
    {
        var sortingData = new SortingData[_numberOfInstances];
        _context.SortingData.GetData(sortingData);
        
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
        // Maybe move instantiation of compute buffers to its own class
        _context.SortingData = new ComputeBuffer(_numberOfInstances, SortingData.Size, ComputeBufferType.Default);
        _context.SortingDataTemp = new ComputeBuffer(_numberOfInstances, SortingData.Size, ComputeBufferType.Default);
        
        var sortingData = new List<SortingData>();
        for (var i = 0; i < _numberOfInstances; i++)
        {
            sortingData.Add(new SortingData
            {
                DrawCallInstanceIndex = (((uint)0 * IndirectRenderer.NUMBER_OF_ARGS_PER_INSTANCE_TYPE) << 16) + ((uint)i), // 0 might be the index of the type in this case
                DistanceToCamera = Vector3.Distance(positions[i], cameraPosition)
            });
        }

        _context.SortingData.SetData(sortingData);
        _context.SortingDataTemp.SetData(sortingData);
    }

    private void CreateSortingCommandBuffer()
    {
        // Parameters.
        var elements = (uint)_numberOfInstances;
        var width = BITONIC_BLOCK_SIZE;
        var height = elements / BITONIC_BLOCK_SIZE;

        _context.SortingCommandBuffer = new CommandBuffer { name = "AsyncGPUSorting" };
        _context.SortingCommandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

        // Sort the data
        // First sort the rows for the levels <= to the block size
        for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
        {
            SetGpuSortConstants(level, level, height, width);

            // Sort the row data
            _context.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodSorter, ShaderProperties.Data, _context.SortingData);
            _context.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for (uint l = (BITONIC_BLOCK_SIZE << 1); l <= elements; l <<= 1)
        {
            // Transpose the data from buffer 1 into buffer 2
            var level = (l / BITONIC_BLOCK_SIZE);
            var mask = (l & ~elements) / BITONIC_BLOCK_SIZE;
            SetGpuSortConstants(level, mask, width, height);

            _context.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, _context.SortingData);
            _context.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, _context.SortingDataTemp);
            _context.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodTransposedSorter, (int)(width / TRANSPOSE_BLOCK_SIZE), (int)(height / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            _context.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodSorter, ShaderProperties.Data, _context.SortingDataTemp);
            _context.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGpuSortConstants(BITONIC_BLOCK_SIZE, l, height, width);
            _context.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Input, _context.SortingDataTemp);
            _context.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodTransposedSorter, ShaderProperties.Data, _context.SortingData);
            _context.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodTransposedSorter, (int)(height / TRANSPOSE_BLOCK_SIZE), (int)(width / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            _context.SortingCommandBuffer.SetComputeBufferParam(_computeShader, ShaderKernels.LodSorter, ShaderProperties.Data, _context.SortingData);
            _context.SortingCommandBuffer.DispatchCompute(_computeShader, ShaderKernels.LodSorter, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }
    }

    private void SetGpuSortConstants(uint level, uint levelMask, uint width, uint height)
    {
        _context.SortingCommandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.Level,     (int)level);
        _context.SortingCommandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.LevelMask, (int)levelMask);
        _context.SortingCommandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.Width,     (int)width);
        _context.SortingCommandBuffer.SetComputeIntParam(_computeShader, ShaderProperties.Height,    (int)height);
    }
}