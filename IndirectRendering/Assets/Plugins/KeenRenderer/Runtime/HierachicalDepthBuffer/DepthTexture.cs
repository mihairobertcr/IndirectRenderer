using System;
using UnityEngine;

public class DepthTexture : IDisposable
{
    public DepthMapComponent Component { get; }
    public int Size { get; }
    
    // public int LodCount => (int)Mathf.Floor(Mathf.Log(Size, 2f));
    
    private const RenderTextureFormat TEXTURE_FORMAT = RenderTextureFormat.RHalf;
    
    private readonly int _textureId;
    private readonly Camera _camera;
    private readonly Material _material;

    public DepthTexture(DepthMapComponent component, Camera camera)
    {
        Component = component;
        Size = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));
        
        _textureId = Shader.PropertyToID("_CameraDepthTexture");
        _material = new Material(component.Shader);

        _camera = camera;
        _camera.depthTextureMode |= DepthTextureMode.Depth;
        
        component.Texture = new RenderTexture(Size, Size, 0, TEXTURE_FORMAT);
        component.Texture.autoGenerateMips = false;
        component.Texture.useMipMap = true;
        component.Texture.filterMode = FilterMode.Point;
        component.Texture.Create();
    }

    public void UpdateTexture()
    {
        var width = Component.Texture.width;
        var mipLevel = 0;

        RenderTexture currentRenderTexture = null;
        RenderTexture preRenderTexture = null;

        while(width > 8) 
        {
            currentRenderTexture = RenderTexture.GetTemporary(width, width, 0, TEXTURE_FORMAT);
            currentRenderTexture.filterMode = FilterMode.Point;
            if(preRenderTexture == null) 
            {
                Graphics.Blit(Shader.GetGlobalTexture(_textureId), currentRenderTexture);
            }
            else 
            {
                Graphics.Blit(preRenderTexture, currentRenderTexture, _material);
                RenderTexture.ReleaseTemporary(preRenderTexture);
            }
            
            Graphics.CopyTexture(currentRenderTexture, 0, 0, Component.Texture, 0, mipLevel);
            preRenderTexture = currentRenderTexture;

            width /= 2;
            mipLevel++;
        }
        
        RenderTexture.ReleaseTemporary(preRenderTexture);
    }

    public void Dispose()
    {
        Component.Texture.Release();
        UnityEngine.Object.Destroy(Component.Texture);
    }
}
