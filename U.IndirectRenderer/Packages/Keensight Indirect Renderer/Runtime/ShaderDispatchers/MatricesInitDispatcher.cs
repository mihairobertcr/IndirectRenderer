using System.Collections.Generic;
using UnityEngine;
using Keensight.Rendering.Context;

namespace Keensight.Rendering.ShaderDispatchers
{
    public class MatricesInitDispatcher : ComputeShaderDispatcher
    {
        private static readonly int PositionsId = Shader.PropertyToID("_Positions");
        private static readonly int RotationsId = Shader.PropertyToID("_Rotations");
        private static readonly int ScalesId = Shader.PropertyToID("_Scales");

        private readonly int _kernel;
        private readonly int _threadGroupX;

        private readonly ComputeBuffer _positionsBuffer;
        private readonly ComputeBuffer _rotationsBuffer;
        private readonly ComputeBuffer _scalesBuffer;
        private readonly MatrixBuffer _matrixBuffer;

        public MatricesInitDispatcher(RendererContext context)
            : base(context.Config.MatricesInit, context)
        {
            _kernel = GetKernel("CSMain");
            _threadGroupX = Mathf.Max(1, Context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));

            InitializeTransformBuffers(
                out _positionsBuffer,
                out _rotationsBuffer,
                out _scalesBuffer,
                out _matrixBuffer);
        }

        public override ComputeShaderDispatcher Initialize()
        {
            SetTransformData();
            SubmitTransformsData();

            return this;
        }

        public override void Dispatch() => ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);

        private void SetTransformData()
        {
            var positions = new List<Vector3>();
            var rotations = new List<Vector3>();
            var scales = new List<Vector3>();

            foreach (var mesh in Context.MeshesProperties)
            {
                foreach (var transform in mesh.Transforms)
                {
                    positions.Add(transform.Position);
                    rotations.Add(transform.Rotation);
                    scales.Add(transform.Scale);
                }
            }

            _positionsBuffer.SetData(positions);
            _rotationsBuffer.SetData(rotations);
            _scalesBuffer.SetData(scales);
        }

        private void SubmitTransformsData()
        {
            ComputeShader.SetBuffer(_kernel, PositionsId, _positionsBuffer);
            ComputeShader.SetBuffer(_kernel, RotationsId, _rotationsBuffer);
            ComputeShader.SetBuffer(_kernel, ScalesId, _scalesBuffer);
            ComputeShader.SetBuffer(_kernel, MatrixRows01sId, _matrixBuffer.Rows01);
            ComputeShader.SetBuffer(_kernel, MatrixRows23sId, _matrixBuffer.Rows23);
            ComputeShader.SetBuffer(_kernel, MatrixRows45sId, _matrixBuffer.Rows45);
        }

        private void InitializeTransformBuffers(out ComputeBuffer position, out ComputeBuffer rotation,
            out ComputeBuffer scale, out MatrixBuffer matrix)
        {
            position = Context.Transforms.Positions;
            rotation = Context.Transforms.Rotations;
            scale = Context.Transforms.Scales;
            matrix = Context.Transforms.Matrices;
        }
    }
}