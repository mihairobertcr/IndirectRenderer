using UnityEngine;

public class GroupSumsScanningDispatcher : ComputeShaderDispatcher
{
    private static readonly int NumberOfGroups = Shader.PropertyToID("_NumberOfGroups");
    private static readonly int GroupSumsInput = Shader.PropertyToID("_GroupSumsInput");
    private static readonly int GroupSumsOutput = Shader.PropertyToID("_GroupSumsOutput");
    
    private readonly int _kernel;
    
    private readonly ComputeBuffer _groupSumsBuffer;
    private readonly ComputeBuffer _scannedGroupSumsBuffer;

    public GroupSumsScanningDispatcher(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _kernel = GetKernel("CSMain");
        InitializeGroupSumsBuffers(out _groupSumsBuffer, out _scannedGroupSumsBuffer);
    }

    public GroupSumsScanningDispatcher SubmitGroupCount()
    {
        ComputeShader.SetInt(NumberOfGroups, Context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));
        return this;
    }

    public GroupSumsScanningDispatcher SubmitGroupSumsData()
    {
        ComputeShader.SetBuffer(_kernel, GroupSumsInput, _groupSumsBuffer);
        ComputeShader.SetBuffer(_kernel, GroupSumsOutput, _scannedGroupSumsBuffer);
        
        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_kernel, 1, 1, 1);

    private void InitializeGroupSumsBuffers(out ComputeBuffer groupSums, out ComputeBuffer scannedGroupSums)
    {
        groupSums = Context.GroupSums;
        scannedGroupSums = Context.ScannedGroupSums;
    }
}
