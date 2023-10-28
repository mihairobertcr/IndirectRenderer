using System;
using UnityEngine;

[CreateAssetMenu(order = 0,
    menuName = "Indirect Renderer/RendererConfig", 
    fileName = "RendererConfig")]
public class RendererConfig : ScriptableObject
{
    [Header("Rendering")]
    public PowersOfTwo NumberOfInstances;
    public int NumberOfLods;
    public bool SortLodsAsync;
    
    [Range(00.00f, 00.02f)] 
    public float DetailCullingPercentage = 0.005f;
    
    [Space]
    [Header("Compute Shaders")]
    public ComputeShader MatricesInit;
    public ComputeShader LodSorting;
    public ComputeShader VisibilityCulling;
    public ComputeShader PredicatesScanning;
    public ComputeShader GroupSumsScanning;
    public ComputeShader DataCopying;
    
    [Space]
    [Header("Features")]
    public bool EnableLod = true;
    public bool EnableFrustumCulling = true;
    public bool EnableOcclusionCulling = true;
    public bool EnableDetailCulling = true;
    
    #if UNITY_EDITOR
    [Serializable]
    public class DebuggerConfig
    {
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
    
    [Space] 
    public DebuggerConfig Debugger;
    #endif
}