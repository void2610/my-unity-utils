using System.Collections.Generic;
using LitMotion;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// シネマティック演出のテスト再生・アセット書き出し/読み込みを行う専用ウィンドウ。
/// シーケンスデータはウィンドウ自身が保持し、プレイモード開始時にディレクターへ注入する。
/// </summary>
public class CinematicTestWindow : EditorWindow
{
    // シーケンスデータはウィンドウが保持（ドメインリロードを跨いで保持される）
    [SerializeField] private List<CinematicSequenceAsset.Step> windowSequence = new();
    // Ease.InOutSine の int 値（LitMotion.Ease enum の順序に依存）
    private const int EASE_IN_OUT_SINE_VALUE = (int)Ease.InOutSine;

    // ── ウィンドウ状態 ──────────────────────────────────────
    private CinematicEffectDirector _director;

    private SerializedObject _serializedWindow;
    private SerializedProperty _testSequenceProp;
    private ReorderableList _list;

    private Vector2 _scrollPos;
    private CinematicSequenceAsset _importSource;

    // ── 開き方 ─────────────────────────────────────────────

    [MenuItem("Window/Cinematic Test")]
    public static void Open() => GetWindow<CinematicTestWindow>("Cinematic Test");

    /// <summary>コンポーネントのインスペクターから対象を指定して開く。</summary>
    public static void Open(CinematicEffectDirector director)
    {
        var window = GetWindow<CinematicTestWindow>("Cinematic Test");
        window._director = director;
        window.Repaint();
    }

    // ── アセット読み込み / 上書き保存 ──────────────────

    private void ImportFromAsset(CinematicSequenceAsset asset)
    {
        if (asset == null)
        {
            return;
        }

        windowSequence = new List<CinematicSequenceAsset.Step>(asset.steps);

        // SerializedObject を再構築してリストに反映
        RebuildList();
        Repaint();

        // プレイ中はディレクターにも即時注入する
        if (Application.isPlaying)
        {
            TryFindDirector();
            _director?.InjectTestSequence(windowSequence);
        }

        ShowNotification(new GUIContent("読み込みました"));
    }

    /// <summary>現在のシーケンスを指定アセットに上書き保存する。</summary>
    private void OverwriteAsset(CinematicSequenceAsset asset)
    {
        asset.steps = new List<CinematicSequenceAsset.Step>(windowSequence);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        ShowNotification(new GUIContent("上書き保存しました"));
    }

    // ── ヘルパー ───────────────────────────────────────────

    /// <summary>シーンに Director が存在すれば自動で割り当てる。</summary>
    private void TryFindDirector()
    {
        if (_director == null)
        {
            _director = FindFirstObjectByType<CinematicEffectDirector>();
        }
    }

