using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using IndirectRendering;

public class RendererDataContext
{
    public int MeshesCount { get; }

    public ComputeBuffer Args { get; }
    public ComputeBuffer ShadowsArgs { get; }

    public ComputeBuffer MatrixRows01 { get; }
    public ComputeBuffer MatrixRows23 { get; }
    public ComputeBuffer MatrixRows45 { get; }

    public ComputeBuffer CulledMatrixRows01 { get; }
    public ComputeBuffer CulledMatrixRows23 { get; }
    public ComputeBuffer CulledMatrixRows45 { get; }

    public ComputeBuffer ShadowsCulledMatrixRows01 { get; }
    public ComputeBuffer ShadowsCulledMatrixRows23 { get; }
    public ComputeBuffer ShadowsCulledMatrixRows45 { get; }

    public ComputeBuffer SortingData { get; }
    public ComputeBuffer SortingDataTemp { get; }

    public ComputeBuffer IsVisible { get; }
    public ComputeBuffer IsShadowVisible { get; }
    public ComputeBuffer BoundsData { get; }

    public ComputeBuffer GroupSumsBuffer { get; }
    public ComputeBuffer ShadowsGroupSumsBuffer { get; }

    public ComputeBuffer ScannedPredicates { get; }
    public ComputeBuffer ShadowsScannedPredicates { get; }

    public ComputeBuffer ScannedGroupSums { get; }
    public ComputeBuffer ShadowsScannedGroupSums { get; }

    public ComputeBuffer LodArgs0 { get; }
    public ComputeBuffer LodArgs1 { get; }
    public ComputeBuffer LodArgs2 { get; }

    public const int NUMBER_OF_ARGS_PER_INSTANCE_TYPE = NUMBER_OF_DRAW_CALLS * NUMBER_OF_ARGS_PER_DRAW; // 3draws * 5args = 15args

    private const int NUMBER_OF_DRAW_CALLS = 3; // (LOD00 + LOD01 + LOD02)
    private const int NUMBER_OF_ARGS_PER_DRAW = 5; // (indexCount, instanceCount, startIndex, baseVertex, startInstance)
    private const int ARGS_BYTE_SIZE_PER_DRAW_CALL = NUMBER_OF_ARGS_PER_DRAW * sizeof(uint); // 5args * 4bytes = 20 bytes

    private readonly MeshProperties _meshProperties;
    private readonly uint[] _args;

