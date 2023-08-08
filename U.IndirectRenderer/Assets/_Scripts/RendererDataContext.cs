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
        Rows01 = new ComputeBuffer(count, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        Rows23 = new ComputeBuffer(count, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        Rows45 = new ComputeBuffer(count, Indirect2x2Matrix.Size, ComputeBufferType.Default);
    }

    public void Dispose()
    {
        Rows01?.Dispose();
        Rows23?.Dispose();
        Rows45?.Dispose();
    }
}

public class ArgumentsBuffer : IDisposable
{
    public ComputeBuffer Meshes { get; }
    public ComputeBuffer Shadows { get; }

    public ComputeBuffer LodArgs0 { get; }
    public ComputeBuffer LodArgs1 { get; }
    public ComputeBuffer LodArgs2 { get; }

    public const int ARGS_PER_INSTANCE_TYPE_COUNT = DRAW_CALLS_COUNT * ARGS_PER_DRAW_COUNT;

    private const int DRAW_CALLS_COUNT = 3;
    private const int ARGS_PER_DRAW_COUNT = 5;
    private const int ARGS_BYTE_SIZE_PER_DRAW_CALL = ARGS_PER_DRAW_COUNT * sizeof(uint);

    private readonly MeshProperties _meshProperties;
    private readonly uint[] _args;

    public ArgumentsBuffer(MeshProperties meshProperties, IndirectRendererConfig config)
    {
        _meshProperties = meshProperties;
        _args = InitializeArgumentsBuffer();

        Meshes = new ComputeBuffer(ARGS_PER_INSTANCE_TYPE_COUNT, sizeof(uint), ComputeBufferType.IndirectArguments);
        Shadows = new ComputeBuffer(ARGS_PER_INSTANCE_TYPE_COUNT, sizeof(uint), ComputeBufferType.IndirectArguments);
        Reset();

        var args0 = new uint[] { 0, 0, 0, 0, 0 };
        args0[0] = config.Lod0Mesh.GetIndexCount(0);
        args0[2] = config.Lod0Mesh.GetIndexStart(0);
        args0[3] = config.Lod0Mesh.GetBaseVertex(0);
        LodArgs0 = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        LodArgs0.SetData(args0);

        var args1 = new uint[] { 0, 0, 0, 0, 0 };
        args1[0] = config.Lod1Mesh.GetIndexCount(0);
        args1[2] = config.Lod1Mesh.GetIndexStart(0);
        args1[3] = config.Lod1Mesh.GetBaseVertex(0);
        LodArgs1 = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        LodArgs1.SetData(args1);

        var args2 = new uint[] { 0, 0, 0, 0, 0 };
        args2[0] = config.Lod2Mesh.GetIndexCount(0);
        args2[2] = config.Lod2Mesh.GetIndexStart(0);
        args2[3] = config.Lod2Mesh.GetBaseVertex(0);
        LodArgs2 = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        LodArgs2.SetData(args2);
    }

    public void Reset()
    {
        Meshes.SetData(_args);
        Shadows.SetData(_args);
    }

    public void Dispose()
    {
        Meshes?.Dispose();
        Shadows?.Dispose();
        LodArgs0?.Dispose();
        LodArgs1?.Dispose();
        LodArgs2?.Dispose();
    }

    public void Log(string instancePrefix = "", string shadowPrefix = "")
    {
        var args = new uint[ARGS_PER_INSTANCE_TYPE_COUNT];
        var shadowArgs = new uint[ARGS_PER_INSTANCE_TYPE_COUNT];
        Meshes.GetData(args);
        Shadows.GetData(shadowArgs);

        var instancesSB = new StringBuilder();
        var shadowsSB = new StringBuilder();

        if (!string.IsNullOrEmpty(instancePrefix)) instancesSB.AppendLine(instancePrefix);
        if (!string.IsNullOrEmpty(shadowPrefix)) shadowsSB.AppendLine(shadowPrefix);

        instancesSB.AppendLine("");
        shadowsSB.AppendLine("");

        instancesSB.AppendLine("IndexCountPerInstance InstanceCount StartIndexLocation BaseVertexLocation StartInstanceLocation");
        shadowsSB.AppendLine("IndexCountPerInstance InstanceCount StartIndexLocation BaseVertexLocation StartInstanceLocation");

        instancesSB.AppendLine(_meshProperties.Mesh.name);
        shadowsSB.AppendLine(_meshProperties.Mesh.name);
        for (var i = 0; i < args.Length; i++)
        {
            instancesSB.Append(args[i] + " ");
            shadowsSB.Append(shadowArgs[i] + " ");

            if ((i + 1) % 5 != 0) continue;
            instancesSB.AppendLine("");
            shadowsSB.AppendLine("");

            if ((i + 1) >= args.Length || (i + 1) % ARGS_PER_INSTANCE_TYPE_COUNT != 0) continue;
            instancesSB.AppendLine("");
            shadowsSB.AppendLine("");

            var mesh = _meshProperties.Mesh;
            instancesSB.AppendLine(mesh.name);
            shadowsSB.AppendLine(mesh.name);
        }

        Debug.Log(instancesSB.ToString());
        Debug.Log(shadowsSB.ToString());
    }

