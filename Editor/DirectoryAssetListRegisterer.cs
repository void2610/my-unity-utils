using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Void2610.UnityTemplate.Editor
{
    /// <summary>
    /// アセットの追加・削除・移動に合わせて、同じフォルダの IDirectoryAssetList を登録し直して保存する
    /// </summary>
    internal sealed class DirectoryAssetListRegisterer : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var changedPaths = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths).ToArray();
            var visitedPaths = new HashSet<string>();
            var listTypes = TypeCache.GetTypesDerivedFrom<IDirectoryAssetList>()
                .Where(t => !t.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(t));
            foreach (var type in listTypes)
            {
                foreach (var guid in AssetDatabase.FindAssets($"t:{type.Name}"))
                {
                    var listPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!visitedPaths.Add(listPath)) continue;

                    var directory = Path.GetDirectoryName(listPath)?.Replace('\\', '/') + "/";
                    // 一覧自身の作成・移動でも登録し直す。保存で呼ばれ直したときは中身が変わらないので保存せず、連鎖は止まる
                    if (!changedPaths.Any(p => p.StartsWith(directory, StringComparison.Ordinal))) continue;

                    if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(listPath) is not IDirectoryAssetList list) continue;

                    var asset = (ScriptableObject)list;
                    var before = EditorJsonUtility.ToJson(asset);
                    list.RegisterAssets();
                    if (EditorJsonUtility.ToJson(asset) == before)
                    {
                        EditorUtility.ClearDirty(asset);
                        continue;
                    }

                    AssetDatabase.SaveAssetIfDirty(asset);
                }
            }
        }
    }
}
