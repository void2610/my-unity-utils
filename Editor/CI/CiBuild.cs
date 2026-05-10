using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

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

        public static void BuildWebGL() => BuildWebGLInternal(BuildOptions.None);

        // Development Build を有効化した WebGL ビルド。DEVELOPMENT_BUILD シンボルが
        // 定義されるため、defineConstraints: UNITY_EDITOR || DEVELOPMENT_BUILD のアセンブリ
        // (デバッグツール / ランタイムコマンドパレット 等) が配信ビルドに含まれる。
        // 製品リリース時は BuildWebGL を使う。
        public static void BuildWebGLDevelopment() => BuildWebGLInternal(BuildOptions.Development);

        private static void BuildWebGLInternal(BuildOptions extraOptions)
        {
            // セルフホストランナーで Library が古い WebGL 圧縮設定を保持していると
            // ProjectSettings 側の Brotli 指定が反映されず非圧縮成果物が出るため、
            // ビルド直前に API 経由で強制上書きする。
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Development Build はデフォルトで debug symbol を WASM 内に埋め込むため、
            // Cloudflare Pages の 25MB / ファイル上限を超える (実測 141MB)。
            // CI では DEVELOPMENT_BUILD シンボル定義だけが必要 (Debug.asmdef を有効化する目的) で、
            // ブラウザでの C# デバッガ接続用 DWARF / SourceMap までは不要なので Off にする。
            PlayerSettings.WebGL.debugSymbolMode =
                (extraOptions & BuildOptions.Development) != 0
                    ? WebGLDebugSymbolMode.Off
                    : WebGLDebugSymbolMode.External;

            // PlayerDataCache が古い状態で残っているとビルドが失敗することがあるため、念のため削除する。
            DeleteIfExists("Library/PlayerDataCache");

            Build(
                BuildTarget.WebGL,
                GetBuildOutputPath(Path.Combine("CIBuilds", "WebGL", "build")),
                extraOptions);
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
