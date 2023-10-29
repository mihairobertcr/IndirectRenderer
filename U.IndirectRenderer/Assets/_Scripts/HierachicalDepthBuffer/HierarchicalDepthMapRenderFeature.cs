using UnityEngine.Rendering.Universal;

public partial class HierarchicalDepthMapRenderFeature : ScriptableRendererFeature
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


