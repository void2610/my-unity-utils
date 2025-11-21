using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 複数の子Graphicコンポーネントに対して一括で色遷移を適用するボタン
    /// MyButtonを継承し、ボタンの状態変化を全ての子要素に伝播させる
    ///
    /// 使用例:
    /// - アイコン+テキスト+背景など複数の要素を持つ複雑なボタン
    /// - ホバー時に全ての子要素を同時にハイライトさせたい場合
    /// - 統一感のあるボタンアニメーションの実装
    ///
    /// 使い方:
    /// 1. MyButtonの代わりにMultiImageButtonコンポーネントをアタッチ
    /// 2. ボタンの下に複数のImage/Text等のGraphicコンポーネントを配置
    /// 3. ColorBlockで状態ごとの色を設定
    /// 4. 全ての子Graphicに自動的に色遷移が適用される
    ///
    /// 注意:
    /// - MyButtonコンポーネントが存在することを前提としています
    /// - 子要素のGraphicコンポーネント全てに色が適用されます
    /// </summary>
    public class MultiImageButton : MyButton
    {
        /// <summary>
        /// ボタンの状態遷移時に呼ばれる
        /// 全ての子Graphicコンポーネントに対して色遷移を適用
        /// </summary>
        /// <param name="state">ボタンの状態</param>
        /// <param name="instant">即座に適用するか</param>
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            var targetColor =
                state == SelectionState.Disabled ? colors.disabledColor :
                state == SelectionState.Highlighted ? colors.highlightedColor :
                state == SelectionState.Normal ? colors.normalColor :
                state == SelectionState.Pressed ? colors.pressedColor :
                state == SelectionState.Selected ? colors.selectedColor : Color.white;

            foreach (var graphic in GetComponentsInChildren<Graphic>())
            {
                graphic.CrossFadeColor(targetColor, instant ? 0f : colors.fadeDuration, true, true);
            }
        }
    }
}
