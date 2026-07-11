using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

/// <summary>放射状 (ズーム) ブラーを LitMotion でフェードイン/アウトする演出。マテリアル (RadialBlur.shader) は未配線時 Resources から自己調達する。</summary>
public sealed class RadialBlurEffect : ConfigurableCinematicEffectBase<RadialBlurConfig>
{
    public override string EffectName => "放射状ブラー";

    private static readonly int StrengthId = Shader.PropertyToID("_Strength");
    private static readonly int CenterId = Shader.PropertyToID("_Center");

    private const string DefaultMaterialResourcePath = "RadialBlur";

    private readonly Material _material;
    private float _currentStrength;

    // 未配線時は同梱マテリアルを自己ロードする (VisionWarp と同じ自己調達フォールバック。事前配置不要)
    public RadialBlurEffect() : this(LoadDefaultMaterial()) { }

    public RadialBlurEffect(Material radialBlurMaterial) : base()
    {
        _material = radialBlurMaterial;
        OnResetImmediate();
    }

    // RendererFeature と同一マテリアルアセットを共有する。パス設定ミスを後段の NRE ではなくロード時点で顕在化させる
    private static Material LoadDefaultMaterial()
    {
        var material = Resources.Load<Material>(DefaultMaterialResourcePath);
        if (material == null) throw new InvalidOperationException($"Resources から RadialBlur マテリアルをロードできません: {DefaultMaterialResourcePath}");

        return material;
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        // RendererFeature はレンダラ資産へ事前配置せず、初回再生時にコードで注入する (可搬性優先)
        CinematicRendererFeatureInjector.EnsureFeature<RadialBlurRendererFeature>();

        _material.SetVector(CenterId, CurrentConfig.Center);
        RadialBlurRendererFeature.Active = true;

        await AnimateStrengthAsync(0f, CurrentConfig.Strength, CurrentConfig.EnterDuration, CurrentConfig.Ease, ct);

        while (!ct.IsCancellationRequested)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    protected override async UniTask OnStopAsync(CancellationToken ct)
    {
        await AnimateStrengthAsync(_currentStrength, 0f, CurrentConfig.ExitDuration, CurrentConfig.Ease, ct);
        OnResetImmediate();
    }

    protected override void OnResetImmediate()
    {
        if (_material == null) return;
        _currentStrength = 0f;
        _material.SetFloat(StrengthId, 0f);
        RadialBlurRendererFeature.Active = false;
    }

    private async UniTask AnimateStrengthAsync(float from, float to, float duration, Ease ease, CancellationToken ct)
    {
        if (duration <= 0f)
        {
            _currentStrength = to;
            _material.SetFloat(StrengthId, to);
            return;
        }

        var motion = LMotion.Create(from, to, duration)
            .WithEase(ease)
            .Bind(value =>
            {
                _currentStrength = value;
                _material.SetFloat(StrengthId, value);
            });

        await motion.ToUniTask(cancellationToken: ct);
    }
}
