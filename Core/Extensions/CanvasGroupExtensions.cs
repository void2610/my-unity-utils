using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// CanvasGroupの拡張メソッド
    /// </summary>
    public static class CanvasGroupExtensions
    {
        /// <summary>
        /// alpha=1, interactable=true, blocksRaycasts=trueに即時設定
        /// </summary>
        public static void Show(this CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// alpha=0, interactable=false, blocksRaycasts=falseに即時設定
        /// </summary>
        public static void Hide(this CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
