using Keensight.Rendering.Data;
using UnityEngine;

public class DepthMapComponent : MonoBehaviour
{
    [field: SerializeField] public PowersOfTwo Resolution { get; set; }
    [field: SerializeField] public Shader Shader { get; set; }
    [field: SerializeField] public RenderTexture Texture { get; set; }
}