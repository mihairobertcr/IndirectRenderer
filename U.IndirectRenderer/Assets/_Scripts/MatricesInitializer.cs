using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MatricesInitializer : IDisposable
{
    private const int  SCAN_THREAD_GROUP_SIZE = 64;

    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;

    private ComputeBuffer _positionsBuffer;
    private ComputeBuffer _rotationsBuffer;
    private ComputeBuffer _scalesBuffer;

    private ComputeBuffer _instanceMatrixRows01;
    private ComputeBuffer _instanceMatrixRows23;
    private ComputeBuffer _instanceMatrixRows45;

    public MatricesInitializer(ComputeShader computeShader, int numberOfInstances)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
    }

    public void Initialize(Material material, List<Vector3> positions, List<Vector3> rotations, List<Vector3> scales)
    {
        _instanceMatrixRows01 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        _instanceMatrixRows23 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        _instanceMatrixRows45 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        
        _positionsBuffer      = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        _scalesBuffer         = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        _rotationsBuffer      = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        
        //TODO: Consider to move every thing beside this block to ctor
        _positionsBuffer.SetData(positions);
        _rotationsBuffer.SetData(rotations);
        _scalesBuffer.SetData(scales);
        
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.Positions,            _positionsBuffer);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.Rotations,            _rotationsBuffer);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.Scales,               _scalesBuffer);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.InstanceMatrixRows01, _instanceMatrixRows01);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.InstanceMatrixRows23, _instanceMatrixRows23);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.InstanceMatrixRows45, _instanceMatrixRows45);

        material.SetBuffer(ShaderProperties.InstanceMatrixRows01, _instanceMatrixRows01);
        material.SetBuffer(ShaderProperties.InstanceMatrixRows23, _instanceMatrixRows23);
        material.SetBuffer(ShaderProperties.InstanceMatrixRows45, _instanceMatrixRows45);
    }

    public void Dispatch()
    {
        var groupX = Mathf.Max(1, _numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE));
        _computeShader.Dispatch(ShaderKernels.MatricesInitializer, groupX, 1, 1);
        
        _positionsBuffer?.Release();
        _rotationsBuffer?.Release();
        _scalesBuffer?.Release();
    }

    public void Dispose()
    {
        _instanceMatrixRows01.Release();
        _instanceMatrixRows23.Release();
        _instanceMatrixRows45.Release();
    }
}
