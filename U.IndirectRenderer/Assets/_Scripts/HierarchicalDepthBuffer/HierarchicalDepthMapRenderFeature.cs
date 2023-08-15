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
        
        private Material _material;
        private RenderTexture _texture;
        private int _size;
        
        private int _lodCount;
        private int[] _temporaries;

        public RenderPass()
        {
            HierarchicalDepthMap.OnInitialize(ctx =>
            {
                _material = ctx.Item1;
                _texture = ctx.Item2;
                _size = ctx.Item3;
                
                _lodCount = CalculateLoadCount(_size);
                _temporaries = new int[_lodCount];
            });
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            var cameraHeight = renderingData.cameraData.camera.pixelHeight;
            var cameraWidth = renderingData.cameraData.camera.pixelWidth;
            HierarchicalDepthMap.Initialize(cameraHeight, cameraWidth);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            var command = CommandBufferPool.Get();
            using (new ProfilingScope(command, new ProfilingSampler("Indirect Camera Depth Buffer")))
            {
                var id = new RenderTargetIdentifier(_texture);
                Blit(command, BuiltinRenderTextureType.None, id, _material, (int)Pass.Blit);

                var size = _size;
                for (var i = 0; i < _lodCount; ++i)
                {
                    _temporaries[i] = Shader.PropertyToID($"_09659d57_Temporaries{i}");
            
                    size >>= 1;
                    size = Mathf.Max(size, 1);
                    command.GetTemporaryRT(_temporaries[i], size, size, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
                    if (i == 0)
                    {
                        Blit(command, id, _temporaries[0], _material, (int)Pass.Reduce);
                    }
                    else
                    {
                        Blit(command, _temporaries[i - 1], _temporaries[i], _material, (int)Pass.Reduce);
                    }
                
                    command.CopyTexture(_temporaries[i], 0, 0, id, 0, i + 1);
                    if (i >= 1)
                    {
                        command.ReleaseTemporaryRT(_temporaries[i - 1]);
                    }
                }

                command.ReleaseTemporaryRT(_temporaries[_lodCount - 1]);
            }
            
            context.ExecuteCommandBuffer(command);
            command.Clear();

            CommandBufferPool.Release(command);
        }

        private int CalculateLoadCount(int size) => (int)Mathf.Floor(Mathf.Log(size, 2f));
    }

    private RenderPass _pass;

    public override void Create()
    {
        _pass = new RenderPass();
        _pass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) => 
        renderer.EnqueuePass(_pass);
}


