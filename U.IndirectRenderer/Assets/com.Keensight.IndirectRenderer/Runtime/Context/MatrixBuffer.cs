using System;
using UnityEngine;
using IndirectRendering;

public class MatrixBuffer : IDisposable
{
    public ComputeBuffer Rows01 { get; }
    public ComputeBuffer Rows23 { get; }
    public ComputeBuffer Rows45 { get; }

    public MatrixBuffer(int count)
    {
        Rows01 = new ComputeBuffer(count, Matrix2x2.Size, ComputeBufferType.Default);
        Rows23 = new ComputeBuffer(count, Matrix2x2.Size, ComputeBufferType.Default);
        Rows45 = new ComputeBuffer(count, Matrix2x2.Size, ComputeBufferType.Default);
    }

    public void Dispose()
    {
        Rows01?.Dispose();
        Rows23?.Dispose();
        Rows45?.Dispose();
    }
}