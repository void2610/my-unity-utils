using System;
using System.Collections.Generic;

/// <summary>
/// 映画的演出の手順を記述する純粋なデータクラス。
/// エフェクトのインスタンスを持たず、型情報と操作種別のみを保持する。
/// 実行は <see cref="CinematicDirector.RunAsync"/> が担当する。
/// </summary>
public sealed class CinematicSequence
{
    /// <summary>ステップの操作種別。</summary>
    public enum StepKind
    {
        /// <summary>Fire-and-Forget で開始。</summary>
        Play,

        /// <summary>完了を待機して開始。</summary>
        PlayAndAwait,

        /// <summary>停止を待機。</summary>
        Stop,

        /// <summary>指定秒数だけ待機。</summary>
        Delay,
    }

    /// <summary>シーケンスの1ステップ。</summary>
    internal readonly record struct Step(
        StepKind Kind,
        Type EffectType = null,
        float DelaySeconds = 0f,
        CinematicEffectConfig Config = null);

    private readonly List<Step> _steps = new();

    private CinematicSequence() { }

    /// <summary>新しいシーケンスを生成する。</summary>
    public static CinematicSequence Create() => new();

    /// <summary>演出を Fire-and-Forget で開始する（ループ型エフェクト向け）。</summary>
    public CinematicSequence Play<T>(CinematicEffectConfig config = null) where T : class, ICinematicEffect
    {
        _steps.Add(new Step(StepKind.Play, typeof(T), Config: config));
        return this;
    }

    /// <summary>演出を開始し、完了を待機する（1回完結型エフェクト向け）。</summary>
    public CinematicSequence PlayAndAwait<T>(CinematicEffectConfig config = null) where T : class, ICinematicEffect
    {
        _steps.Add(new Step(StepKind.PlayAndAwait, typeof(T), Config: config));
        return this;
    }

    /// <summary>演出を停止し、停止アニメーションの完了を待機する。</summary>
    public CinematicSequence Stop<T>(CinematicEffectConfig config = null) where T : class, ICinematicEffect
    {
        _steps.Add(new Step(StepKind.Stop, typeof(T), Config: config));
        return this;
    }

    /// <summary>演出を Fire-and-Forget で開始する（ランタイム型解決用）。</summary>
    public CinematicSequence Play(Type effectType, CinematicEffectConfig config = null)
    {
        _steps.Add(new Step(StepKind.Play, effectType, Config: config));
        return this;
    }

    /// <summary>演出を開始し、完了を待機する（ランタイム型解決用）。</summary>
    public CinematicSequence PlayAndAwait(Type effectType, CinematicEffectConfig config = null)
    {
        _steps.Add(new Step(StepKind.PlayAndAwait, effectType, Config: config));
        return this;
    }

    /// <summary>演出を停止し、停止アニメーションの完了を待機する（ランタイム型解決用）。</summary>
    public CinematicSequence Stop(Type effectType, CinematicEffectConfig config = null)
    {
        _steps.Add(new Step(StepKind.Stop, effectType, Config: config));
        return this;
    }

    /// <summary>指定秒数だけ待機する。</summary>
    public CinematicSequence Delay(float seconds)
    {
        _steps.Add(new Step(StepKind.Delay, DelaySeconds: seconds));
        return this;
    }

    /// <summary><see cref="CinematicDirector"/> がステップを読み取るための公開プロパティ。</summary>
    internal IReadOnlyList<Step> Steps => _steps;
}
