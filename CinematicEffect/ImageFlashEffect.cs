using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// スプライト画像を全画面オーバーレイとしてフェードイン/アウトさせる演出。
/// <see cref="ScreenFadeEffect"/> がソリッドカラーで塗るのに対し、
/// あらかじめ Image コンポーネントに設定したスプライトをそのまま表示する。
/// </summary>
public sealed class ImageFlashEffect : ConfigurableCinematicEffectBase<ImageFlashConfig>
{
    public override string EffectName => "フラッシュ";
    private readonly Image _overlayImage;

    public ImageFlashEffect(Image overlayImage) : base()
    {
        _overlayImage = overlayImage;
        OnResetImmediate();
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        _overlayImage.raycastTarget = false;
        _overlayImage.transform.SetAsLastSibling();

        // ティントカラーを適用してアルファを 0 から開始
        var color = CurrentConfig.TintColor;
        color.a = 0f;
        _overlayImage.color = color;

        // フェードイン → ホールド → フェードアウト
        await _overlayImage.FadeIn(CurrentConfig.EnterDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);

        if (CurrentConfig.HoldDuration > 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.HoldDuration), cancellationToken: ct);
        }

        await _overlayImage.FadeOut(CurrentConfig.ExitDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);
        ResetAlpha();
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
