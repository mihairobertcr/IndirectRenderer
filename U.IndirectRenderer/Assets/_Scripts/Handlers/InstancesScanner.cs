using UnityEngine;

public class InstancesScanner
{
    private const int SCAN_THREAD_GROUP_SIZE = 64; //TODO: Move to base class
    
    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    
    private readonly int _scanInstancesGroupX;

    private readonly RendererDataContext _context;

    public InstancesScanner(ComputeShader computeShader, int numberOfInstances, RendererDataContext context)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _context = context;
        
        _scanInstancesGroupX = Mathf.Max(1, numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups
    }

    public void Initialize()
    {
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.PredicatesInput,   _context.IsVisible);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.GroupSums,         _context.GroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.ScannedPredicates, _context.ScannedPredicates);
        
        _computeShader.Dispatch(ShaderKernels.InstancesScanner, _scanInstancesGroupX, 1, 1);
            
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.PredicatesInput,   _context.IsShadowVisible);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.GroupSums,         _context.ShadowsGroupSumsBuffer);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.ScannedPredicates, _context.ShadowsScannedPredicates);
        
        _computeShader.Dispatch(ShaderKernels.InstancesScanner, _scanInstancesGroupX, 1, 1);
    }
}
