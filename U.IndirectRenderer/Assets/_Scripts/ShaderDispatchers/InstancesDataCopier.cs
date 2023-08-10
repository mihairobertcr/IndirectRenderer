using UnityEngine;

public class InstancesDataCopier
{
    private const int SCAN_THREAD_GROUP_SIZE = 64; //TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly RendererDataContext _context;
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
    {
        _computeShader = computeShader;
        _context = context;

        _threadGroupX = Mathf.Max(1, _context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups;

        _matricesRows01 = _context.Transform.Matrix.Rows01;
        _matricesRows23 = _context.Transform.Matrix.Rows23;
        _matricesRows45 = _context.Transform.Matrix.Rows45;
        _sortingData = _context.Sorting.Data;
        _boundsData = _context.BoundsData;

        _meshesVisibility = _context.Visibility.Meshes;
        _meshesScannedGroupSums = _context.ScannedGroupSums.Meshes;
        _meshesScannedPredicates = _context.ScannedPredicates.Meshes;
        _meshesCulledMatricesRows01 = _context.Transform.CulledMatrix.Rows01;
        _meshesCulledMatricesRows23 = _context.Transform.CulledMatrix.Rows23;
        _meshesCulledMatricesRows45 = _context.Transform.CulledMatrix.Rows45;
        _meshesArguments = _context.Arguments.Meshes;
        
        _shadowsVisibility = _context.Visibility.Shadows;
        _shadowsScannedGroupSums = _context.ScannedGroupSums.Shadows;
        _shadowsScannedPredicates = _context.ScannedPredicates.Shadows;
        _shadowsCulledMatricesRows01 = _context.Transform.ShadowsCulledMatrix.Rows01;
        _shadowsCulledMatricesRows23 = _context.Transform.ShadowsCulledMatrix.Rows23;
        _shadowsCulledMatricesRows45 = _context.Transform.ShadowsCulledMatrix.Rows45;
        _shadowsArguments = _context.Arguments.Shadows;

        _lodArgs0 = _context.Arguments.LodArgs0;
        _lodArgs1 = _context.Arguments.LodArgs1;
        _lodArgs2 = _context.Arguments.LodArgs2;
    }

    public void Initialize(MeshProperties properties)
    {
        InitializeMaterialProperties(properties);

        _computeShader.SetInt(ShaderProperties.NumberOfDrawCalls, ArgumentsBuffer.ARGS_PER_INSTANCE_TYPE_COUNT);

        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows01, _matricesRows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows23, _matricesRows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows45, _matricesRows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.SortingData, _sortingData);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.BoundsData, _boundsData);
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.PredicatesInput, _meshesVisibility);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.GroupSums, _meshesScannedGroupSums);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.ScannedPredicates, _meshesScannedPredicates);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows01, _meshesCulledMatricesRows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows23, _meshesCulledMatricesRows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows45, _meshesCulledMatricesRows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.DrawCallsDataOutput, _meshesArguments);

        _computeShader.Dispatch(ShaderKernels.DataCopier, _threadGroupX, 1, 1);

        // Shadows
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.PredicatesInput, _shadowsVisibility);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.GroupSums, _shadowsScannedGroupSums);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.ScannedPredicates, _shadowsScannedPredicates);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows01, _shadowsCulledMatricesRows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows23, _shadowsCulledMatricesRows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows45, _shadowsCulledMatricesRows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.DrawCallsDataOutput, _shadowsArguments);

        _computeShader.Dispatch(ShaderKernels.DataCopier, _threadGroupX, 1, 1);

        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.DrawCallsDataOutput, _meshesArguments);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs0, _lodArgs0);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs1, _lodArgs1);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs2, _lodArgs2);

        _computeShader.Dispatch(ShaderKernels.ArgumentsSplitter, 1, 1, 1);
    }

    private void InitializeMaterialProperties(MeshProperties properties)
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
}