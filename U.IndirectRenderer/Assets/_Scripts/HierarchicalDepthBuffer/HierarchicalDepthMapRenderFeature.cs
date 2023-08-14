using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

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
        private Material _material;
        
        private int _cameraHeight;
        private int _cameraWidth;
        private int _lodCount;
        private int[] _temporaries;
        private int _size;

        public RenderPass(HierarchicalDepthMap config) : base()
        {
            _config = config;
            _config.OnInitialize(ctx =>
            {
                _material = ctx.Item1;
            });
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            _cameraHeight = renderingData.cameraData.camera.pixelHeight;
            _cameraWidth = renderingData.cameraData.camera.pixelWidth;
            
            _config.Initialize(_cameraWidth, _cameraHeight);
            _size = _config.Size;
            _lodCount = CalculateLoadCount(_size);
            
            _temporaries = new int[_lodCount];
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("Indirect Camera Depth Buffer")))
            {
                var id = new RenderTargetIdentifier(_config.Texture);
                Blit(cmd, BuiltinRenderTextureType.None, id, _material, (int)Pass.Blit);
            
                for (var i = 0; i < _lodCount; ++i)
                {
                    _temporaries[i] = Shader.PropertyToID($"_09659d57_Temporaries{i}");
            
                    _size >>= 1;
                    _size = Mathf.Max(_size, 1);
            
                    cmd.GetTemporaryRT(_temporaries[i], _size, _size, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
                    if (i == 0)
                    {
                        Blit(cmd, id, _temporaries[0], _material, (int)Pass.Reduce);
                    }
                    else
                    {
                        Blit(cmd, _temporaries[i - 1], _temporaries[i], _material, (int)Pass.Reduce);
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

        private int CalculateLoadCount(int size) => (int)Mathf.Floor(Mathf.Log(size, 2f));
    }

    [SerializeField] private HierarchicalDepthMap _depthMap;
    
    private RenderPass _pass;

    public override void Create()
    {
        if (_depthMap == null)
        {
            _depthMap = Resources.Load("HierarchicalDepthMap") as HierarchicalDepthMap;
        }
        
        _pass = new RenderPass(_depthMap);
        _pass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) => 
        renderer.EnqueuePass(_pass);
}


