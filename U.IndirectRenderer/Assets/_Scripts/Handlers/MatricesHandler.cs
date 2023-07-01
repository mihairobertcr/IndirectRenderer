using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class MatricesHandler
{
    private const int  SCAN_THREAD_GROUP_SIZE = 64;

    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    
    private readonly MeshProperties _meshProperties;

    private ComputeBuffer _positionsBuffer;
    private ComputeBuffer _rotationsBuffer;
    private ComputeBuffer _scalesBuffer;

    public MatricesHandler(ComputeShader computeShader, int numberOfInstances, MeshProperties meshProperties)
    {
        _computeShader = computeShader;
        _meshProperties = meshProperties;
        _numberOfInstances = numberOfInstances;

        InitializeMatricesBuffers();
        InitializeTransformBuffers();
        InitializeComputeShader();
        InitializeMaterialProperties();
    }

    public void Initialize(List<Vector3> positions, List<Vector3> rotations, List<Vector3> scales)
    {
        _positionsBuffer.SetData(positions);
        _rotationsBuffer.SetData(rotations);
        _scalesBuffer.SetData(scales);
    }

    public void Dispatch()
    {
        var groupX = Mathf.Max(1, _numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE));
        _computeShader.Dispatch(ShaderKernels.MatricesInitializer, groupX, 1, 1);
        
        _positionsBuffer?.Release();
        _rotationsBuffer?.Release();
        _scalesBuffer?.Release();
    }
    
    // TODO: #EDITOR
    public void LogInstanceDrawMatrices(string prefix = "")
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

    private void InitializeMatricesBuffers()
    {
        ShaderBuffers.InstanceMatrixRows01 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        ShaderBuffers.InstanceMatrixRows23 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        ShaderBuffers.InstanceMatrixRows45 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
    }

    private void InitializeTransformBuffers()
    {
        _positionsBuffer = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        _scalesBuffer    = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        _rotationsBuffer = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
    }

    private void InitializeComputeShader()
    {
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.Positions,            _positionsBuffer);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.Rotations,            _rotationsBuffer);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.Scales,               _scalesBuffer);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
    }

    private void InitializeMaterialProperties()
    {
        //TODO: Change with culled buffers
        _meshProperties.Lod0PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
        _meshProperties.Lod1PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
        _meshProperties.Lod2PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
            
        _meshProperties.Lod0PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
        _meshProperties.Lod1PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
        _meshProperties.Lod2PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
            
        _meshProperties.Lod0PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
        _meshProperties.Lod1PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
        _meshProperties.Lod2PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
        
        _meshProperties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
        _meshProperties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
        _meshProperties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
            
        _meshProperties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
        _meshProperties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
        _meshProperties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
            
        _meshProperties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
        _meshProperties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
        _meshProperties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
    }
}
