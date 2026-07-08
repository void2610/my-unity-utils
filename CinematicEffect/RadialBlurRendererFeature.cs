using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>放射状ブラーを全画面適用する URP RendererFeature。Active が true のときだけパスを積み、強度は RadialBlurEffect がマテリアルの _Strength を書き換える。</summary>
public sealed class RadialBlurRendererFeature : ScriptableRendererFeature
{
    /// <summary>演出中のみ true にしてパスを有効化する (RadialBlurEffect が切り替える)。</summary>
    public static bool Active;

    [SerializeField] private Material material;
    [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    private RadialBlurPass _pass;

    public override void Create()
    {
        _pass = new RadialBlurPass(material) { renderPassEvent = injectionPoint };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!Active || material == null) return;
        renderer.EnqueuePass(_pass);
    }

    private sealed class RadialBlurPass : ScriptableRenderPass
    {
        private readonly Material _material;

        public RadialBlurPass(Material material)
        {
            _material = material;
            requiresIntermediateTexture = true; // 3D Renderer 向けの保険 (Renderer2D はこのフラグを無視する)
            ConfigureInput(ScriptableRenderPassInput.Color); // 2D Renderer で中間カラーテクスチャを確保させる唯一の手段
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType != CameraType.Game) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return; // ConfigureInput が効かず中間テクスチャ未確保のフレームに対する保険

            var cameraTexture = resourceData.activeColorTexture;

            var tempDesc = renderGraph.GetTextureDesc(cameraTexture);
            tempDesc.name = "_RadialBlurTempTexture";
            var tempTexture = renderGraph.CreateTexture(tempDesc);

            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(cameraTexture, tempTexture, _material, 0), "Radial Blur Effect");
            renderGraph.AddCopyPass(tempTexture, cameraTexture, passName: "Radial Blur Copy");
        }
    }
}
