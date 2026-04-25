using System;
using System.Reflection;
using UnityEngine;

namespace Void2610.UnityTemplate.Editor.CI
{
    /// <summary>
    /// CI 環境で NuGet for Unity のパッケージ復元を -executeMethod から呼び出すためのラッパー。
    /// NugetForUnity 名前空間を直接参照するとパッケージ未復元時にコンパイルエラーになるため、
    /// リフレクション経由で PackageRestorer.Restore(bool) を呼び出す。
    /// </summary>
    public static class NuGetRestore
    {
        public static void Restore()
        {
            Debug.Log("[CI] NuGet パッケージの復元を開始します。");

            // NugetForUnity アセンブリが読み込まれているか確認する。
            var type = Type.GetType("NugetForUnity.PackageRestorer, NugetForUnity.Editor");
            if (type == null)
            {
                // アセンブリ名が異なる可能性があるため、ロード済み全アセンブリから探す。
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType("NugetForUnity.PackageRestorer");
                    if (type != null) break;
                }
            }

            if (type == null)
            {
                Debug.LogError("[CI] NugetForUnity.PackageRestorer が見つかりません。NuGet for Unity がインストールされているか確認してください。");
                return;
            }

            var method = type.GetMethod("Restore", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                Debug.LogError("[CI] PackageRestorer.Restore メソッドが見つかりません。");
                return;
            }

            // slimRestore=true: 高速復元モード。NuGet.config の slimRestore 設定に合わせる。
            method.Invoke(null, new object[] { true });
            Debug.Log("[CI] NuGet パッケージの復元が完了しました。");
        }
    }
}
