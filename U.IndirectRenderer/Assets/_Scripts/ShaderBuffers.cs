using UnityEngine;
using UnityEngine.Rendering;

public static class ShaderBuffers
{
    public static ComputeBuffer Args;
    public static ComputeBuffer ShadowsArgs;

    public static ComputeBuffer MatrixRows01;
    public static ComputeBuffer MatrixRows23;
    public static ComputeBuffer MatrixRows45;
    
    public static ComputeBuffer CulledMatrixRows01;
    public static ComputeBuffer CulledMatrixRows23;
    public static ComputeBuffer CulledMatrixRows45;
    
    public static ComputeBuffer ShadowsCulledMatrixRows01;
    public static ComputeBuffer ShadowsCulledMatrixRows23;
    public static ComputeBuffer ShadowsCulledMatrixRows45;
    
    public static ComputeBuffer SortingData;
    public static ComputeBuffer SortingDataTemp;

    public static ComputeBuffer IsVisible;
    public static ComputeBuffer IsShadowVisible;
    public static ComputeBuffer BoundsData;

    public static ComputeBuffer GroupSumsBuffer;
    public static ComputeBuffer ShadowsGroupSumsBuffer;
    
    public static ComputeBuffer ScannedPredicates;
    public static ComputeBuffer ShadowsScannedPredicates;
    
    public static ComputeBuffer ScannedGroupSums;
    public static ComputeBuffer ShadowsScannedGroupSums;
    
    public static CommandBuffer SortingCommandBuffer;

    public static ComputeBuffer LodArgs0;
    public static ComputeBuffer LodArgs1;
    public static ComputeBuffer LodArgs2;
    
    public static void Dispose()
    {
        Args.Release();
        ShadowsArgs.Release();
        
        MatrixRows01.Release();
        MatrixRows23.Release();
        MatrixRows45.Release();
        
        CulledMatrixRows01.Release();
        CulledMatrixRows23.Release();
        CulledMatrixRows45.Release();
        
        ShadowsCulledMatrixRows01.Release();
        ShadowsCulledMatrixRows23.Release();
        ShadowsCulledMatrixRows45.Release();
        
        BoundsData.Release();
        IsVisible.Release();
        IsShadowVisible.Release();

        SortingData.Release();
        SortingDataTemp.Release();

        GroupSumsBuffer.Release();
        ShadowsGroupSumsBuffer.Release();
        
        ScannedPredicates.Release();
        ShadowsScannedPredicates.Release();
        
        ScannedGroupSums.Release();
        ShadowsScannedGroupSums.Release();
        
        SortingCommandBuffer.Release();
        
        LodArgs0.Release();
        LodArgs1.Release();
        LodArgs2.Release();
    }
}
