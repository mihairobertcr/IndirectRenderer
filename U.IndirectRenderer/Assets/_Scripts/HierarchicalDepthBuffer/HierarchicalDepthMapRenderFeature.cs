using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
                
        private RTHandle _texture;
        private RTHandle _emptyTexture;
        private RTHandle[] _temporaries;
        
        private Material _material;
        private int _size;
        private int _lodCount;

        // public RenderPass()
        // {
        //     HierarchicalDepthMap.OnInitialize(ctx =>
        //     {
        //         _texture = ctx.texture;
        //         _emptyTexture = ctx.empty;
        //         _temporaries = ctx.temporaries;
        //         _material = ctx.material;
        //         _size = ctx.size;
        //         _lodCount = ctx.lods;
        //     });
        // }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (!Application.isPlaying) return;
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            var cameraHeight = renderingData.cameraData.camera.pixelHeight;
            var cameraWidth = renderingData.cameraData.camera.pixelWidth;
            HierarchicalDepthMap.Initialize(cameraHeight, cameraWidth, ctx =>
            {
                _texture = ctx.texture;
                _emptyTexture = ctx.empty;
                _temporaries = ctx.temporaries;
                _material = ctx.material;
                _size = ctx.size;
                _lodCount = ctx.lods;
            });
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Application.isPlaying) return;
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            
            var command = CommandBufferPool.Get();
            using (new ProfilingScope(command, new ProfilingSampler("Indirect Camera Depth Buffer")))
            {
                var size = _size;
                Blit(command, _emptyTexture, _texture, _material, (int)Pass.Blit);
                for (var i = 0; i < _lodCount; ++i)
                {
                    // _temporaries[i] = RTHandles.Alloc(Shader.PropertyToID($"_09659d57_Temporaries{i}"), name: $"_09659d57_Temporaries{i}");
            
                    size >>= 1;
                    size = Mathf.Max(size, 1);
                    command.GetTemporaryRT(Shader.PropertyToID(_temporaries[i].name), size, size, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
                    if (i == 0)
                    {
                        Blit(command, _texture, _temporaries[0], _material, (int)Pass.Reduce);
                    }
                    else
                    {
                        Blit(command, _temporaries[i - 1], _temporaries[i], _material, (int)Pass.Reduce);
                    }
                
                    command.CopyTexture(_temporaries[i], 0, 0, _texture, 0, i + 1);
                    if (i >= 1)
                    {
                        command.ReleaseTemporaryRT(Shader.PropertyToID(_temporaries[i - 1].name));
                    }
                }

                command.ReleaseTemporaryRT(Shader.PropertyToID(_temporaries[_lodCount - 1].name));
            }
            
            context.ExecuteCommandBuffer(command);
            command.Clear();

            CommandBufferPool.Release(command);

        }

        public void Dispose()
        {
            // HierarchicalDepthMap.Instance.Dispose();
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _pass.Dispose();
    }
}


