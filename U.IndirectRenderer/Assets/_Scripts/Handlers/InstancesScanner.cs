using UnityEngine;

public class InstancesScanner
{
    private const int SCAN_THREAD_GROUP_SIZE = 64; //TODO: Move to base class
    
    private readonly ComputeShader _computeShader;
    private readonly int _scanInstancesGroupX;

    private readonly RendererDataContext _context;

    public InstancesScanner(ComputeShader computeShader, RendererDataContext context)
    {
        _computeShader = computeShader;
        _context = context;
        
        _scanInstancesGroupX = Mathf.Max(1, _context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE)); //TODO: Extract common method for groups
    }

    public void Initialize()
    {
    }

    public void Dispatch()
    {
        // Normal
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.PredicatesInput,   _context.Visibility.Meshes);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.GroupSums,         _context.GroupSums.Meshes);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.ScannedPredicates, _context.ScannedPredicates.Meshes);
        
        _computeShader.Dispatch(ShaderKernels.InstancesScanner, _scanInstancesGroupX, 1, 1);
            
        // Shadows
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.PredicatesInput,   _context.Visibility.Shadows);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.GroupSums,         _context.GroupSums.Shadows);
        _computeShader.SetBuffer(ShaderKernels.InstancesScanner, ShaderProperties.ScannedPredicates, _context.ScannedPredicates.Shadows);
        
        _computeShader.Dispatch(ShaderKernels.InstancesScanner, _scanInstancesGroupX, 1, 1);
    }
}
