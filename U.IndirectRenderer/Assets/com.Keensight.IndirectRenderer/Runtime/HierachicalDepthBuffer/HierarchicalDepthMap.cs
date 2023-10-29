using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(order = 2,
    menuName = "Indirect Renderer/HierarchicalDepthMap",
    fileName = "HierarchicalDepthMap")]
public class HierarchicalDepthMap : ScriptableObject
{
    public static RenderTexture Texture
    {
        get
        {
            if (s_Instance != null)
                return s_Instance._texture;
            
            Initialize();
            return s_Instance._texture;
        }
    }

    public static Material Material 
    {
        get
        {
            if (s_Instance != null)
                return s_Instance._material;
            
            Initialize();
            return s_Instance._material;
        }
    }
    
    public static Vector2 Resolution 
    {
        get
        {
            if (s_Instance != null)
                return s_Instance._resolution;
            
            Initialize();
            return s_Instance._resolution;
        }
    }
    
    public static int Size 
    {
        get
        {
            if (s_Instance != null)
                return s_Instance._size;
            
            Initialize();
            return s_Instance._size;
        }
    }
    
    public static int LodCount => (int)Mathf.Floor(Mathf.Log(Size, 2f));
    
    private static HierarchicalDepthMap s_Instance;

    private static void Initialize()
    {
        s_Instance = Resources.Load("HierarchicalDepthMap") as HierarchicalDepthMap;
        s_Instance.InitializeInternal();
    }

    [SerializeField] private PowersOfTwo _maximumResolution;
    [SerializeField] private Shader _shader;
    [SerializeField] private RenderTexture _texture;
    [SerializeField] private Vector2 _resolution;
    
    [Range(1.8f, 2f)]
    [SerializeField] private float _precision;

    private Material _material;
    private int _size;
    
    private void InitializeInternal()
    {
        _material = CreateMaterial();
        _size = CalculateTextureResolution();
        _texture = CreateRenderTexture();
    }

    private void OnDestroy() => _texture.Release();

    private Material CreateMaterial()
    {
        var material = CoreUtils.CreateEngineMaterial(_shader);
        material.SetFloat("_DepthMapPrecision", _precision);
        
        return material;
    }

    private int CalculateTextureResolution()
    {
        var size = Mathf.Max(Screen.width, Screen.height);
        size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), (int)_maximumResolution);
        
        _resolution = new Vector2
        {
            x = size,
            y = size
        };

        return size;
    }

    private RenderTexture CreateRenderTexture()
    {
        var texture = new RenderTexture(_size, _size, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        
        texture.filterMode = FilterMode.Point;
        texture.useMipMap = true;
        texture.autoGenerateMips = false;
        texture.Create();
        texture.hideFlags = HideFlags.HideAndDontSave;

        return texture;
    }
}