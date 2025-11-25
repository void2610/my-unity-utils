#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// エディター専用の拡張メソッド
    /// </summary>
    public static class EditorExtensions
    {
        /// <summary>
        /// 指定したScriptableObjectと同じディレクトリ内の指定した型のアセットを一括登録
        /// ScriptableObjectベースのデータ管理で便利
        /// </summary>
        /// <typeparam name="T">登録するScriptableObjectの型</typeparam>
        /// <param name="targetObject">検索基準となるScriptableObject</param>
        /// <param name="targetList">登録先のリスト</param>
        /// <param name="sortKeySelector">ソート用のキーセレクタ（nullの場合はソートしない）</param>
        public static void RegisterAssetsInSameDirectory<T>(
            this ScriptableObject targetObject,
            List<T> targetList,
            Func<T, string> sortKeySelector = null) where T : ScriptableObject
        {
            // このScriptableObjectと同じディレクトリパスを取得
            var path = AssetDatabase.GetAssetPath(targetObject);
            path = System.IO.Path.GetDirectoryName(path);

            // 指定ディレクトリ内の指定した型のアセットを検索
            var typeName = typeof(T).Name;
            var guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { path });

            // 検索結果をリストに追加
            targetList.Clear();
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset) targetList.Add(asset);
            }

            // ソートが指定されている場合はソート実行
            if (sortKeySelector != null)
            {
                targetList.Sort((a, b) => string.Compare(sortKeySelector(a), sortKeySelector(b)));
            }

            // ScriptableObjectを更新
            EditorUtility.SetDirty(targetObject);
        }
    }
}
#endif
