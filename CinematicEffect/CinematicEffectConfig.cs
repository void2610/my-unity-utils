using System;
using LitMotion;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public abstract class CinematicEffectConfig
{
    public abstract CinematicEffectConfig Clone();
}

[Serializable]
public abstract class TimedEffectConfig : CinematicEffectConfig
{
    [SerializeField] private float enterDuration = 0.3f;
    [SerializeField] private float exitDuration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    public float EnterDuration => enterDuration;
    public float ExitDuration => exitDuration;
    public Ease Ease => ease;

    protected TimedEffectConfig() { }

    protected TimedEffectConfig(float enterDuration, float exitDuration, Ease ease)
    {
        this.enterDuration = enterDuration;
        this.exitDuration = exitDuration;
        this.ease = ease;
    }
}

[Serializable]
public abstract class HoldableEffectConfig : TimedEffectConfig
{
    [SerializeField] private float holdDuration;
    [SerializeField] private bool autoComplete;

    public float HoldDuration => holdDuration;
    public bool AutoComplete => autoComplete;

    protected HoldableEffectConfig() { }

    protected HoldableEffectConfig(float enterDuration, float exitDuration, Ease ease, float holdDuration, bool autoComplete)
        : base(enterDuration, exitDuration, ease)
    {
        this.holdDuration = holdDuration;
        this.autoComplete = autoComplete;
    }
}

[Serializable]
public sealed class CameraShakeConfig : CinematicEffectConfig
{
    [SerializeField] private float magnitude = 0.1f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private int frequency = 10;
    [SerializeField] private bool loop = true;

    public float Magnitude => magnitude;
    public float Duration => duration;
    public int Frequency => frequency;
    public bool Loop => loop;

    public CameraShakeConfig() { }

    public CameraShakeConfig(float magnitude, float duration, bool loop, int frequency = 10)
    {
        this.magnitude = magnitude;
        this.duration = duration;
        this.frequency = frequency;
        this.loop = loop;
    }

    public override CinematicEffectConfig Clone() => new CameraShakeConfig(magnitude, duration, loop, frequency);
}

[Serializable]
public sealed class LetterboxConfig : TimedEffectConfig
{
    [SerializeField] private float barHeight = 80f;

    public float BarHeight => barHeight;

    public LetterboxConfig() { }

    public LetterboxConfig(float barHeight, float duration, Ease ease)
        : base(duration, duration, ease)
    {
        this.barHeight = barHeight;
    }

    public LetterboxConfig(float barHeight, float enterDuration, float exitDuration, Ease ease)
        : base(enterDuration, exitDuration, ease)
    {
        this.barHeight = barHeight;
    }

    public override CinematicEffectConfig Clone() => new LetterboxConfig(barHeight, EnterDuration, ExitDuration, Ease);
}

[Serializable]
public sealed class ScreenFadeConfig : HoldableEffectConfig
{
    [SerializeField] private Color fadeColor = Color.black;

    public Color FadeColor => fadeColor;

    public ScreenFadeConfig() { }

    public ScreenFadeConfig(Color fadeColor, float fadeInDuration, float fadeOutDuration, float holdDuration, Ease ease, bool autoComplete)
        : base(fadeInDuration, fadeOutDuration, ease, holdDuration, autoComplete)
    {
        this.fadeColor = fadeColor;
    }

    public override CinematicEffectConfig Clone() => new ScreenFadeConfig(fadeColor, EnterDuration, ExitDuration, HoldDuration, Ease, AutoComplete);
}

[Serializable]
public sealed class CameraDisorientationConfig : TimedEffectConfig
{
    [SerializeField] private Vector3 positionOffset = new(0f, -0.8f, 0f);
    [SerializeField] private float rotationZ = -4f;
    [SerializeField] private float orthographicSizeScale = 0.82f;
    [SerializeField] private float perspectiveFovDelta = -12f;
    [SerializeField] private float swayPositionAmplitude = 0.05f;
    [SerializeField] private float swayRotationAmplitude = 2.5f;
    [SerializeField] private float swayFrequency = 1.25f;

