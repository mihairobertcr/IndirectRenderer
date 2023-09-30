using System;
using System.Runtime.InteropServices;
using System.Text;
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

public class TransformBuffer : IDisposable
{
    public ComputeBuffer Positions { get; }
    public ComputeBuffer Rotations { get; }
    public ComputeBuffer Scales { get; }

    public MatrixBuffer Matrices { get; }
    public MatrixBuffer CulledMeshesMatrices { get; }
    public MatrixBuffer CulledShadowsMatrices { get; }

    private readonly int _count;

    public TransformBuffer(int count)
    {
        Positions = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        Rotations = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        Scales = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);

        Matrices = new MatrixBuffer(count);
        CulledMeshesMatrices = new MatrixBuffer(count);
        CulledShadowsMatrices = new MatrixBuffer(count);

        _count = count;
    }

    public void Dispose()
    {
        Positions?.Dispose();
        Rotations?.Dispose();
        Scales?.Dispose();
        Matrices?.Dispose();
        CulledMeshesMatrices?.Dispose();
        CulledShadowsMatrices?.Dispose();
    }
    
    public void LogMatrices(string prefix = "")
    {
        var matrix01 = new Matrix2x2[_count];
        var matrix23 = new Matrix2x2[_count];
        var matrix45 = new Matrix2x2[_count];
        Matrices.Rows01.GetData(matrix01);
        Matrices.Rows23.GetData(matrix23);
        Matrices.Rows45.GetData(matrix45);

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
    
    public void LogCulledMatrices(string meshPrefix = "", string shadowPrefix = "")
    {
        var meshesMatrix01 = new Matrix2x2[_count];
        var meshesMatrix23 = new Matrix2x2[_count];
        var meshesMatrix45 = new Matrix2x2[_count];
        CulledMeshesMatrices.Rows01.GetData(meshesMatrix01);
        CulledMeshesMatrices.Rows23.GetData(meshesMatrix23);
        CulledMeshesMatrices.Rows45.GetData(meshesMatrix45);
        
        var shadowsMatrix01 = new Matrix2x2[_count];
        var shadowsMatrix23 = new Matrix2x2[_count];
        var shadowsMatrix45 = new Matrix2x2[_count];
        CulledShadowsMatrices.Rows01.GetData(shadowsMatrix01);
        CulledShadowsMatrices.Rows23.GetData(shadowsMatrix23);
        CulledShadowsMatrices.Rows45.GetData(shadowsMatrix45);
        
        var meshesLog = new StringBuilder();
        var shadowsLog = new StringBuilder();
        if (!string.IsNullOrEmpty(meshPrefix))
        {
            meshesLog.AppendLine(meshPrefix);
        }

        if (!string.IsNullOrEmpty(shadowPrefix))
        {
            shadowsLog.AppendLine(shadowPrefix);
        }
        
        for (int i = 0; i < meshesMatrix01.Length; i++)
        {
            meshesLog.AppendLine(
                i + "\n" 
                  + meshesMatrix01[i].Row0 + "\n"
                  + meshesMatrix01[i].Row1 + "\n"
                  + meshesMatrix23[i].Row0 + "\n"
                  + "\n\n"
                  + meshesMatrix23[i].Row1 + "\n"
                  + meshesMatrix45[i].Row0 + "\n"
                  + meshesMatrix45[i].Row1 + "\n"
                  + "\n"
            );
            
            shadowsLog.AppendLine(
                i + "\n" 
                  + shadowsMatrix01[i].Row0 + "\n"
                  + shadowsMatrix01[i].Row1 + "\n"
                  + shadowsMatrix23[i].Row0 + "\n"
                  + "\n\n"
                  + shadowsMatrix23[i].Row1 + "\n"
                  + shadowsMatrix45[i].Row0 + "\n"
                  + shadowsMatrix45[i].Row1 + "\n"
                  + "\n"
            );
        }

        Debug.Log(meshesLog.ToString());
        Debug.Log(shadowsLog.ToString());
    }
}