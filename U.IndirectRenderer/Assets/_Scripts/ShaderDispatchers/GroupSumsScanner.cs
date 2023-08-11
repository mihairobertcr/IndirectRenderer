using UnityEngine;

public class GroupSumsScanner : ComputeShaderDispatcher
{
    private readonly int _kernel;
    
    private readonly ComputeBuffer _meshesGroupSums;
    private readonly ComputeBuffer _meshesScannedGroupSums;

    private readonly ComputeBuffer _shadowsGroupSums;
    private readonly ComputeBuffer _shadowsScannedGroupSums;

    public GroupSumsScanner(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _kernel = GetKernel("CSMain");
        
        InitializeGroupSumsBuffers(
            out _meshesGroupSums,
            out _meshesScannedGroupSums,
            out _shadowsGroupSums,
            out _shadowsScannedGroupSums);
    }

    public void SubmitGroupCount() => ComputeShader
        .SetInt(ShaderProperties.NumberOfGroups, Context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));

    public GroupSumsScanner SubmitMeshData()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsInput, _meshesGroupSums);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsOutput, _meshesScannedGroupSums);
        
        return this;
    }
    
    public GroupSumsScanner SubmitShadowsData()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsInput, _shadowsGroupSums);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsOutput, _shadowsScannedGroupSums);
        
        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_kernel, 1, 1, 1);

    private void InitializeGroupSumsBuffers(out ComputeBuffer meshesGroupSums, out ComputeBuffer meshesScannedGroupSums, 
        out ComputeBuffer shadowsGroupSums, out ComputeBuffer shadowsScannedGroupSums)
    {
        meshesGroupSums = Context.GroupSums.Meshes;
        meshesScannedGroupSums = Context.ScannedGroupSums.Meshes;

        shadowsGroupSums = Context.GroupSums.Shadows;
        shadowsScannedGroupSums = Context.ScannedGroupSums.Shadows;
    }
}
