using UnityEngine;

public static class ShaderProperties
{
    public static readonly int Positions = Shader.PropertyToID("_Positions");
    public static readonly int Rotations = Shader.PropertyToID("_Rotations");
    public static readonly int Scales    = Shader.PropertyToID("_Scales");
    
    public static readonly int InstanceMatrixRows01 = Shader.PropertyToID("_InstanceMatrixRows01");
    public static readonly int InstanceMatrixRows23 = Shader.PropertyToID("_InstanceMatrixRows23");
    public static readonly int InstanceMatrixRows45 = Shader.PropertyToID("_InstanceMatrixRows45");
    
    public static readonly int Data  = Shader.PropertyToID("_Data");
    public static readonly int Input = Shader.PropertyToID("_Input");
    
    public static readonly int Level     = Shader.PropertyToID("_Level");
    public static readonly int LevelMask = Shader.PropertyToID("_LevelMask");
    public static readonly int Width     = Shader.PropertyToID("_Width");
    public static readonly int Height    = Shader.PropertyToID("_Height");
}