    private void RebuildList()
    {
        _list = null;
        _testSequenceProp = null;

        // ウィンドウ自身を SerializedObject として扱いシーケンスを管理する
        _serializedWindow = new SerializedObject(this);
        _testSequenceProp = _serializedWindow.FindProperty("windowSequence");

        _list = new ReorderableList(
            _serializedWindow,
            _testSequenceProp,
            draggable: true,
            displayHeader: true,
            displayAddButton: true,
            displayRemoveButton: true)
        {
            drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Test Sequence", EditorStyles.boldLabel),

            elementHeightCallback = index =>
            {
                var elem = _testSequenceProp.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(elem, true) + 4f;
            },

            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var elem = _testSequenceProp.GetArrayElementAtIndex(index);
                rect.yMin += 2f;
                rect.yMax -= 2f;

                // [InspectorName] を反映した enumDisplayNames でラベルを構築
                var kindProp = elem.FindPropertyRelative("kind");
                var effectProp = elem.FindPropertyRelative("effect");
                var kind = (CinematicSequence.StepKind)kindProp.enumValueIndex;
                var label = kind == CinematicSequence.StepKind.Delay
                    ? "[遅延]"
                    : $"[{kindProp.enumDisplayNames[kindProp.enumValueIndex]}] {effectProp.enumDisplayNames[effectProp.enumValueIndex]}";

                EditorGUI.PropertyField(rect, elem, new GUIContent(label), true);
            },

            onAddCallback = list =>
            {
                _testSequenceProp.arraySize++;
                var newElem = _testSequenceProp.GetArrayElementAtIndex(_testSequenceProp.arraySize - 1);
                ApplyStepDefaults(newElem);
                list.index = list.count - 1;
            },
        };
    }

    private static void DrawSeparator(string label)
    {
        var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
        var lineRect = new Rect(rect.x, rect.yMax + 1, rect.width, 1);
        EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        GUILayout.Space(2);
    }

    // ── 新規要素のデフォルト値 ─────────────────────────────

    /// <summary>
    /// 新規要素のデフォルト値を設定する。
    /// Unity は配列要素追加時に C# コンストラクタを呼ばないため手動で設定する。
    /// </summary>
    private static void ApplyStepDefaults(SerializedProperty step)
    {
        step.FindPropertyRelative("kind").enumValueIndex = (int)CinematicSequence.StepKind.PlayAndAwait;
        step.FindPropertyRelative("delaySeconds").floatValue = 1f;
        step.FindPropertyRelative("useCustomConfig").boolValue = false;

        ApplyTimedDefaults(step.FindPropertyRelative("letterboxConfig"));
        step.FindPropertyRelative("letterboxConfig").FindPropertyRelative("barHeight").floatValue = 80f;

        ApplyHoldableDefaults(step.FindPropertyRelative("screenFadeConfig"));
        step.FindPropertyRelative("screenFadeConfig").FindPropertyRelative("fadeColor").colorValue = Color.black;

        var shake = step.FindPropertyRelative("cameraShakeConfig");
        shake.FindPropertyRelative("magnitude").floatValue = 0.1f;
        shake.FindPropertyRelative("duration").floatValue = 0.5f;
        shake.FindPropertyRelative("loop").boolValue = true;

        var disor = step.FindPropertyRelative("cameraDisorientationConfig");
        ApplyTimedDefaults(disor);
        disor.FindPropertyRelative("positionOffset").vector3Value = new Vector3(0f, -0.8f, 0f);
        disor.FindPropertyRelative("rotationZ").floatValue = -4f;
        disor.FindPropertyRelative("orthographicSizeScale").floatValue = 0.82f;
        disor.FindPropertyRelative("perspectiveFovDelta").floatValue = -12f;
        disor.FindPropertyRelative("swayPositionAmplitude").floatValue = 0.05f;
        disor.FindPropertyRelative("swayRotationAmplitude").floatValue = 2.5f;
        disor.FindPropertyRelative("swayFrequency").floatValue = 1.25f;

        var vignette = step.FindPropertyRelative("vignetteConfig");
        ApplyVolumeDefaults(vignette);
        vignette.FindPropertyRelative("intensity").floatValue = 0.3f;
        vignette.FindPropertyRelative("smoothness").floatValue = 0.7f;

        var chromatic = step.FindPropertyRelative("chromaticAberrationConfig");
        ApplyVolumeDefaults(chromatic);
        chromatic.FindPropertyRelative("intensity").floatValue = 0.2f;

        var lens = step.FindPropertyRelative("lensDistortionConfig");
        ApplyVolumeDefaults(lens);
        lens.FindPropertyRelative("intensity").floatValue = -0.2f;

        var grain = step.FindPropertyRelative("filmGrainConfig");
        ApplyVolumeDefaults(grain);
        grain.FindPropertyRelative("intensity").floatValue = 0.25f;

        var contrast = step.FindPropertyRelative("contrastConfig");
        ApplyVolumeDefaults(contrast);
        contrast.FindPropertyRelative("contrast").floatValue = 0f;

        var saturation = step.FindPropertyRelative("saturationConfig");
        ApplyVolumeDefaults(saturation);
        saturation.FindPropertyRelative("saturation").floatValue = 0f;

        var colorFilter = step.FindPropertyRelative("colorFilterConfig");
        ApplyVolumeDefaults(colorFilter);
        colorFilter.FindPropertyRelative("filterColor").colorValue = Color.white;

        var dof = step.FindPropertyRelative("depthOfFieldConfig");
        ApplyVolumeDefaults(dof);
        dof.FindPropertyRelative("focusDistance").floatValue = 0.1f;

        var flash = step.FindPropertyRelative("imageFlashConfig");
        ApplyTimedDefaults(flash);
        flash.FindPropertyRelative("tintColor").colorValue = Color.white;
        flash.FindPropertyRelative("holdDuration").floatValue = 0f;

        var dofDiz = step.FindPropertyRelative("doFDizzinessConfig");
        ApplyVolumeDefaults(dofDiz);
        dofDiz.FindPropertyRelative("focusDistance").floatValue = 0.1f;
        dofDiz.FindPropertyRelative("transitionDuration").floatValue = 0.15f;
        dofDiz.FindPropertyRelative("holdBlur").floatValue = 0.3f;
        dofDiz.FindPropertyRelative("holdClear").floatValue = 0.5f;

        var pulseVignette = step.FindPropertyRelative("pulseVignetteConfig");
        ApplyVolumeDefaults(pulseVignette);
        pulseVignette.FindPropertyRelative("baseIntensity").floatValue = 0.35f;
        pulseVignette.FindPropertyRelative("pulseAmplitude").floatValue = 0.18f;
        pulseVignette.FindPropertyRelative("pulseFrequency").floatValue = 1.2f;
        pulseVignette.FindPropertyRelative("smoothness").floatValue = 0.75f;

        var wave = step.FindPropertyRelative("waveDistortionConfig");
        ApplyTimedDefaults(wave);
        wave.FindPropertyRelative("frequency").floatValue = 8f;
        wave.FindPropertyRelative("amplitude").floatValue = 0.005f;
        wave.FindPropertyRelative("speed").floatValue = 2f;

        var blink = step.FindPropertyRelative("blinkConfig");
        ApplyTimedDefaults(blink);
        blink.FindPropertyRelative("blinkColor").colorValue = Color.black;
        blink.FindPropertyRelative("holdDuration").floatValue = 0.1f;
        blink.FindPropertyRelative("intervalDuration").floatValue = 0.5f;
    }

    private static void ApplyTimedDefaults(SerializedProperty prop)
    {
        prop.FindPropertyRelative("enterDuration").floatValue = 0.3f;
        prop.FindPropertyRelative("exitDuration").floatValue = 0.5f;
        prop.FindPropertyRelative("ease").enumValueIndex = EASE_IN_OUT_SINE_VALUE;
    }

    private static void ApplyHoldableDefaults(SerializedProperty prop)
    {
        ApplyTimedDefaults(prop);
        prop.FindPropertyRelative("holdDuration").floatValue = 0f;
        prop.FindPropertyRelative("autoComplete").boolValue = false;
    }

    private static void ApplyVolumeDefaults(SerializedProperty prop)
    {
        ApplyTimedDefaults(prop);
        prop.FindPropertyRelative("volumeWeight").floatValue = 1f;
    }

    /// <summary>プレイモード開始時にシーケンスをディレクターへ注入する。</summary>
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        TryFindDirector();

        if (_director != null)
        {
            _director.InjectTestSequence(windowSequence);
        }

        Repaint();
    }

    // ── ライフサイクル ────────────────────────────────────

    private void OnEnable()
    {
        windowSequence ??= new List<CinematicSequenceAsset.Step>();
        TryFindDirector();
        RebuildList();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    // ── 描画 ───────────────────────────────────────────────

    private void OnGUI()
    {
        // Director 選択
        EditorGUI.BeginChangeCheck();
        var newDirector = (CinematicEffectDirector)EditorGUILayout.ObjectField(
            "Director", _director, typeof(CinematicEffectDirector), true);

        if (EditorGUI.EndChangeCheck())
        {
            _director = newDirector;
        }

        if (_director == null)
        {
            EditorGUILayout.HelpBox("CinematicEffectDirector をシーンから選択してください。プレイモード開始時に自動で注入されます。", MessageType.Info);
        }

        EditorGUILayout.Space(4);
        _serializedWindow.Update();

        // ── シーケンスリスト ────────────────────────────────
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        _list.DoLayoutList();
        EditorGUILayout.EndScrollView();

        _serializedWindow.ApplyModifiedProperties();

        // ── テスト再生 ──────────────────────────────────────
        EditorGUILayout.Space(4);
        DrawSeparator("テスト再生");

        if (_director == null)
        {
            EditorGUILayout.HelpBox("Director が未設定です。", MessageType.Warning);
        }
        else if (Application.isPlaying)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ 再生", GUILayout.Height(30)))
                {
                    _director.PlayTestSequenceAsync().Forget();
                }

                if (GUILayout.Button("■ 停止", GUILayout.Height(30)))
                {
                    _director.StopTest();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Play Mode 中のみ使用できます。", MessageType.Info);
        }

        // ── アセット ───────────────────────────────────────
        EditorGUILayout.Space(4);
        DrawSeparator("アセット");

        EditorGUI.BeginChangeCheck();
        _importSource = (CinematicSequenceAsset)EditorGUILayout.ObjectField(
            _importSource, typeof(CinematicSequenceAsset), false);

        // 選択が変わったら自動で読み込む
        if (EditorGUI.EndChangeCheck())
        {
            ImportFromAsset(_importSource);
        }

        using (new EditorGUI.DisabledScope(_importSource == null))
        {
            if (GUILayout.Button("💾 上書き保存", GUILayout.Height(26)))
            {
                OverwriteAsset(_importSource);
            }
        }
    }
}