    /// <summary>
    /// Sway の波形イージング。1周期を4分割し、各クォーターにこのEaseを適用する。
    /// InOutSine が sin 波に相当。EaseInElastic などでオーバーシュートも表現できる。
    /// </summary>
    [SerializeField] private Ease swayEase = Ease.InOutSine;

    public Vector3 PositionOffset => positionOffset;
    public float RotationZ => rotationZ;
    public float OrthographicSizeScale => orthographicSizeScale;
    public float PerspectiveFovDelta => perspectiveFovDelta;
    public float SwayPositionAmplitude => swayPositionAmplitude;
    public float SwayRotationAmplitude => swayRotationAmplitude;
    public float SwayFrequency => swayFrequency;
    public Ease SwayEase => swayEase;

    public CameraDisorientationConfig() { }

    public CameraDisorientationConfig(
        Vector3 positionOffset,
        float rotationZ,
        float orthographicSizeScale,
        float perspectiveFovDelta,
        float swayPositionAmplitude,
        float swayRotationAmplitude,
        float swayFrequency,
        float enterDuration,
        float exitDuration,
        Ease ease = Ease.InOutSine,
        Ease swayEase = Ease.InOutSine)
        : base(enterDuration, exitDuration, ease)
    {
        this.positionOffset = positionOffset;
        this.rotationZ = rotationZ;
        this.orthographicSizeScale = orthographicSizeScale;
        this.perspectiveFovDelta = perspectiveFovDelta;
        this.swayPositionAmplitude = swayPositionAmplitude;
        this.swayRotationAmplitude = swayRotationAmplitude;
        this.swayFrequency = swayFrequency;
        this.swayEase = swayEase;
    }

    public override CinematicEffectConfig Clone() => new CameraDisorientationConfig(positionOffset, rotationZ, orthographicSizeScale, perspectiveFovDelta, swayPositionAmplitude, swayRotationAmplitude, swayFrequency, EnterDuration, ExitDuration, Ease, swayEase);
}

[Serializable]
public abstract class VolumeEffectConfig : TimedEffectConfig
{
    [SerializeField] private float volumeWeight = 1f;

    public float VolumeWeight => volumeWeight;

    protected VolumeEffectConfig() { }

    protected VolumeEffectConfig(float volumeWeight, float enterDuration, float exitDuration, Ease ease)
        : base(enterDuration, exitDuration, ease)
    {
        this.volumeWeight = volumeWeight;
    }
}

[Serializable]
public sealed class VignetteConfig : VolumeEffectConfig
{
    [SerializeField] private float intensity = 0.3f;
    [SerializeField] private float smoothness = 0.7f;

    public float Intensity => intensity;
    public float Smoothness => smoothness;

    public VignetteConfig() { }

    public VignetteConfig(float volumeWeight, float intensity, float smoothness, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.intensity = intensity;
        this.smoothness = smoothness;
    }

    public override CinematicEffectConfig Clone() => new VignetteConfig(VolumeWeight, intensity, smoothness, EnterDuration, ExitDuration, Ease);
}

[Serializable]
public sealed class ChromaticAberrationConfig : VolumeEffectConfig
{
    [SerializeField] private float intensity = 0.2f;

    public float Intensity => intensity;

    public ChromaticAberrationConfig() { }

    public ChromaticAberrationConfig(float volumeWeight, float intensity, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.intensity = intensity;
    }

    public override CinematicEffectConfig Clone() => new ChromaticAberrationConfig(VolumeWeight, intensity, EnterDuration, ExitDuration, Ease);
}

[Serializable]
public sealed class LensDistortionConfig : VolumeEffectConfig
{
    [SerializeField] private float intensity = -0.2f;

    public float Intensity => intensity;

    public LensDistortionConfig() { }

    public LensDistortionConfig(float volumeWeight, float intensity, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.intensity = intensity;
    }

    public override CinematicEffectConfig Clone() => new LensDistortionConfig(VolumeWeight, intensity, EnterDuration, ExitDuration, Ease);
}

