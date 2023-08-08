using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using IndirectRendering;

public class MatricesInitializer
{
    private const int SCAN_THREAD_GROUP_SIZE = 64; // TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;

    private readonly RendererDataContext _context;

    public MatricesInitializer(ComputeShader computeShader, int numberOfInstances, RendererDataContext context)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _context = context;

        InitializeTransformBuffers();
        InitializeComputeShader();
    }

    public void Initialize(List<Vector3> positions, List<Vector3> rotations, List<Vector3> scales)
    {
        _context.Transform.PositionsBuffer.SetData(positions);
        _context.Transform.RotationsBuffer.SetData(rotations);
        _context.Transform.ScalesBuffer.SetData(scales);
    }

    public void Dispatch()
    {
        var groupX = Mathf.Max(1, _numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE));
        _computeShader.Dispatch(ShaderKernels.MatricesInitializer, groupX, 1, 1);

        // _positionsBuffer?.Release();
        // _rotationsBuffer?.Release();
        // _scalesBuffer?.Release();
    }

    // TODO: #EDITOR


    private void InitializeTransformBuffers()
    {
    }

    private void InitializeComputeShader()
    {
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.Positions, _context.Transform.PositionsBuffer);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.Rotations, _context.Transform.RotationsBuffer);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.Scales, _context.Transform.ScalesBuffer);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.MatrixRows01, _context.Transform.Matrix.Rows01);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.MatrixRows23, _context.Transform.Matrix.Rows23);
        _computeShader.SetBuffer(ShaderKernels.MatricesInitializer, ShaderProperties.MatrixRows45, _context.Transform.Matrix.Rows45);
    }
}