using System;
using UnityEngine;
using Keensight.Rendering.Data;

namespace Keensight.Rendering.Configs
{
    [CreateAssetMenu(order = 0,
        menuName = "Indirect Renderer/RendererConfig", 
        fileName = "RendererConfig")]
    public class RendererConfig : ScriptableObject
    {
        [Header("Rendering"), Space(5)]
        public PowersOfTwo NumberOfInstances;
        public int NumberOfLods;
        public bool SortLodsAsync;
        
        [Range(00.00f, 00.02f)] 
        public float DetailCullingPercentage = 0.005f;
        
        [Space, Header("Compute Shaders"), Space(5)]
        public ComputeShader MatricesInit;
        public ComputeShader LodSorting;
        public ComputeShader VisibilityCulling;
        public ComputeShader PredicatesScanning;
        public ComputeShader GroupSumsScanning;
        public ComputeShader DataCopying;
        
        [Space ,Header("Features"), Space(5)]
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
            public bool LogArgumentsAfterCulling;
            public bool LogVisibilityBuffer;
            public bool LogGroupSums;
            public bool LogScannedPredicates;
            public bool LogScannedGroupSums;
            public bool LogCulledMatrices;
            public bool LogArgumentsAfterCopy;
            public bool LogSortingData;
        }
        
        [Space(5)] 
        public DebuggerConfig Debugger;
        #endif
    }
}