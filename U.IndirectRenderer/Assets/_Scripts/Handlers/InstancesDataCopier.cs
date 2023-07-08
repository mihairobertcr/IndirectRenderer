using UnityEngine;

public class InstancesDataCopier
{
    private const int SCAN_THREAD_GROUP_SIZE = 64; //TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    private readonly int _copyInstanceDataGroupX;
    private readonly int _numberOfInstanceTypes;

    public InstancesDataCopier(ComputeShader computeShader, int numberOfInstances, int numberOfInstanceTypes)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _copyInstanceDataGroupX = Mathf.Max(1, numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups;
        _numberOfInstanceTypes = numberOfInstanceTypes;
    }

    public void Initialize(MeshProperties properties, IndirectRendererConfig config)
    {
        
        ShaderBuffers.CulledMatrixRows01 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        ShaderBuffers.CulledMatrixRows23 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        ShaderBuffers.CulledMatrixRows45 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        
        ShaderBuffers.ShadowsCulledMatrixRows01 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        ShaderBuffers.ShadowsCulledMatrixRows23 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);
        ShaderBuffers.ShadowsCulledMatrixRows45 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.Default);

        var args0 = new uint[] { 0, 0, 0, 0, 0 };
        args0[0] = config.Lod0Mesh.GetIndexCount(0);
        args0[2] = config.Lod0Mesh.GetIndexStart(0);
        args0[3] = config.Lod0Mesh.GetBaseVertex(0);
        ShaderBuffers.LodArgs0 = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        ShaderBuffers.LodArgs0.SetData(args0);
        
        var args1 = new uint[] { 0, 0, 0, 0, 0 };
        args1[0] = config.Lod1Mesh.GetIndexCount(0);
        args1[2] = config.Lod1Mesh.GetIndexStart(0);
        args1[3] = config.Lod1Mesh.GetBaseVertex(0);
        ShaderBuffers.LodArgs1 = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        ShaderBuffers.LodArgs1.SetData(args1);
        
        var args2 = new uint[] { 0, 0, 0, 0, 0 };
        args2[0] = config.Lod2Mesh.GetIndexCount(0);
        args2[2] = config.Lod2Mesh.GetIndexStart(0);
        args2[3] = config.Lod2Mesh.GetBaseVertex(0);
        ShaderBuffers.LodArgs2 = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        ShaderBuffers.LodArgs2.SetData(args2);
        
        InitializeMaterialProperties(properties);

        
        _computeShader.SetInt(ShaderProperties.NumberOfDrawCalls, _numberOfInstanceTypes * IndirectRenderer.NUMBER_OF_ARGS_PER_INSTANCE_TYPE);
        
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.BoundsData,   ShaderBuffers.BoundsData);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows01, ShaderBuffers.MatrixRows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows23, ShaderBuffers.MatrixRows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.MatrixRows45, ShaderBuffers.MatrixRows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.SortingData,  ShaderBuffers.SortingData);
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.PredicatesInput,     ShaderBuffers.IsVisible);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.GroupSums,           ShaderBuffers.ScannedGroupSums);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.ScannedPredicates,   ShaderBuffers.ScannedPredicates);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows01,  ShaderBuffers.CulledMatrixRows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows23,  ShaderBuffers.CulledMatrixRows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows45,  ShaderBuffers.CulledMatrixRows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.DrawCallsDataOutput, ShaderBuffers.Args);

        _computeShader.Dispatch(ShaderKernels.DataCopier, _copyInstanceDataGroupX, 1, 1);
        
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.PredicatesInput,     ShaderBuffers.IsShadowVisible);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.GroupSums,           ShaderBuffers.ShadowsScannedGroupSums);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.ScannedPredicates,   ShaderBuffers.ShadowsScannedPredicates);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows01,  ShaderBuffers.ShadowsCulledMatrixRows01);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows23,  ShaderBuffers.ShadowsCulledMatrixRows23);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.CulledMatrixRows45,  ShaderBuffers.ShadowsCulledMatrixRows45);
        _computeShader.SetBuffer(ShaderKernels.DataCopier, ShaderProperties.DrawCallsDataOutput, ShaderBuffers.ShadowsArgs);
        
        _computeShader.Dispatch(ShaderKernels.DataCopier, _copyInstanceDataGroupX, 1, 1);
        
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.DrawCallsDataOutput, ShaderBuffers.Args);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs0, ShaderBuffers.LodArgs0);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs1, ShaderBuffers.LodArgs1);
        _computeShader.SetBuffer(ShaderKernels.ArgumentsSplitter, ShaderProperties.LodArgs2, ShaderBuffers.LodArgs2);
        
        _computeShader.Dispatch(ShaderKernels.ArgumentsSplitter, 1, 1, 1);
    }

    private static void InitializeMaterialProperties(MeshProperties properties)
    {
        properties.Lod0PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4); //See if all 3 of them are required
        properties.Lod1PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4);
        properties.Lod2PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4);
        
        properties.ShadowLod0PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 4);
        properties.ShadowLod1PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 9);
        properties.ShadowLod2PropertyBlock.SetInt(ShaderProperties.ArgsOffset, 14);
        
        // properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, ShaderBuffers.Args);
        // properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, ShaderBuffers.Args);
        // properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, ShaderBuffers.Args);
        
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, ShaderBuffers.LodArgs0);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, ShaderBuffers.LodArgs1);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, ShaderBuffers.LodArgs2);
        
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, ShaderBuffers.ShadowsArgs);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, ShaderBuffers.ShadowsArgs);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.ArgsBuffer, ShaderBuffers.ShadowsArgs);
        
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, ShaderBuffers.CulledMatrixRows01);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, ShaderBuffers.CulledMatrixRows01);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, ShaderBuffers.CulledMatrixRows01);
            
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, ShaderBuffers.CulledMatrixRows23);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, ShaderBuffers.CulledMatrixRows23);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, ShaderBuffers.CulledMatrixRows23);
            
        properties.Lod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, ShaderBuffers.CulledMatrixRows45);
        properties.Lod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, ShaderBuffers.CulledMatrixRows45);
        properties.Lod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, ShaderBuffers.CulledMatrixRows45);
        
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, ShaderBuffers.ShadowsCulledMatrixRows01);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, ShaderBuffers.ShadowsCulledMatrixRows01);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows01, ShaderBuffers.ShadowsCulledMatrixRows01);
            
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, ShaderBuffers.ShadowsCulledMatrixRows23);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, ShaderBuffers.ShadowsCulledMatrixRows23);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows23, ShaderBuffers.ShadowsCulledMatrixRows23);
            
        properties.ShadowLod0PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, ShaderBuffers.ShadowsCulledMatrixRows45);
        properties.ShadowLod1PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, ShaderBuffers.ShadowsCulledMatrixRows45);
        properties.ShadowLod2PropertyBlock.SetBuffer(ShaderProperties.MatrixRows45, ShaderBuffers.ShadowsCulledMatrixRows45);
    }
}
