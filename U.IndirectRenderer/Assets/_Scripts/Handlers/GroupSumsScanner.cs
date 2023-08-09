using UnityEngine;

public class GroupSumsScanner
{
    private const int  SCAN_THREAD_GROUP_SIZE = 64; // TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly RendererDataContext _context;

    private readonly ComputeBuffer _meshesGroupSums;
    private readonly ComputeBuffer _meshesScannedGroupSums;

    private readonly ComputeBuffer _shadowsGroupSums;
    private readonly ComputeBuffer _shadowsScannedGroupSums;

    public GroupSumsScanner(ComputeShader computeShader, RendererDataContext context)
    {
        _computeShader = computeShader;
        _context = context;

        _meshesGroupSums = _context.GroupSums.Meshes;
        _meshesScannedGroupSums = _context.ScannedGroupSums.Meshes;

        _shadowsGroupSums = _context.GroupSums.Shadows;
        _shadowsScannedGroupSums = _context.ScannedGroupSums.Shadows;
    }

    public void Initialize()
    {
        _computeShader.SetInt(ShaderProperties.NumberOfGroups, _context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsInput, _meshesGroupSums);
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsOutput, _meshesScannedGroupSums);
        
        _computeShader.Dispatch(ShaderKernels.GroupSumsScanner, 1, 1, 1);
            
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsInput, _shadowsGroupSums);
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsOutput, _shadowsScannedGroupSums);
        
        _computeShader.Dispatch(ShaderKernels.GroupSumsScanner, 1, 1, 1);
    }
}
