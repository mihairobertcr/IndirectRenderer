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
        
        private RenderTargetIdentifier _textureId;
        private Material _material;
        
        private int _size;
        private int _lodCount;
        private int[] _temporaries;

        public RenderPass()
        {
            HierarchicalDepthMap.Initialize();
            
            _textureId = new RenderTargetIdentifier(HierarchicalDepthMap.Instance.Texture);
            _material = HierarchicalDepthMap.Instance.Material;
            _size = HierarchicalDepthMap.Instance.Size;
            _lodCount = HierarchicalDepthMap.Instance.LodCount;
            _temporaries = new int[_lodCount];
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            var command = CommandBufferPool.Get();
            using (new ProfilingScope(command, new ProfilingSampler("Indirect Camera Depth Buffer")))
            {
                var size = _size;
                Blit(command, BuiltinRenderTextureType.None, _textureId, _material, (int)Pass.Blit);
                for (var i = 0; i < _lodCount; ++i)
                {
                    _temporaries[i] = Shader.PropertyToID($"_09659d57_Temporaries{i}");
            
                    size >>= 1;
                    size = Mathf.Max(size, 1);
                    command.GetTemporaryRT(_temporaries[i], size, size, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
                    if (i == 0)
                    {
                        Blit(command, _textureId, _temporaries[0], _material, (int)Pass.Reduce);
                    }
                    else
                    {
                        Blit(command, _temporaries[i - 1], _temporaries[i], _material, (int)Pass.Reduce);
                    }
                
                    command.CopyTexture(_temporaries[i], 0, 0, _textureId, 0, i + 1);
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


