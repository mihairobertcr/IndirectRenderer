using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(
    fileName = "HierarchicalDepthMap", 
    menuName = "Indirect Renderer/HierarchicalDepthMap")]
public class HierarchicalDepthMap : ScriptableObject
{
    public static HierarchicalDepthMap Instance { get; private set; }
    private static bool s_Initialized = false;
    
    private static Action<(Material material, RenderTexture texture, int size, int lods)> s_OnInitializeCallback;
    public static void OnInitialize(Action<(Material material, RenderTexture texture, int size, int lods)> callback) => 
        s_OnInitializeCallback = callback;

    public static void Initialize()
    {
        if (!s_Initialized)
        {
            Instance = Resources.Load("HierarchicalDepthMap") as HierarchicalDepthMap;
            Instance.InitializeInternal();
        
            s_Initialized = true;
        }

        s_OnInitializeCallback?.Invoke((Instance.Material, Instance.Texture, Instance.Size, Instance.LodCount));
    }
    
    [SerializeField] private PowersOfTwo _maximumResolution;
    [SerializeField] private Shader _shader;
    
    [field: SerializeField] public RenderTexture Texture { get; private set; }
    [field: SerializeField] public Vector2 Resolution { get; private set; }

    public Material Material { get; private set; }
    public int Size { get; private set; }
    
    public int LodCount => (int)Mathf.Floor(Mathf.Log(Size, 2f));

    private void InitializeInternal()
    {
        Material = CreateMaterial();
        Size = CalculateTextureResolution();
        Texture = CreateRenderTexture();
    }

    private Material CreateMaterial() => CoreUtils.CreateEngineMaterial(_shader);
    
    private int CalculateTextureResolution()
    {
        var size = Mathf.Max(Screen.width, Screen.height);
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