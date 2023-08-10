using UnityEngine;

[CreateAssetMenu(
    fileName = "HierarchicalDepthMap", 
    menuName = "Indirect Renderer/HierarchicalDepthMap")]
public class HierarchicalDepthMap : ScriptableObject
{
    [SerializeField] private Shader _shader;
    public Shader Shader => _shader;

    [field: SerializeField] public RenderTexture Texture { get; set; }
    [field: SerializeField] public Vector2 TextureSize { get; set; }
}