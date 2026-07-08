using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

/// <summary>
/// フルスクリーンシェーダーで視界を歪ませる（陽炎・酩酊感）演出を LitMotion でフェードイン/アウトする。
/// 強度 (_Strength) を 0 からフェードインし、キャンセルまでホールドする。周波数・速度は即時反映。
/// マテリアルは VisionWarp.shader を使用したものをコンストラクター引数で受け取る（RadialBlurEffect と同じ構成）。
/// </summary>
public sealed class VisionWarpEffect : ConfigurableCinematicEffectBase<VisionWarpConfig>
{
    public override string EffectName => "視界歪み";

    private static readonly int StrengthId = Shader.PropertyToID("_Strength");
    private static readonly int FrequencyId = Shader.PropertyToID("_Frequency");
    private static readonly int SpeedId = Shader.PropertyToID("_Speed");

    private readonly Material _material;
    private float _currentStrength;

    public VisionWarpEffect(Material visionWarpMaterial) : base()
    {
        _material = visionWarpMaterial;
        OnResetImmediate();
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        _material.SetFloat(FrequencyId, CurrentConfig.Frequency);
        _material.SetFloat(SpeedId, CurrentConfig.Speed);
        VisionWarpRendererFeature.Active = true;

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
        VisionWarpRendererFeature.Active = false;
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
