using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Void2610.UnityTemplate.Editor
{
    public static class AnimationClipMissingPathUtility
    {
        public static bool IsBindingPathMissing(Transform root, string path)
        {
            if (root == null) return true;
            if (string.IsNullOrEmpty(path)) return false;

            return root.Find(path) == null;
        }

        public static string ReplaceBindingPath(string path, string search, string replacement)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(search)) return path;

            return path.Replace(search, replacement ?? string.Empty);
        }
    }

    public class ReplaceAnimatorMissingPathWindow : EditorWindow
    {
        private readonly List<MissingBindingInfo> _missingBindings = new();

        private Animator _animator;
        private Vector2 _scrollPosition;
        private string _searchPath = string.Empty;
        private string _replacePath = string.Empty;

        [MenuItem("Tools/Utils/Replace Animator Missing Path")]
        private static void Open()
        {
            var window = GetWindow<ReplaceAnimatorMissingPathWindow>("Replace Missing Path");
            window.TryAssignAnimatorFromSelection();
            window.RefreshMissingBindings();
        }

        private void TryAssignAnimatorFromSelection()
        {
            if (Selection.activeGameObject == null) return;

            _animator = Selection.activeGameObject.GetComponentInParent<Animator>();
        }

        private void RefreshMissingBindings()
        {
            _missingBindings.Clear();

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                Repaint();
                return;
            }

            foreach (var clip in _animator.runtimeAnimatorController.animationClips.Distinct())
            {
                CollectMissingBindings(clip, AnimationUtility.GetCurveBindings(clip));
                CollectMissingBindings(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip));
            }

            Repaint();
        }

        private void CollectMissingBindings(AnimationClip clip, EditorCurveBinding[] bindings)
        {
            foreach (var binding in bindings)
            {
                if (AnimationClipMissingPathUtility.IsBindingPathMissing(_animator.transform, binding.path))
                {
                    _missingBindings.Add(new MissingBindingInfo(clip, binding));
                }
            }
        }

        private void ApplyReplacement()
        {
            if (_animator == null)
            {
                EditorUtility.DisplayDialog("Replace Missing Path", "Animator が指定されていません。", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_searchPath))
            {
                EditorUtility.DisplayDialog("Replace Missing Path", "Search Path を入力してください。", "OK");
                return;
            }

            var changedClips = new HashSet<AnimationClip>();

            foreach (var clip in _animator.runtimeAnimatorController.animationClips.Distinct())
            {
                var clipChanged = ReplaceClipBindings(clip);
                if (!clipChanged) continue;

                EditorUtility.SetDirty(clip);
                changedClips.Add(clip);
            }

            if (changedClips.Count == 0)
            {
                EditorUtility.DisplayDialog("Replace Missing Path", "置換対象がありませんでした。", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            RefreshMissingBindings();
            EditorUtility.DisplayDialog("Replace Missing Path", $"{changedClips.Count} 個の AnimationClip を更新しました。", "OK");
        }

        private bool ReplaceClipBindings(AnimationClip clip)
        {
            var changed = false;

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!ShouldReplaceBinding(binding.path)) continue;

                var replacedBinding = binding;
                replacedBinding.path = AnimationClipMissingPathUtility.ReplaceBindingPath(binding.path, _searchPath, _replacePath);
                if (replacedBinding.path == binding.path) continue;

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                AnimationUtility.SetEditorCurve(clip, binding, null);
                AnimationUtility.SetEditorCurve(clip, replacedBinding, curve);
                changed = true;
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (!ShouldReplaceBinding(binding.path)) continue;

                var replacedBinding = binding;
                replacedBinding.path = AnimationClipMissingPathUtility.ReplaceBindingPath(binding.path, _searchPath, _replacePath);
                if (replacedBinding.path == binding.path) continue;

                var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                AnimationUtility.SetObjectReferenceCurve(clip, replacedBinding, curve);
                changed = true;
            }

            return changed;
        }

        private bool ShouldReplaceBinding(string path)
        {
            return AnimationClipMissingPathUtility.IsBindingPathMissing(_animator.transform, path)
                   && !string.IsNullOrEmpty(path)
                   && path.Contains(_searchPath, StringComparison.Ordinal);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Animator", EditorStyles.boldLabel);

            using (var changeScope = new EditorGUI.ChangeCheckScope())
            {
                _animator = (Animator)EditorGUILayout.ObjectField(_animator, typeof(Animator), true);
                if (changeScope.changed)
                {
                    RefreshMissingBindings();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Selection"))
                {
                    TryAssignAnimatorFromSelection();
                    RefreshMissingBindings();
                }

                if (GUILayout.Button("Refresh"))
                {
                    RefreshMissingBindings();
                }
            }

            if (_animator == null)
            {
                EditorGUILayout.HelpBox("Animator が付いた GameObject を指定してください。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Missing Bindings", EditorStyles.boldLabel);

            if (_missingBindings.Count == 0)
            {
                EditorGUILayout.HelpBox("missing なパスは見つかりませんでした。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"missing なバインディングが {_missingBindings.Count} 件見つかりました。", MessageType.Warning);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(260f));
                foreach (var binding in _missingBindings)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Clip", binding.Clip.name);
                    EditorGUILayout.LabelField("Path", binding.Binding.path);
                    EditorGUILayout.LabelField("Property", binding.Binding.propertyName);
                    EditorGUILayout.LabelField("Type", binding.Binding.type.Name);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Replace", EditorStyles.boldLabel);
            _searchPath = EditorGUILayout.TextField("Search Path", _searchPath);
            _replacePath = EditorGUILayout.TextField("Replace Path", _replacePath);

            using (new EditorGUI.DisabledScope(_missingBindings.Count == 0))
            {
                if (GUILayout.Button("Apply"))
                {
                    ApplyReplacement();
                }
            }
        }

        private readonly struct MissingBindingInfo
        {
            public AnimationClip Clip { get; }
            public EditorCurveBinding Binding { get; }
            public MissingBindingInfo(AnimationClip clip, EditorCurveBinding binding)
            {
                Clip = clip;
                Binding = binding;
            }
        }
    }
}