    private uint[] InitializeArgumentsBuffer()
    {
        var args = new uint[ARGS_PER_INSTANCE_TYPE_COUNT];

        // Lod 0
        args[0] = _meshProperties.Lod0Indices; // 0 - index count per instance, 
        args[1] = 0;                           // 1 - instance count
        args[2] = 0;                           // 2 - start index location
        args[3] = 0;                           // 3 - base vertex location
        args[4] = 0;                           // 4 - start instance location

        // Lod 1
        args[5] = _meshProperties.Lod1Indices; // 0 - index count per instance, 
        args[6] = 0;                           // 1 - instance count
        args[7] = args[0] + args[2];           // 2 - start index location
        args[8] = 0;                           // 3 - base vertex location
        args[9] = 0;                           // 4 - start instance location

        // Lod 2
        args[10] = _meshProperties.Lod2Indices; // 0 - index count per instance, 
        args[11] = 0;                           // 1 - instance count
        args[12] = args[5] + args[7];           // 2 - start index location
        args[13] = 0;                           // 3 - base vertex location
        args[14] = 0;                           // 4 - start instance location

        return args;
    }
}

public class TransformBuffer : IDisposable
{
    public ComputeBuffer PositionsBuffer { get; }
    public ComputeBuffer RotationsBuffer { get; }
    public ComputeBuffer ScalesBuffer { get; }

    public MatrixBuffer Matrix { get; }
    public MatrixBuffer CulledMatrix { get; }
    public MatrixBuffer ShadowsCulledMatrix { get; }

    private readonly int _count;

    public TransformBuffer(int count)
    {
        PositionsBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        RotationsBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        ScalesBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);

        Matrix = new MatrixBuffer(count);
        CulledMatrix = new MatrixBuffer(count);
        ShadowsCulledMatrix = new MatrixBuffer(count);

        _count = count;
    }

    public void Dispose()
    {
        PositionsBuffer?.Dispose();
        RotationsBuffer?.Dispose();
        ScalesBuffer?.Dispose();
        Matrix?.Dispose();
        CulledMatrix?.Dispose();
        ShadowsCulledMatrix?.Dispose();
    }
    
    public void LogMatrices(string prefix = "")
    {
        var matrix01 = new Indirect2x2Matrix[_count];
        var matrix23 = new Indirect2x2Matrix[_count];
        var matrix45 = new Indirect2x2Matrix[_count];

        Matrix.Rows01.GetData(matrix01);
        Matrix.Rows23.GetData(matrix23);
        Matrix.Rows45.GetData(matrix45);

        var log = new StringBuilder();
        if (!string.IsNullOrEmpty(prefix))
        {
            log.AppendLine(prefix);
        }

        for (var i = 0; i < matrix01.Length; i++)
        {
            log.AppendLine(
                i + "\n"
                  + matrix01[i].FirstRow + "\n"
                  + matrix01[i].SecondRow + "\n"
                  + matrix23[i].FirstRow + "\n"
                  + "\n\n"
                  + matrix23[i].SecondRow + "\n"
                  + matrix45[i].FirstRow + "\n"
                  + matrix45[i].SecondRow + "\n"
                  + "\n"
            );
        }

        Debug.Log(log.ToString());
    }
    
    public void LogCulledMatrices(string meshPrefix = "", string shadowPrefix = "")
    {
        var instancesMatrix1 = new Indirect2x2Matrix[_count];
        var instancesMatrix2 = new Indirect2x2Matrix[_count];
        var instancesMatrix3 = new Indirect2x2Matrix[_count];
        CulledMatrix.Rows01.GetData(instancesMatrix1);
        CulledMatrix.Rows23.GetData(instancesMatrix2);
        CulledMatrix.Rows45.GetData(instancesMatrix3);
        
        var shadowsMatrix1 = new Indirect2x2Matrix[_count];
        var shadowsMatrix2 = new Indirect2x2Matrix[_count];
        var shadowsMatrix3 = new Indirect2x2Matrix[_count];
        ShadowsCulledMatrix.Rows01.GetData(shadowsMatrix1);
        ShadowsCulledMatrix.Rows23.GetData(shadowsMatrix2);
        ShadowsCulledMatrix.Rows45.GetData(shadowsMatrix3);
        
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
        
        for (int i = 0; i < instancesMatrix1.Length; i++)
        {
            meshesLog.AppendLine(
                i + "\n" 
                + instancesMatrix1[i].FirstRow + "\n"
                + instancesMatrix1[i].SecondRow + "\n"
                + instancesMatrix2[i].FirstRow + "\n"
                + "\n\n"
                + instancesMatrix2[i].SecondRow + "\n"
                + instancesMatrix3[i].FirstRow + "\n"
                + instancesMatrix3[i].SecondRow + "\n"
                + "\n"
            );
            
            shadowsLog.AppendLine(
                i + "\n" 
                + shadowsMatrix1[i].FirstRow + "\n"
                + shadowsMatrix1[i].SecondRow + "\n"
                + shadowsMatrix2[i].FirstRow + "\n"
                + "\n\n"
                + shadowsMatrix2[i].SecondRow + "\n"
                + shadowsMatrix3[i].FirstRow + "\n"
                + shadowsMatrix3[i].SecondRow + "\n"
                + "\n"
            );
        }

        Debug.Log(meshesLog.ToString());
        Debug.Log(shadowsLog.ToString());
    }
}

