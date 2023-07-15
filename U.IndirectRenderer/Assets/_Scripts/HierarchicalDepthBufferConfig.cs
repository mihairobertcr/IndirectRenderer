using UnityEngine;

[CreateAssetMenu(
    fileName = "HierarchicalDepthBufferConfig", 
    menuName = "Indirect Renderer/HierarchicalDepthBufferConfig")]
public class HierarchicalDepthBufferConfig : ScriptableObject
{
    [SerializeField] private Shader _shader;
    public Shader Shader => _shader;

    [field: SerializeField] public RenderTexture Texture { get; set; }
    [field: SerializeField] public Vector2 TextureSize { get; set; }
}