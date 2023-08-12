using UnityEngine;

public class InstancesDataCopier : ComputeShaderDispatcher
{
    private readonly int _dataCopierKernel;
    private readonly int _argumentsSplitterKernel;
    private readonly int _threadGroupX;

    private readonly ComputeBuffer _matricesRows01;
    private readonly ComputeBuffer _matricesRows23;
    private readonly ComputeBuffer _matricesRows45;
    private readonly ComputeBuffer _sortingData;
    private readonly ComputeBuffer _boundsData;
    
    private readonly ComputeBuffer _meshesVisibility;
    private readonly ComputeBuffer _meshesScannedGroupSums;
    private readonly ComputeBuffer _meshesScannedPredicates;
    private readonly ComputeBuffer _meshesCulledMatricesRows01;
    private readonly ComputeBuffer _meshesCulledMatricesRows23;
    private readonly ComputeBuffer _meshesCulledMatricesRows45;
    private readonly ComputeBuffer _meshesArguments;

    private readonly ComputeBuffer _shadowsVisibility;
    private readonly ComputeBuffer _shadowsScannedGroupSums;
    private readonly ComputeBuffer _shadowsScannedPredicates;
    private readonly ComputeBuffer _shadowsCulledMatricesRows01;
    private readonly ComputeBuffer _shadowsCulledMatricesRows23;
    private readonly ComputeBuffer _shadowsCulledMatricesRows45;
    private readonly ComputeBuffer _shadowsArguments;
    
    private readonly ComputeBuffer _lodArgs0;
    private readonly ComputeBuffer _lodArgs1;
    private readonly ComputeBuffer _lodArgs2;
    
    public InstancesDataCopier(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _dataCopierKernel = GetKernel("CSMain");
        _argumentsSplitterKernel = GetKernel("SplitArguments");
        _threadGroupX = Mathf.Max(1, context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));

        InitializeCopingBuffers(
            out _matricesRows01, 
            out _matricesRows23, 
            out _matricesRows45,
            out _sortingData,
            out _boundsData);
        
        InitializeMeshesBuffer(
            out _meshesVisibility,
            out _meshesScannedGroupSums,
            out _meshesScannedPredicates,
            out _meshesCulledMatricesRows01,
            out _meshesCulledMatricesRows23,
            out _meshesCulledMatricesRows45,
            out _meshesArguments);

        InitializeShadowsBuffer(
            out _shadowsVisibility,
            out _shadowsScannedGroupSums,
            out _shadowsScannedPredicates,
            out _shadowsCulledMatricesRows01,
            out _shadowsCulledMatricesRows23,
            out _shadowsCulledMatricesRows45,
            out _shadowsArguments);

