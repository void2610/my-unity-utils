using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Void2610.UnityTemplate.Editor.CI
{
    /// <summary>
    /// CI 環境からプラットフォーム別ビルドを呼び出すための共通エントリポイント。
    /// 出力先・ビルド名はそれぞれ UNITY_BUILD_OUTPUT_PATH / UNITY_BUILD_NAME 環境変数で
    /// 上書きできるため、利用側プロジェクトに固有値を持たせる必要はない。
    /// </summary>
    public static class CiBuild
    {
        public static void BuildWindows() => Build(
            BuildTarget.StandaloneWindows64,
            GetBuildOutputPath(Path.Combine("builds", "StandaloneWindows64", $"{GetBuildName()}.exe")),
            BuildOptions.None);

        public static void BuildMac() => Build(
            BuildTarget.StandaloneOSX,
            GetBuildOutputPath(Path.Combine("builds", "StandaloneOSX", $"{GetBuildName()}.app")),
            BuildOptions.None);

        public static void BuildWebGL()
        {
            // セルフホストランナーで Library が古い WebGL 圧縮設定を保持していると
            // ProjectSettings 側の Brotli 指定が反映されず非圧縮成果物が出るため、
            // ビルド直前に API 経由で強制上書きする。
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;

            // PlayerDataCache が古い状態で残っているとビルドが失敗することがあるため、念のため削除する。
            DeleteIfExists("Library/PlayerDataCache");

            // PR プレビュー等で wasm 最適化を弱めてビルドを高速化できるようにする (BuildTimes / RuntimeSpeed / DiskSize)。
            var codeOptimization = Environment.GetEnvironmentVariable("UNITY_WEBGL_CODE_OPTIMIZATION")?.Trim();
            if (string.IsNullOrEmpty(codeOptimization))
            {
                BuildWebGLPlayer();
                return;
            }

            var allowedValues = new[] { "BuildTimes", "RuntimeSpeed", "DiskSize" };
            if (!allowedValues.Contains(codeOptimization)) throw new ArgumentException($"UNITY_WEBGL_CODE_OPTIMIZATION が無効です: '{codeOptimization}' (許容値: {string.Join(", ", allowedValues)})");

            // EditorUserBuildSettings は Library に永続化するため、退避・復元しないと次回以降のビルド (本番含む) に漏れる。
            var originalCodeOptimization = EditorUserBuildSettings.GetPlatformSettings("WebGL", "CodeOptimization");
            EditorUserBuildSettings.SetPlatformSettings("WebGL", "CodeOptimization", codeOptimization);
            try
            {
                BuildWebGLPlayer();
            }
            finally
            {
                EditorUserBuildSettings.SetPlatformSettings("WebGL", "CodeOptimization", originalCodeOptimization);
            }
        }

        private static void BuildWebGLPlayer()
        {
            Build(
                BuildTarget.WebGL,
                GetBuildOutputPath(Path.Combine("CIBuilds", "WebGL", "build")),
                BuildOptions.None);
        }

        private static void DeleteIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        private static void Build(BuildTarget target, string locationPathName, BuildOptions extraOptions)
        {
            ApplyBuildVersion();

            var outputDirectory = Path.GetDirectoryName(locationPathName);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray(),
                locationPathName = locationPathName,
                target = target,
                options = extraOptions,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException($"Build failed: {report.summary.result}");
        }

        private static void ApplyBuildVersion()
        {
            var buildVersion = Environment.GetEnvironmentVariable("BUILD_VERSION");
            if (!string.IsNullOrEmpty(buildVersion))
            {
                PlayerSettings.bundleVersion = buildVersion;
            }
        }

        private static string GetBuildOutputPath(string defaultPath)
        {
            var buildOutputPath = Environment.GetEnvironmentVariable("UNITY_BUILD_OUTPUT_PATH");
            return string.IsNullOrEmpty(buildOutputPath) ? defaultPath : buildOutputPath;
        }

        private static string GetBuildName()
        {
            var buildName = Environment.GetEnvironmentVariable("UNITY_BUILD_NAME");
            return string.IsNullOrEmpty(buildName) ? "build" : buildName;
        }
    }
}
