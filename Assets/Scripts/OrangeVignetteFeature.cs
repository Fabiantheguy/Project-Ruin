using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OrangeVignetteFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OrangeVignetteSettings
    {
        public Material vignetteMaterial;
    }

    public OrangeVignetteSettings settings = new OrangeVignetteSettings();
    private OrangeVignettePass vignettePass;

    public override void Create()
    {
        vignettePass = new OrangeVignettePass(settings.vignetteMaterial);
        vignettePass.renderPassEvent = RenderPassEvent.AfterRendering;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.vignetteMaterial == null) return;

        // We don't call cameraColorTargetHandle here anymore
        renderer.EnqueuePass(vignettePass);
    }

    class OrangeVignettePass : ScriptableRenderPass
    {
        private Material vignetteMaterial;

        public OrangeVignettePass(Material material)
        {
            this.vignetteMaterial = material;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (vignetteMaterial == null) return;

            var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get("Orange Vignette");
            Blit(cmd, cameraColorTarget, cameraColorTarget, vignetteMaterial);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
