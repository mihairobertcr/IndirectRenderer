using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

public class IndirectRendererComponent : MonoBehaviour
{
    private const int  SCAN_THREAD_GROUP_SIZE = 64;
    private const uint BITONIC_BLOCK_SIZE     = 256;
    private const uint TRANSPOSE_BLOCK_SIZE   = 8;

    [SerializeField] private Mesh _mesh;
    [SerializeField] private Material _material;

    // Compute Shader
    [SerializeField] private ComputeShader _matricesInitializer;
    [SerializeField] private ComputeShader _lodBitonicSorter;
    
    // Kernel ID's
    private int _matricesInitializerKernelId;
    private int _lodSorterKernelId;
    private int _lodTransposeSorterKernelID;
    
    // Compute Buffers
    private ComputeBuffer _instancesArgsBuffer;

    private ComputeBuffer _instanceMatrixRows01;
    private ComputeBuffer _instanceMatrixRows23;
    private ComputeBuffer _instanceMatrixRows45;
    
    private ComputeBuffer _instancesSortingData;
    private ComputeBuffer _instancesSortingDataTemp;
    
    // Command Buffers
    private CommandBuffer _sortingCommandBuffer;
    
    // Shader Property ID's

    
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
        args[0] = _mesh.GetIndexCount(0);
        args[1] = (uint)_numberOfInstances;
        args[2] = _mesh.GetIndexStart(0);
        args[3] = _mesh.GetBaseVertex(0);
        
        var size = args.Length * sizeof(uint);
        
        _instancesArgsBuffer = new ComputeBuffer(_numberOfInstances, size, ComputeBufferType.IndirectArguments);
        
        _instanceMatrixRows01 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        _instanceMatrixRows23 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        _instanceMatrixRows45 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        
        _instancesArgsBuffer.SetData(args);
        
        

        TryGetKernels();
        
        var positionsBuffer = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        var scalesBuffer = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        var rotationsBuffer = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        
        positionsBuffer.SetData(positions);
        scalesBuffer.SetData(scales);
        rotationsBuffer.SetData(rotations);
        
        //TODO: Set up compute shaders
        
