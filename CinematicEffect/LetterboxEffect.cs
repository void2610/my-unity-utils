using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// 画面上下に黒帯（レターボックス）をスライドインさせる演出。
/// Canvas UI を用いて実装する。
/// </summary>
public sealed class LetterboxEffect : ConfigurableCinematicEffectBase<LetterboxConfig>
{
    public override string EffectName => "レターボックス";
    private readonly RectTransform _topBar;
    private readonly RectTransform _bottomBar;

    public LetterboxEffect(RectTransform topBar, RectTransform bottomBar) : base()
    {
        _topBar = topBar;
        _bottomBar = bottomBar;
        OnResetImmediate();
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        // 画面外の初期位置へリセット
        SetBarsToHidden();

        // 上下の黒帯を同時にスライドイン
        var topTask = _topBar.MoveToY(-150f, CurrentConfig.EnterDuration, CurrentConfig.Ease)
            .ToUniTask(cancellationToken: ct);
        var bottomTask = _bottomBar.MoveToY(150f, CurrentConfig.EnterDuration, CurrentConfig.Ease)
            .ToUniTask(cancellationToken: ct);
        await UniTask.WhenAll(topTask, bottomTask);

        // キャンセルされるまでホールド
        await UniTask.WaitUntilCanceled(ct);
    }

    protected override async UniTask OnStopAsync(CancellationToken ct)
    {
        // 黒帯を画面外へスライドアウト
        var topTask = _topBar.MoveToY(CurrentConfig.BarHeight, CurrentConfig.ExitDuration, CurrentConfig.Ease)
            .ToUniTask(cancellationToken: ct);
        var bottomTask = _bottomBar.MoveToY(-CurrentConfig.BarHeight, CurrentConfig.ExitDuration, CurrentConfig.Ease)
            .ToUniTask(cancellationToken: ct);
        await UniTask.WhenAll(topTask, bottomTask);

        SetBarsToHidden();
    }

    protected override void OnResetImmediate()
    {
        SetBarsToHidden();
    }

    /// <summary>黒帯を画面外の初期位置へ即座に移動する。</summary>
    private void SetBarsToHidden()
    {
        if (_topBar == null || _bottomBar == null) return;

        var topPos = _topBar.anchoredPosition;
        _topBar.anchoredPosition = new Vector2(topPos.x, CurrentConfig.BarHeight);

        var bottomPos = _bottomBar.anchoredPosition;
        _bottomBar.anchoredPosition = new Vector2(bottomPos.x, -CurrentConfig.BarHeight);
    }
}
