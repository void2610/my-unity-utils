using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// 画面を指定色に暗転→復帰のループを繰り返す断続的明滅演出。
/// DoFDizzinessEffect と同様に Stop されるまでループし続ける。
/// overlayImage はコンストラクター引数で受け取る。
/// </summary>
public sealed class BlinkEffect : ConfigurableCinematicEffectBase<BlinkConfig>
{
    public override string EffectName => "明滅";

    private readonly Image _overlayImage;

    // 未配線時は CinematicOverlay の全画面 Image を共用する (ScreenFade/ImageFlash と同じ自己調達。事前配置不要)
    public BlinkEffect() : this(CinematicOverlay.Instance.Image) { }

    public BlinkEffect(Image overlayImage) : base()
    {
        _overlayImage = overlayImage;
        OnResetImmediate();
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        _overlayImage.raycastTarget = false;

        while (!ct.IsCancellationRequested)
        {
            // 暗転色・アルファ 0 で初期化
            var color = CurrentConfig.BlinkColor;
            color.a = 0f;
            _overlayImage.color = color;

            // フェードイン（暗転）
            await _overlayImage.FadeIn(CurrentConfig.EnterDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);

            // 暗転保持
            if (CurrentConfig.HoldDuration > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.HoldDuration), cancellationToken: ct);
            }

            // フェードアウト（復帰）
            await _overlayImage.FadeOut(CurrentConfig.ExitDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);

            // インターバル（明るい状態で待機）
            if (CurrentConfig.IntervalDuration > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.IntervalDuration), cancellationToken: ct);
            }
        }
    }

    protected override async UniTask OnStopAsync(CancellationToken ct)
    {
        await _overlayImage.FadeOut(CurrentConfig.ExitDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);
        ResetAlpha();
    }

    protected override void OnResetImmediate()
    {
        if (_overlayImage == null) return;

        _overlayImage.raycastTarget = false;
        ResetAlpha();
    }

    private void ResetAlpha()
    {
        var color = _overlayImage.color;
        color.a = 0f;
        _overlayImage.color = color;
    }
}
