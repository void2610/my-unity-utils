using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Graphic（UI要素）操作の拡張メソッド
    /// </summary>
    public static class GraphicExtensions
    {
        /// <summary>
        /// Graphic（UI要素）のアルファ値を設定
        /// </summary>
        public static void SetAlpha(this Graphic graphic, float alpha)
        {
            if (graphic == null) return;

            var color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }
    }
}
