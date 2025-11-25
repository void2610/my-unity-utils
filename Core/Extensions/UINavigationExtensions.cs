using System.Collections.Generic;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// UIナビゲーションの拡張メソッド
    /// </summary>
    public static class UINavigationExtensions
    {
        /// <summary>
        /// UI Selectableのリストにナビゲーションを設定
        /// </summary>
        public static void SetNavigation(this List<Selectable> selectables, bool isHorizontal = true, bool wrapAround = false)
        {
            if (selectables == null || selectables.Count == 0)
                return;

            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                if (selectable == null) continue;

                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;

                if (isHorizontal)
                {
                    // 水平ナビゲーション
                    if (i > 0)
                        navigation.selectOnLeft = selectables[i - 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnLeft = selectables[selectables.Count - 1];

                    if (i < selectables.Count - 1)
                        navigation.selectOnRight = selectables[i + 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnRight = selectables[0];
                }
                else
                {
                    // 垂直ナビゲーション
                    if (i > 0)
                        navigation.selectOnUp = selectables[i - 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnUp = selectables[selectables.Count - 1];

                    if (i < selectables.Count - 1)
                        navigation.selectOnDown = selectables[i + 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnDown = selectables[0];
                }

                selectable.navigation = navigation;
            }
        }

        /// <summary>
        /// UI Selectableのリストにグリッド状のナビゲーションを設定
        /// グリッドレイアウトのUIに最適
        /// </summary>
        /// <param name="selectables">対象のSelectableリスト</param>
        /// <param name="columns">グリッドの列数</param>
        /// <param name="leftBoundary">左端列の左側に配置するSelectable（オプション）</param>
        /// <param name="rightBoundary">右端列の右側に配置するSelectable（オプション）</param>
        public static void SetGridNavigation(
            this List<Selectable> selectables,
            int columns,
            Selectable leftBoundary = null,
            Selectable rightBoundary = null)
        {
            if (selectables == null || selectables.Count == 0 || columns <= 0)
                return;

            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                if (selectable == null) continue;

                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;

                // 列と行のインデックスを計算
                var col = i % columns;
                var row = i / columns;

                // 左右のナビゲーション
                if (col > 0) // 左列以外
                {
                    navigation.selectOnLeft = selectables[i - 1];
                }
                else if (leftBoundary != null) // 左端列
                {
                    navigation.selectOnLeft = leftBoundary;
                }

                if (col < columns - 1 && i + 1 < selectables.Count) // 右列以外
                {
                    navigation.selectOnRight = selectables[i + 1];
                }
                else if (col == columns - 1 && rightBoundary != null) // 右端列
                {
                    navigation.selectOnRight = rightBoundary;
                }

                // 上下のナビゲーション
                if (row > 0) // 上に行がある
                {
                    var upIndex = i - columns;
                    if (upIndex >= 0)
                        navigation.selectOnUp = selectables[upIndex];
                }

                if (i + columns < selectables.Count) // 下に行がある
                {
                    navigation.selectOnDown = selectables[i + columns];
                }

                selectable.navigation = navigation;
            }
        }
    }
}
