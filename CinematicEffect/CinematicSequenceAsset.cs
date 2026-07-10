using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 映画的演出シーケンスをアセットとして保存できる ScriptableObject。
/// <see cref="Build"/> で実行可能な <see cref="CinematicSequence"/> に変換できる。
/// </summary>
[CreateAssetMenu(fileName = "NewCinematicSequence", menuName = "Cinematic/Sequence Asset")]
public class CinematicSequenceAsset : ScriptableObject
{
    /// <summary>操作対象のエフェクト種別。値はシリアライズされる asset の effect 値と対応するため、削除・並べ替えで既存値をズラさないよう明示的に固定する。19 は削除済み SaturationPulse の欠番。</summary>
    public enum EffectKind
    {
        [InspectorName("レターボックス")] Letterbox = 0,
        [InspectorName("スクリーンフェード")] ScreenFade = 1,
        [InspectorName("カメラ振動")] CameraShake = 2,
        [InspectorName("カメラ歪み")] CameraDisorientation = 3,
        [InspectorName("ビネット")] Vignette = 4,
        [InspectorName("色収差")] ChromaticAberration = 5,
        [InspectorName("レンズ歪み")] LensDistortion = 6,
        [InspectorName("フィルムグレイン")] FilmGrain = 7,
        [InspectorName("フィルムノイズ")] FilmNoise = 8,
        [InspectorName("コントラスト")] Contrast = 9,
        [InspectorName("彩度")] Saturation = 10,
        [InspectorName("カラーフィルター")] ColorFilter = 11,
        [InspectorName("被写界深度")] DepthOfField = 12,
        [InspectorName("フラッシュ")] ImageFlash = 13,
        [InspectorName("めまい")] DoFDizziness = 14,
        [InspectorName("脈動ビネット")] PulseVignette = 15,
        [InspectorName("波打ち歪み")] WaveDistortion = 16,
        [InspectorName("明滅")] Blink = 17,
        [InspectorName("カメラ揺れ（パーリン）")] CameraPerlinShake = 18,
        [InspectorName("カラーグレード")] ColorGrade = 20,
        [InspectorName("フラッシュバック")] Flashback = 21,
        [InspectorName("視界歪み")] VisionWarp = 22,
    }

    /// <summary>インスペクターからシリアライズ可能な演出ステップ。</summary>
    [Serializable]
    public class Step
    {
        [SerializeField] public CinematicSequence.StepKind kind = CinematicSequence.StepKind.PlayAndAwait;
        [SerializeField] public EffectKind effect;
        [SerializeField] public float delaySeconds = 1f;
        [SerializeField] public bool useCustomConfig;

        // エフェクト別設定（PropertyDrawer で対応するものだけ表示）
        [SerializeField] public LetterboxConfig letterboxConfig = new();
        [SerializeField] public ScreenFadeConfig screenFadeConfig = new();
        [SerializeField] public CameraShakeConfig cameraShakeConfig = new();
        [SerializeField] public CameraDisorientationConfig cameraDisorientationConfig = new();
        [SerializeField] public VignetteConfig vignetteConfig = new();
        [SerializeField] public ChromaticAberrationConfig chromaticAberrationConfig = new();
        [SerializeField] public LensDistortionConfig lensDistortionConfig = new();
        [SerializeField] public FilmGrainConfig filmGrainConfig = new();
        [SerializeField] public FilmNoiseConfig filmNoiseConfig = new();
        [SerializeField] public ContrastConfig contrastConfig = new();
        [SerializeField] public SaturationConfig saturationConfig = new();
        [SerializeField] public ColorFilterConfig colorFilterConfig = new();
        [SerializeField] public DepthOfFieldConfig depthOfFieldConfig = new();
        [SerializeField] public ImageFlashConfig imageFlashConfig = new();
        [SerializeField] public DoFDizzinessConfig doFDizzinessConfig = new();
        [SerializeField] public PulseVignetteConfig pulseVignetteConfig = new();
        [SerializeField] public WaveDistortionConfig waveDistortionConfig = new();
        [SerializeField] public BlinkConfig blinkConfig = new();
        [SerializeField] public CameraPerlinShakeConfig cameraPerlinShakeConfig = new();
        [SerializeField] public ColorGradeConfig colorGradeConfig = new();
        [SerializeField] public FlashbackConfig flashbackConfig = new();
        [SerializeField] public VisionWarpConfig visionWarpConfig = new();

