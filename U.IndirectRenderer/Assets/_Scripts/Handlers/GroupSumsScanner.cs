using UnityEngine;

public class GroupSumsScanner
{
    private const int  SCAN_THREAD_GROUP_SIZE = 64; // TODO: Move to base class

    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    private readonly int _threadGroupsX;

    private readonly RendererDataContext _context;

    public GroupSumsScanner(ComputeShader computeShader, int numberOfInstances, RendererDataContext context)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _threadGroupsX = 1;

        _context = context;
    }

    public void Initialize()
    {
        _context.ScannedGroupSums        = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        _context.ShadowsScannedGroupSums = new ComputeBuffer(_numberOfInstances, sizeof(uint), ComputeBufferType.Default);
        
        _computeShader.SetInt(ShaderProperties.NumberOfGroups, _numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE));
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsInput,  _context.GroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsOutput, _context.ScannedGroupSums);
        
        _computeShader.Dispatch(ShaderKernels.GroupSumsScanner, _threadGroupsX, 1, 1);
            
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsInput,  _context.ShadowsGroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.GroupSumsScanner, ShaderProperties.GroupSumsOutput, _context.ShadowsScannedGroupSums);
        
        _computeShader.Dispatch(ShaderKernels.GroupSumsScanner, _threadGroupsX, 1, 1);
    }
}
