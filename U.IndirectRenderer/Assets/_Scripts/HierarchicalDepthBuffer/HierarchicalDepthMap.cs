using System;
using UnityEngine;
using UnityEngine.Rendering;

public enum PowersOfTwo
{
    _1024 = 1024,
    _2048 = 2048,
    _4096 = 4096,
    _8192 = 8192,
    _16384 = 16384,
    _32768 = 32768,
    _65536 = 65536,
    _131072 = 131072,
    _262144 = 262144
}

[CreateAssetMenu(
    fileName = "HierarchicalDepthMap", 
    menuName = "Indirect Renderer/HierarchicalDepthMap")]
public class HierarchicalDepthMap : ScriptableObject
{
    [SerializeField] private PowersOfTwo _maximumResolution;
    [SerializeField] private Shader _shader;
    
    [field: SerializeField] public RenderTexture Texture { get; private set; }
    [field: SerializeField] public Vector2 Resolution { get; private set; }

    public Material Material { get; private set; }
    public int Size { get; private set; }

    private bool _initialized = false;
    private Action<(Material, RenderTexture, int)> _onInitializeCallback;
    public void OnInitialize(Action<(Material, RenderTexture, int)> callback) => _onInitializeCallback = callback;
    
    public void Initialize(int cameraWidth, int cameraHeight)
    {
        if (_initialized) return;
        
        Material = CreateMaterial();
        Size = CalculateTextureResolution(cameraWidth, cameraHeight);
        Texture = CreateRenderTexture();
        
        _onInitializeCallback?.Invoke((Material, Texture, Size));
        _initialized = true;
    }
    
    private Material CreateMaterial() => CoreUtils.CreateEngineMaterial(_shader);
    
    private int CalculateTextureResolution(int cameraWidth, int cameraHeight)
    {
        var size = Mathf.Max(cameraWidth, cameraHeight);
        size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), (int)_maximumResolution);
        
        Resolution = new Vector2
        {
            x = size,
            y = size
        };

        return size;
    }

    private RenderTexture CreateRenderTexture()
    {
        var texture = new RenderTexture(Size, Size, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        
        texture.filterMode = FilterMode.Point;
        texture.useMipMap = true;
        texture.autoGenerateMips = false;
        texture.Create();
        texture.hideFlags = HideFlags.HideAndDontSave;

        return texture;
    }
}