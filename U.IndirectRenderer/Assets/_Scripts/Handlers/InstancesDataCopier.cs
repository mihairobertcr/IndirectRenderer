using UnityEngine;

public class InstancesDataCopier
{
    private const int SCAN_THREAD_GROUP_SIZE = 64; //TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly int _copyInstanceDataGroupX;
    private readonly int _numberOfInstanceTypes;

    public InstancesDataCopier(ComputeShader computeShader, int numberOfInstances, int numberOfInstanceTypes)
    {
        _computeShader = computeShader;
        _copyInstanceDataGroupX = Mathf.Max(1, numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups;
        _numberOfInstanceTypes = numberOfInstanceTypes;
    }

    public void Initialize(MeshProperties properties)
    {
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
    }

    private static void InitializeMaterialProperties(MeshProperties properties)
    {
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
