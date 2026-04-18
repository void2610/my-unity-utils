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
            // 原点を collider の底辺ではなく「底辺から box 高さの半分だけ下」に置く。
            // BoxCast は box の中心を origin として配置するため、origin を底辺に置くと box が
            // 半分 collider 内にはみ出し、Physics2D.queriesStartInColliders の既定値 (true) と
            // 組み合わさって自コライダーを hit と見なす偽陽性が発生する。
            // box の上端を bounds.min.y に一致させることで、接触はしても内側にめり込まないようにする。
            var origin = new Vector2(bounds.center.x, bounds.min.y - boxHeight * 0.5f);
            var size = new Vector2(bounds.size.x * widthScale, boxHeight);

            // 接地判定では trigger を地面扱いしたくないため、ContactFilter2D で明示的に除外する。
            // Physics2D.BoxCast の第 6 引数なしオーバーロードは既定で trigger も hit に含めてしまい、
            // カメラ領域などの広い trigger が常に接地扱いになる偽陽性を生んでいた。
            var filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = groundLayer,
            };
            var results = new RaycastHit2D[1];
            var hitCount = Physics2D.BoxCast(origin, size, 0f, Vector2.down, filter, results, groundCheckDistance);
            return hitCount > 0;
        }
    }
}