        // InitializeLodArgsBuffers(
        //     out _lodArgs0,
        //     out _lodArgs1,
        //     out _lodArgs2);
    }

    public void SubmitCopingBuffers()
    {
        ComputeShader.SetInt(ShaderProperties.NumberOfDrawCalls, ArgumentsBuffer.ARGS_PER_INSTANCE_TYPE_COUNT);

        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.MatrixRows01, _matricesRows01);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.MatrixRows23, _matricesRows23);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.MatrixRows45, _matricesRows45);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.SortingData, _sortingData);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.BoundsData, _boundsData);
    }
    
    public void BindMaterialProperties(MeshProperties properties)
    {
        properties.Lod0PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4); //See if all 3 of them are required
        properties.Lod1PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 9);
        properties.Lod2PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 14);

        properties.ShadowLod0PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4);
        properties.ShadowLod1PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 9);
        properties.ShadowLod2PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 14);

        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _meshesArguments);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _meshesArguments);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _meshesArguments);

        // properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _lodArgs0);
        // properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _lodArgs1);
        // properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _lodArgs2);

        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _shadowsArguments);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _shadowsArguments);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _shadowsArguments);

        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _meshesCulledMatricesRows01);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _meshesCulledMatricesRows01);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _meshesCulledMatricesRows01);

        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _meshesCulledMatricesRows23);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _meshesCulledMatricesRows23);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _meshesCulledMatricesRows23);

        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _meshesCulledMatricesRows45);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _meshesCulledMatricesRows45);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _meshesCulledMatricesRows45);

        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _shadowsCulledMatricesRows01);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _shadowsCulledMatricesRows01);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _shadowsCulledMatricesRows01);

        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _shadowsCulledMatricesRows23);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _shadowsCulledMatricesRows23);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _shadowsCulledMatricesRows23);

        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _shadowsCulledMatricesRows45);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _shadowsCulledMatricesRows45);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _shadowsCulledMatricesRows45);
    }

    public InstancesDataCopier SubmitMeshesData()
    {
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.PredicatesInput, _meshesVisibility);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.GroupSums, _meshesScannedGroupSums);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.ScannedPredicates, _meshesScannedPredicates);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows01, _meshesCulledMatricesRows01);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows23, _meshesCulledMatricesRows23);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows45, _meshesCulledMatricesRows45);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.DrawCallsDataOutput, _meshesArguments);
        
        return this;
    }

    public InstancesDataCopier SubmitShadowsData()
    {
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.PredicatesInput, _shadowsVisibility);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.GroupSums, _shadowsScannedGroupSums);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.ScannedPredicates, _shadowsScannedPredicates);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows01, _shadowsCulledMatricesRows01);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows23, _shadowsCulledMatricesRows23);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows45, _shadowsCulledMatricesRows45);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.DrawCallsDataOutput, _shadowsArguments);

        return this;
    }

    public InstancesDataCopier SubmitArgumentsData()
    {
        ComputeShader.SetBuffer(_argumentsSplitterKernel, ShaderProperties.DrawCallsDataOutput, _meshesArguments);
        ComputeShader.SetBuffer(_argumentsSplitterKernel, ShaderProperties.LodArgs0, _lodArgs0);
        ComputeShader.SetBuffer(_argumentsSplitterKernel, ShaderProperties.LodArgs1, _lodArgs1);
        ComputeShader.SetBuffer(_argumentsSplitterKernel, ShaderProperties.LodArgs2, _lodArgs2);

        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_dataCopierKernel, _threadGroupX, 1, 1);
    public void DispatchArgumentsSplitter() => ComputeShader.Dispatch(_argumentsSplitterKernel, 1, 1, 1);

    private void InitializeCopingBuffers(out ComputeBuffer matricesRows01, 
        out ComputeBuffer matricesRows23, out ComputeBuffer matricesRows45,
        out ComputeBuffer sortingData, out ComputeBuffer boundsData)
    {
        matricesRows01 = Context.Transform.Matrix.Rows01;
        matricesRows23 = Context.Transform.Matrix.Rows23;
        matricesRows45 = Context.Transform.Matrix.Rows45;
        sortingData = Context.Sorting.Data;
        boundsData = Context.BoundsData;
    }

    private void InitializeMeshesBuffer(out ComputeBuffer meshesVisibility,
        out ComputeBuffer meshesScannedGroupSums, out ComputeBuffer meshesScannedPredicates,
        out ComputeBuffer meshesCulledMatricesRows01, out ComputeBuffer meshesCulledMatricesRows23,
        out ComputeBuffer meshesCulledMatricesRows45, out ComputeBuffer meshesArguments)
    {
        meshesVisibility = Context.Visibility.Meshes;
        meshesScannedGroupSums = Context.ScannedGroupSums.Meshes;
        meshesScannedPredicates = Context.ScannedPredicates.Meshes;
        meshesCulledMatricesRows01 = Context.Transform.CulledMatrix.Rows01;
        meshesCulledMatricesRows23 = Context.Transform.CulledMatrix.Rows23;
        meshesCulledMatricesRows45 = Context.Transform.CulledMatrix.Rows45;
        meshesArguments = Context.Arguments.Meshes;
    }
    
    private void InitializeShadowsBuffer(out ComputeBuffer shadowsVisibility,
        out ComputeBuffer shadowsScannedGroupSums, out ComputeBuffer shadowsScannedPredicates,
        out ComputeBuffer shadowsCulledMatricesRows01, out ComputeBuffer shadowsCulledMatricesRows23,
        out ComputeBuffer shadowsCulledMatricesRows45, out ComputeBuffer shadowsArguments)
    {
        shadowsVisibility = Context.Visibility.Shadows;
        shadowsScannedGroupSums = Context.ScannedGroupSums.Shadows;
        shadowsScannedPredicates = Context.ScannedPredicates.Shadows;
        shadowsCulledMatricesRows01 = Context.Transform.ShadowsCulledMatrix.Rows01;
        shadowsCulledMatricesRows23 = Context.Transform.ShadowsCulledMatrix.Rows23;
        shadowsCulledMatricesRows45 = Context.Transform.ShadowsCulledMatrix.Rows45;
        shadowsArguments = Context.Arguments.Shadows;
    }

    //TODO: Move to dedicated class
    // private void InitializeLodArgsBuffers(out ComputeBuffer lodArgs0, 
    //     out ComputeBuffer lodArgs1, out ComputeBuffer lodArgs2)
    // {
    //     lodArgs0 = Context.Arguments.LodArgs0;
    //     lodArgs1 = Context.Arguments.LodArgs1;
    //     lodArgs2 = Context.Arguments.LodArgs2;
    // }
}