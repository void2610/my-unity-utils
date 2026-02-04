using System;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// LayoutGroup向けの拡張メソッド
    /// </summary>
    public static class LayoutGroupExtensions
    {
        /// <summary>
        /// LayoutGroupの子要素に対してディレイ付きでアクションを実行
        /// </summary>
        /// <param name="layoutGroup">対象のLayoutGroup</param>
        /// <param name="action">各子要素に対して実行するアクション（Transform, インデックス, ディレイ時間）</param>
        /// <param name="staggerDelay">各要素間の遅延時間</param>
        /// <param name="activeOnly">アクティブな子要素のみ対象にするか</param>
        public static void ForEachChildWithDelay(
            this LayoutGroup layoutGroup,
            Action<Transform, int, float> action,
            float staggerDelay = 0.1f,
            bool activeOnly = true)
        {
            var transform = layoutGroup.transform;
            var childCount = transform.childCount;
            var activeIndex = 0;

            for (var i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                if (activeOnly && !child.gameObject.activeInHierarchy) continue;

                var delay = staggerDelay * activeIndex;
                action(child, activeIndex, delay);
                activeIndex++;
            }
        }

        /// <summary>
        /// 全てのMotionHandleをキャンセル
        /// </summary>
        /// <param name="handles">キャンセルするMotionHandleのリスト</param>
        public static void CancelAll(this List<MotionHandle> handles)
        {
            foreach (var handle in handles)
            {
                handle.TryCancel();
            }
            handles.Clear();
        }
    }
}
