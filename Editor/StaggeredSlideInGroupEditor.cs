using System.Collections.Generic;
using LitMotion;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StaggeredSlideInGroup))]
public class StaggeredSlideInGroupEditor : Editor
{
    private bool _isPreviewing;
    private double _previewStartTime;
    private List<PreviewTarget> _targets;

    private struct PreviewTarget
    {
        public RectTransform Rect;
        public Vector2 StartPos;
        public Vector2 TargetPos;
        public CanvasGroup CanvasGroup;
        public float Delay;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var group = (StaggeredSlideInGroup)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("レイアウトを適用"))
        {
            group.ApplyLayout();
            if (!Application.isPlaying)
                EditorUtility.SetDirty(group);
        }

        if (Application.isPlaying)
        {
            if (GUILayout.Button("アニメーションをプレビュー"))
                group.Play();
        }
        else
        {
            using (new EditorGUI.DisabledScope(_isPreviewing))
            {
                if (GUILayout.Button("アニメーションをプレビュー"))
                    StartPreview();
            }

            if (_isPreviewing && GUILayout.Button("プレビュー停止"))
                StopPreview();
        }
    }

    private void StartPreview()
    {
        serializedObject.Update();
        var group = (StaggeredSlideInGroup)target;
        var groupTransform = group.transform;

        var slideOffset = serializedObject.FindProperty("slideOffset").vector2Value;
        var enableFade = serializedObject.FindProperty("enableFade").boolValue;
        var activeOnly = serializedObject.FindProperty("activeOnly").boolValue;
        var staggerDelay = serializedObject.FindProperty("staggerDelay").floatValue;

        _targets = new List<PreviewTarget>();

        for (int i = 0, index = 0; i < groupTransform.childCount; i++)
        {
            var child = groupTransform.GetChild(i) as RectTransform;
            if (!child) continue;
            if (activeOnly && !child.gameObject.activeSelf) continue;

            var targetPos = child.anchoredPosition;
            var startPos = targetPos + slideOffset;
            var canvasGroup = enableFade ? child.GetComponent<CanvasGroup>() : null;

            child.anchoredPosition = startPos;
            if (canvasGroup) canvasGroup.alpha = 0f;

            _targets.Add(new PreviewTarget
            {
                Rect = child,
                StartPos = startPos,
                TargetPos = targetPos,
                CanvasGroup = canvasGroup,
                Delay = staggerDelay * index
            });
            index++;
        }

        _previewStartTime = EditorApplication.timeSinceStartup;
        _isPreviewing = true;
        EditorApplication.update += UpdatePreview;
    }

    private void UpdatePreview()
    {
        serializedObject.Update();
        var elapsed = (float)(EditorApplication.timeSinceStartup - _previewStartTime);
        var duration = serializedObject.FindProperty("duration").floatValue;
        var moveEase = (Ease)serializedObject.FindProperty("moveEase").intValue;
        var enableFade = serializedObject.FindProperty("enableFade").boolValue;

        var allDone = true;

        foreach (var t in _targets)
        {
            if (!t.Rect) continue;

            var localTime = elapsed - t.Delay;
            if (localTime < 0)
            {
                allDone = false;
                continue;
            }

            var progress = Mathf.Clamp01(localTime / duration);
            var easedProgress = EvaluateEase(moveEase, progress);

            t.Rect.anchoredPosition = Vector2.LerpUnclamped(t.StartPos, t.TargetPos, easedProgress);

            if (enableFade && t.CanvasGroup)
                t.CanvasGroup.alpha = easedProgress;

            if (progress < 1f) allDone = false;
        }

        SceneView.RepaintAll();

        if (allDone)
            StopPreview();
    }

    private void StopPreview()
    {
        EditorApplication.update -= UpdatePreview;
        _isPreviewing = false;

        if (_targets != null)
        {
            // 最終位置に復元
            foreach (var t in _targets)
            {
                if (!t.Rect) continue;
                t.Rect.anchoredPosition = t.TargetPos;
                if (t.CanvasGroup) t.CanvasGroup.alpha = 1f;
            }
            _targets = null;
        }

        Repaint();
    }

    private static float EvaluateEase(Ease ease, float t)
    {
        switch (ease)
        {
            case Ease.Linear:
                return t;
            case Ease.OutQuad:
                return 1f - (1f - t) * (1f - t);
            case Ease.OutCubic:
                return 1f - Mathf.Pow(1f - t, 3f);
            case Ease.OutQuart:
                return 1f - Mathf.Pow(1f - t, 4f);
            case Ease.OutQuint:
                return 1f - Mathf.Pow(1f - t, 5f);
            case Ease.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
            default:
                return t;
        }
    }

    private void OnDisable()
    {
        if (_isPreviewing)
            StopPreview();
    }
}
