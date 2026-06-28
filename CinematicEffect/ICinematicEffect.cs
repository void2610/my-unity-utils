using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 映画的演出の Strategy インターフェース。
/// 各演出コンポーネントはこのインターフェースを実装する。
/// </summary>
public interface ICinematicEffect
{
    /// <summary>演出名。</summary>
    public string EffectName { get; }

    /// <summary>演出再生中かどうか。</summary>
    public bool IsPlaying { get; }

    /// <summary>
    /// 演出を再生する。
    /// ct がキャンセルされるまでホールドする演出の場合、キャンセルで停止する。
    /// </summary>
    public UniTask PlayAsync(CancellationToken ct = default);

    /// <summary>
    /// 演出を停止する。
    /// 逆アニメーションなどを伴う場合はそれを待機してから完了する。
    /// </summary>
    public UniTask StopAsync(CancellationToken ct = default);

    /// <summary>演出を即座にリセットする（アニメーションなし）。</summary>
    public void ResetImmediate();
}
