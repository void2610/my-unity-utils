using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

/// <summary>
/// 中心からカラーが「ブワッ」と全画面へ広がり、ゆっくり収縮して白黒へ戻る一発演出。
/// パラメータ (時間・ぼかし) は <see cref="RadialMonochromeConfig"/> = SO 側で調整する。RendererFeature と同一マテリアルを Resources 共有する (VisionWarp と同じ自己調達)。
/// </summary>
public sealed class RadialMonochromeEffect : ConfigurableCinematicEffectBase<RadialMonochromeConfig>
{
    public override string EffectName => "モノクロ収縮";

    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");

    private const string DefaultMaterialResourcePath = "RadialMonochrome";

    private readonly Material _material;

    public RadialMonochromeEffect() : this(LoadDefaultMaterial()) { }

    public RadialMonochromeEffect(Material material) : base()
    {
        _material = material;
        OnResetImmediate();
    }

    // RendererFeature と同一マテリアルアセットを共有する。パス設定ミスを後段の NRE ではなくロード時点で顕在化させる
    private static Material LoadDefaultMaterial()
    {
        var material = Resources.Load<Material>(DefaultMaterialResourcePath);
        if (material == null) throw new InvalidOperationException($"Resources から RadialMonochrome マテリアルをロードできません: {DefaultMaterialResourcePath}");

        return material;
    }

    // 一発完結: 広げる→保持→収縮まで走らせて自然終了する (PlayAndAwait 前提)
    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        // RendererFeature はレンダラ資産へ事前配置せず、初回再生時にコードで注入する (可搬性優先)
        CinematicRendererFeatureInjector.EnsureFeature<RadialMonochromeRendererFeature>();

        _material.SetVector(CenterId, new Vector4(0.5f, 0.5f, 0f, 0f));
        _material.SetFloat(SoftnessId, CurrentConfig.Softness);
        var aspect = (float)Screen.width / Screen.height;
        _material.SetFloat(AspectId, aspect);
        // 中心から画面隅までの最大距離 (アスペクト補正済み)。ここまで広げれば全画面カラーになる
        var full = Mathf.Sqrt(0.25f * aspect * aspect + 0.25f) + 0.15f;
        RadialMonochromeRendererFeature.Active = true;

        await AnimateRadiusAsync(0f, full, CurrentConfig.EnterDuration, Ease.OutCubic, ct); // ブワッと色が広がる
        if (CurrentConfig.HoldDuration > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.HoldDuration), Cysharp.Threading.Tasks.DelayType.DeltaTime, PlayerLoopTiming.Update, ct);
        await AnimateRadiusAsync(full, 0f, CurrentConfig.ExitDuration, CurrentConfig.Ease, ct); // ゆっくり白黒が収縮

        OnResetImmediate();
    }

    protected override UniTask OnStopAsync(CancellationToken ct)
    {
        OnResetImmediate();
        return UniTask.CompletedTask;
    }

    protected override void OnResetImmediate()
    {
        if (_material == null) return;
        _material.SetFloat(RadiusId, 0f); // 半径 0 = 全画面白黒 (収縮しきった状態)
        RadialMonochromeRendererFeature.Active = false;
    }

    private async UniTask AnimateRadiusAsync(float from, float to, float duration, Ease ease, CancellationToken ct)
    {
        if (duration <= 0f)
        {
            _material.SetFloat(RadiusId, to);
            return;
        }

        await LMotion.Create(from, to, duration)
            .WithEase(ease)
            .Bind(this, (value, self) => self._material.SetFloat(RadiusId, value))
            .ToUniTask(cancellationToken: ct);
    }
}
