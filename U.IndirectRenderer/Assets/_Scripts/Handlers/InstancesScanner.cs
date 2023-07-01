using UnityEngine;

public class InstancesScanner
{
    private const int SCAN_THREAD_GROUP_SIZE = 64;
    
    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    
    private readonly int _scanInstancesGroupX;

    public InstancesScanner(ComputeShader computeShader, int numberOfInstances)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _scanInstancesGroupX = Mathf.Max(1, numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE));
    }

    public void Initialize()
    {
        ShaderBuffers.GroupSumsBuffer          = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        ShaderBuffers.ShadowsGroupSumsBuffer   = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        ShaderBuffers.ScannedPredicates        = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        ShaderBuffers.ShadowsScannedPredicates = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.PredicatesInput,   ShaderBuffers.IsVisible);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.GroupSums,         ShaderBuffers.GroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.ScannedPredicates, ShaderBuffers.ScannedPredicates);
        
        _computeShader.Dispatch(ShaderKernels.InstancesScanner, _scanInstancesGroupX, 1, 1);
            
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.PredicatesInput,   ShaderBuffers.IsShadowVisible);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.GroupSums,         ShaderBuffers.ShadowsGroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.ScannedPredicates, ShaderBuffers.ShadowsScannedPredicates);
        
        _computeShader.Dispatch(ShaderKernels.InstancesScanner, _scanInstancesGroupX, 1, 1);
    }
}
