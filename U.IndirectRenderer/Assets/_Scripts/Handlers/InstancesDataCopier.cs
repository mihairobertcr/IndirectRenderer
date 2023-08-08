using UnityEngine;

public class InstancesDataCopier
{
    private const int SCAN_THREAD_GROUP_SIZE = 64; //TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly int _copyInstanceDataGroupX;

    private readonly RendererDataContext _context;

    public InstancesDataCopier(ComputeShader computeShader, RendererDataContext context)
    {
        _computeShader = computeShader;
        _context = context;
        
        _copyInstanceDataGroupX = Mathf.Max(1, _context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups;
    }

    public void Initialize(MeshProperties properties, IndirectRendererConfig config)
    {
        InitializeMaterialProperties(properties);

        
        _computeShader.SetInt(ShaderProperties.NumberOfDrawCalls, ArgumentsBuffer.ARGS_PER_INSTANCE_TYPE_COUNT);
        
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.BoundsData,   _context.BoundsData);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows01, _context.Transform.Matrix.Rows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows23, _context.Transform.Matrix.Rows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows45, _context.Transform.Matrix.Rows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.SortingData,  _context.Sorting.Data);
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.PredicatesInput,     _context.Visibility.Meshes);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.GroupSums,           _context.ScannedGroupSums.Meshes);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.ScannedPredicates,   _context.ScannedPredicates.Meshes);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows01,  _context.Transform.CulledMatrix.Rows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows23,  _context.Transform.CulledMatrix.Rows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows45,  _context.Transform.CulledMatrix.Rows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.DrawCallsDataOutput, _context.Arguments.Meshes);

        _computeShader.Dispatch(ShaderKernels.DataCopier, _copyInstanceDataGroupX, 1, 1);
        
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.PredicatesInput,     _context.Visibility.Shadows);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.GroupSums,           _context.ScannedGroupSums.Shadows);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.ScannedPredicates,   _context.ScannedPredicates.Shadows);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows01,  _context.Transform.ShadowsCulledMatrix.Rows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows23,  _context.Transform.ShadowsCulledMatrix.Rows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows45,  _context.Transform.ShadowsCulledMatrix.Rows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.DrawCallsDataOutput, _context.Arguments.Shadows);
        
        _computeShader.Dispatch(ShaderKernels.DataCopier, _copyInstanceDataGroupX, 1, 1);
        
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.DrawCallsDataOutput, _context.Arguments.Meshes);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs0, _context.Arguments.LodArgs0);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs1, _context.Arguments.LodArgs1);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs2, _context.Arguments.LodArgs2);
        
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
        
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.Arguments.LodArgs0);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.Arguments.LodArgs1);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.Arguments.LodArgs2);
        
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.Arguments.Shadows);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.Arguments.Shadows);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.Arguments.Shadows);
        
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.Transform.CulledMatrix.Rows01);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.Transform.CulledMatrix.Rows01);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.Transform.CulledMatrix.Rows01);
            
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.Transform.CulledMatrix.Rows23);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.Transform.CulledMatrix.Rows23);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.Transform.CulledMatrix.Rows23);
            
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.Transform.CulledMatrix.Rows45);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.Transform.CulledMatrix.Rows45);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.Transform.CulledMatrix.Rows45);
        
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.Transform.ShadowsCulledMatrix.Rows01);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.Transform.ShadowsCulledMatrix.Rows01);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.Transform.ShadowsCulledMatrix.Rows01);
            
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.Transform.ShadowsCulledMatrix.Rows23);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.Transform.ShadowsCulledMatrix.Rows23);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.Transform.ShadowsCulledMatrix.Rows23);
            
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.Transform.ShadowsCulledMatrix.Rows45);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.Transform.ShadowsCulledMatrix.Rows45);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.Transform.ShadowsCulledMatrix.Rows45);
    }
}
