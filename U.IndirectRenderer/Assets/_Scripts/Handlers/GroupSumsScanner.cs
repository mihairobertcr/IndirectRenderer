using UnityEngine;

public class GroupSumsScanner
{
    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    private readonly int _scanThreadGroupsGroupX;

    public GroupSumsScanner(ComputeShader computeShader, int numberOfInstances)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _scanThreadGroupsGroupX = 1;
    }

    public void Initialize()
    {
        ShaderBuffers.ScannedGroupSums        = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        ShaderBuffers.ShadowsScannedGroupSums = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsInput,  ShaderBuffers.GroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsOutput, ShaderBuffers.ScannedGroupSums);
        
        _computeShader.Dispatch(ShaderKernels.GroupSumsScanner, _scanThreadGroupsGroupX, 1, 1);
            
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsInput,  ShaderBuffers.ShadowsGroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsOutput, ShaderBuffers.ShadowsScannedGroupSums);
        
        _computeShader.Dispatch(ShaderKernels.GroupSumsScanner, _scanThreadGroupsGroupX, 1, 1);
    }
}
