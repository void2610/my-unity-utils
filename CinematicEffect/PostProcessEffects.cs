using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class VignetteEffect : PostProcessVolumeEffectBase<Vignette, VignetteConfig>
{
    public override string EffectName => "ビネット";

    public VignetteEffect(GameObject owner) : base(owner) { }

    protected override void ApplyComponent(Vignette component, VignetteConfig config, float blend)
    {
        component.intensity.value = config.Intensity * blend;
        component.smoothness.value = Mathf.Lerp(0.2f, config.Smoothness, blend);
    }

    protected override void ResetComponent(Vignette component)
    {
        component.intensity.value = 0f;
        component.smoothness.value = 0.2f;
    }
}

public sealed class ChromaticAberrationEffect : PostProcessVolumeEffectBase<ChromaticAberration, ChromaticAberrationConfig>
{
    public override string EffectName => "色収差";

    public ChromaticAberrationEffect(GameObject owner) : base(owner) { }

    protected override void ApplyComponent(ChromaticAberration component, ChromaticAberrationConfig config, float blend)
    {
        component.intensity.value = config.Intensity * blend;
    }

    protected override void ResetComponent(ChromaticAberration component)
    {
        component.intensity.value = 0f;
    }
}

public sealed class LensDistortionEffect : PostProcessVolumeEffectBase<LensDistortion, LensDistortionConfig>
{
    public override string EffectName => "レンズ歪み";

    public LensDistortionEffect(GameObject owner) : base(owner) { }

    protected override void ApplyComponent(LensDistortion component, LensDistortionConfig config, float blend)
    {
        component.intensity.value = config.Intensity * blend;
    }

    protected override void ResetComponent(LensDistortion component)
    {
        component.intensity.value = 0f;
    }
}

public sealed class FilmGrainEffect : PostProcessVolumeEffectBase<FilmGrain, FilmGrainConfig>
{
    public override string EffectName => "フィルムグレイン";

    public FilmGrainEffect(GameObject owner) : base(owner) { }

    protected override void InitializeComponent(FilmGrain component)
    {
        component.type.value = CurrentConfig.Type;
    }

    protected override void ApplyComponent(FilmGrain component, FilmGrainConfig config, float blend)
    {
        component.type.value = config.Type;
        component.intensity.value = config.Intensity * blend;
    }

    protected override void ResetComponent(FilmGrain component)
    {
        component.intensity.value = 0f;
    }
}

public sealed class FilmNoiseEffect : PostProcessVolumeEffectBase<FilmNoise, FilmNoiseConfig>
{
    public override string EffectName => "フィルムノイズ";

    private Texture2D _defaultFilmDartTexture;

    public FilmNoiseEffect(GameObject owner) : base(owner) { }

    protected override void InitializeComponent(FilmNoise component)
    {
        _defaultFilmDartTexture ??= Resources.Load<Texture2D>("Sprites/Background/Night");
    }

    protected override void ApplyComponent(FilmNoise component, FilmNoiseConfig config, float blend)
    {
        component.enabledEffect.value = blend > 0.0001f;
        component.intensity.value = config.Intensity * blend;
        component.scratchFreq.value = config.ScratchFreq;
        component.speed.value = config.Speed;
        component.noiseFreq.value = config.NoiseFreq;
        component.threshold.value = config.Threshold;
        component.noiseColor.value = config.NoiseColor;
        component.filmDartTexture.value = config.FilmDartTexture != null ? config.FilmDartTexture : _defaultFilmDartTexture;
        component.flipWidth.value = config.FlipWidth;
        component.flipHeight.value = config.FlipHeight;
        component.flipSec.value = config.FlipSec;
    }

    protected override void ResetComponent(FilmNoise component)
    {
        component.enabledEffect.value = false;
        component.intensity.value = 0f;
        component.scratchFreq.value = 50;
        component.speed.value = 80;
        component.noiseFreq.value = 100;
        component.threshold.value = 0.1f;
        component.noiseColor.value = Color.white;
        component.filmDartTexture.value = _defaultFilmDartTexture;
        component.flipWidth.value = 185;
        component.flipHeight.value = 1;
        component.flipSec.value = 0.2f;
    }
}

/// <summary>コントラストのみを制御する。彩度・フィルターは他のエフェクトに委ねる。</summary>
public sealed class ContrastEffect : PostProcessVolumeEffectBase<ColorAdjustments, ContrastConfig>
{
    public override string EffectName => "コントラスト";

    public ContrastEffect(GameObject owner) : base(owner) { }

    protected override void InitializeComponent(ColorAdjustments component)
    {
        // 自分が担当しないパラメータは Override しない
        component.saturation.overrideState = false;
        component.colorFilter.overrideState = false;
        component.postExposure.overrideState = false;
        component.hueShift.overrideState = false;
    }

    protected override void ApplyComponent(ColorAdjustments component, ContrastConfig config, float blend)
    {
        component.contrast.value = config.Contrast * blend;
    }

    protected override void ResetComponent(ColorAdjustments component)
    {
        component.contrast.value = 0f;
    }
}

/// <summary>彩度のみを制御する。コントラスト・フィルターは他のエフェクトに委ねる。</summary>
public sealed class SaturationEffect : PostProcessVolumeEffectBase<ColorAdjustments, SaturationConfig>
{
    public override string EffectName => "彩度";

    public SaturationEffect(GameObject owner) : base(owner) { }

    protected override void InitializeComponent(ColorAdjustments component)
    {
        component.contrast.overrideState = false;
        component.colorFilter.overrideState = false;
        component.postExposure.overrideState = false;
        component.hueShift.overrideState = false;
    }

