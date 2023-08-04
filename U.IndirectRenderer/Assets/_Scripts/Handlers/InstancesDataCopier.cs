using UnityEngine;

public class InstancesDataCopier
{
    private const int SCAN_THREAD_GROUP_SIZE = 64; //TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    private readonly int _copyInstanceDataGroupX;
    private readonly int _numberOfInstanceTypes;

    private readonly RendererDataContext _context;

    public InstancesDataCopier(ComputeShader computeShader, int numberOfInstances,
        RendererDataContext context,
        int numberOfInstanceTypes)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _context = context;
        
        _copyInstanceDataGroupX = Mathf.Max(1, numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups;
        _numberOfInstanceTypes = numberOfInstanceTypes;
    }

    public void Initialize(MeshProperties properties, IndirectRendererConfig config)
    {
        
        _context.CulledMatrixRows01 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        _context.CulledMatrixRows23 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        _context.CulledMatrixRows45 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        
        _context.ShadowsCulledMatrixRows01 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        _context.ShadowsCulledMatrixRows23 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        _context.ShadowsCulledMatrixRows45 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);

        var args0 = new uint[] { 0, 0, 0, 0, 0 };
        args0[0] = config.Lod0Mesh.GetIndexCount(0);
        args0[2] = config.Lod0Mesh.GetIndexStart(0);
        args0[3] = config.Lod0Mesh.GetBaseVertex(0);
        _context.LodArgs0 = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        _context.LodArgs0.SetData(args0);
        
        var args1 = new uint[] { 0, 0, 0, 0, 0 };
        args1[0] = config.Lod1Mesh.GetIndexCount(0);
        args1[2] = config.Lod1Mesh.GetIndexStart(0);
        args1[3] = config.Lod1Mesh.GetBaseVertex(0);
        _context.LodArgs1 = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        _context.LodArgs1.SetData(args1);
        
        var args2 = new uint[] { 0, 0, 0, 0, 0 };
        args2[0] = config.Lod2Mesh.GetIndexCount(0);
        args2[2] = config.Lod2Mesh.GetIndexStart(0);
        args2[3] = config.Lod2Mesh.GetBaseVertex(0);
        _context.LodArgs2 = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        _context.LodArgs2.SetData(args2);
        
        InitializeMaterialProperties(properties);

        
        _computeShader.SetInt(ShaderProperties.NumberOfDrawCalls, _numberOfInstanceTypes * IndirectRenderer.NUMBER_OF_ARGS_PER_INSTANCE_TYPE);
        
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.BoundsData,   _context.BoundsData);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows01, _context.MatrixRows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows23, _context.MatrixRows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows45, _context.MatrixRows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.SortingData,  _context.SortingData);
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.PredicatesInput,     _context.IsVisible);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.GroupSums,           _context.ScannedGroupSums);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.ScannedPredicates,   _context.ScannedPredicates);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows01,  _context.CulledMatrixRows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows23,  _context.CulledMatrixRows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows45,  _context.CulledMatrixRows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.DrawCallsDataOutput, _context.Args);

        _computeShader.Dispatch(ShaderKernels.DataCopier, _copyInstanceDataGroupX, 1, 1);
        
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.PredicatesInput,     _context.IsShadowVisible);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.GroupSums,           _context.ShadowsScannedGroupSums);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.ScannedPredicates,   _context.ShadowsScannedPredicates);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows01,  _context.ShadowsCulledMatrixRows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows23,  _context.ShadowsCulledMatrixRows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows45,  _context.ShadowsCulledMatrixRows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.DrawCallsDataOutput, _context.ShadowsArgs);
        
        _computeShader.Dispatch(ShaderKernels.DataCopier, _copyInstanceDataGroupX, 1, 1);
        
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.DrawCallsDataOutput, _context.Args);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs0, _context.LodArgs0);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs1, _context.LodArgs1);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs2, _context.LodArgs2);
        
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
        
        // properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, RendererDataContext.Args);
        // properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, RendererDataContext.Args);
        // properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, RendererDataContext.Args);
        
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.LodArgs0);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.LodArgs1);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.LodArgs2);
        
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.ShadowsArgs);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.ShadowsArgs);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, _context.ShadowsArgs);
        
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.CulledMatrixRows01);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.CulledMatrixRows01);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.CulledMatrixRows01);
            
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.CulledMatrixRows23);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.CulledMatrixRows23);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.CulledMatrixRows23);
            
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.CulledMatrixRows45);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.CulledMatrixRows45);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.CulledMatrixRows45);
        
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.ShadowsCulledMatrixRows01);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.ShadowsCulledMatrixRows01);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, _context.ShadowsCulledMatrixRows01);
            
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.ShadowsCulledMatrixRows23);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.ShadowsCulledMatrixRows23);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, _context.ShadowsCulledMatrixRows23);
            
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.ShadowsCulledMatrixRows45);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.ShadowsCulledMatrixRows45);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, _context.ShadowsCulledMatrixRows45);
    }
}
