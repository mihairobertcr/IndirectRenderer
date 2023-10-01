using System;
using UnityEngine;

[Serializable]
public class IndirectRendererSettings
{
    [Header("Execution")]
    public bool RunCompute = true;
    public bool DrawInstances = true;
    public bool DrawShadows = true;
    public bool ComputeAsync = true;
    
    [Space]
    [Header("Features")]
    public bool EnableFrustumCulling = true;
    public bool EnableOcclusionCulling = true;
    public bool EnableDetailCulling = true;
    public bool EnableLod = true;
    public bool EnableOnlyLod2Shadows = true;
    
    [Space]
    [Header("Details")]
    [Range(00.00f, 00.02f)] 
    public float DetailCullingPercentage = 0.005f;
    
    [Space]
    [Header("Debug")]
    public bool DrawBounds;
    public bool LogMatrices;
    public bool LogArgumentsAfterReset;
    public bool LogArgumentsAfterOcclusion;
    public bool LogVisibilityBuffer;
    public bool LogGroupSums;
    public bool LogScannedPredicates;
    public bool LogScannedGroupSums;
    public bool LogCulledMatrices;
    public bool LogArgumentsAfterCopy;
    public bool LogSortingData;
}