    public RendererDataContext(MeshProperties meshProperties, int meshesCount, IndirectRendererConfig config)
    {
        _meshProperties = meshProperties;
        _args = InitializeArgumentsBuffer();

        MeshesCount = meshesCount;

        Args = new ComputeBuffer(NUMBER_OF_ARGS_PER_INSTANCE_TYPE, sizeof(uint), ComputeBufferType.IndirectArguments);
        ShadowsArgs = new ComputeBuffer(NUMBER_OF_ARGS_PER_INSTANCE_TYPE, sizeof(uint), ComputeBufferType.IndirectArguments);
        ResetArguments();

        MatrixRows01 = new ComputeBuffer(MeshesCount, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        MatrixRows23 = new ComputeBuffer(MeshesCount, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        MatrixRows45 = new ComputeBuffer(MeshesCount, Indirect2x2Matrix.Size, ComputeBufferType.Default);

        SortingData = new ComputeBuffer(MeshesCount, IndirectRendering.SortingData.Size, ComputeBufferType.Default);
        SortingDataTemp = new ComputeBuffer(MeshesCount, IndirectRendering.SortingData.Size, ComputeBufferType.Default);

        IsVisible = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        IsShadowVisible = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        BoundsData = new ComputeBuffer(MeshesCount, IndirectRendering.BoundsData.Size, ComputeBufferType.Default);

        GroupSumsBuffer = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        ShadowsGroupSumsBuffer = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        ScannedPredicates = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        ShadowsScannedPredicates = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        
        ScannedGroupSums = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        ShadowsScannedGroupSums = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        
        CulledMatrixRows01 = new ComputeBuffer(MeshesCount, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        CulledMatrixRows23 = new ComputeBuffer(MeshesCount, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        CulledMatrixRows45 = new ComputeBuffer(MeshesCount, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        
        ShadowsCulledMatrixRows01 = new ComputeBuffer(MeshesCount, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        ShadowsCulledMatrixRows23 = new ComputeBuffer(MeshesCount, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        ShadowsCulledMatrixRows45 = new ComputeBuffer(MeshesCount, Indirect2x2Matrix.Size, ComputeBufferType.Default);

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

    public void ResetArguments()
    {
        Args.SetData(_args);
        ShadowsArgs.SetData(_args);
    }

    public void Dispose()
    {
        Args.Release();
        ShadowsArgs.Release();

        MatrixRows01.Release();
        MatrixRows23.Release();
        MatrixRows45.Release();

        CulledMatrixRows01.Release();
        CulledMatrixRows23.Release();
        CulledMatrixRows45.Release();

        ShadowsCulledMatrixRows01.Release();
        ShadowsCulledMatrixRows23.Release();
        ShadowsCulledMatrixRows45.Release();

        BoundsData.Release();
        IsVisible.Release();
        IsShadowVisible.Release();

        SortingData.Release();
        SortingDataTemp.Release();

        GroupSumsBuffer.Release();
        ShadowsGroupSumsBuffer.Release();

        ScannedPredicates.Release();
        ShadowsScannedPredicates.Release();

        ScannedGroupSums.Release();
        ShadowsScannedGroupSums.Release();


        LodArgs0.Release();
        LodArgs1.Release();
        LodArgs2.Release();
    }

    private uint[] InitializeArgumentsBuffer()
    {
        var args = new uint[NUMBER_OF_ARGS_PER_INSTANCE_TYPE];

        // Lod 0
        args[0] = _meshProperties.Lod0Indices; // 0 - index count per instance, 
        args[1] = 0; // 1 - instance count
        args[2] = 0; // 2 - start index location
        args[3] = 0; // 3 - base vertex location
        args[4] = 0; // 4 - start instance location

        // Lod 1
        args[5] = _meshProperties.Lod1Indices; // 0 - index count per instance, 
        args[6] = 0; // 1 - instance count
        args[7] = args[0] + args[2]; // 2 - start index location
        args[8] = 0; // 3 - base vertex location
        args[9] = 0; // 4 - start instance location

        // Lod 2
        args[10] = _meshProperties.Lod2Indices; // 0 - index count per instance, 
        args[11] = 0; // 1 - instance count
        args[12] = args[5] + args[7]; // 2 - start index location
        args[13] = 0; // 3 - base vertex location
        args[14] = 0; // 4 - start instance location

        return args;
    }

    public void LogArgumentsBuffers(string instancePrefix = "", string shadowPrefix = "")
    {
        var args = new uint[NUMBER_OF_ARGS_PER_INSTANCE_TYPE];
        var shadowArgs = new uint[NUMBER_OF_ARGS_PER_INSTANCE_TYPE];
        Args.GetData(args);
        ShadowsArgs.GetData(shadowArgs);

        var instancesSB = new StringBuilder();
        var shadowsSB = new StringBuilder();

        if (!string.IsNullOrEmpty(instancePrefix)) instancesSB.AppendLine(instancePrefix);
        if (!string.IsNullOrEmpty(shadowPrefix)) shadowsSB.AppendLine(shadowPrefix);

        instancesSB.AppendLine("");
        shadowsSB.AppendLine("");

        instancesSB.AppendLine("IndexCountPerInstance InstanceCount StartIndexLocation BaseVertexLocation StartInstanceLocation");
        shadowsSB.AppendLine("IndexCountPerInstance InstanceCount StartIndexLocation BaseVertexLocation StartInstanceLocation");

        // var counter = 0;
        instancesSB.AppendLine(_meshProperties.Mesh.name);
        shadowsSB.AppendLine(_meshProperties.Mesh.name);
        for (var i = 0; i < args.Length; i++)
        {
            instancesSB.Append(args[i] + " ");
            shadowsSB.Append(shadowArgs[i] + " ");

            if ((i + 1) % 5 != 0) continue;
            instancesSB.AppendLine("");
            shadowsSB.AppendLine("");

            if ((i + 1) >= args.Length || (i + 1) % NUMBER_OF_ARGS_PER_INSTANCE_TYPE != 0) continue;
            instancesSB.AppendLine("");
            shadowsSB.AppendLine("");

            // counter++;
            var mesh = _meshProperties.Mesh;
            instancesSB.AppendLine(mesh.name);
            shadowsSB.AppendLine(mesh.name);
        }

        Debug.Log(instancesSB.ToString());
        Debug.Log(shadowsSB.ToString());
    }
}