public class SortingBuffer : IDisposable
{
    public ComputeBuffer Data { get; }
    public ComputeBuffer Temp { get; }

    private readonly int _count;

    public SortingBuffer(int count)
    {
        Data = new ComputeBuffer(count, SortingData.Size, ComputeBufferType.Default);
        Temp = new ComputeBuffer(count, SortingData.Size, ComputeBufferType.Default);
        _count = count;
    }

    public void Dispose()
    {
        Data?.Dispose();
        Temp?.Dispose();
    }
    
    public void Log(string prefix = "")
    {
        var data = new SortingData[_count];
        Data.GetData(data);
        
        var log = new StringBuilder();
        if (!string.IsNullOrEmpty(prefix))
        {
            log.AppendLine(prefix);
        }
        
        uint lastDrawCallIndex = 0;
        for (var i = 0; i < data.Length; i++)
        {
            var drawCallIndex = (data[i].DrawCallInstanceIndex >> 16);
            var instanceIndex = (data[i].DrawCallInstanceIndex) & 0xFFFF;
            if (i == 0)
            {
                lastDrawCallIndex = drawCallIndex;
            }
            
            log.AppendLine($"({drawCallIndex}) --> {data[i].DistanceToCamera} instanceIndex:{instanceIndex}");

            if (lastDrawCallIndex == drawCallIndex) continue;
            
            Debug.Log(log.ToString());
            log = new StringBuilder();
            lastDrawCallIndex = drawCallIndex;
        }

        Debug.Log(log.ToString());
    }
}

public class InstancesDataBuffer : IDisposable
{
    public ComputeBuffer Meshes { get; }
    public ComputeBuffer Shadows { get; }

    private readonly int _count;
    
    public InstancesDataBuffer(int count)
    {
        Meshes = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Default);
        Shadows = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Default);
        _count = count;
    }

    public void Dispose()
    {
        Meshes?.Dispose();
        Shadows?.Dispose();
    }
    
    public void Log(string meshPrefix = "", string shadowPrefix = "")
    {
        var meshesData = new uint[_count];
        var shadowsData = new uint[_count];
        
        Meshes.GetData(meshesData);
        Shadows.GetData(shadowsData);
        
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
        
        for (var i = 0; i < meshesData.Length; i++)
        {
            meshesLog.AppendLine(i + ": " + meshesData[i]);
            shadowsLog.AppendLine(i + ": " + shadowsData[i]);
        }

        Debug.Log(meshesLog.ToString());
        Debug.Log(shadowsLog.ToString());
    }
}

public class RendererDataContext : IDisposable
{
    public int MeshesCount { get; }
    public ComputeBuffer BoundsData { get; }
    
    public ArgumentsBuffer Arguments { get; }
    public TransformBuffer Transform { get; }
    public SortingBuffer Sorting { get; }
    public InstancesDataBuffer Visibility { get; }
    public InstancesDataBuffer GroupSums { get; }
    public InstancesDataBuffer ScannedPredicates { get; }
    public InstancesDataBuffer ScannedGroupSums { get; }

    public RendererDataContext(MeshProperties meshProperties, int meshesCount, IndirectRendererConfig config)
    {
        MeshesCount = meshesCount;
        BoundsData = new ComputeBuffer(MeshesCount, IndirectRendering.BoundsData.Size, ComputeBufferType.Default);

        Arguments = new ArgumentsBuffer(meshProperties, config);
        Transform = new TransformBuffer(meshesCount);
        Sorting = new SortingBuffer(meshesCount);
        Visibility = new InstancesDataBuffer(meshesCount);
        GroupSums = new InstancesDataBuffer(meshesCount);
        ScannedPredicates = new InstancesDataBuffer(meshesCount);
        ScannedGroupSums = new InstancesDataBuffer(meshesCount);
    }

    public void Dispose()
    {
        BoundsData?.Dispose();

        Arguments?.Dispose();
        Transform?.Dispose();
        Sorting?.Dispose();
        Visibility?.Dispose();
        GroupSums?.Dispose();
        ScannedPredicates?.Dispose();
        ScannedGroupSums?.Dispose();
    }
}