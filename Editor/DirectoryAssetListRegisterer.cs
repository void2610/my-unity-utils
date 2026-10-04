using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// アセットの追加・削除・移動に合わせて、[SameDirectoryAssets] を付けた一覧へ同じフォルダのアセットを登録し直して保存する
    /// </summary>
    internal sealed class DirectoryAssetListRegisterer : AssetPostprocessor
    {
        private static readonly MethodInfo _registerMethod =
            typeof(DirectoryAssetListRegisterer).GetMethod(nameof(Register), BindingFlags.NonPublic | BindingFlags.Static);

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var changedPaths = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths).ToArray();
            var fieldsByType = TypeCache.GetFieldsWithAttribute<SameDirectoryAssetsAttribute>()
                .Where(f => typeof(ScriptableObject).IsAssignableFrom(f.DeclaringType))
                .GroupBy(f => f.DeclaringType);
            foreach (var fields in fieldsByType)
            {
                foreach (var guid in AssetDatabase.FindAssets($"t:{fields.Key.Name}"))
                {
                    var listPath = AssetDatabase.GUIDToAssetPath(guid);
                    var directory = Path.GetDirectoryName(listPath)?.Replace('\\', '/') + "/";
                    // 一覧自身の作成・移動でも登録し直す。保存で呼ばれ直したときは中身が変わらないので保存せず、連鎖は止まる
                    if (!changedPaths.Any(p => p.StartsWith(directory, StringComparison.Ordinal))) continue;

                    var list = AssetDatabase.LoadAssetAtPath<ScriptableObject>(listPath);
                    if (!fields.Key.IsInstanceOfType(list)) continue;

                    var before = EditorJsonUtility.ToJson(list);
                    foreach (var field in fields)
                    {
                        var elementType = field.FieldType.GetGenericArguments()[0];
                        _registerMethod.MakeGenericMethod(elementType).Invoke(null, new[] { list, field.GetValue(list) });
                    }

                    if (EditorJsonUtility.ToJson(list) == before)
                    {
                        EditorUtility.ClearDirty(list);
                        continue;
                    }

                    AssetDatabase.SaveAssetIfDirty(list);
                }
            }
        }

        // 表示名はロケール依存で並びが変わりアセットに差分が出るため、アセット名で並べる
        private static void Register<T>(ScriptableObject owner, List<T> assets) where T : ScriptableObject =>
            owner.RegisterAssetsInSameDirectory(assets, x => x.name);
    }
}
