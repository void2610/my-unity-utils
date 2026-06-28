using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 単一の VolumeComponent を制御するポストプロセス演出の共通基底。
/// CinematicEffectBase を継承し、ICinematicEffect のライフサイクルを再利用する。
/// 各 effect は専用の Volume / VolumeProfile を持ち、型ごとに独立して再生される。
/// </summary>
public abstract class PostProcessVolumeEffectBase<TComponent, TConfig>
    : CinematicEffectBase, IConfigurableCinematicEffect
    where TComponent : VolumeComponent
    where TConfig : VolumeEffectConfig, new()
{
    protected TConfig CurrentConfig { get; private set; }

    private readonly GameObject _owner;
    private Volume _volume;
    private VolumeProfile _runtimeProfile;
    private TComponent _component;
    private float _currentBlend;
    private readonly TConfig _defaultConfig = new();

    protected PostProcessVolumeEffectBase(GameObject owner)
    {
        _owner = owner;
        ResetConfig();
    }

    public void ResetConfig() => CurrentConfig = (TConfig)_defaultConfig.Clone();

    public void ApplyConfig(CinematicEffectConfig config)
    {
        if (config is not TConfig typedConfig)
        {
            throw new InvalidOperationException($"設定型 {config.GetType().Name} は {GetType().Name} に適用できません。");
        }

        CurrentConfig = (TConfig)typedConfig.Clone();
    }

    // ── テンプレートメソッド ──────────────────────────────────

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        EnsureInitialized();
        await AnimateBlendAsync(_currentBlend, 1f, CurrentConfig.EnterDuration, ct);

        while (!ct.IsCancellationRequested)
        {
            Apply(_currentBlend);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    protected override async UniTask OnStopAsync(CancellationToken ct)
    {
        EnsureInitialized();

        // _currentBlend が停止タイミングによってはズレているケースがあるため、
        // 実際の volume.weight を基準に直接補間して確実にフェードアウトする
        var startWeight = _volume ? _volume.weight : 0f;
        await AnimateBlendAsync(0f, 1f, CurrentConfig.ExitDuration, CurrentConfig.Ease, t =>
        {
            _currentBlend = startWeight / Mathf.Max(CurrentConfig.VolumeWeight, 0.0001f) * (1f - t);
            if (_volume) _volume.weight = Mathf.Lerp(startWeight, 0f, t);
        }, ct);

        ResetVolumeState();
    }

    protected override void OnResetImmediate()
    {
        if (!_volume) return;

        _currentBlend = 0f;
        ResetVolumeState();
    }

    /// <summary>VolumeComponent の初期設定（サブクラスで必要な場合にオーバーライド）。</summary>
    protected virtual void InitializeComponent(TComponent component) { }

    /// <summary>blend 値に応じて VolumeComponent のパラメータを適用する。</summary>
    protected abstract void ApplyComponent(TComponent component, TConfig config, float blend);

    /// <summary>VolumeComponent のパラメータをデフォルト値に戻す。</summary>
    protected abstract void ResetComponent(TComponent component);

    /// <summary>インスタンス固有の 4 引数ラッパー。共通静的ヘルパーに委譲する。</summary>
    protected UniTask AnimateBlendAsync(float from, float to, float duration, CancellationToken ct)
    {
        return AnimateBlendAsync(from, to, duration, CurrentConfig.Ease, value =>
        {
            _currentBlend = value;
            Apply(_currentBlend);
        }, ct);
    }

    protected void EnsureInitialized()
    {
        if (_volume) return;
        if (!_owner) throw new InvalidOperationException($"{GetType().Name} の owner が設定されていません。");

        _volume = _owner.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 100f;
        _volume.weight = 0f;

        _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        _runtimeProfile.name = $"{GetType().Name}RuntimeVolumeProfile";
        _volume.sharedProfile = null;
        _volume.profile = _runtimeProfile;

        _component = GetOrAddOverride<TComponent>();
        InitializeComponent(_component);
        ResetVolumeState();
    }

    private void Apply(float blend)
    {
        if (!_volume) return;

        _volume.weight = CurrentConfig.VolumeWeight * blend;
        ApplyComponent(_component, CurrentConfig, blend);
    }

    private void ResetVolumeState()
    {
        _volume.weight = 0f;
        ResetComponent(_component);
    }

    private T GetOrAddOverride<T>() where T : VolumeComponent
    {
        if (_runtimeProfile.TryGet<T>(out var component))
        {
            component.active = true;
            SetOverrideStates(component);
            return component;
        }

        component = _runtimeProfile.Add<T>(true);
        component.active = true;
        SetOverrideStates(component);
        return component;
    }

    private static void SetOverrideStates(VolumeComponent component)
    {
        foreach (var parameter in component.parameters)
        {
            parameter.overrideState = true;
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_runtimeProfile)
        {
            UnityEngine.Object.Destroy(_runtimeProfile);
            _runtimeProfile = null;
        }
    }
}
