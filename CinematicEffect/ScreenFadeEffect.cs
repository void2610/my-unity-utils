using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// 全画面オーバーレイ Image をフェードイン／アウトさせる演出。
/// fadeColor を黒にすれば暗転、白にすれば白フラッシュとして使用できる。
/// </summary>
public sealed class ScreenFadeEffect : ConfigurableCinematicEffectBase<ScreenFadeConfig>
{
    public override string EffectName => "スクリーンフェード";
    private readonly Image _overlayImage;

    /// <summary>
    /// オーバーレイ Image を自動取得するコンストラクタ。 <see cref="CinematicOverlay"/> が
    /// 必要な Canvas + Image をシーン上に自動生成するため、 事前配置は不要。
    /// </summary>
    public ScreenFadeEffect() : this(CinematicOverlay.Instance.Image) { }

    public ScreenFadeEffect(Image overlayImage) : base()
    {
        _overlayImage = overlayImage;
        OnResetImmediate();
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        _overlayImage.raycastTarget = false;
        _overlayImage.transform.SetAsLastSibling();

        // フェード色を設定してアルファを0に初期化
        var color = CurrentConfig.FadeColor;
        color.a = 0f;
        _overlayImage.color = color;

        // フェードイン（オーバーレイを不透明へ）
        await _overlayImage.FadeIn(CurrentConfig.EnterDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);

        if (CurrentConfig.HoldDuration > 0f)
        {
            // 指定秒数だけ保持
            await UniTask.Delay(System.TimeSpan.FromSeconds(CurrentConfig.HoldDuration), cancellationToken: ct);
        }
        else if (!CurrentConfig.AutoComplete)
        {
            // StopAsync が呼ばれるまで保持
            await UniTask.WaitUntilCanceled(ct);
        }

        if (CurrentConfig.AutoComplete)
        {
            await FadeOutAsync(ct);
            ResetAlpha();
        }
    }

    protected override async UniTask OnStopAsync(CancellationToken ct)
    {
        // フェードアウト（オーバーレイを透明へ）
        await FadeOutAsync(ct);

        ResetAlpha();
    }

    protected override void OnResetImmediate()
    {
        if (_overlayImage == null) return;

        _overlayImage.raycastTarget = false;
        ResetAlpha();
    }

    /// <summary>オーバーレイ Image のアルファを即座に0にする。</summary>
    private void ResetAlpha()
    {
        var color = _overlayImage.color;
        color.a = 0f;
        _overlayImage.color = color;
    }

    private UniTask FadeOutAsync(CancellationToken ct)
    {
        return _overlayImage.FadeOut(CurrentConfig.ExitDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);
    }
}