        /// <summary>
        /// 選択中のエフェクト設定を返す。<see cref="useCustomConfig"/> が false の場合は null。
        /// </summary>
        public CinematicEffectConfig GetActiveConfig()
        {
            if (!useCustomConfig)
            {
                return null;
            }

            return effect switch
            {
                EffectKind.Letterbox => letterboxConfig,
                EffectKind.ScreenFade => screenFadeConfig,
                EffectKind.CameraShake => cameraShakeConfig,
                EffectKind.CameraDisorientation => cameraDisorientationConfig,
                EffectKind.Vignette => vignetteConfig,
                EffectKind.ChromaticAberration => chromaticAberrationConfig,
                EffectKind.LensDistortion => lensDistortionConfig,
                EffectKind.FilmGrain => filmGrainConfig,
                EffectKind.FilmNoise => filmNoiseConfig,
                EffectKind.Contrast => contrastConfig,
                EffectKind.Saturation => saturationConfig,
                EffectKind.ColorFilter => colorFilterConfig,
                EffectKind.DepthOfField => depthOfFieldConfig,
                EffectKind.ImageFlash => imageFlashConfig,
                EffectKind.DoFDizziness => doFDizzinessConfig,
                EffectKind.PulseVignette => pulseVignetteConfig,
                EffectKind.WaveDistortion => waveDistortionConfig,
                EffectKind.Blink => blinkConfig,
                EffectKind.CameraPerlinShake => cameraPerlinShakeConfig,
                EffectKind.ColorGrade => colorGradeConfig,
                EffectKind.Flashback => flashbackConfig,
                EffectKind.VisionWarp => visionWarpConfig,
                _ => null,
            };
        }

        /// <summary>選択中のエフェクトの <see cref="System.Type"/> を返す。</summary>
        public Type GetEffectSystemType()
        {
            return effect switch
            {
                EffectKind.Letterbox => typeof(LetterboxEffect),
                EffectKind.ScreenFade => typeof(ScreenFadeEffect),
                EffectKind.CameraShake => typeof(CameraShakeEffect),
                EffectKind.CameraDisorientation => typeof(CameraDisorientationEffect),
                EffectKind.Vignette => typeof(VignetteEffect),
                EffectKind.ChromaticAberration => typeof(ChromaticAberrationEffect),
                EffectKind.LensDistortion => typeof(LensDistortionEffect),
                EffectKind.FilmGrain => typeof(FilmGrainEffect),
                EffectKind.FilmNoise => typeof(FilmNoiseEffect),
                EffectKind.Contrast => typeof(ContrastEffect),
                EffectKind.Saturation => typeof(SaturationEffect),
                EffectKind.ColorFilter => typeof(ColorFilterEffect),
                EffectKind.DepthOfField => typeof(DepthOfFieldEffect),
                EffectKind.ImageFlash => typeof(ImageFlashEffect),
                EffectKind.DoFDizziness => typeof(DoFDizzinessEffect),
                EffectKind.PulseVignette => typeof(PulseVignetteEffect),
                EffectKind.WaveDistortion => typeof(WaveDistortionEffect),
                EffectKind.Blink => typeof(BlinkEffect),
                EffectKind.CameraPerlinShake => typeof(CameraPerlinShakeEffect),
                EffectKind.ColorGrade => typeof(ColorGradeEffect),
                EffectKind.Flashback => typeof(FlashbackEffect),
                EffectKind.VisionWarp => typeof(VisionWarpEffect),
                _ => throw new InvalidOperationException($"不明なエフェクト種別: {effect}"),
            };
        }
    }

    [SerializeField] public List<Step> steps = new();

    /// <summary>このアセットの内容から <see cref="CinematicSequence"/> を構築して返す。</summary>
    public CinematicSequence Build()
    {
        var seq = CinematicSequence.Create();
        foreach (var step in steps)
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
}
