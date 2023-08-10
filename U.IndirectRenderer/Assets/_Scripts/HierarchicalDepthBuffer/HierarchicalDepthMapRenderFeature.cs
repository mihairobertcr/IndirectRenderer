using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HierarchicalDepthMapRenderFeature : ScriptableRendererFeature
{
    private class RenderPass : ScriptableRenderPass
    {
        private enum Pass
        {
            Blit,
            Reduce
        }
        
        private const int MAXIMUM_BUFFER_SIZE = 1024;

        private readonly HierarchicalDepthMap _config;
        private readonly Material _hizMaterial;
        
        private int _cameraHeight;
        private int _cameraWidth;
        private int _lodCount;
        private int[] _temporaries;
        private int _size;

        public RenderPass(HierarchicalDepthMap config) : base()
        {
            _config = config;
            _hizMaterial = CoreUtils.CreateEngineMaterial(_config.Shader);
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            _cameraHeight = renderingData.cameraData.camera.pixelHeight;
            _cameraWidth = renderingData.cameraData.camera.pixelWidth;
            
            _config.TextureSize = CalculateTextureSize(out _size);
            _lodCount = CalculateLoadCount(_size);
            
            _temporaries = new int[_lodCount];

            if (_config.Texture != null) return;

            _config.Texture = new RenderTexture(_size, _size, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
            _config.Texture.filterMode = FilterMode.Point;
            _config.Texture.useMipMap = true;
            _config.Texture.autoGenerateMips = false;
            _config.Texture.Create();
            _config.Texture.hideFlags = HideFlags.HideAndDontSave;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("Indirect Camera Depth Buffer")))
            {
                var id = new RenderTargetIdentifier(_config.Texture);
                Blit(cmd, BuiltinRenderTextureType.None, id, _hizMaterial, (int)Pass.Blit);
            
                for (var i = 0; i < _lodCount; ++i)
                {
                    _temporaries[i] = Shader.PropertyToID($"_09659d57_Temporaries{i}");
            
                    _size >>= 1;
                    _size = Mathf.Max(_size, 1);
            
                    cmd.GetTemporaryRT(_temporaries[i], _size, _size, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
                    if (i == 0)
                    {
                        Blit(cmd, id, _temporaries[0], _hizMaterial, (int)Pass.Reduce);
                    }
                    else
                    {
                        Blit(cmd, _temporaries[i - 1], _temporaries[i], _hizMaterial, (int)Pass.Reduce);
                    }
                
                    cmd.CopyTexture(_temporaries[i], 0, 0, id, 0, i + 1);
                    if (i >= 1)
                    {
                        cmd.ReleaseTemporaryRT(_temporaries[i - 1]);
                    }
                }

                cmd.ReleaseTemporaryRT(_temporaries[_lodCount - 1]);
            }
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // if (_config.Texture != null)
            //     _config.Texture.Release();
        }
        
        public void Dispose()
        {
            CoreUtils.Destroy(_hizMaterial);
        }
        
        private Vector2 CalculateTextureSize(out int size)
        {
            size = Mathf.Max(_cameraWidth, _cameraHeight);
            size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), MAXIMUM_BUFFER_SIZE);
        
            return new Vector2
            {
                x = size,
                y = size
            };
        }

        private int CalculateLoadCount(int size) => (int)Mathf.Floor(Mathf.Log(size, 2f));
    }

    [SerializeField] private HierarchicalDepthMap _hizConfig;
    
    private RenderPass _pass;
    private Material _hizMaterial;

    public override void Create()
    {
        _pass = new RenderPass(_hizConfig);

        // Configures where the render pass should be injected.
        _pass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        //base.Dispose(disposing);
        _pass.Dispose();
    }
}


