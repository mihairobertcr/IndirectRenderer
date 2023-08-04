using UnityEngine;
using UnityEngine.Rendering;

public class RendererDataContext
{
    public ComputeBuffer Args;
    public ComputeBuffer ShadowsArgs;

    public ComputeBuffer MatrixRows01;
    public ComputeBuffer MatrixRows23;
    public ComputeBuffer MatrixRows45;
    
    public ComputeBuffer CulledMatrixRows01;
    public ComputeBuffer CulledMatrixRows23;
    public ComputeBuffer CulledMatrixRows45;
    
    public ComputeBuffer ShadowsCulledMatrixRows01;
    public ComputeBuffer ShadowsCulledMatrixRows23;
    public ComputeBuffer ShadowsCulledMatrixRows45;
    
    public ComputeBuffer SortingData;
    public ComputeBuffer SortingDataTemp;

    public ComputeBuffer IsVisible;
    public ComputeBuffer IsShadowVisible;
    public ComputeBuffer BoundsData;

    public ComputeBuffer GroupSumsBuffer;
    public ComputeBuffer ShadowsGroupSumsBuffer;
    
    public ComputeBuffer ScannedPredicates;
    public ComputeBuffer ShadowsScannedPredicates;
    
    public ComputeBuffer ScannedGroupSums;
    public ComputeBuffer ShadowsScannedGroupSums;
    
    public CommandBuffer SortingCommandBuffer;

    public ComputeBuffer LodArgs0;
    public ComputeBuffer LodArgs1;
    public ComputeBuffer LodArgs2;
    
    public void Dispose()
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
