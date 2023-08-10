using UnityEngine;

public class InstancesScanner : ComputeShaderDispatcher
{
    private readonly int _kernel;
    private readonly int _threadGroupX;

    private readonly ComputeBuffer _meshesVisibility;
    private readonly ComputeBuffer _meshesGroupSums;
    private readonly ComputeBuffer _meshesScannedPredicates;

    private readonly ComputeBuffer _shadowsVisibility;
    private readonly ComputeBuffer _shadowsGroupSums;
    private readonly ComputeBuffer _shadowsScannedPredicates;

    public InstancesScanner(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _kernel = GetKernel("CSMain");
        _threadGroupX = Mathf.Max(1, context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups

        _meshesVisibility = context.Visibility.Meshes;
        _meshesGroupSums = context.GroupSums.Meshes;
        _meshesScannedPredicates = context.ScannedPredicates.Meshes;

        _shadowsVisibility = context.Visibility.Shadows;
        _shadowsGroupSums = context.GroupSums.Shadows;
        _shadowsScannedPredicates = context.ScannedPredicates.Shadows;
    }

    public override void Dispatch()
    {
        // Normal
        ComputeShader.SetBuffer(_kernel, ShaderProperties.PredicatesInput, _meshesVisibility);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSums, _meshesGroupSums);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.ScannedPredicates, _meshesScannedPredicates);
        
        ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);

        // Shadows
        ComputeShader.SetBuffer(_kernel, ShaderProperties.PredicatesInput, _shadowsVisibility);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSums, _shadowsGroupSums);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.ScannedPredicates, _shadowsScannedPredicates);
        
        ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);
    }
}