using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class HiZBufferConfig
{
    public RenderTexture TopDownView;
    public Shader GenerateBufferShader;
    public Shader DebugShader; // ???
}

public class HiZBuffer
{
    private enum Pass
    {
        Blit,
        Reduce
    }
    
    private const int MAXIMUM_BUFFER_SIZE = 1024;

    public RenderTexture Texture { get; private set; }
    public Vector2 TextureSize { get; }

    // private readonly HiZBufferConfig _config;
    // private readonly HiZBufferConfig _config;
    private readonly Camera _renderCamera;
    private readonly Camera _debugCamera;
    private readonly Material _generateBufferMaterial;
    private readonly Material _debugMaterial;
    
    private int _lodCount = 0;
    private int[] _temporaries = null;
    private CameraEvent _cameraEvent = CameraEvent.AfterReflections;
    private CommandBuffer _commandBuffer = null;
    // private RenderTexture _shadowmapCopy;

    public HiZBuffer(HiZBufferConfig config, Camera renderCamera, Camera debugCamera = null)
    {
        _renderCamera = renderCamera;
        _debugCamera = debugCamera;
        
        _generateBufferMaterial = new Material(config.GenerateBufferShader);
        _debugMaterial = new Material(config.DebugShader);
        _renderCamera.depthTextureMode = DepthTextureMode.Depth;

        TextureSize = CalculateTextureSize(out var size);
        _lodCount = CalculateLoadCount(size);
        
        InitializeTexture(size);
        InitializeCommandBuffer(size);
    }
    
    // TODO: Implement debug
    // TODO: See if shadowmap need to be implementted
    // TODO: Dispose Texture and Remove Command Buffer
    // if (_commandBuffer != null)
    // {
    //     _config.Camera.RemoveCommandBuffer(_cameraEvent, _commandBuffer);
    // }

    private void InitializeTexture(int size)
    {
        Texture = new RenderTexture(size, size, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        Texture.filterMode = FilterMode.Point;
        Texture.useMipMap = true;
        Texture.autoGenerateMips = false;
        Texture.Create();
        Texture.hideFlags = HideFlags.HideAndDontSave;
    }

    private void InitializeCommandBuffer(int size)
    {
        _temporaries = new int[_lodCount];
        _commandBuffer = new CommandBuffer();
        _commandBuffer.name = "Hi-Z Buffer";
            
        var id = new RenderTargetIdentifier(Texture);
        // _commandBuffer.SetGlobalTexture("_LightTexture", _shadowmapCopy);
        _commandBuffer.Blit(null, id, _generateBufferMaterial, (int)Pass.Blit);
            
        for (var i = 0; i < _lodCount; ++i)
        {
            _temporaries[i] = Shader.PropertyToID($"_09659d57_Temporaries{i}");
            
            size >>= 1;
            size = Mathf.Max(size, 1);
            
            _commandBuffer.GetTemporaryRT(_temporaries[i], size, size, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
            if (i == 0)
            {
                _commandBuffer.Blit(id, _temporaries[0], _generateBufferMaterial, (int)Pass.Reduce);
            }
            else
            {
                _commandBuffer.Blit(_temporaries[i - 1], _temporaries[i], _generateBufferMaterial, (int)Pass.Reduce);
            }
                
            _commandBuffer.CopyTexture(_temporaries[i], 0, 0, id, 0, i + 1);
            if (i >= 1)
            {
                _commandBuffer.ReleaseTemporaryRT(_temporaries[i - 1]);
            }
        }
            
        _commandBuffer.ReleaseTemporaryRT(_temporaries[_lodCount - 1]);
        _renderCamera.AddCommandBuffer(_cameraEvent, _commandBuffer);
    }

    private Vector2 CalculateTextureSize(out int size)
    {
        size = Mathf.Max(_renderCamera.pixelWidth, _renderCamera.pixelHeight);
        size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), MAXIMUM_BUFFER_SIZE);
        
        return new Vector2
        {
            x = size,
            y = size
        };
    }

    private int CalculateLoadCount(int size) => (int)Mathf.Floor(Mathf.Log(size, 2f));
}
