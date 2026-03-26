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

        [SerializeField] private string selectedKey = string.Empty;
        [SerializeField] private string searchText = string.Empty;

        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string name;
        }
        [SerializeField] private string editValue = string.Empty;
        [SerializeField] private ValueType editType = ValueType.String;
        [SerializeField] private List<string> selectedAssemblyScopes = new() { ALL_ASSEMBLIES_SCOPE };
        [SerializeField] private Vector2 keyListScrollPosition;
        [SerializeField] private Vector2 detailScrollPosition;
        private const string ALL_ASSEMBLIES_SCOPE = "All Assemblies";
        private const string DEFAULT_ASSETS_ASSEMBLY_SCOPE = "Assembly-CSharp";

        private static readonly Regex ConstStringRegex = new(
            @"const\s+string\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<value>(?:\\.|[^""\\])*)""\s*;",
            RegexOptions.Compiled);

        private static readonly Regex PlayerPrefsCallRegex = new(
            @"PlayerPrefs\.(?:GetString|GetInt|GetFloat|SetString|SetInt|SetFloat|DeleteKey|HasKey)\s*\(\s*(?:""(?<literal>(?:\\.|[^""\\])*)""|(?<identifier>[A-Za-z_][A-Za-z0-9_]*))",
            RegexOptions.Compiled);

        private readonly HashSet<string> _sourceKnownKeys = new(StringComparer.Ordinal);
        private readonly HashSet<string> _existingKeys = new(StringComparer.Ordinal);
        private readonly List<string> _assemblyScopeOptions = new();

        [MenuItem("Tools/Utils/PlayerPrefs Debug Window")]
        private static void Open()
        {
            GetWindow<PlayerPrefsDebugWindow>("PlayerPrefs Debug");
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
            }

            if (GUILayout.Button("x", cancelButtonStyle))
            {
                searchText = string.Empty;
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();

            DrawAssemblyScopeDropdown();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64f)))
            {
                RefreshKnownKeys();
            }
        }

        private void DrawKeyListPane()
        {
            using var verticalScope = new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.42f));
            EditorGUILayout.LabelField("Known Keys", EditorStyles.boldLabel);

            keyListScrollPosition = EditorGUILayout.BeginScrollView(keyListScrollPosition);
            DrawKeySection("From Code", _sourceKnownKeys);
            EditorGUILayout.Space(6f);
            DrawKeySection("Existing Prefs", _existingKeys);
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

        private void DrawKeySection(string label, IEnumerable<string> keys)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            var visibleKeys = keys
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Where(key => string.IsNullOrWhiteSpace(searchText) || key.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(static key => key, StringComparer.Ordinal)
                .ToList();

            if (visibleKeys.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("(none)");
                }

                return;
            }

            foreach (var key in visibleKeys)
            {
                var isSelected = string.Equals(selectedKey, key, StringComparison.Ordinal);
                if (GUILayout.Toggle(isSelected, key, "Button") && !isSelected)
                {
                    selectedKey = key;
                    LoadSelectedKey();
                }
            }
        }

        private void DrawAssemblyScopeDropdown()
        {
            if (_assemblyScopeOptions.Count == 0)
            {
                RefreshAssemblyScopes();
            }

            var label = BuildAssemblyScopeLabel();
            if (!GUILayout.Button(label, EditorStyles.toolbarDropDown, GUILayout.Width(180f)))
            {
                return;
            }

            var menu = new GenericMenu();
            var allSelected = IsAllAssembliesSelected();
            menu.AddItem(new GUIContent(ALL_ASSEMBLIES_SCOPE), allSelected, () =>
            {
                selectedAssemblyScopes.Clear();
                selectedAssemblyScopes.Add(ALL_ASSEMBLIES_SCOPE);
                RefreshKnownKeys();
            });

            menu.AddSeparator(string.Empty);

            foreach (var scope in _assemblyScopeOptions)
            {
                if (string.Equals(scope, ALL_ASSEMBLIES_SCOPE, StringComparison.Ordinal))
                {
                    continue;
                }

                var isSelected = selectedAssemblyScopes.Contains(scope);
                menu.AddItem(new GUIContent(scope), isSelected, () => ToggleAssemblyScope(scope));
            }

            menu.DropDown(GUILayoutUtility.GetLastRect());
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
                if (_existingKeys.Add(selectedKey))
                {
                    Repaint();
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
            _existingKeys.Remove(selectedKey);
            LoadSelectedKey();
            Repaint();
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
            _existingKeys.Clear();
            LoadSelectedKey();
            Repaint();
            ShowNotification(new GUIContent("Deleted All"));
        }

        private void RefreshKnownKeys()
        {
            RefreshAssemblyScopes();
            _sourceKnownKeys.Clear();

            foreach (var path in EnumerateScriptPaths())
            {
                CollectKeysFromFile(path, _sourceKnownKeys);
            }

            RefreshExistingKeys();
            LoadSelectedKey();
            Repaint();
        }

        private void RefreshExistingKeys()
        {
            _existingKeys.Clear();

            foreach (var key in _sourceKnownKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    _existingKeys.Add(key);
                }
            }

            if (!string.IsNullOrWhiteSpace(selectedKey) && PlayerPrefs.HasKey(selectedKey))
            {
                _existingKeys.Add(selectedKey);
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
                    && !path.StartsWith("Packages/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IsPathInSelectedAssemblyScope(path))
                {
                    continue;
                }

                yield return path;
            }
        }

        private void RefreshAssemblyScopes()
        {
            var scopes = new HashSet<string>(StringComparer.Ordinal)
            {
                ALL_ASSEMBLIES_SCOPE,
                DEFAULT_ASSETS_ASSEMBLY_SCOPE,
            };

            foreach (var assetPath in AssetDatabase.GetAllAssetPaths())
            {
                if (!assetPath.EndsWith(".asmdef", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    continue;
                }

                var assemblyName = TryReadAssemblyName(assetPath);
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    scopes.Add(assemblyName);
                }
            }

            _assemblyScopeOptions.Clear();
            _assemblyScopeOptions.AddRange(scopes.OrderBy(static scope => scope, StringComparer.Ordinal));

            if (!_assemblyScopeOptions.Contains(ALL_ASSEMBLIES_SCOPE))
            {
                _assemblyScopeOptions.Insert(0, ALL_ASSEMBLIES_SCOPE);
            }

            if (selectedAssemblyScopes.Count == 0)
            {
                selectedAssemblyScopes.Add(ALL_ASSEMBLIES_SCOPE);
            }

            selectedAssemblyScopes = selectedAssemblyScopes
                .Where(scope => string.Equals(scope, ALL_ASSEMBLIES_SCOPE, StringComparison.Ordinal) || _assemblyScopeOptions.Contains(scope))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (selectedAssemblyScopes.Count == 0)
            {
                selectedAssemblyScopes.Add(ALL_ASSEMBLIES_SCOPE);
            }
        }

        private bool IsPathInSelectedAssemblyScope(string assetPath)
        {
            if (IsAllAssembliesSelected())
            {
                return true;
            }

            var resolvedAssembly = ResolveAssemblyScopeForPath(assetPath);
            return selectedAssemblyScopes.Contains(resolvedAssembly);
        }

        private void ToggleAssemblyScope(string scope)
        {
            selectedAssemblyScopes.RemoveAll(value => string.Equals(value, ALL_ASSEMBLIES_SCOPE, StringComparison.Ordinal));

            if (!selectedAssemblyScopes.Remove(scope))
            {
                selectedAssemblyScopes.Add(scope);
            }

            if (selectedAssemblyScopes.Count == 0)
            {
                selectedAssemblyScopes.Add(ALL_ASSEMBLIES_SCOPE);
            }

            RefreshKnownKeys();
        }

        private bool IsAllAssembliesSelected()
        {
            return selectedAssemblyScopes.Count == 0
                || selectedAssemblyScopes.Contains(ALL_ASSEMBLIES_SCOPE);
        }

        private string BuildAssemblyScopeLabel()
        {
            if (IsAllAssembliesSelected())
            {
                return ALL_ASSEMBLIES_SCOPE;
            }

            if (selectedAssemblyScopes.Count == 1)
            {
                return selectedAssemblyScopes[0];
            }

            return $"{selectedAssemblyScopes.Count} Assemblies";
        }

        private static string ResolveAssemblyScopeForPath(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            while (!string.IsNullOrEmpty(directory))
            {
                var asmdefPaths = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { directory });
                foreach (var guid in asmdefPaths)
                {
                    var asmdefPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.Equals(Path.GetDirectoryName(asmdefPath)?.Replace('\\', '/'), directory, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var assemblyName = TryReadAssemblyName(asmdefPath);
                    if (!string.IsNullOrWhiteSpace(assemblyName))
                    {
                        return assemblyName;
                    }
                }

                var parentDirectory = Path.GetDirectoryName(directory)?.Replace('\\', '/');
                if (string.Equals(parentDirectory, directory, StringComparison.Ordinal))
                {
                    break;
                }

                directory = parentDirectory;
            }

            return assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                ? DEFAULT_ASSETS_ASSEMBLY_SCOPE
                : string.Empty;
        }

        private static string TryReadAssemblyName(string assetPath)
        {
            var fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                return string.Empty;
            }

            var json = File.ReadAllText(fullPath);
            var data = JsonUtility.FromJson<AssemblyDefinitionData>(json);
            return data?.name ?? string.Empty;
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
    }
}
