using UnityEngine;

public abstract class ComputeShaderDispatcher
{
    public abstract void Dispatch();

    protected const int SCAN_THREAD_GROUP_SIZE = 64;

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