[Serializable]
public sealed class FilmGrainConfig : VolumeEffectConfig
{
    [SerializeField] private float intensity = 0.25f;
    [SerializeField] private FilmGrainLookup type = FilmGrainLookup.Thin1;

    public float Intensity => intensity;
    public FilmGrainLookup Type => type;

    public FilmGrainConfig() { }

    public FilmGrainConfig(float volumeWeight, float intensity, FilmGrainLookup type, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.intensity = intensity;
        this.type = type;
    }

    public override CinematicEffectConfig Clone() => new FilmGrainConfig(VolumeWeight, intensity, type, EnterDuration, ExitDuration, Ease);
}

/// <summary>コントラスト調整エフェクトの設定。</summary>
[Serializable]
public sealed class ContrastConfig : VolumeEffectConfig
{
    [SerializeField] private float contrast;

    public float Contrast => contrast;

    public ContrastConfig() { }

    public ContrastConfig(float volumeWeight, float contrast, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.contrast = contrast;
    }

    public override CinematicEffectConfig Clone() => new ContrastConfig(VolumeWeight, contrast, EnterDuration, ExitDuration, Ease);
}

/// <summary>彩度調整エフェクトの設定。</summary>
[Serializable]
public sealed class SaturationConfig : VolumeEffectConfig
{
    [SerializeField] private float saturation;

    public float Saturation => saturation;

    public SaturationConfig() { }

    public SaturationConfig(float volumeWeight, float saturation, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.saturation = saturation;
    }

    public override CinematicEffectConfig Clone() => new SaturationConfig(VolumeWeight, saturation, EnterDuration, ExitDuration, Ease);
}

/// <summary>カラーフィルターエフェクトの設定。</summary>
[Serializable]
public sealed class ColorFilterConfig : VolumeEffectConfig
{
    [SerializeField] private Color filterColor = Color.white;

    public Color FilterColor => filterColor;

    public ColorFilterConfig() { }

    public ColorFilterConfig(float volumeWeight, Color filterColor, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.filterColor = filterColor;
    }

    public override CinematicEffectConfig Clone() => new ColorFilterConfig(VolumeWeight, filterColor, EnterDuration, ExitDuration, Ease);
}

/// <summary>脈動ビネット演出（動悸感）の設定。</summary>
[Serializable]
public sealed class PulseVignetteConfig : VolumeEffectConfig
{
    [SerializeField] private float baseIntensity = 0.35f;
    [SerializeField] private float pulseAmplitude = 0.18f;
    [SerializeField] private float pulseFrequency = 1.2f;
    [SerializeField] private float smoothness = 0.75f;

    public float BaseIntensity => baseIntensity;
    public float PulseAmplitude => pulseAmplitude;
    public float PulseFrequency => pulseFrequency;
    public float Smoothness => smoothness;

    public PulseVignetteConfig() { }

    public PulseVignetteConfig(float volumeWeight, float baseIntensity, float pulseAmplitude, float pulseFrequency, float smoothness, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.baseIntensity = baseIntensity;
        this.pulseAmplitude = pulseAmplitude;
        this.pulseFrequency = pulseFrequency;
        this.smoothness = smoothness;
    }

    public override CinematicEffectConfig Clone() => new PulseVignetteConfig(VolumeWeight, baseIntensity, pulseAmplitude, pulseFrequency, smoothness, EnterDuration, ExitDuration, Ease);
}

/// <summary>波打ち画面歪み演出の設定。</summary>
[Serializable]
public sealed class WaveDistortionConfig : TimedEffectConfig
{
    [SerializeField] private float frequency = 8f;
    [SerializeField] private float amplitude = 0.005f;
    [SerializeField] private float speed = 2f;

    public float Frequency => frequency;
    public float Amplitude => amplitude;
    public float Speed => speed;

    public WaveDistortionConfig() { }

