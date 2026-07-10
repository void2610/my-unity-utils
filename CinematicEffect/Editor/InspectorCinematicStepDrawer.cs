using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="CinematicSequenceAsset.Step"/> のカスタム PropertyDrawer。
/// エフェクト種別に応じて対応する設定フィールドのみを条件表示する。
/// </summary>
[CustomPropertyDrawer(typeof(CinematicSequenceAsset.Step))]
public class CinematicSequenceStepDrawer : PropertyDrawer
{
    // 各プロパティ名の定数（SerializedProperty はフィールド名と一致させる）
    private const string PROP_KIND = "kind";
    private const string PROP_EFFECT = "effect";
    private const string PROP_DELAY_SECONDS = "delaySeconds";
    private const string PROP_USE_CUSTOM_CONFIG = "useCustomConfig";
    private const string PROP_LETTERBOX_CONFIG = "letterboxConfig";
    private const string PROP_SCREEN_FADE_CONFIG = "screenFadeConfig";
    private const string PROP_CAMERA_SHAKE_CONFIG = "cameraShakeConfig";
    private const string PROP_CAMERA_DISORIENTATION_CONFIG = "cameraDisorientationConfig";
    private const string PROP_VIGNETTE_CONFIG = "vignetteConfig";
    private const string PROP_CHROMATIC_ABERRATION_CONFIG = "chromaticAberrationConfig";
    private const string PROP_LENS_DISTORTION_CONFIG = "lensDistortionConfig";
    private const string PROP_FILM_GRAIN_CONFIG = "filmGrainConfig";
    private const string PROP_FILM_NOISE_CONFIG = "filmNoiseConfig";
    private const string PROP_CONTRAST_CONFIG = "contrastConfig";
    private const string PROP_SATURATION_CONFIG = "saturationConfig";
    private const string PROP_COLOR_FILTER_CONFIG = "colorFilterConfig";
    private const string PROP_DEPTH_OF_FIELD_CONFIG = "depthOfFieldConfig";
    private const string PROP_IMAGE_FLASH_CONFIG = "imageFlashConfig";
    private const string PROP_DOF_DIZZINESS_CONFIG = "doFDizzinessConfig";
    private const string PROP_PULSE_VIGNETTE_CONFIG = "pulseVignetteConfig";
    private const string PROP_WAVE_DISTORTION_CONFIG = "waveDistortionConfig";
    private const string PROP_BLINK_CONFIG = "blinkConfig";
    private const string PROP_CAMERA_PERLIN_SHAKE_CONFIG = "cameraPerlinShakeConfig";
    private const string PROP_SATURATION_PULSE_CONFIG = "saturationPulseConfig";
    private const string PROP_COLOR_GRADE_CONFIG = "colorGradeConfig";
    private const string PROP_FLASHBACK_CONFIG = "flashbackConfig";
    private const string PROP_VISION_WARP_CONFIG = "visionWarpConfig";

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var lineHeight = EditorGUIUtility.singleLineHeight;
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var lineStep = lineHeight + spacing;

        // Kind フィールド分
        var total = lineStep;

        var kind = (CinematicSequence.StepKind)property.FindPropertyRelative(PROP_KIND).enumValueIndex;
        if (kind == CinematicSequence.StepKind.Delay)
        {
            // DelaySeconds のみ
            total += lineStep;
        }
        else
        {
            // Effect + UseCustomConfig チェック
            total += lineStep;

            var useCustom = property.FindPropertyRelative(PROP_USE_CUSTOM_CONFIG).boolValue;
            if (useCustom)
            {
                // フォールドアウトヘッダー分
                total += lineStep;

                var configProp = GetActiveConfigProperty(property);
                if (configProp != null && configProp.isExpanded)
                {
                    total += MeasureConfigHeight(configProp);
                }
            }
        }

