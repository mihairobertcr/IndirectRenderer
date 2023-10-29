using UnityEngine;

public abstract class ComputeShaderDispatcher
{
    protected const int SCAN_THREAD_GROUP_SIZE = 64;

    protected static readonly int ArgsBufferId = Shader.PropertyToID("_ArgsBuffer");

    protected static readonly int MatrixRows01Id = Shader.PropertyToID("_MatrixRows01");
    protected static readonly int MatrixRows23Id = Shader.PropertyToID("_MatrixRows23");
    protected static readonly int MatrixRows45Id = Shader.PropertyToID("_MatrixRows45");

    protected static readonly int BoundsDataId = Shader.PropertyToID("_BoundsData");
    protected static readonly int SortingDataId = Shader.PropertyToID("_SortingData");

    protected static readonly int PredicatesInputId = Shader.PropertyToID("_PredicatesInput");
    protected static readonly int GroupSumsId = Shader.PropertyToID("_GroupSums");
    protected static readonly int ScannedPredicatesId = Shader.PropertyToID("_ScannedPredicates");
    
    protected readonly ComputeShader ComputeShader;
    protected readonly RendererContext Context;
    
    public abstract ComputeShaderDispatcher Initialize();
    public abstract void Dispatch();

    public virtual ComputeShaderDispatcher Update() => this;

    protected ComputeShaderDispatcher(ComputeShader computeShader, RendererContext context)
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
