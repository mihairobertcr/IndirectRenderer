using UnityEngine;

public class GroupSumsScanner
{
    private const int  SCAN_THREAD_GROUP_SIZE = 64; // TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    private readonly int _threadGroupsX;

    public GroupSumsScanner(ComputeShader computeShader, int numberOfInstances)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _threadGroupsX = 1;
    }

    public void Initialize()
    {
        ShaderBuffers.ScannedGroupSums        = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        ShaderBuffers.ShadowsScannedGroupSums = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        
        _computeShader.SetInt(ShaderProperties.NumberOfGroups, _numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE));
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsInput,  ShaderBuffers.GroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsOutput, ShaderBuffers.ScannedGroupSums);
        
        _computeShader.Dispatch(ShaderKernels.GroupSumsScanner, _threadGroupsX, 1, 1);
            
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsInput,  ShaderBuffers.ShadowsGroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsOutput, ShaderBuffers.ShadowsScannedGroupSums);
        
        _computeShader.Dispatch(ShaderKernels.GroupSumsScanner, _threadGroupsX, 1, 1);
    }
}