        return total - spacing;
    }

    /// <summary>選択中エフェクトに対応する config SerializedProperty を返す。</summary>
    private static SerializedProperty GetActiveConfigProperty(SerializedProperty property)
    {
        var effectProp = property.FindPropertyRelative(PROP_EFFECT);
        var effectKind = (CinematicSequenceAsset.EffectKind)effectProp.enumValueIndex;

        var propName = effectKind switch
        {
            CinematicSequenceAsset.EffectKind.Letterbox => PROP_LETTERBOX_CONFIG,
            CinematicSequenceAsset.EffectKind.ScreenFade => PROP_SCREEN_FADE_CONFIG,
            CinematicSequenceAsset.EffectKind.CameraShake => PROP_CAMERA_SHAKE_CONFIG,
            CinematicSequenceAsset.EffectKind.CameraDisorientation => PROP_CAMERA_DISORIENTATION_CONFIG,
            CinematicSequenceAsset.EffectKind.Vignette => PROP_VIGNETTE_CONFIG,
            CinematicSequenceAsset.EffectKind.ChromaticAberration => PROP_CHROMATIC_ABERRATION_CONFIG,
            CinematicSequenceAsset.EffectKind.LensDistortion => PROP_LENS_DISTORTION_CONFIG,
            CinematicSequenceAsset.EffectKind.FilmGrain => PROP_FILM_GRAIN_CONFIG,
            CinematicSequenceAsset.EffectKind.FilmNoise => PROP_FILM_NOISE_CONFIG,
            CinematicSequenceAsset.EffectKind.Contrast => PROP_CONTRAST_CONFIG,
            CinematicSequenceAsset.EffectKind.Saturation => PROP_SATURATION_CONFIG,
            CinematicSequenceAsset.EffectKind.ColorFilter => PROP_COLOR_FILTER_CONFIG,
            CinematicSequenceAsset.EffectKind.DepthOfField => PROP_DEPTH_OF_FIELD_CONFIG,
            CinematicSequenceAsset.EffectKind.ImageFlash => PROP_IMAGE_FLASH_CONFIG,
            CinematicSequenceAsset.EffectKind.DoFDizziness => PROP_DOF_DIZZINESS_CONFIG,
            CinematicSequenceAsset.EffectKind.PulseVignette => PROP_PULSE_VIGNETTE_CONFIG,
            CinematicSequenceAsset.EffectKind.WaveDistortion => PROP_WAVE_DISTORTION_CONFIG,
            CinematicSequenceAsset.EffectKind.Blink => PROP_BLINK_CONFIG,
            CinematicSequenceAsset.EffectKind.CameraPerlinShake => PROP_CAMERA_PERLIN_SHAKE_CONFIG,
            CinematicSequenceAsset.EffectKind.SaturationPulse => PROP_SATURATION_PULSE_CONFIG,
            CinematicSequenceAsset.EffectKind.ColorGrade => PROP_COLOR_GRADE_CONFIG,
            CinematicSequenceAsset.EffectKind.Flashback => PROP_FLASHBACK_CONFIG,
            CinematicSequenceAsset.EffectKind.VisionWarp => PROP_VISION_WARP_CONFIG,
            _ => null,
        };

        return propName != null ? property.FindPropertyRelative(propName) : null;
    }

    /// <summary>
    /// config の子プロパティの合計描画高さを返す。
    /// Vector2/Vector3 など複数行プロパティを正確に計測する。
    /// </summary>
    private static float MeasureConfigHeight(SerializedProperty configProp)
    {
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var total = 0f;
        var iterator = configProp.Copy();
        var end = iterator.GetEndProperty();
        var enterChildren = true;

        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;
            total += EditorGUI.GetPropertyHeight(iterator, true) + spacing;
        }

        return total;
    }

    /// <summary>config フィールドの子プロパティを実際の高さで描画する。</summary>
    private static void DrawConfigFields(ref Rect rect, SerializedProperty configProp)
    {
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var iterator = configProp.Copy();
        var end = iterator.GetEndProperty();
        var enterChildren = true;

        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;
            var propHeight = EditorGUI.GetPropertyHeight(iterator, true);
            var childRect = new Rect(rect.x + 12f, rect.y, rect.width - 12f, propHeight);
            EditorGUI.PropertyField(childRect, iterator, true);
            rect.y += propHeight + spacing;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var lineHeight = EditorGUIUtility.singleLineHeight;
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var lineStep = lineHeight + spacing;

        var rect = new Rect(position.x, position.y, position.width, lineHeight);

        // Kind（横並びボタン）
        var kindProp = property.FindPropertyRelative(PROP_KIND);
        EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, lineHeight), "Kind");
        var toolbarRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth, lineHeight);
        kindProp.enumValueIndex = GUI.Toolbar(toolbarRect, kindProp.enumValueIndex, kindProp.enumDisplayNames);
        rect.y += lineStep;

        var kind = (CinematicSequence.StepKind)kindProp.enumValueIndex;

        if (kind == CinematicSequence.StepKind.Delay)
        {
            // Delay ステップは DelaySeconds のみ表示
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(PROP_DELAY_SECONDS), new GUIContent("Delay (秒)"));
        }
        else
        {
            // Effect ドロップダウン + UseCustomConfig チェックボックス（同行右端）
            const float checkWidth = 16f;
            const float checkLabelWidth = 52f;
            const float checkTotalWidth = checkLabelWidth + checkWidth + 4f;

            var useCustomProp = property.FindPropertyRelative(PROP_USE_CUSTOM_CONFIG);
            var effectRect = new Rect(rect.x, rect.y, rect.width - checkTotalWidth, lineHeight);
            var checkLabelRect = new Rect(rect.x + rect.width - checkTotalWidth, rect.y, checkLabelWidth, lineHeight);
            var checkRect = new Rect(rect.x + rect.width - checkWidth, rect.y, checkWidth, lineHeight);

            EditorGUI.PropertyField(effectRect, property.FindPropertyRelative(PROP_EFFECT), new GUIContent("Effect"));
            EditorGUI.LabelField(checkLabelRect, new GUIContent("カスタム", "カスタム設定を使う"));
            useCustomProp.boolValue = EditorGUI.Toggle(checkRect, useCustomProp.boolValue);
            rect.y += lineStep;

            if (useCustomProp.boolValue)
            {
                var configProp = GetActiveConfigProperty(property);
                if (configProp != null)
                {
                    // フォールドアウトヘッダー（インデント付き）
                    var foldRect = new Rect(rect.x + 12f, rect.y, rect.width - 12f, lineHeight);
                    configProp.isExpanded = EditorGUI.Foldout(foldRect, configProp.isExpanded, "カスタム設定", true);
                    rect.y += lineStep;

                    if (configProp.isExpanded)
                    {
                        DrawConfigFields(ref rect, configProp);
                    }
                }
            }
        }

        EditorGUI.EndProperty();
    }
}
