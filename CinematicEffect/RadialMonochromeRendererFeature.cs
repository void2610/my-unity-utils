using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>中心からの半径でカラー/白黒を分ける全画面マスクを適用する URP RendererFeature。Active が true のときだけパスを積み、半径は RadialMonochromeEffect がマテリアルの _Radius を書き換える。</summary>
public sealed class RadialMonochromeRendererFeature : ScriptableRendererFeature
{
    /// <summary>演出中のみ true にしてパスを有効化する (RadialMonochromeEffect が切り替える)。</summary>
    public static bool Active;

    [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    private const string MaterialResourcePath = "RadialMonochrome";

    private Material _material;
    private RadialMonochromePass _pass;

    // マテリアルはシーン/レンダラに配線せず Resources から自己調達する (RadialMonochromeEffect と同一アセット = 同一インスタンスを共有し、_Radius 書き換えを描画へ反映する)
    public override void Create()
    {
        _material = Resources.Load<Material>(MaterialResourcePath);
        _pass = new RadialMonochromePass(_material) { renderPassEvent = injectionPoint };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!Active || _material == null) return;
        renderer.EnqueuePass(_pass);
    }

    private sealed class RadialMonochromePass : ScriptableRenderPass
    {
        private readonly Material _material;

        public RadialMonochromePass(Material material)
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

            var destDesc = renderGraph.GetTextureDesc(cameraTexture);
            destDesc.name = "_RadialMonochromeTexture";
            var destination = renderGraph.CreateTexture(destDesc);

            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(cameraTexture, destination, _material, 0), "Radial Monochrome Effect");
            // AddCopyPass での書き戻しは framebuffer fetch 非対応の WebGL で動かないため、カメラカラーを差し替える
            resourceData.cameraColor = destination;
        }
    }
}
