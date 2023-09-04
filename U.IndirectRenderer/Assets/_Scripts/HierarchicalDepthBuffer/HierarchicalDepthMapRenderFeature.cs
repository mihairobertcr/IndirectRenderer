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

        private const string TEMPORARY_NAME = "_09659d57_Temporaries{0}";
        
        private RenderTargetIdentifier _textureId;
        private Material _material;
        
        private int _size;
        private int _lodCount;
        private int[] _temporaries;

        public RenderPass()
        {
            _textureId = new RenderTargetIdentifier(HierarchicalDepthMap.Texture);
            _material = HierarchicalDepthMap.Material;
            _size = HierarchicalDepthMap.Size;
            _lodCount = HierarchicalDepthMap.LodCount;
            
            _temporaries = new int[_lodCount];
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            var command = CommandBufferPool.Get();
            FillHeightMap(command);
            
            context.ExecuteCommandBuffer(command);
            command.Clear();

            CommandBufferPool.Release(command);
        }

        private void FillHeightMap(CommandBuffer command)
        {
#if UNITY_EDITOR
            using var scope = new ProfilingScope(command, new ProfilingSampler("Indirect Camera Depth Buffer"));
#endif
            
            var size = _size;
            command.Blit(BuiltinRenderTextureType.None, _textureId, _material, (int)Pass.Blit);
            for (var i = 0; i < _lodCount; ++i)
            {
                _temporaries[i] = Shader.PropertyToID(string.Format(TEMPORARY_NAME, i));
                
                size >>= 1;
                size = Mathf.Max(size, 1);
                command.GetTemporaryRT(_temporaries[i], size, size, 0, FilterMode.Point, 
                    RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
                
                if (i == 0)
                {
                    command.Blit(_textureId, _temporaries[0], _material, (int)Pass.Reduce);
                }
                else
                {
                    command.Blit(_temporaries[i - 1], _temporaries[i], _material, (int)Pass.Reduce);
                }
                
                command.CopyTexture(_temporaries[i], 0, 0, _textureId, 0, i + 1);
                if (i >= 1)
                {
                    command.ReleaseTemporaryRT(_temporaries[i - 1]);
                }
            }
                
            command.ReleaseTemporaryRT(_temporaries[_lodCount - 1]);
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


