using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;

/// <summary>
/// 映画的演出の抽象基底クラス（純粋C#クラス）。
/// 再生中の CancellationTokenSource を管理し、サブクラスは
/// OnPlayAsync / OnStopAsync / OnResetImmediate を実装する。
/// </summary>
public abstract class CinematicEffectBase : ICinematicEffect, IDisposable
{
    public abstract string EffectName { get; }
    public bool IsPlaying { get; private set; }
    private CancellationTokenSource _playingCts;

    /// <inheritdoc/>
    public async UniTask PlayAsync(CancellationToken ct = default)
    {
        // 前の再生を即座に中断してリセット
        ResetImmediate();

        var myCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _playingCts = myCts;
        IsPlaying = true;

        try
        {
            await OnPlayAsync(myCts.Token);
        }
        catch (OperationCanceledException)
        {
            // キャンセルは正常終了扱い
        }
        finally
        {
            myCts.Dispose();
            // 後続 Play に置き換わった古い世代は共有状態を触らない (新しい再生の IsPlaying=true を上書きしないため)
            if (_playingCts == myCts)
            {
                _playingCts = null;
                IsPlaying = false;
            }
        }
    }

    /// <inheritdoc/>
    public async UniTask StopAsync(CancellationToken ct = default)
    {
        // キャンセルのみ。Dispose は PlayAsync の finally に委ねることで
        // ループ中の await が ObjectDisposedException を投げるのを防ぐ
        _playingCts?.Cancel();
        IsPlaying = false;

        try
        {
            await OnStopAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // 停止アニメーションのキャンセルも正常終了扱い
        }
    }

    /// <inheritdoc/>
    public void ResetImmediate()
    {
        // キャンセルのみ。Dispose は PlayAsync の finally に委ねる
        _playingCts?.Cancel();
        IsPlaying = false;
        OnResetImmediate();
    }

    /// <summary>演出本体の実装。ct がキャンセルされたら終了すること。</summary>
    protected abstract UniTask OnPlayAsync(CancellationToken ct);

    /// <summary>演出を逆再生・非表示にする実装。</summary>
    protected abstract UniTask OnStopAsync(CancellationToken ct);

    /// <summary>演出を即座に初期状態へ戻す実装（アニメーションなし）。</summary>
    protected abstract void OnResetImmediate();

    /// <summary>
    /// ブレンド値を指定イージングでアニメーションさせる共通ヘルパー。
    /// CameraDisorientationEffect と PostProcessVolumeEffectBase の両方で利用する。
    /// </summary>
    protected static async UniTask AnimateBlendAsync(
        float from, float to, float duration, Ease ease, Action<float> onUpdate, CancellationToken ct)
    {
        if (duration <= 0f)
        {
            onUpdate(to);
            return;
        }

        var motion = LMotion.Create(from, to, duration)
            .WithEase(ease)
            .Bind(value => onUpdate(value));

        await motion.ToUniTask(cancellationToken: ct);
    }

    public virtual void Dispose()
    {
        _playingCts?.Cancel();
        _playingCts?.Dispose();
        _playingCts = null;
        IsPlaying = false;
        OnResetImmediate();
    }
}
