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
        _threadGroupX = Mathf.Max(1, context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups;

        _matricesRows01 = Context.Transform.Matrix.Rows01;
        _matricesRows23 = Context.Transform.Matrix.Rows23;
        _matricesRows45 = Context.Transform.Matrix.Rows45;
        _sortingData = Context.Sorting.Data;
        _boundsData = Context.BoundsData;

        _meshesVisibility = Context.Visibility.Meshes;
        _meshesScannedGroupSums = Context.ScannedGroupSums.Meshes;
        _meshesScannedPredicates = Context.ScannedPredicates.Meshes;
        _meshesCulledMatricesRows01 = Context.Transform.CulledMatrix.Rows01;
        _meshesCulledMatricesRows23 = Context.Transform.CulledMatrix.Rows23;
        _meshesCulledMatricesRows45 = Context.Transform.CulledMatrix.Rows45;
        _meshesArguments = Context.Arguments.Meshes;
        
        _shadowsVisibility = Context.Visibility.Shadows;
        _shadowsScannedGroupSums = Context.ScannedGroupSums.Shadows;
        _shadowsScannedPredicates = Context.ScannedPredicates.Shadows;
        _shadowsCulledMatricesRows01 = Context.Transform.ShadowsCulledMatrix.Rows01;
        _shadowsCulledMatricesRows23 = Context.Transform.ShadowsCulledMatrix.Rows23;
        _shadowsCulledMatricesRows45 = Context.Transform.ShadowsCulledMatrix.Rows45;
        _shadowsArguments = Context.Arguments.Shadows;

        _lodArgs0 = Context.Arguments.LodArgs0;
        _lodArgs1 = Context.Arguments.LodArgs1;
        _lodArgs2 = Context.Arguments.LodArgs2;
    }

    public void SetCopingBuffers()
    {
        ComputeShader.SetInt(ShaderProperties.NumberOfDrawCalls, ArgumentsBuffer.ARGS_PER_INSTANCE_TYPE_COUNT);

        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.MatrixRows01, _matricesRows01);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.MatrixRows23, _matricesRows23);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.MatrixRows45, _matricesRows45);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.SortingData, _sortingData);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.BoundsData, _boundsData);
    }
    
    public void InitializeMaterialProperties(MeshProperties properties)
    {
        properties.Lod0PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4); //See if all 3 of them are required
        properties.Lod1PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4);
        properties.Lod2PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4);

        properties.ShadowLod0PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4);
        properties.ShadowLod1PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 9);
        properties.ShadowLod2PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 14);

        // properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, RendererDataContext.Meshes);
        // properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, RendererDataContext.Meshes);
        // properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, RendererDataContext.Meshes);

        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _lodArgs0);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _lodArgs1);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _lodArgs2);

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

    public override void Dispatch()
    {
        // Normal
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.PredicatesInput, _meshesVisibility);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.GroupSums, _meshesScannedGroupSums);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.ScannedPredicates, _meshesScannedPredicates);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows01, _meshesCulledMatricesRows01);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows23, _meshesCulledMatricesRows23);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows45, _meshesCulledMatricesRows45);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.DrawCallsDataOutput, _meshesArguments);

        ComputeShader.Dispatch(_dataCopierKernel, _threadGroupX, 1, 1);

        // Shadows
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.PredicatesInput, _shadowsVisibility);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.GroupSums, _shadowsScannedGroupSums);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.ScannedPredicates, _shadowsScannedPredicates);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows01, _shadowsCulledMatricesRows01);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows23, _shadowsCulledMatricesRows23);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.CulledMatrixRows45, _shadowsCulledMatricesRows45);
        ComputeShader.SetBuffer(_dataCopierKernel, ShaderProperties.DrawCallsDataOutput, _shadowsArguments);

        ComputeShader.Dispatch(_dataCopierKernel, _threadGroupX, 1, 1);

        ComputeShader.SetBuffer(_argumentsSplitterKernel, ShaderProperties.DrawCallsDataOutput, _meshesArguments);
        ComputeShader.SetBuffer(_argumentsSplitterKernel, ShaderProperties.LodArgs0, _lodArgs0);
        ComputeShader.SetBuffer(_argumentsSplitterKernel, ShaderProperties.LodArgs1, _lodArgs1);
        ComputeShader.SetBuffer(_argumentsSplitterKernel, ShaderProperties.LodArgs2, _lodArgs2);

        ComputeShader.Dispatch(_argumentsSplitterKernel, 1, 1, 1);
    }
}