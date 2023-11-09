using UnityEngine.Rendering.Universal;

namespace Keensight.Rendering.HierarchicalDepthBuffer
{
    public class HierarchicalDepthMapRenderFeature : ScriptableRendererFeature
    {
        private HierarchicalDepthMapRenderPass _pass;

        public override void Create()
        {
            _pass = new HierarchicalDepthMapRenderPass();
            _pass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }
    
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_pass);
        }
    }
}
