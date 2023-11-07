using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Keensight.Rendering.Data;

namespace Keensight.Rendering.Context
{
    public class TransformBuffer : IDisposable
    {
        public ComputeBuffer Positions { get; }
        public ComputeBuffer Rotations { get; }
        public ComputeBuffer Scales { get; }

        public MatrixBuffer Matrices { get; }
        public MatrixBuffer CulledMatrices { get; }

        private readonly int _count;

        public TransformBuffer(int count)
        {
            Positions = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
            Rotations = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
            Scales = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);

            Matrices = new MatrixBuffer(count);
            CulledMatrices = new MatrixBuffer(count);

            _count = count;
        }

        public void Dispose()
        {
            Positions?.Dispose();
            Rotations?.Dispose();
            Scales?.Dispose();
            Matrices?.Dispose();
            CulledMatrices?.Dispose();
        }
        
        public void LogMatrices(string prefix = "") => Log(Matrices, prefix);

        public void LogCulledMatrices(string prefix = "") => Log(CulledMatrices, prefix);

        private void Log(MatrixBuffer matrixBuffer, string prefix = "")
        {
            var matrix01 = new Matrix2x2[_count];
            var matrix23 = new Matrix2x2[_count];
            var matrix45 = new Matrix2x2[_count];
            matrixBuffer.Rows01.GetData(matrix01);
            matrixBuffer.Rows23.GetData(matrix23);
            matrixBuffer.Rows45.GetData(matrix45);

            var log = new StringBuilder();
            if (!string.IsNullOrEmpty(prefix))
            {
                log.AppendLine(prefix);
            }

            for (var i = 0; i < matrix01.Length; i++)
            {
                log.AppendLine(
                    i + "\n" 
                      + matrix01[i].Row0 + "\n"
                      + matrix01[i].Row1 + "\n"
                      + matrix23[i].Row0 + "\n"
                      + "\n\n"
                      + matrix23[i].Row1 + "\n"
                      + matrix45[i].Row0 + "\n"
                      + matrix45[i].Row1 + "\n"
                      + "\n"
                );
            }

            Debug.Log(log.ToString());
        }
    }
}