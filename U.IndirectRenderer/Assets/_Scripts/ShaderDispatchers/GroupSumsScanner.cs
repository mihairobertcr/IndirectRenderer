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
        
        _meshesGroupSums = context.GroupSums.Meshes;
        _meshesScannedGroupSums = context.ScannedGroupSums.Meshes;

        _shadowsGroupSums = context.GroupSums.Shadows;
        _shadowsScannedGroupSums = context.ScannedGroupSums.Shadows;
    }

    public void Initialize()
    {
        ComputeShader.SetInt(ShaderProperties.NumberOfGroups, Context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));
    }

    public override void Dispatch()
    {
        // Normal
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsInput, _meshesGroupSums);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsOutput, _meshesScannedGroupSums);
        
        ComputeShader.Dispatch(_kernel, 1, 1, 1);
            
        // Shadows
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsInput, _shadowsGroupSums);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSumsOutput, _shadowsScannedGroupSums);
        
        ComputeShader.Dispatch(_kernel, 1, 1, 1);
    }
}
