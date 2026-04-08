using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Collider2D向けの拡張メソッド
    /// </summary>
    public static class Collider2DExtensions
    {
        // 足元判定に使う BoxCast の既定値。
        private const float DefaultWidthScale = 0.9f;
        private const float DefaultBoxHeight = 0.05f;

        /// <summary>
        /// コライダー足元から薄いBoxCastを飛ばして接地判定する
        /// </summary>
        /// <param name="collider">判定に使うCollider2D</param>
        /// <param name="groundCheckDistance">足元から下に調べる距離</param>
        /// <param name="groundLayer">接地対象レイヤー</param>
        /// <param name="widthScale">判定幅に掛ける倍率</param>
        /// <param name="boxHeight">判定用Boxの高さ</param>
        /// <returns>足元に地面があればtrue</returns>
        public static bool IsGroundedByBoxCast(
            this Collider2D collider,
            float groundCheckDistance,
            LayerMask groundLayer,
            float widthScale = DefaultWidthScale,
            float boxHeight = DefaultBoxHeight)
        {
            if (collider == null)
            {
                return false;
            }

            var bounds = collider.bounds;
            var origin = new Vector2(bounds.center.x, bounds.min.y);
            var size = new Vector2(bounds.size.x * widthScale, boxHeight);

            return Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
        }
    }
}
