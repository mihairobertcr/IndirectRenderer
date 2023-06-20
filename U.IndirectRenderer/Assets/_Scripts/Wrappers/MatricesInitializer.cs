using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MatricesInitializer
{
    private const int  SCAN_THREAD_GROUP_SIZE = 64;

    private readonly ComputeShader _computeShader;
    private readonly MeshProperties _meshProperties;
    private readonly int _numberOfInstances;

    private ComputeBuffer _positionsBuffer;
    private ComputeBuffer _rotationsBuffer;
    private ComputeBuffer _scalesBuffer;

    public MatricesInitializer(ComputeShader computeShader, MeshProperties meshProperties, int numberOfInstances)
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
        // TODO: Consider moving this to MeshProperty ctor
        _meshProperties.Lod0PropertyBlock = new MaterialPropertyBlock();
        _meshProperties.Lod1PropertyBlock = new MaterialPropertyBlock();
        _meshProperties.Lod2PropertyBlock = new MaterialPropertyBlock();
        // irm.shadowLod00MatPropBlock = new MaterialPropertyBlock();
        // irm.shadowLod01MatPropBlock = new MaterialPropertyBlock();
        // irm.shadowLod02MatPropBlock = new MaterialPropertyBlock();
        
        //TODO: Change to culled buffers
        _meshProperties.Lod0PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
        _meshProperties.Lod1PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
        _meshProperties.Lod2PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows01, ShaderBuffers.InstanceMatrixRows01);
            
        _meshProperties.Lod0PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
        _meshProperties.Lod1PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
        _meshProperties.Lod2PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows23, ShaderBuffers.InstanceMatrixRows23);
            
        _meshProperties.Lod0PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
        _meshProperties.Lod1PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
        _meshProperties.Lod2PropertyBlock.SetBuffer(ShaderProperties.InstanceMatrixRows45, ShaderBuffers.InstanceMatrixRows45);
            
        // irm.shadowLod00MatPropBlock.SetBuffer(_InstancesDrawMatrixRows01, m_shadowCulledMatrixRows01);
        // irm.shadowLod01MatPropBlock.SetBuffer(_InstancesDrawMatrixRows01, m_shadowCulledMatrixRows01);
        // irm.shadowLod02MatPropBlock.SetBuffer(_InstancesDrawMatrixRows01, m_shadowCulledMatrixRows01);
        //     
        // irm.shadowLod00MatPropBlock.SetBuffer(_InstancesDrawMatrixRows23, m_shadowCulledMatrixRows23);
        // irm.shadowLod01MatPropBlock.SetBuffer(_InstancesDrawMatrixRows23, m_shadowCulledMatrixRows23);
        // irm.shadowLod02MatPropBlock.SetBuffer(_InstancesDrawMatrixRows23, m_shadowCulledMatrixRows23);
        //     
        // irm.shadowLod00MatPropBlock.SetBuffer(_InstancesDrawMatrixRows45, m_shadowCulledMatrixRows45);
        // irm.shadowLod01MatPropBlock.SetBuffer(_InstancesDrawMatrixRows45, m_shadowCulledMatrixRows45);
        // irm.shadowLod02MatPropBlock.SetBuffer(_InstancesDrawMatrixRows45, m_shadowCulledMatrixRows45);
    }
}
