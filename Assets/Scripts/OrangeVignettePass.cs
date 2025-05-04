using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OrangeVignettePass : ScriptableRenderPass
{
    public Material vignetteMaterial;
    public string profilerTag;
    private RTHandle tempTexture;
    private RTHandle source;
    public float vignetteStrength;

    public void Setup(RTHandle sourceHandle)
    {
        source = sourceHandle;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // Allocate a temporary texture matching the camera
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor, name: "_TemporaryColorTexture");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (vignetteMaterial == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

        vignetteMaterial.SetFloat("_VignetteStrength", vignetteStrength);

        // Blit with vignette effect
        Blit(cmd, source, tempTexture, vignetteMaterial);
        Blit(cmd, tempTexture, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // RTHandles are released automatically by URP, nothing to clean manually here.
    }
}
