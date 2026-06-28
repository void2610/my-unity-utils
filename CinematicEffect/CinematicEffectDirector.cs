using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 映画的演出の薄いオーケストレータ。
/// VContainer から RegisterComponent で注入される。
/// <see cref="CinematicSequence"/> を受け取り、型からエフェクトを解決して実行する。
/// </summary>
public class CinematicEffectDirector : MonoBehaviour
{
    [SerializeField] private RectTransform letterboxTopBar;
    [SerializeField] private RectTransform letterboxBottomBar;
    [SerializeField] private Image screenFadeOverlay;
    [SerializeField] private Image imageFlashOverlay;
    [SerializeField] private Image blinkOverlay;
    [SerializeField] private Material waveDistortionMaterial;
    /// <summary>
    /// 演出の有効/無効フラグ。false の場合 RunAsync は即座に返る。
    /// </summary>
    public static bool IsEffectsEnabled { get; set; } = true;
#if UNITY_EDITOR
    // シーンに保存しない。CinematicTestWindow からプレイモード開始時に注入される
    private List<CinematicSequenceAsset.Step> _testSequence = new();
#endif

    private Dictionary<Type, ICinematicEffect> _effects;
#if UNITY_EDITOR
    private CancellationTokenSource _testCts;
#endif

    public UniTask RunAsync(CinematicSequenceAsset sequenceAsset, CancellationToken ct = default) => RunAsync(sequenceAsset.Build(), ct);

    /// <summary>
    /// 指定したシーケンスのステップを順番に実行する。
    /// </summary>
    public async UniTask RunAsync(CinematicSequence sequence, CancellationToken ct = default)
    {
        if (sequence == null || sequence.Steps == null)
        {
            return;
        }

        // 演出が無効の場合は何もしない
        if (!IsEffectsEnabled) return;

        foreach (var step in sequence.Steps)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            if (step.Kind == CinematicSequence.StepKind.Delay)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(step.DelaySeconds), cancellationToken: ct);
                continue;
            }

            if (step.EffectType == null)
            {
                continue;
            }

            var effect = Resolve(step.EffectType);
            if (effect == null)
            {
                continue;
            }

            if (effect is IConfigurableCinematicEffect configurableEffect)
            {
                if (step.Config != null)
                {
                    configurableEffect.ApplyConfig(step.Config);
                }
                else
                {
                    configurableEffect.ResetConfig();
                }
            }
            else if (step.Config != null)
            {
                throw new InvalidOperationException($"エフェクト {step.EffectType.Name} は設定オブジェクトを受け取れません。");
            }

            switch (step.Kind)
            {
                case CinematicSequence.StepKind.Play:
                    effect.PlayAsync(ct).Forget();
                    break;
                case CinematicSequence.StepKind.PlayAndAwait:
                    await effect.PlayAsync(ct);
                    break;
                case CinematicSequence.StepKind.Stop:
                    await effect.StopAsync(ct);
                    break;
            }
        }
    }

    /// <summary>全演出を即座にリセットする。</summary>
    public void ResetAll()
    {
        if (_effects == null)
        {
            return;
        }

        foreach (var effect in _effects.Values)
        {
            effect.ResetImmediate();
        }
    }

#if UNITY_EDITOR
    /// <summary>CinematicTestWindow からプレイモード開始時にシーケンスを注入する。</summary>
    public void InjectTestSequence(List<CinematicSequenceAsset.Step> steps)
    {
        _testSequence = new List<CinematicSequenceAsset.Step>(steps);
    }

    /// <summary>エディタのインスペクターからテスト再生シーケンスを実行する。</summary>
    public async UniTaskVoid PlayTestSequenceAsync()
    {
        _testCts?.Cancel();
        _testCts?.Dispose();
        _testCts = new CancellationTokenSource();

        var seq = BuildTestSequence();
        await RunAsync(seq, _testCts.Token);
    }

    /// <summary>テスト再生を停止し、全エフェクトをリセットする。</summary>
    public void StopTest()
    {
        _testCts?.Cancel();
        _testCts?.Dispose();
        _testCts = null;
        ResetAll();
    }

    /// <summary>注入済みのシーケンスから <see cref="CinematicSequence"/> を構築する。</summary>
    private CinematicSequence BuildTestSequence()
    {
        var seq = CinematicSequence.Create();
        foreach (var step in _testSequence)
        {
            if (step.kind == CinematicSequence.StepKind.Delay)
            {
                seq.Delay(step.delaySeconds);
                continue;
            }

            var config = step.GetActiveConfig();
            var type = step.GetEffectSystemType();
            _ = step.kind switch
            {
                CinematicSequence.StepKind.Play => seq.Play(type, config),
                CinematicSequence.StepKind.PlayAndAwait => seq.PlayAndAwait(type, config),
                CinematicSequence.StepKind.Stop => seq.Stop(type, config),
                _ => seq,
            };
        }

        return seq;
    }
#endif

    private ICinematicEffect Resolve(Type effectType)
    {
        EnsureEffectsRegistered();

        if (_effects.TryGetValue(effectType, out var effect))
        {
            return effect;
        }

        throw new InvalidOperationException($"エフェクト {effectType.Name} が登録されていません。");
    }

    /// <summary>
    /// ICinematicEffect を型キーで登録する。
    /// </summary>
    private void Register(ICinematicEffect effect)
    {
        _effects[effect.GetType()] = effect;
    }

    private void EnsureEffectsRegistered()
    {
        if (_effects != null)
        {
            return;
        }

        _effects = new Dictionary<Type, ICinematicEffect>();

        // ── 純粋 C# エフェクト（コンストラクターで参照を注入）
        Register(new LetterboxEffect(letterboxTopBar, letterboxBottomBar));
        Register(new ScreenFadeEffect(screenFadeOverlay));
        Register(new ImageFlashEffect(imageFlashOverlay));
        Register(new WaveDistortionEffect(waveDistortionMaterial));
        Register(new BlinkEffect(blinkOverlay));
        Register(new CameraShakeEffect());
        Register(new CameraPerlinShakeEffect());
        Register(new CameraDisorientationEffect());

        // ── PP エフェクト（Volume を gameObject に AddComponent する）
        Register(new VignetteEffect(gameObject));
        Register(new ChromaticAberrationEffect(gameObject));
        Register(new LensDistortionEffect(gameObject));
        Register(new FilmGrainEffect(gameObject));
        Register(new FilmNoiseEffect(gameObject));
        Register(new ContrastEffect(gameObject));
        Register(new SaturationEffect(gameObject));
        Register(new ColorFilterEffect(gameObject));
        Register(new DepthOfFieldEffect(gameObject));
        Register(new DoFDizzinessEffect(gameObject));
        Register(new PulseVignetteEffect(gameObject));
    }

    private void Awake()
    {
        EnsureEffectsRegistered();
    }

    private void OnDestroy()
    {
        if (_effects != null)
        {
            ResetAll();
            // IDisposable を実装しているエフェクトを破棄
            foreach (var effect in _effects.Values)
            {
                if (effect is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

#if UNITY_EDITOR
        _testCts?.Cancel();
        _testCts?.Dispose();
#endif
    }
}
