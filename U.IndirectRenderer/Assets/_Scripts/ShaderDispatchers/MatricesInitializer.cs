using System.Collections.Generic;
using UnityEngine;

public class MatricesInitializer : ComputeShaderDispatcher
{
    private readonly int _kernel;
    private readonly int _threadGroupX;

    private readonly ComputeBuffer _positionsBuffer;
    private readonly ComputeBuffer _rotationsBuffer;
    private readonly ComputeBuffer _scalesBuffer;
    private readonly MatrixBuffer _matrixBuffer;

    public MatricesInitializer(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _kernel = GetKernel("CSMain");
        _threadGroupX = Mathf.Max(1, Context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));

        InitializeTransformBuffers(out _positionsBuffer, out _rotationsBuffer, out _scalesBuffer, out _matrixBuffer);
        InitializeComputeShader();
    }

    public void SetTransformData(List<Vector3> positions, List<Vector3> rotations, List<Vector3> scales)
    {
        _positionsBuffer.SetData(positions);
        _rotationsBuffer.SetData(rotations);
        _scalesBuffer.SetData(scales);
    }

    public override void Dispatch()
    {
        ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);

        _positionsBuffer?.Release();
        _rotationsBuffer?.Release();
        _scalesBuffer?.Release();
    }

    private void InitializeTransformBuffers(out ComputeBuffer position, out ComputeBuffer rotation, 
        out ComputeBuffer scale, out MatrixBuffer matrix)
    {
        position = Context.Transform.PositionsBuffer;
        rotation = Context.Transform.RotationsBuffer;
        scale = Context.Transform.ScalesBuffer;
        matrix = Context.Transform.Matrix;
    }

    private void InitializeComputeShader()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.Positions, _positionsBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.Rotations, _rotationsBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.Scales, _scalesBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.MatrixRows01, _matrixBuffer.Rows01);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.MatrixRows23, _matrixBuffer.Rows23);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.MatrixRows45, _matrixBuffer.Rows45);
    }
}