using UnityEngine;

public abstract class ComputeShaderDispatcher
{
    public abstract void Dispatch();
    
    protected const int SCAN_THREAD_GROUP_SIZE = 64;

    protected static readonly int ArgsBuffer = Shader.PropertyToID("_ArgsBuffer");

    protected static readonly int MatrixRows01 = Shader.PropertyToID("_MatrixRows01");
    protected static readonly int MatrixRows23 = Shader.PropertyToID("_MatrixRows23");
    protected static readonly int MatrixRows45 = Shader.PropertyToID("_MatrixRows45");

    protected static readonly int BoundsData = Shader.PropertyToID("_BoundsDataBuffer");
    protected static readonly int SortingData = Shader.PropertyToID("_SortingData");

    protected static readonly int PredicatesInput = Shader.PropertyToID("_PredicatesInput");
    protected static readonly int GroupSums = Shader.PropertyToID("_GroupSums");
    protected static readonly int ScannedPredicates = Shader.PropertyToID("_ScannedPredicates");
    
    protected readonly ComputeShader ComputeShader;
    protected readonly RendererDataContext Context;

    protected ComputeShaderDispatcher(ComputeShader computeShader, RendererDataContext context)
    {
        ComputeShader = computeShader;
        Context = context;
    }

    protected int GetKernel(string kernelName)
    {
        var kernelId = default(int);
        if (!ComputeShader.HasKernel(kernelName))
        {
            Debug.LogError($"{kernelName} kernel not found in {ComputeShader.name}!");
            return kernelId;
        }

        kernelId = ComputeShader.FindKernel(kernelName);
        return kernelId;
    }
}
