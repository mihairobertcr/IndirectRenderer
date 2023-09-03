using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[CreateAssetMenu(
    fileName = "HierarchicalDepthMap", 
    menuName = "Indirect Renderer/HierarchicalDepthMap")]
public class HierarchicalDepthMap : ScriptableObject
{
    public static HierarchicalDepthMap Instance { get; private set; }
    private static bool s_Initialized = false;
    
    private static Action<(RTHandle texture, RTHandle empty, RTHandle[] temporaries, Material material, int size, int lods)> s_OnInitializeCallback;
    public static void OnInitialize(Action<(RTHandle texture, RTHandle empty, RTHandle[] temporaries, Material material, int size, int lods)> callback) => 
        s_OnInitializeCallback = callback;

    public static void Initialize(int cameraWidth, int cameraHeight,
        Action<(RTHandle texture, RTHandle empty, RTHandle[] temporaries, Material material, int size, int lods)> callback)
    {
        if (!s_Initialized)
        {
            Instance = Resources.Load("HierarchicalDepthMap") as HierarchicalDepthMap;
            Instance.InitializeInternal(cameraWidth, cameraHeight);

            callback?.Invoke((
                Instance.Texture,
                Instance.EmptyTexture,
                Instance.Temporaries,
                Instance.Material, 
                Instance.Size, 
                Instance.LodCount));
            
            s_Initialized = true;
        }
    }
    
    [SerializeField] private PowersOfTwo _maximumResolution;
    [SerializeField] private Shader _shader;
    
    [field: SerializeField] public RenderTexture RenderTexture { get; private set; }
    [field: SerializeField] public Vector2 Resolution { get; private set; }

    public RTHandle Texture { get; private set; }
    public RTHandle EmptyTexture { get; private set; }
    public RTHandle[] Temporaries { get; private set; }
    public Material Material { get; private set; }
    public int Size { get; private set; }
    
    public int LodCount => (int)Mathf.Floor(Mathf.Log(Size, 2f));

    public void Dispose()
    {
        Texture?.Release();
        EmptyTexture?.Release();
        foreach (var temporary in Temporaries)
        {
            temporary?.Release();
        }
    }

    private void InitializeInternal(int cameraWidth, int cameraHeight)
    {
        Material = CreateMaterial();
        Size = CalculateTextureResolution(cameraWidth, cameraHeight);
        RenderTexture = CreateRenderTexture(); 
        Texture = RTHandles.Alloc(RenderTexture); //TODO: Remove RenderTexture and change it to a debug
        EmptyTexture = RTHandles.Alloc(Size, Size, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureDimension.Tex2D);
        Temporaries = CreateTemporaries();
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

    private RTHandle[] CreateTemporaries()
    {
        var temporaries = new RTHandle[LodCount];
        var size = Size;
        for (var i = 0; i < LodCount; ++i)
        {
            //TODO: Extract method
            size >>= 1;
            size = Mathf.Max(size, 1);
            temporaries[i] = RTHandles.Alloc(size, size, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureDimension.Tex2D);
        }

        return temporaries;
    }
}