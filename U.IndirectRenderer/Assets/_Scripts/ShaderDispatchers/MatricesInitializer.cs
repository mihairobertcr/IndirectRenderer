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

        InitializeTransformBuffers(
            out _positionsBuffer, 
            out _rotationsBuffer, 
            out _scalesBuffer, 
            out _matrixBuffer);
    }

    public MatricesInitializer SetTransformData(IndirectMesh[] meshes)
    {
        var positions = new List<Vector3>();
        var rotations = new List<Vector3>();
        var scales = new List<Vector3>();

        for (var i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            for (var k = 0; k < Context.MeshesCount; k++)
            {
                positions.Add(mesh.Positions[k]);
                rotations.Add(mesh.Rotations[k]);
                scales.Add(mesh.Scales[k]);
            }
        }

        _positionsBuffer.SetData(positions);
        _rotationsBuffer.SetData(rotations);
        _scalesBuffer.SetData(scales);

        return this;
    }
    
    public MatricesInitializer SubmitTransformsData()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.Positions, _positionsBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.Rotations, _rotationsBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.Scales, _scalesBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.MatrixRows01, _matrixBuffer.Rows01);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.MatrixRows23, _matrixBuffer.Rows23);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.MatrixRows45, _matrixBuffer.Rows45);
        
        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);

    private void InitializeTransformBuffers(out ComputeBuffer position, out ComputeBuffer rotation, 
        out ComputeBuffer scale, out MatrixBuffer matrix)
    {
        position = Context.Transform.PositionsBuffer;
        rotation = Context.Transform.RotationsBuffer;
        scale = Context.Transform.ScalesBuffer;
        matrix = Context.Transform.Matrix;
    }
}