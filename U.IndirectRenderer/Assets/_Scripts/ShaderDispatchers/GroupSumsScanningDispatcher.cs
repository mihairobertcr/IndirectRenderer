using UnityEngine;

public class GroupSumsScanningDispatcher : ComputeShaderDispatcher
{
    private readonly int _kernel;
    
    private readonly ComputeBuffer _groupSumsBuffer;
    private readonly ComputeBuffer _scannedGroupSumsBuffer;

    public GroupSumsScanningDispatcher(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _kernel = GetKernel("CSMain");
        InitializeGroupSumsBuffers(out _groupSumsBuffer, out _scannedGroupSumsBuffer);
    }

    public void SubmitGroupCount() => ComputeShader
        .SetInt(ShaderProperties.NumberOfGroups, Context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));

    public GroupSumsScanningDispatcher SubmitGroupSumsData()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsInput, _groupSumsBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsOutput, _scannedGroupSumsBuffer);
        
        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_kernel, 1, 1, 1);

    private void InitializeGroupSumsBuffers(out ComputeBuffer groupSums, out ComputeBuffer scannedGroupSums)
    {
        groupSums = Context.GroupSums;
        scannedGroupSums = Context.ScannedGroupSums;
    }
}