    public WaveDistortionConfig(float frequency, float amplitude, float speed, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(enterDuration, exitDuration, ease)
    {
        this.frequency = frequency;
        this.amplitude = amplitude;
        this.speed = speed;
    }

    public override CinematicEffectConfig Clone() => new WaveDistortionConfig(frequency, amplitude, speed, EnterDuration, ExitDuration, Ease);
}

/// <summary>フィルムノイズ演出の設定。</summary>
[Serializable]
public sealed class FilmNoiseConfig : VolumeEffectConfig
{
    [SerializeField] private float intensity = 0.674f;
    [SerializeField] private int scratchFreq = 50;
    [SerializeField] private int speed = 80;
    [SerializeField] private int noiseFreq = 100;
    [SerializeField] private float threshold = 0.1f;
    [SerializeField] private Color noiseColor = Color.white;
    [SerializeField] private Texture2D filmDartTexture;
    [SerializeField] private int flipWidth = 185;
    [SerializeField] private int flipHeight = 1;
    [SerializeField] private float flipSec = 0.2f;

    public float Intensity => intensity;
    public int ScratchFreq => scratchFreq;
    public int Speed => speed;
    public int NoiseFreq => noiseFreq;
    public float Threshold => threshold;
    public Color NoiseColor => noiseColor;
    public Texture2D FilmDartTexture => filmDartTexture;
    public int FlipWidth => flipWidth;
    public int FlipHeight => flipHeight;
    public float FlipSec => flipSec;

    public FilmNoiseConfig() { }

    public FilmNoiseConfig(
        float volumeWeight,
        float intensity,
        int scratchFreq,
        int speed,
        int noiseFreq,
        float threshold,
        Color noiseColor,
        Texture2D filmDartTexture,
        int flipWidth,
        int flipHeight,
        float flipSec,
        float enterDuration,
        float exitDuration,
        Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.intensity = intensity;
        this.scratchFreq = scratchFreq;
        this.speed = speed;
        this.noiseFreq = noiseFreq;
        this.threshold = threshold;
        this.noiseColor = noiseColor;
        this.filmDartTexture = filmDartTexture;
        this.flipWidth = flipWidth;
        this.flipHeight = flipHeight;
        this.flipSec = flipSec;
    }

    public override CinematicEffectConfig Clone() => new FilmNoiseConfig(
        VolumeWeight,
        intensity,
        scratchFreq,
        speed,
        noiseFreq,
        threshold,
        noiseColor,
        filmDartTexture,
        flipWidth,
        flipHeight,
        flipSec,
        EnterDuration,
        ExitDuration,
        Ease);
}

[Serializable]
public sealed class DepthOfFieldConfig : VolumeEffectConfig
{
    [SerializeField] private float focusDistance = 0.1f;

    public float FocusDistance => focusDistance;

    public DepthOfFieldConfig() { }

    public DepthOfFieldConfig(float volumeWeight, float focusDistance, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.focusDistance = focusDistance;
    }

    public override CinematicEffectConfig Clone() => new DepthOfFieldConfig(VolumeWeight, focusDistance, EnterDuration, ExitDuration, Ease);
}

/// <summary>スプライト画像を使ったフラッシュ/暗転演出の設定。</summary>
[Serializable]
public sealed class ImageFlashConfig : TimedEffectConfig
{
    [SerializeField] private Color tintColor = Color.white;
    [SerializeField] private float holdDuration;

    public Color TintColor => tintColor;
    public float HoldDuration => holdDuration;

    public ImageFlashConfig() { }

    public ImageFlashConfig(Color tintColor, float enterDuration, float exitDuration, float holdDuration, Ease ease)
        : base(enterDuration, exitDuration, ease)
    {
        this.tintColor = tintColor;
        this.holdDuration = holdDuration;
    }

    public override CinematicEffectConfig Clone() => new ImageFlashConfig(tintColor, EnterDuration, ExitDuration, holdDuration, Ease);
}

/// <summary>断続的な暗転（明滅ループ）演出の設定。</summary>
[Serializable]
public sealed class BlinkConfig : TimedEffectConfig
{
    [SerializeField] private Color blinkColor = Color.black;
    [SerializeField] private float holdDuration = 0.1f;
    [SerializeField] private float intervalDuration = 0.5f;

