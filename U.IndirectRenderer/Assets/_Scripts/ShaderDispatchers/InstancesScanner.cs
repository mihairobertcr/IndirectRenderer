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
        _threadGroupX = Mathf.Max(1, context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));
        
        InitializeScanningBuffers(
            out _meshesVisibility,
            out _meshesGroupSums,
            out _meshesScannedPredicates,
            out _shadowsVisibility,
            out _shadowsGroupSums,
            out _shadowsScannedPredicates);
    }

    public InstancesScanner SubmitMeshesData()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.PredicatesInput, _meshesVisibility);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSums, _meshesGroupSums);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.ScannedPredicates, _meshesScannedPredicates);

        return this;
    }

    public InstancesScanner SubmitShadowsData()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.PredicatesInput, _shadowsVisibility);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSums, _shadowsGroupSums);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.ScannedPredicates, _shadowsScannedPredicates);
        
        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);

    private void InitializeScanningBuffers(out ComputeBuffer meshesVisibility, out ComputeBuffer meshesGroupSums, 
        out ComputeBuffer meshesScannedPredicates, out ComputeBuffer shadowsVisibility, 
        out ComputeBuffer shadowsGroupSums, out ComputeBuffer shadowsScannedPredicates)
    {
        meshesVisibility = Context.Visibility.Meshes;
        meshesGroupSums = Context.GroupSums.Meshes;
        meshesScannedPredicates = Context.ScannedPredicates.Meshes;

        shadowsVisibility = Context.Visibility.Shadows;
        shadowsGroupSums = Context.GroupSums.Shadows;
        shadowsScannedPredicates = Context.ScannedPredicates.Shadows;
    }
}