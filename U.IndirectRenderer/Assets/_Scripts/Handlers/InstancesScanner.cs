using UnityEngine;

public class InstancesScanner
{
    private const int SCAN_THREAD_GROUP_SIZE = 64; //TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly RendererDataContext _context;
    private readonly int _threadGroupX;

    private readonly ComputeBuffer _meshesVisibility;
    private readonly ComputeBuffer _meshesGroupSums;
    private readonly ComputeBuffer _meshesScannedPredicates;

    private readonly ComputeBuffer _shadowsVisibility;
    private readonly ComputeBuffer _shadowsGroupSums;
    private readonly ComputeBuffer _shadowsScannedPredicates;

    public InstancesScanner(ComputeShader computeShader, RendererDataContext context)
    {
        _computeShader = computeShader;
        _context = context;

        _threadGroupX = Mathf.Max(1, _context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups

        _meshesVisibility = _context.Visibility.Meshes;
        _meshesGroupSums = _context.GroupSums.Meshes;
        _meshesScannedPredicates = _context.ScannedPredicates.Meshes;

        _shadowsVisibility = _context.Visibility.Shadows;
        _shadowsGroupSums = _context.GroupSums.Shadows;
        _shadowsScannedPredicates = _context.ScannedPredicates.Shadows;
    }

    public void Initialize()
    {
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.PredicatesInput, _meshesVisibility);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.GroupSums, _meshesGroupSums);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.ScannedPredicates, _meshesScannedPredicates);
        _computeShader.Dispatch(ShaderKernels.InstancesScanner, _threadGroupX, 1, 1);

        // Shadows
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.PredicatesInput, _shadowsVisibility);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.GroupSums, _shadowsGroupSums);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.ScannedPredicates, _shadowsScannedPredicates);
        _computeShader.Dispatch(ShaderKernels.InstancesScanner, _threadGroupX, 1, 1);
    }
}