    public Color BlinkColor => blinkColor;

    /// <summary>暗転状態を保持する時間（秒）。</summary>
    public float HoldDuration => holdDuration;

    /// <summary>明るい状態を保持するインターバル時間（秒）。</summary>
    public float IntervalDuration => intervalDuration;

    public BlinkConfig() { }

    public BlinkConfig(Color blinkColor, float holdDuration, float intervalDuration, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(enterDuration, exitDuration, ease)
    {
        this.blinkColor = blinkColor;
        this.holdDuration = holdDuration;
        this.intervalDuration = intervalDuration;
    }

    public override CinematicEffectConfig Clone() => new BlinkConfig(blinkColor, holdDuration, intervalDuration, EnterDuration, ExitDuration, Ease);
}

/// <summary>パーリンノイズを使ったゆっくりとした滑らかなカメラ揺れ演出の設定。</summary>
[Serializable]
public sealed class CameraPerlinShakeConfig : TimedEffectConfig
{
    [SerializeField] private float magnitude = 0.1f;
    [SerializeField] private float speed = 1.0f;

    /// <summary>揺れの大きさ（ワールド単位）。</summary>
    public float Magnitude => magnitude;

    /// <summary>パーリンノイズのサンプリング速度。小さいほどゆっくり。</summary>
    public float Speed => speed;

    public CameraPerlinShakeConfig() { }

    public CameraPerlinShakeConfig(float magnitude, float speed)
    {
        this.magnitude = magnitude;
        this.speed = speed;
    }

    public override CinematicEffectConfig Clone() => new CameraPerlinShakeConfig(magnitude, speed);
}

/// <summary>DoF Bokeh ぼかし ON/OFF ループによるめまい演出の設定。</summary>
[Serializable]
public sealed class DoFDizzinessConfig : VolumeEffectConfig
{
    [SerializeField] private float focusDistance = 0.1f;
    [SerializeField] private float transitionDuration = 0.15f;
    [SerializeField] private float holdBlur = 0.3f;
    [SerializeField] private float holdClear = 0.5f;

    public float FocusDistance => focusDistance;
    public float TransitionDuration => transitionDuration;
    public float HoldBlur => holdBlur;
    public float HoldClear => holdClear;

    public DoFDizzinessConfig() { }

    public DoFDizzinessConfig(float volumeWeight, float focusDistance,
        float transitionDuration, float holdBlur, float holdClear,
        float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.focusDistance = focusDistance;
        this.transitionDuration = transitionDuration;
        this.holdBlur = holdBlur;
        this.holdClear = holdClear;
    }

    public override CinematicEffectConfig Clone() => new DoFDizzinessConfig(
        VolumeWeight, focusDistance,
        transitionDuration, holdBlur, holdClear, EnterDuration, ExitDuration, Ease);
}

/// <summary>放射状 (ズーム) ブラー演出の設定。</summary>
[Serializable]
public sealed class RadialBlurConfig : TimedEffectConfig
{
    [SerializeField] private float strength = 0.18f;
    [SerializeField] private Vector2 center = new(0.5f, 0.5f);

    public float Strength => strength;
    public Vector2 Center => center;

    public RadialBlurConfig() { }

    public RadialBlurConfig(float strength, Vector2 center, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(enterDuration, exitDuration, ease)
    {
        this.strength = strength;
        this.center = center;
    }

    public override CinematicEffectConfig Clone() => new RadialBlurConfig(strength, center, EnterDuration, ExitDuration, Ease);
}

/// <summary>彩度・カラーフィルター・コントラスト・露出をまとめて制御する汎用カラーグレード演出の設定。</summary>
[Serializable]
public sealed class ColorGradeConfig : VolumeEffectConfig
{
    [SerializeField] private float saturation = -40f;
    [SerializeField] private Color colorFilter = new(1f, 0.85f, 0.65f, 1f);
    [SerializeField] private float contrast = 10f;
    [SerializeField] private float postExposure;

    /// <summary>彩度（-100..100）。</summary>
    public float Saturation => saturation;

