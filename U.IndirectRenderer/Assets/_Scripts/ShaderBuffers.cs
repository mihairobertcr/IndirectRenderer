using UnityEngine;
using UnityEngine.Rendering;

public static class ShaderBuffers
{
    public static ComputeBuffer Args;
    public static ComputeBuffer ShadowsArgs;

    public static ComputeBuffer InstanceMatrixRows01;
    public static ComputeBuffer InstanceMatrixRows23;
    public static ComputeBuffer InstanceMatrixRows45;

    public static ComputeBuffer BoundsData;
    public static ComputeBuffer IsVisible;
    public static ComputeBuffer IsShadowVisible;

    public static ComputeBuffer SortingData;
    public static ComputeBuffer SortingDataTemp;
    
    public static CommandBuffer SortingCommandBuffer;
    
    public static void Dispose()
    {
        Args.Release();
        ShadowsArgs.Release();
        
        InstanceMatrixRows01.Release();
        InstanceMatrixRows23.Release();
        InstanceMatrixRows45.Release();
        
        BoundsData.Release();
        IsVisible.Release();
        IsShadowVisible.Release();

        SortingData.Release();
        SortingDataTemp.Release();
        
        SortingCommandBuffer.Release();
    }
}
