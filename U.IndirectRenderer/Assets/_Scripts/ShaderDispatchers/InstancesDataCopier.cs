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
    private readonly GraphicsBuffer _meshesArguments;

    private readonly ComputeBuffer _shadowsVisibility;
    private readonly ComputeBuffer _shadowsScannedGroupSums;
    private readonly ComputeBuffer _shadowsScannedPredicates;
    private readonly ComputeBuffer _shadowsCulledMatricesRows01;
    private readonly ComputeBuffer _shadowsCulledMatricesRows23;
    private readonly ComputeBuffer _shadowsCulledMatricesRows45;
    private readonly GraphicsBuffer _shadowsArguments;

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

    public void BindMaterialProperties(InstanceProperties[] properties)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            // property.Lod0PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4); //See if all 3 of them are required
            // property.Lod1PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 9);
            // property.Lod2PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 14);
            //
            // property.ShadowLod0PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4);
            // property.ShadowLod1PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 9);
            // property.ShadowLod2PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 14);
            //
            // property.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _meshesArguments);
            // property.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _meshesArguments);
            // property.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _meshesArguments);
            //
            // property.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _shadowsArguments);
            // property.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _shadowsArguments);
            // property.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _shadowsArguments);
            //
            // property.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _meshesCulledMatricesRows01);
            // property.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _meshesCulledMatricesRows01);
            // property.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _meshesCulledMatricesRows01);
            //
            // property.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _meshesCulledMatricesRows23);
            // property.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _meshesCulledMatricesRows23);
            // property.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _meshesCulledMatricesRows23);
            //
            // property.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _meshesCulledMatricesRows45);
            // property.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _meshesCulledMatricesRows45);
            // property.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _meshesCulledMatricesRows45);
            //
            // property.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _shadowsCulledMatricesRows01);
            // property.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _shadowsCulledMatricesRows01);
            // property.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _shadowsCulledMatricesRows01);
            //
            // property.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _shadowsCulledMatricesRows23);
            // property.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _shadowsCulledMatricesRows23);
            // property.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _shadowsCulledMatricesRows23);
            //
            // property.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _shadowsCulledMatricesRows45);
            // property.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _shadowsCulledMatricesRows45);
            // property.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _shadowsCulledMatricesRows45);

            for (var k = 0; k < property.Lods.Count; k++)
            {
                var lod = property.Lods[k];
                var argsOffset = (i * 15) + (4 + (k * 5)); // 15 is the number of arguments for 3 lods

                lod.MeshPropertyBlock.SetInt(ShaderProperties.ArgsOffset, argsOffset);
                lod.MeshPropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _meshesArguments);
                lod.MeshPropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _meshesCulledMatricesRows01);
                lod.MeshPropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _meshesCulledMatricesRows23);
                lod.MeshPropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _meshesCulledMatricesRows45);

                lod.ShadowPropertyBlock.SetInt(ShaderProperties.ArgsOffset, argsOffset);
                lod.ShadowPropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _shadowsArguments);
                lod.ShadowPropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _shadowsCulledMatricesRows01);
                lod.ShadowPropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _shadowsCulledMatricesRows23);
                lod.ShadowPropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _shadowsCulledMatricesRows45);
            }
        }
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
        matricesRows01 = Context.Transforms.Matrices.Rows01;
        matricesRows23 = Context.Transforms.Matrices.Rows23;
        matricesRows45 = Context.Transforms.Matrices.Rows45;
        sortingData = Context.Sorting.Data;
        boundsData = Context.BoundingBoxes;
    }

    private void InitializeMeshesBuffer(out ComputeBuffer meshesVisibility,
        out ComputeBuffer meshesScannedGroupSums, out ComputeBuffer meshesScannedPredicates,
        out ComputeBuffer meshesCulledMatricesRows01, out ComputeBuffer meshesCulledMatricesRows23,
        out ComputeBuffer meshesCulledMatricesRows45, out GraphicsBuffer meshesArguments)
    {
        meshesVisibility = Context.Visibility.Meshes;
        meshesScannedGroupSums = Context.ScannedGroupSums.Meshes;
        meshesScannedPredicates = Context.ScannedPredicates.Meshes;
        meshesCulledMatricesRows01 = Context.Transforms.CulledMeshesMatrices.Rows01;
        meshesCulledMatricesRows23 = Context.Transforms.CulledMeshesMatrices.Rows23;
        meshesCulledMatricesRows45 = Context.Transforms.CulledMeshesMatrices.Rows45;
        meshesArguments = Context.Arguments.MeshesBuffer;
    }

    private void InitializeShadowsBuffer(out ComputeBuffer shadowsVisibility,
        out ComputeBuffer shadowsScannedGroupSums, out ComputeBuffer shadowsScannedPredicates,
        out ComputeBuffer shadowsCulledMatricesRows01, out ComputeBuffer shadowsCulledMatricesRows23,
        out ComputeBuffer shadowsCulledMatricesRows45, out GraphicsBuffer shadowsArguments)
    {
        shadowsVisibility = Context.Visibility.Shadows;
        shadowsScannedGroupSums = Context.ScannedGroupSums.Shadows;
        shadowsScannedPredicates = Context.ScannedPredicates.Shadows;
        shadowsCulledMatricesRows01 = Context.Transforms.CulledShadowsMatrices.Rows01;
        shadowsCulledMatricesRows23 = Context.Transforms.CulledShadowsMatrices.Rows23;
        shadowsCulledMatricesRows45 = Context.Transforms.CulledShadowsMatrices.Rows45;
        shadowsArguments = Context.Arguments.ShadowsBuffer;
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