    /// <summary>乗算カラーフィルター。白でフィルターなし。</summary>
    public Color ColorFilter => colorFilter;

    /// <summary>コントラスト（-100..100）。</summary>
    public float Contrast => contrast;

    /// <summary>露出補正（EV。0 で変化なし）。</summary>
    public float PostExposure => postExposure;

    public ColorGradeConfig() { }

    public ColorGradeConfig(float volumeWeight, float saturation, Color colorFilter, float contrast, float postExposure, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(volumeWeight, enterDuration, exitDuration, ease)
    {
        this.saturation = saturation;
        this.colorFilter = colorFilter;
        this.contrast = contrast;
        this.postExposure = postExposure;
    }

    public override CinematicEffectConfig Clone() => new ColorGradeConfig(VolumeWeight, saturation, colorFilter, contrast, postExposure, EnterDuration, ExitDuration, Ease);
}

/// <summary>回想（フラッシュバック）を表す複合演出の設定。カラーグレード＋フィルムグレイン＋ビネット＋白フラッシュを束ねる。</summary>
[Serializable]
public sealed class FlashbackConfig : TimedEffectConfig
{
    [SerializeField] private float holdDuration;
    [SerializeField] private bool flashOnEnter = true;
    [SerializeField] private float saturation = -55f;
    [SerializeField] private Color colorFilter = new(1f, 0.82f, 0.6f, 1f);
    [SerializeField] private float contrast = 12f;
    [SerializeField] private float postExposure = 0.15f;
    [SerializeField] private float grainIntensity = 0.5f;
    [SerializeField] private float vignetteIntensity = 0.38f;

    /// <summary>グレード維持時間（秒）。0 なら Stop まで持続する。</summary>
    public float HoldDuration => holdDuration;

    /// <summary>開始時に白フラッシュを差し込むか。</summary>
    public bool FlashOnEnter => flashOnEnter;

    public float Saturation => saturation;
    public Color ColorFilter => colorFilter;
    public float Contrast => contrast;
    public float PostExposure => postExposure;
    public float GrainIntensity => grainIntensity;
    public float VignetteIntensity => vignetteIntensity;

    public FlashbackConfig() { }

    public FlashbackConfig(float holdDuration, bool flashOnEnter, float saturation, Color colorFilter, float contrast, float postExposure, float grainIntensity, float vignetteIntensity, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(enterDuration, exitDuration, ease)
    {
        this.holdDuration = holdDuration;
        this.flashOnEnter = flashOnEnter;
        this.saturation = saturation;
        this.colorFilter = colorFilter;
        this.contrast = contrast;
        this.postExposure = postExposure;
        this.grainIntensity = grainIntensity;
        this.vignetteIntensity = vignetteIntensity;
    }

    public override CinematicEffectConfig Clone() => new FlashbackConfig(holdDuration, flashOnEnter, saturation, colorFilter, contrast, postExposure, grainIntensity, vignetteIntensity, EnterDuration, ExitDuration, Ease);
}

/// <summary>フルスクリーンシェーダーで視界を歪ませる（陽炎・酩酊感）演出の設定。</summary>
[Serializable]
public sealed class VisionWarpConfig : TimedEffectConfig
{
    [SerializeField] private float strength = 0.02f;
    [SerializeField] private float frequency = 12f;
    [SerializeField] private float speed = 2f;

    /// <summary>UV 変位の最大量（画面比。0.02 程度で顕著）。</summary>
    public float Strength => strength;

    /// <summary>歪みの空間周波数（大きいほど細かい波）。</summary>
    public float Frequency => frequency;

    /// <summary>歪みの時間変化速度。</summary>
    public float Speed => speed;

    public VisionWarpConfig() { }

    public VisionWarpConfig(float strength, float frequency, float speed, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(enterDuration, exitDuration, ease)
    {
        this.strength = strength;
        this.frequency = frequency;
        this.speed = speed;
    }

    public override CinematicEffectConfig Clone() => new VisionWarpConfig(strength, frequency, speed, EnterDuration, ExitDuration, Ease);
}