    protected override void ApplyComponent(ColorAdjustments component, SaturationConfig config, float blend)
    {
        component.saturation.value = config.Saturation * blend;
    }

    protected override void ResetComponent(ColorAdjustments component)
    {
        component.saturation.value = 0f;
    }
}

/// <summary>カラーフィルターのみを制御する。コントラスト・彩度は他のエフェクトに委ねる。</summary>
public sealed class ColorFilterEffect : PostProcessVolumeEffectBase<ColorAdjustments, ColorFilterConfig>
{
    public override string EffectName => "カラーフィルター";

    public ColorFilterEffect(GameObject owner) : base(owner) { }

    protected override void InitializeComponent(ColorAdjustments component)
    {
        component.contrast.overrideState = false;
        component.saturation.overrideState = false;
        component.postExposure.overrideState = false;
        component.hueShift.overrideState = false;
    }

    protected override void ApplyComponent(ColorAdjustments component, ColorFilterConfig config, float blend)
    {
        component.colorFilter.value = Color.Lerp(Color.white, config.FilterColor, blend);
    }

    protected override void ResetComponent(ColorAdjustments component)
    {
        component.colorFilter.value = Color.white;
    }
}

/// <summary>
/// サイン波で Vignette Intensity を脈動させ、動悸感を表現するエフェクト。
/// Time.unscaledTime を使うため、TimeScale の影響を受けない。
/// </summary>
public sealed class PulseVignetteEffect : PostProcessVolumeEffectBase<Vignette, PulseVignetteConfig>
{
    public override string EffectName => "脈動ビネット";

    public PulseVignetteEffect(GameObject owner) : base(owner) { }

    protected override void ApplyComponent(Vignette component, PulseVignetteConfig config, float blend)
    {
        var pulse = Mathf.Sin(Time.unscaledTime * config.PulseFrequency * Mathf.PI * 2f) * config.PulseAmplitude;
        component.intensity.value = (config.BaseIntensity + pulse) * blend;
        component.smoothness.value = Mathf.Lerp(0.2f, config.Smoothness, blend);
    }

    protected override void ResetComponent(Vignette component)
    {
        component.intensity.value = 0f;
        component.smoothness.value = 0.2f;
    }
}

public sealed class DepthOfFieldEffect : PostProcessVolumeEffectBase<DepthOfField, DepthOfFieldConfig>
{
    public override string EffectName => "被写界深度";

    private const float DEFAULT_FOCUS_DISTANCE = 10f;
    private const float FOCAL_LENGTH = 300f;
    private const float APERTURE = 16f;

    public DepthOfFieldEffect(GameObject owner) : base(owner) { }

    protected override void InitializeComponent(DepthOfField component)
    {
        component.mode.value = DepthOfFieldMode.Bokeh;
        component.focusDistance.overrideState = true;
        component.focalLength.overrideState = true;
        component.aperture.overrideState = true;
        component.highQualitySampling.overrideState = true;
        component.highQualitySampling.value = true;
        component.focalLength.value = FOCAL_LENGTH;
        component.aperture.value = APERTURE;
    }

    protected override void ApplyComponent(DepthOfField component, DepthOfFieldConfig config, float blend)
    {
        component.mode.value = DepthOfFieldMode.Bokeh;
        component.focusDistance.value = Mathf.Lerp(DEFAULT_FOCUS_DISTANCE, config.FocusDistance, blend);
    }

    protected override void ResetComponent(DepthOfField component)
    {
        component.focusDistance.value = DEFAULT_FOCUS_DISTANCE;
    }
}

/// <summary>
/// Depth of Field（Bokeh）のぼかしを ON/OFF でループさせてめまいを表現する演出。
/// <see cref="PostProcessVolumeEffectBase{TComponent,TConfig}"/> を継承し、
/// Volume / VolumeProfile の管理は基底クラスに委譲する。
/// </summary>
public sealed class DoFDizzinessEffect : PostProcessVolumeEffectBase<DepthOfField, DoFDizzinessConfig>
{
    public override string EffectName => "めまい";

    private const float DEFAULT_FOCUS_DISTANCE = 10f;
    private const float FOCAL_LENGTH = 300f;
    private const float APERTURE = 16f;

    public DoFDizzinessEffect(GameObject owner) : base(owner) { }

    protected override void InitializeComponent(DepthOfField component)
    {
        component.mode.value = DepthOfFieldMode.Bokeh;
        component.focusDistance.overrideState = true;
        component.focalLength.overrideState = true;
        component.aperture.overrideState = true;
        component.highQualitySampling.overrideState = true;
        component.highQualitySampling.value = true;
        component.focalLength.value = FOCAL_LENGTH;
        component.aperture.value = APERTURE;
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        EnsureInitialized();

        // ぼかし ON/OFF をキャンセルされるまでループ
        while (!ct.IsCancellationRequested)
        {
            await AnimateBlendAsync(0f, 1f, CurrentConfig.TransitionDuration, ct);
            await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.HoldBlur), cancellationToken: ct);
            await AnimateBlendAsync(1f, 0f, CurrentConfig.TransitionDuration, ct);
            await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.HoldClear), cancellationToken: ct);
        }
    }

    protected override void ApplyComponent(DepthOfField component, DoFDizzinessConfig config, float blend)
    {
        component.mode.value = DepthOfFieldMode.Bokeh;
        component.focusDistance.value = Mathf.Lerp(DEFAULT_FOCUS_DISTANCE, config.FocusDistance, blend);
    }

    protected override void ResetComponent(DepthOfField component)
    {
        component.focusDistance.value = DEFAULT_FOCUS_DISTANCE;
    }
}
