using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

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

    private GameObject _owner;
    private Volume _volume;
    private VolumeProfile _runtimeProfile;
    private TComponent _component;
    private float _currentBlend;
    private readonly TConfig _defaultConfig = new();

    /// <summary>
    /// owner を省略すると <see cref="CinematicPostProcessHost"/> Singleton が初回再生時に
    /// 自動生成され、 シーン上の事前配置は不要になる。
    /// </summary>
    protected PostProcessVolumeEffectBase(GameObject owner = null)
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

    // 連続 Play では毎回ハードリセットせず、値を現在→ターゲットへ補間して基準値への飛びを防ぐ
    protected override bool ResetVisualsOnReplay => false;

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        EnsureInitialized();

        var startWeight = _volume.weight;
        // weight が残っている = 前の再生を引き継ぐ。weight は保ち値のみ補間 (無効時はベースから weight を立ち上げ値は即ターゲット)
        var carryOver = startWeight > 0.0001f;
        CaptureStart(_component);

        var targetWeight = CurrentConfig.VolumeWeight;
        await AnimateBlendAsync(0f, 1f, CurrentConfig.EnterDuration, CurrentConfig.Ease, t =>
        {
            _currentBlend = carryOver ? 1f : t;
            _volume.weight = carryOver ? targetWeight : Mathf.Lerp(startWeight, targetWeight, t);
            ApplyComponent(_component, CurrentConfig, carryOver ? t : 1f);
        }, ct);

        _currentBlend = 1f;
        while (!ct.IsCancellationRequested)
        {
            _volume.weight = targetWeight;
            ApplyComponent(_component, CurrentConfig, 1f);
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

    /// <summary>再生開始時の現在値を記録する。連続 Play で現在値からターゲットへ補間する派生が使う。</summary>
    protected virtual void CaptureStart(TComponent component) { }

    /// <summary>blend 値に応じて VolumeComponent のパラメータを適用する。</summary>
    protected abstract void ApplyComponent(TComponent component, TConfig config, float blend);

    /// <summary>VolumeComponent のパラメータをデフォルト値に戻す。</summary>
    protected abstract void ResetComponent(TComponent component);

    /// <summary>blend で weight と値の両方を駆動する 4 引数ラッパー。ON/OFF をループさせる派生 (DoF 眩暈等) が使う。</summary>
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
        // owner 未指定なら Host Singleton を lazy 解決 (シーン上の事前配置を不要にする)
        if (!_owner) _owner = CinematicPostProcessHost.Instance.gameObject;
        // URP カメラの renderPostProcessing が OFF だと Volume が画面に反映されないため、 初回再生時に強制 ON
        CinematicPostProcessHost.EnsureCameraPostProcessingEnabled();

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
