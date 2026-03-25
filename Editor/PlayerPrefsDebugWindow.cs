using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// PlayerPrefs の既知キー確認と編集を行うデバッグウィンドウ。
    /// </summary>
    public sealed class PlayerPrefsDebugWindow : EditorWindow
    {
        private enum ValueType
        {
            Int,
            Float,
            String,
        }

        private static readonly Regex ConstStringRegex = new(
            @"const\s+string\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<value>(?:\\.|[^""\\])*)""\s*;",
            RegexOptions.Compiled);

        private static readonly Regex PlayerPrefsCallRegex = new(
            @"PlayerPrefs\.(?:GetString|GetInt|GetFloat|SetString|SetInt|SetFloat|DeleteKey|HasKey)\s*\(\s*(?:""(?<literal>(?:\\.|[^""\\])*)""|(?<identifier>[A-Za-z_][A-Za-z0-9_]*))",
            RegexOptions.Compiled);

        [SerializeField] private string selectedKey = string.Empty;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private string editValue = string.Empty;
        [SerializeField] private ValueType editType = ValueType.String;
        [SerializeField] private bool includePackages;
        [SerializeField] private Vector2 keyListScrollPosition;
        [SerializeField] private Vector2 detailScrollPosition;

        private readonly HashSet<string> _knownKeys = new(StringComparer.Ordinal);
        private readonly List<string> _filteredKeys = new();

        [MenuItem("Tools/Utils/PlayerPrefs Debug Window")]
        private static void Open()
        {
            GetWindow<PlayerPrefsDebugWindow>("PlayerPrefs Debug");
        }

        private void OnEnable()
        {
            RefreshKnownKeys();
            if (!string.IsNullOrEmpty(selectedKey))
            {
                LoadSelectedKey();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            using var horizontalScope = new EditorGUILayout.HorizontalScope();
            DrawKeyListPane();
            DrawDetailPane();
        }

        private void DrawToolbar()
        {
            using var toolbarScope = new EditorGUILayout.HorizontalScope(EditorStyles.toolbar);
            var searchFieldStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField;
            var cancelButtonStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? EditorStyles.toolbarButton;

            var nextSearchText = GUILayout.TextField(
                searchText,
                searchFieldStyle,
                GUILayout.MinWidth(180f),
                GUILayout.ExpandWidth(true));
            if (nextSearchText != searchText)
            {
                searchText = nextSearchText;
                ApplyFilter();
            }

            if (GUILayout.Button("x", cancelButtonStyle))
            {
                searchText = string.Empty;
                GUI.FocusControl(null);
                ApplyFilter();
            }

            GUILayout.FlexibleSpace();

            var nextIncludePackages = GUILayout.Toggle(includePackages, "Packages", EditorStyles.toolbarButton);
            if (nextIncludePackages != includePackages)
            {
                includePackages = nextIncludePackages;
                RefreshKnownKeys();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64f)))
            {
                RefreshKnownKeys();
                LoadSelectedKey();
            }
        }

        private void DrawKeyListPane()
        {
            using var verticalScope = new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.42f));
            EditorGUILayout.LabelField("Known Keys", EditorStyles.boldLabel);

            keyListScrollPosition = EditorGUILayout.BeginScrollView(keyListScrollPosition);
            foreach (var key in _filteredKeys)
            {
                var isSelected = string.Equals(selectedKey, key, StringComparison.Ordinal);
                if (GUILayout.Toggle(isSelected, key, "Button") && !isSelected)
                {
                    selectedKey = key;
                    LoadSelectedKey();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawDetailPane()
        {
            using var verticalScope = new EditorGUILayout.VerticalScope();
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            selectedKey = EditorGUILayout.TextField("Key", selectedKey);
            if (EditorGUI.EndChangeCheck())
            {
                LoadSelectedKey();
            }

            if (string.IsNullOrWhiteSpace(selectedKey))
            {
                EditorGUILayout.HelpBox("キーを選択するか直接入力してください。", MessageType.Info);
                return;
            }

            detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);

            var hasKey = PlayerPrefs.HasKey(selectedKey);
            DrawExistsLabel(hasKey);
            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Current Value", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Int", PlayerPrefs.GetInt(selectedKey, 0));
                EditorGUILayout.FloatField("Float", PlayerPrefs.GetFloat(selectedKey, 0f));
                EditorGUILayout.TextField("String", PlayerPrefs.GetString(selectedKey, string.Empty));
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Edit", EditorStyles.boldLabel);
            editType = (ValueType)EditorGUILayout.EnumPopup("Type", editType);
            editValue = EditorGUILayout.TextField("Value", editValue);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Current"))
                {
                    LoadSelectedKey();
                }

                if (GUILayout.Button("Save"))
                {
                    SaveSelectedKey();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Delete Key"))
                {
                    DeleteSelectedKey();
                }

                if (GUILayout.Button("Delete All"))
                {
                    DeleteAllKeys();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawExistsLabel(bool hasKey)
        {
            var previousColor = GUI.contentColor;
            GUI.contentColor = hasKey ? new Color(0.2f, 0.65f, 0.25f) : new Color(0.8f, 0.25f, 0.25f);
            EditorGUILayout.LabelField("Exists", hasKey ? "Yes" : "No");
            GUI.contentColor = previousColor;
        }

        private void LoadSelectedKey()
        {
            if (string.IsNullOrWhiteSpace(selectedKey))
            {
                editValue = string.Empty;
                return;
            }

            if (!PlayerPrefs.HasKey(selectedKey))
            {
                editValue = string.Empty;
                return;
            }

            editValue = editType switch
            {
                ValueType.Int => PlayerPrefs.GetInt(selectedKey, 0).ToString(CultureInfo.InvariantCulture),
                ValueType.Float => PlayerPrefs.GetFloat(selectedKey, 0f).ToString(CultureInfo.InvariantCulture),
                _ => PlayerPrefs.GetString(selectedKey, string.Empty),
            };
        }

        private void SaveSelectedKey()
        {
            if (string.IsNullOrWhiteSpace(selectedKey))
            {
                ShowNotification(new GUIContent("Key is empty."));
                return;
            }

            try
            {
                switch (editType)
                {
                    case ValueType.Int:
                        if (!int.TryParse(editValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                        {
                            throw new FormatException("Int として解釈できません。");
                        }

                        PlayerPrefs.SetInt(selectedKey, intValue);
                        break;

                    case ValueType.Float:
                        if (!float.TryParse(editValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatValue))
                        {
                            throw new FormatException("Float として解釈できません。");
                        }

                        PlayerPrefs.SetFloat(selectedKey, floatValue);
                        break;

                    default:
                        PlayerPrefs.SetString(selectedKey, editValue);
                        break;
                }

                PlayerPrefs.Save();
                if (_knownKeys.Add(selectedKey))
                {
                    ApplyFilter();
                }

                LoadSelectedKey();
                ShowNotification(new GUIContent("Saved"));
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("PlayerPrefs Save Error", ex.Message, "OK");
            }
        }

        private void DeleteSelectedKey()
        {
            if (string.IsNullOrWhiteSpace(selectedKey))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete PlayerPrefs Key", $"'{selectedKey}' を削除します。", "Delete", "Cancel"))
            {
                return;
            }

            PlayerPrefs.DeleteKey(selectedKey);
            PlayerPrefs.Save();
            LoadSelectedKey();
            ShowNotification(new GUIContent("Deleted"));
        }

        private void DeleteAllKeys()
        {
            if (!EditorUtility.DisplayDialog("Delete All PlayerPrefs", "すべての PlayerPrefs を削除します。", "Delete All", "Cancel"))
            {
                return;
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            LoadSelectedKey();
            ShowNotification(new GUIContent("Deleted All"));
        }

        private void RefreshKnownKeys()
        {
            _knownKeys.Clear();

            foreach (var path in EnumerateScriptPaths())
            {
                CollectKeysFromFile(path, _knownKeys);
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filteredKeys.Clear();
            foreach (var key in _knownKeys.OrderBy(static key => key, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(searchText)
                    || key.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    _filteredKeys.Add(key);
                }
            }
        }

        private IEnumerable<string> EnumerateScriptPaths()
        {
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var path in assetPaths)
            {
                if (!path.EndsWith(".cs", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!path.StartsWith("Assets/", StringComparison.Ordinal)
                    && !(includePackages && path.StartsWith("Packages/", StringComparison.Ordinal)))
                {
                    continue;
                }

                yield return path;
            }
        }

        private static void CollectKeysFromFile(string assetPath, ICollection<string> destination)
        {
            var fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                return;
            }

            var source = File.ReadAllText(fullPath);
            var constStrings = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match match in ConstStringRegex.Matches(source))
            {
                constStrings[match.Groups["name"].Value] = Regex.Unescape(match.Groups["value"].Value);
            }

            foreach (Match match in PlayerPrefsCallRegex.Matches(source))
            {
                if (match.Groups["literal"].Success)
                {
                    destination.Add(Regex.Unescape(match.Groups["literal"].Value));
                    continue;
                }

                if (match.Groups["identifier"].Success
                    && constStrings.TryGetValue(match.Groups["identifier"].Value, out var resolvedValue))
                {
                    destination.Add(resolvedValue);
                }
            }
        }
    }
}
