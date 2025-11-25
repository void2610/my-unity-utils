using System;
using System.Collections.Generic;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// リスト操作の拡張メソッド
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// リストの全要素をコンソールに出力
        /// </summary>
        public static void Print<T>(this List<T> list)
        {
            if (list == null)
            {
                Debug.Log("リストがnullです");
                return;
            }

            Debug.Log($"リストの内容 ({list.Count}個の要素):");
            for (int i = 0; i < list.Count; i++)
            {
                Debug.Log($"[{i}] {list[i]}");
            }
        }

        /// <summary>
        /// 重み付きリストから確率に基づいてランダムなアイテムを選択
        /// </summary>
        public static T ChooseByWeight<T>(this List<T> list, Func<T, float> weightSelector)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("リストが空またはnullです");

            float totalWeight = 0f;
            foreach (var item in list)
            {
                totalWeight += weightSelector(item);
            }

            if (totalWeight <= 0f)
                throw new ArgumentException("合計重みは0より大きい必要があります");

            var randomValue = UnityEngine.Random.Range(0f, totalWeight);
            var cumulativeWeight = 0f;

            foreach (var item in list)
            {
                cumulativeWeight += weightSelector(item);
                if (randomValue <= cumulativeWeight)
                    return item;
            }

            // フォールバック（浮動小数点精度の問題対策）
            return list[list.Count - 1];
        }
    }
}