        _matricesInitializer.SetBuffer(_matricesInitializerKernelId, Positions, positionsBuffer);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelId, Scales, scalesBuffer);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelId, Rotations, rotationsBuffer);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelId, InstanceMatrixRows01, _instanceMatrixRows01);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelId, InstanceMatrixRows23, _instanceMatrixRows23);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelId, InstanceMatrixRows45, _instanceMatrixRows45);
        
        var groupX = Mathf.Max(1, _numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE));
        _matricesInitializer.Dispatch(_matricesInitializerKernelId, groupX, 1, 1);
        
        positionsBuffer?.Release();
        rotationsBuffer?.Release();
        scalesBuffer?.Release();
        
        _material.SetBuffer(InstanceMatrixRows01, _instanceMatrixRows01);
        _material.SetBuffer(InstanceMatrixRows23, _instanceMatrixRows23);
        _material.SetBuffer(InstanceMatrixRows45, _instanceMatrixRows45);

        CreateSortingCommandBuffer();

        LogInstanceDrawMatrices();
        LogSortingData();
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(
            mesh: _mesh,
            submeshIndex: 0,
            material: _material,
            bounds: new Bounds(Vector3.zero, Vector3.one * 1000),
            bufferWithArgs: _instancesArgsBuffer,
            castShadows: ShadowCastingMode.On,
            receiveShadows: true);
        // camera: Camera.main);   
    }

    private void OnDestroy()
    {
        _instancesArgsBuffer?.Release();
        _instanceMatrixRows01?.Release();
        _instanceMatrixRows23?.Release();
        _instanceMatrixRows45?.Release();
    }

    private bool TryGetKernels() =>
        TryGetKernel("CSMain", _matricesInitializer, out _matricesInitializerKernelId) &&
        TryGetKernel("BitonicSort", _lodBitonicSorter, out _lodSorterKernelId) &&
        TryGetKernel("MatrixTranspose", _lodBitonicSorter, out _lodTransposeSorterKernelID); //&& 
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
            _sortingCommandBuffer.SetComputeBufferParam(_lodBitonicSorter, _lodSorterKernelId, Data, _instancesSortingData);
            _sortingCommandBuffer.DispatchCompute(_lodBitonicSorter, _lodSorterKernelId, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for (uint l = (BITONIC_BLOCK_SIZE << 1); l <= elements; l <<= 1)
        {
            // Transpose the data from buffer 1 into buffer 2
            var level = (l / BITONIC_BLOCK_SIZE);
            var mask = (l & ~elements) / BITONIC_BLOCK_SIZE;
            SetGpuSortConstants(level, mask, width, height);
            
            _sortingCommandBuffer.SetComputeBufferParam(_lodBitonicSorter, _lodTransposeSorterKernelID, Input, _instancesSortingData);
            _sortingCommandBuffer.SetComputeBufferParam(_lodBitonicSorter, _lodTransposeSorterKernelID, Data, _instancesSortingDataTemp);
            _sortingCommandBuffer.DispatchCompute(_lodBitonicSorter, _lodTransposeSorterKernelID, (int)(width / TRANSPOSE_BLOCK_SIZE), (int)(height / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            _sortingCommandBuffer.SetComputeBufferParam(_lodBitonicSorter, _lodSorterKernelId, Data, _instancesSortingDataTemp);
            _sortingCommandBuffer.DispatchCompute(_lodBitonicSorter, _lodSorterKernelId, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGpuSortConstants(BITONIC_BLOCK_SIZE, l, height, width);
            _sortingCommandBuffer.SetComputeBufferParam(_lodBitonicSorter, _lodTransposeSorterKernelID, Input, _instancesSortingDataTemp);
            _sortingCommandBuffer.SetComputeBufferParam(_lodBitonicSorter, _lodTransposeSorterKernelID, Data, _instancesSortingData);
            _sortingCommandBuffer.DispatchCompute(_lodBitonicSorter, _lodTransposeSorterKernelID, (int)(height / TRANSPOSE_BLOCK_SIZE), (int)(width / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            _sortingCommandBuffer.SetComputeBufferParam(_lodBitonicSorter, _lodSorterKernelId, Data, _instancesSortingData);
            _sortingCommandBuffer.DispatchCompute(_lodBitonicSorter, _lodSorterKernelId, (int)(elements / BITONIC_BLOCK_SIZE), 1, 1);
        }
    }
    
    private void SetGpuSortConstants(uint level, uint levelMask, uint width, uint height)
    {
        _sortingCommandBuffer.SetComputeIntParam(_lodBitonicSorter, Level,     (int)level);
        _sortingCommandBuffer.SetComputeIntParam(_lodBitonicSorter, LevelMask, (int)levelMask);
        _sortingCommandBuffer.SetComputeIntParam(_lodBitonicSorter, Width,     (int)width);
        _sortingCommandBuffer.SetComputeIntParam(_lodBitonicSorter, Height,    (int)height);
    }
    
    private static bool TryGetKernel(string kernelName, ComputeShader computeShader, out int kernelId)
    {
        kernelId = default;
        if (!computeShader.HasKernel(kernelName))
        {
            Debug.LogError($"{kernelName} kernel not found in {computeShader.name}!");
            return false;
        }
        
        kernelId = computeShader.FindKernel(kernelName);
        return true;
    }

    #region Logging
    
    private void LogInstanceDrawMatrices(string prefix = "")
    {
        var matrix1 = new Indirect2x2Matrix[_numberOfInstances];
        var matrix2 = new Indirect2x2Matrix[_numberOfInstances];
        var matrix3 = new Indirect2x2Matrix[_numberOfInstances];
        _instanceMatrixRows01.GetData(matrix1);
        _instanceMatrixRows23.GetData(matrix2);
        _instanceMatrixRows45.GetData(matrix3);
        
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
