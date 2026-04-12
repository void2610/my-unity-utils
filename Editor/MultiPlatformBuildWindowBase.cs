using System;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Windows/macOS を 1 操作で連続ビルドする汎用 EditorWindow。
    /// プロジェクト固有の定数には依存せず、既定のアプリ名は PlayerSettings.productName を使う。
    /// </summary>
    public sealed class MultiPlatformBuildWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Utils/Multi Platform Build";
        private const string BuildAndRunShortcutId = "Main Menu/File/Build And Run";
        private const string OpenWindowShortcutId = "Void2610/Multi Platform Build";
        private const string RootDirectoryKey = "Void2610.MultiPlatformBuild.RootDirectory";
        private const string AppNameKey = "Void2610.MultiPlatformBuild.AppName";

        private string _rootDirectory;
        private string _appName;
        private string _generatedVersion;

        private readonly struct BuildExecutionResult
        {
            public BuildExecutionResult(BuildResult result, string outputPath, string durationText)
            {
                Result = result;
                OutputPath = outputPath;
                DurationText = durationText;
            }

            public BuildResult Result { get; }
            public string OutputPath { get; }
            public string DurationText { get; }
        }

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var window = GetWindow<MultiPlatformBuildWindow>();
            window.titleContent = new GUIContent("Multi Build");
            window.minSize = new Vector2(520f, 210f);
            window.Show();
        }

        [Shortcut(OpenWindowShortcutId, KeyCode.B, ShortcutModifiers.Action)]
        private static void OpenFromShortcut()
        {
            Open();
        }

        private void BuildAll()
        {
            var enabledScenes = MultiPlatformBuildUtility.GetEnabledScenePaths();
            if (enabledScenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Build Failed", "EditorBuildSettings に有効なシーンがありません。", "OK");
                return;
            }

            SavePreferences();

            // CI と同じ版数文字列をここで確定させ、PlayerSettings と出力先の両方で使う。
            _generatedVersion = MultiPlatformBuildUtility.GenerateBuildVersion();
            PlayerSettings.bundleVersion = _generatedVersion;

            if (!BuildTargetWithAddressables(
                enabledScenes,
                BuildTarget.StandaloneOSX,
                BuildTargetGroup.Standalone,
                _generatedVersion,
                _appName,
                out var macBuildResult))
            {
                return;
            }

            if (!BuildTargetWithAddressables(
                enabledScenes,
                BuildTarget.StandaloneWindows64,
                BuildTargetGroup.Standalone,
                _generatedVersion,
                _appName,
                out var windowsBuildResult))
            {
                return;
            }

            var macSummary = FormatBuildSummary("macOS", macBuildResult);
            var windowsSummary = FormatBuildSummary("Windows", windowsBuildResult);
            var macZipSummary = CreateZipSummary(
                "macOS",
                macBuildResult,
                MultiPlatformBuildUtility.GetPlatformBuildDirectory(_rootDirectory, _generatedVersion, BuildTarget.StandaloneOSX));
            var windowsZipSummary = CreateZipSummary(
                "Windows",
                windowsBuildResult,
                MultiPlatformBuildUtility.GetPlatformBuildDirectory(_rootDirectory, _generatedVersion, BuildTarget.StandaloneWindows64));

            var message = $"{macSummary}\n{macZipSummary}\n{windowsSummary}\n{windowsZipSummary}";
            var title = windowsBuildResult.Result == BuildResult.Succeeded &&
                        macBuildResult.Result == BuildResult.Succeeded
                ? "Build Succeeded"
                : "Build Finished With Errors";

            UnityEngine.Debug.Log(message);
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private bool BuildTargetWithAddressables(
            string[] scenes,
            BuildTarget target,
            BuildTargetGroup targetGroup,
            string versionName,
            string appName,
            out BuildExecutionResult buildResult)
        {
            buildResult = default;

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target))
            {
                EditorUtility.DisplayDialog("Build Failed", $"ビルドターゲットの切り替えに失敗しました: {target}", "OK");
                return false;
            }

            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Build Failed", "Addressables Settings が見つかりません。", "OK");
                return false;
            }

            settings.OverridePlayerVersion = versionName;
            AddressableAssetSettings.BuildPlayerContent(out var result);

            if (!string.IsNullOrEmpty(result.Error))
            {
                EditorUtility.DisplayDialog("Build Failed", $"Addressables ビルドに失敗しました: {target}\n{result.Error}", "OK");
                return false;
            }

            var report = BuildPlayer(scenes, target, targetGroup, versionName, appName);
            var summary = report.summary;
            buildResult = new BuildExecutionResult(
                summary.result,
                summary.outputPath,
                summary.totalTime.ToString(@"hh\:mm\:ss"));
            return true;
        }

        private BuildReport BuildPlayer(
            string[] scenes,
            BuildTarget target,
            BuildTargetGroup targetGroup,
            string versionName,
            string appName)
        {
            var outputPath = MultiPlatformBuildUtility.GetBuildLocation(_rootDirectory, versionName, appName, target);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? MultiPlatformBuildUtility.ResolveOutputDirectory(_rootDirectory));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                targetGroup = targetGroup,
            };

            return BuildPipeline.BuildPlayer(options);
        }

        private static string FormatBuildSummary(string platformName, BuildExecutionResult buildResult)
        {
            return $"{platformName}: {buildResult.Result} / {buildResult.OutputPath} / {buildResult.DurationText}";
        }

        private static string CreateZipSummary(string platformName, BuildExecutionResult buildResult, string buildDirectory)
        {
            if (buildResult.Result != BuildResult.Succeeded)
            {
                return $"{platformName} Zip: skipped";
            }

            try
            {
                var zipPath = MultiPlatformBuildUtility.CreatePlatformArchive(buildDirectory);
                return $"{platformName} Zip: {zipPath}";
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to create {platformName} zip archive: {e}");
                return $"{platformName} Zip: failed";
            }
        }

        private void SavePreferences()
        {
            // 出力先とアプリ名だけ保持すれば、次回起動時も同じ設定で続けられる。
            EditorPrefs.SetString(RootDirectoryKey, _rootDirectory ?? string.Empty);
            EditorPrefs.SetString(AppNameKey, _appName ?? string.Empty);
        }

        private void OnEnable()
        {
            // 共通ツールなので既定値もプロジェクト名固定ではなく PlayerSettings から取る。
            _rootDirectory = EditorPrefs.GetString(RootDirectoryKey, "Builds");
            _appName = EditorPrefs.GetString(AppNameKey, PlayerSettings.productName);
            _generatedVersion = MultiPlatformBuildUtility.GenerateBuildVersion();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Build Output", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _rootDirectory = EditorGUILayout.TextField("Root Directory", _rootDirectory);

                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                {
                    // 絶対パスで picker を開き、選択後はプロジェクト内なら相対パスへ戻す。
                    var absoluteCurrentDirectory = MultiPlatformBuildUtility.ResolveOutputDirectory(_rootDirectory);
                    var selectedDirectory = EditorUtility.OpenFolderPanel(
                        "Select Build Output Directory",
                        absoluteCurrentDirectory,
                        string.Empty);

                    if (!string.IsNullOrEmpty(selectedDirectory))
                    {
                        _rootDirectory = MultiPlatformBuildUtility.ToProjectRelativePath(selectedDirectory);
                        SavePreferences();
                        GUI.FocusControl(null);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Naming", EditorStyles.boldLabel);

            _appName = EditorGUILayout.TextField("App Name", _appName);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Generated Version", _generatedVersion);
            }

            if (GUILayout.Button("Refresh Version"))
            {
                _generatedVersion = MultiPlatformBuildUtility.GenerateBuildVersion();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Bundle Version: {_generatedVersion}\n" +
                $"Windows: {MultiPlatformBuildUtility.GetBuildLocation(_rootDirectory, _generatedVersion, _appName, BuildTarget.StandaloneWindows64)}\n" +
                $"macOS: {MultiPlatformBuildUtility.GetBuildLocation(_rootDirectory, _generatedVersion, _appName, BuildTarget.StandaloneOSX)}",
                MessageType.Info);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_rootDirectory) ||
                                               string.IsNullOrWhiteSpace(_appName) ||
                                               string.IsNullOrWhiteSpace(_generatedVersion)))
            {
                if (GUILayout.Button("Build Windows + macOS", GUILayout.Height(32f)))
                {
                    BuildAll();
                }
            }

            SavePreferences();
        }

        [InitializeOnLoadMethod]
        private static void DisableBuildAndRunShortcut()
        {
            var shortcutManager = ShortcutManager.instance;
            if (!shortcutManager.GetAvailableShortcutIds().Contains(BuildAndRunShortcutId))
            {
                return;
            }

            shortcutManager.RebindShortcut(BuildAndRunShortcutId, ShortcutBinding.empty);
        }
    }

    /// <summary>
    /// マルチプラットフォームビルド用のパス生成と版数生成をまとめたユーティリティ。
    /// </summary>
    public static class MultiPlatformBuildUtility
    {
        private const string DefaultOutputDirectoryName = "Builds";

        /// <summary>
        /// Build Settings で有効なシーンだけをビルド対象にする。
        /// </summary>
        public static string[] GetEnabledScenePaths() =>
            EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();

        /// <summary>
        /// 例: Builds/win-0.0.1-2026-04-12-13-00-00-abc1234/the-garden-of-garden-gnome.exe
        /// </summary>
        public static string GetBuildLocation(string rootDirectory, string versionName, string appName, BuildTarget target)
        {
            var normalizedDirectory = ResolveOutputDirectory(rootDirectory);
            var normalizedVersion = NormalizeVersionForPath(versionName);
            var normalizedFileName = NormalizeOutputName(appName, target);
            var platformDirectory = GetPlatformDirectoryName(normalizedVersion, target);
            return Path.Combine(normalizedDirectory, platformDirectory, normalizedFileName);
        }

        /// <summary>
        /// 実ファイル名はアプリ名固定で、版数はディレクトリ側に寄せる。
        /// </summary>
        public static string NormalizeOutputName(string fileName, BuildTarget target)
        {
            var trimmed = string.IsNullOrWhiteSpace(fileName) ? PlayerSettings.productName : fileName.Trim();
            var withoutExtension = Path.GetFileNameWithoutExtension(trimmed);

            return target switch
            {
                BuildTarget.StandaloneWindows64 => $"{withoutExtension}.exe",
                BuildTarget.StandaloneOSX => $"{withoutExtension}.app",
                _ => withoutExtension,
            };
        }

        public static string GetPlatformDirectoryName(string versionName, BuildTarget target)
        {
            var prefix = target switch
            {
                BuildTarget.StandaloneWindows64 => "win",
                BuildTarget.StandaloneOSX => "mac",
                _ => target.ToString().ToLowerInvariant(),
            };

            return $"{prefix}-{versionName}";
        }

        /// <summary>
        /// bundleVersion は CI 互換の生文字列を維持し、ファイルパスに使えない文字だけ置換する。
        /// </summary>
        public static string NormalizeVersionForPath(string versionName)
        {
            var trimmed = string.IsNullOrWhiteSpace(versionName) ? "0.0.0" : versionName.Trim();
            var sanitizedChars = trimmed
                .Select(c => c is ' ' or '/' or '\\' or ':' ? '-' : c)
                .ToArray();
            return new string(sanitizedChars);
        }

        /// <summary>
        /// CI と同じく latest tag + JST timestamp + short SHA で版数を組み立てる。
        /// </summary>
        public static string GenerateBuildVersion()
        {
            // Git tag がなければ CI と同じく 0.0.0 を起点にする。
            var baseVersion = RunGitCommand("tag --sort=-version:refname");
            var latestTag = baseVersion
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(latestTag))
            {
                latestTag = "0.0.0";
            }

            var shortSha = RunGitCommand("rev-parse --short=7 HEAD");
            if (string.IsNullOrWhiteSpace(shortSha))
            {
                shortSha = "unknown";
            }

            var jst = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Tokyo");
            var dateJst = jst.ToString("yyyy/MM/dd HH:mm:ss");
            return $"{latestTag} {dateJst} {shortSha.Trim()}";
        }

        /// <summary>
        /// 相対パス入力でもビルド時には常に絶対パスへ解決する。
        /// </summary>
        public static string ResolveOutputDirectory(string outputDirectory)
        {
            var directory = string.IsNullOrWhiteSpace(outputDirectory) ? DefaultOutputDirectoryName : outputDirectory.Trim();
            return Path.IsPathRooted(directory)
                ? directory
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), directory));
        }

        /// <summary>
        /// プロジェクト配下を選んだ場合だけ相対パスに戻し、設定値を持ち運びやすくする。
        /// </summary>
        public static string ToProjectRelativePath(string absolutePath)
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var relativePath = Path.GetRelativePath(projectRoot, absolutePath);
            return relativePath.StartsWith("..", StringComparison.Ordinal) ? absolutePath : relativePath;
        }

        /// <summary>
        /// 各プラットフォームの出力ディレクトリを丸ごと zip 化する。
        /// </summary>
        public static string GetPlatformBuildDirectory(string rootDirectory, string versionName, BuildTarget target)
        {
            var normalizedDirectory = ResolveOutputDirectory(rootDirectory);
            var normalizedVersion = NormalizeVersionForPath(versionName);
            var platformDirectory = GetPlatformDirectoryName(normalizedVersion, target);
            return Path.Combine(normalizedDirectory, platformDirectory);
        }

        public static string CreatePlatformArchive(string buildDirectory)
        {
            if (string.IsNullOrWhiteSpace(buildDirectory) || !Directory.Exists(buildDirectory))
            {
                throw new DirectoryNotFoundException($"Build directory not found: {buildDirectory}");
            }

            var directoryInfo = new DirectoryInfo(buildDirectory);
            var zipPath = Path.Combine(directoryInfo.Parent?.FullName ?? buildDirectory, $"{directoryInfo.Name}.zip");

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                CreateZipWithDitto(buildDirectory, zipPath);
            }
            else
            {
                ZipFile.CreateFromDirectory(buildDirectory, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
            }

            return zipPath;
        }

        private static void CreateZipWithDitto(string buildDirectory, string zipPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/ditto",
                Arguments = $"-c -k --sequesterRsrc --keepParent \"{buildDirectory}\" \"{zipPath}\"",
                WorkingDirectory = Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start ditto process.");
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"ditto failed with exit code {process.ExitCode}. stdout: {stdout} stderr: {stderr}");
            }
        }

        private static string RunGitCommand(string arguments)
        {
            // シェルを介さず直接 git を叩いて、Editor 実行時でも余計な環境差異を減らす。
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return string.Empty;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? output.Trim() : string.Empty;
        }
    }
}
