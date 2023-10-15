using UnityEngine;

public class PredicatesScanningDispatcher : ComputeShaderDispatcher
{
    private readonly int _kernel;
    private readonly int _threadGroupX;

    private readonly ComputeBuffer _visibility;
    private readonly ComputeBuffer _groupSums;
    private readonly ComputeBuffer _scannedPredicates;

    public PredicatesScanningDispatcher(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _kernel = GetKernel("CSMain");
        _threadGroupX = Mathf.Max(1, context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));
        InitializeScanningBuffers(out _visibility, out _groupSums, out _scannedPredicates);
    }

    public PredicatesScanningDispatcher SubmitMeshesData()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.PredicatesInput, _visibility);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.GroupSums, _groupSums);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.ScannedPredicates, _scannedPredicates);

        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);

    private void InitializeScanningBuffers(out ComputeBuffer meshesVisibility, 
        out ComputeBuffer meshesGroupSums, out ComputeBuffer meshesScannedPredicates)
    {
        meshesVisibility = Context.Visibility;
        meshesGroupSums = Context.GroupSums;
        meshesScannedPredicates = Context.ScannedPredicates;
    }
}