using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;
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

    // 未配線時はオーバーレイ Canvas 配下に黒帯を自己生成する (事前配置不要)
    public LetterboxEffect() : this(CreateBar("LetterboxTopBar", true), CreateBar("LetterboxBottomBar", false)) { }

    public LetterboxEffect(RectTransform topBar, RectTransform bottomBar) : base()
    {
        _topBar = topBar;
        _bottomBar = bottomBar;
        OnResetImmediate();
    }

    // 画面上端/下端に張り付く全幅の黒帯を生成する。表示位置は OnPlay/OnStop が anchoredPosition.y で制御する
    private static RectTransform CreateBar(string name, bool top)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(CinematicOverlay.Instance.Canvas.transform, false);
        var edge = top ? 1f : 0f;
        rt.anchorMin = new Vector2(0f, edge);
        rt.anchorMax = new Vector2(1f, edge);
        rt.pivot = new Vector2(0.5f, edge);
        rt.sizeDelta = new Vector2(0f, 150f);
        var img = go.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return rt;
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
