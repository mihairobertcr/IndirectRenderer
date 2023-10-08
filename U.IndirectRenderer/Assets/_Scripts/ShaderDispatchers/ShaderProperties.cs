using UnityEngine;

public static class ShaderProperties
{
    public static readonly int ArgsBuffer = Shader.PropertyToID("_ArgsBuffer");
    public static readonly int ShadowArgsBuffer = Shader.PropertyToID("_ShadowArgsBuffer");
    public static readonly int ArgsOffset = Shader.PropertyToID("_ArgsOffset");

    public static readonly int LodArgs0 = Shader.PropertyToID("_LodArgs0");
    public static readonly int LodArgs1 = Shader.PropertyToID("_LodArgs1");
    public static readonly int LodArgs2 = Shader.PropertyToID("_LodArgs2");

    public static readonly int Positions = Shader.PropertyToID("_Positions");
    public static readonly int Rotations = Shader.PropertyToID("_Rotations");
    public static readonly int Scales = Shader.PropertyToID("_Scales");

    public static readonly int MatrixRows01 = Shader.PropertyToID("_MatrixRows01");
    public static readonly int MatrixRows23 = Shader.PropertyToID("_MatrixRows23");
    public static readonly int MatrixRows45 = Shader.PropertyToID("_MatrixRows45");

    public static readonly int CulledMatrixRows01 = Shader.PropertyToID("_CulledMatrixRows01");
    public static readonly int CulledMatrixRows23 = Shader.PropertyToID("_CulledMatrixRows23");
    public static readonly int CulledMatrixRows45 = Shader.PropertyToID("_CulledMatrixRows45");

    public static readonly int ShouldFrustumCull = Shader.PropertyToID("_ShouldFrustumCull");
    public static readonly int ShouldOcclusionCull = Shader.PropertyToID("_ShouldOcclusionCull");
    public static readonly int ShouldLod = Shader.PropertyToID("_ShouldLod");
    public static readonly int ShouldDetailCull = Shader.PropertyToID("_ShouldDetailCull");
    public static readonly int ShouldOnlyUseLod2Shadows = Shader.PropertyToID("_ShouldOnlyUseLod2Shadows");

    public static readonly int IsVisibleBuffer = Shader.PropertyToID("_IsVisibleBuffer");
    public static readonly int IsShadowVisibleBuffer = Shader.PropertyToID("_IsShadowVisibleBuffer");
    public static readonly int BoundsData = Shader.PropertyToID("_BoundsDataBuffer");
    public static readonly int SortingData = Shader.PropertyToID("_SortingData");

    public static readonly int ShadowDistance = Shader.PropertyToID("_ShadowDistance");
    public static readonly int DetailCullingScreenPercentage = Shader.PropertyToID("_DetailCullingScreenPercentage");
    public static readonly int HiZTextureSize = Shader.PropertyToID("_HiZTextureSize");
    public static readonly int HiZMap = Shader.PropertyToID("_HiZMap");
    public static readonly int MvpMatrix = Shader.PropertyToID("_MvpMatrix");
    public static readonly int CameraPosition = Shader.PropertyToID("_CameraPosition");
    
    public static readonly int LodsIntervals = Shader.PropertyToID("_LodsIntervals");
    public static readonly int LodsCount = Shader.PropertyToID("_LodsCount");

    public static readonly int PredicatesInput = Shader.PropertyToID("_PredicatesInput");
    public static readonly int GroupSums = Shader.PropertyToID("_GroupSums");
    public static readonly int ScannedPredicates = Shader.PropertyToID("_ScannedPredicates");

    public static readonly int NumberOfGroups = Shader.PropertyToID("_NumberOfGroups");
    public static readonly int GroupSumsInput = Shader.PropertyToID("_GroupSumsInput");
    public static readonly int GroupSumsOutput = Shader.PropertyToID("_GroupSumsOutput");

    public static readonly int NumberOfDrawCalls = Shader.PropertyToID("_NumberOfDrawCalls");
    public static readonly int DrawCallsDataOutput = Shader.PropertyToID("_DrawCallsDataOutput");

    public static readonly int Data = Shader.PropertyToID("_Data");
    public static readonly int Input = Shader.PropertyToID("_Input");

    public static readonly int Level = Shader.PropertyToID("_Level");
    public static readonly int LevelMask = Shader.PropertyToID("_LevelMask");
    public static readonly int Width = Shader.PropertyToID("_Width");
    public static readonly int Height = Shader.PropertyToID("_Height");
}