using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// UI Canvas上でカスタムラインを描画するコンポーネント
    /// データ可視化、ミニマップ、手描き風UI効果などに使用可能
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : MaskableGraphic
    {
        [Header("ライン設定")]
        [SerializeField] private Vector2[] points;
        [SerializeField] private float thickness = 10f;
        [SerializeField] private bool center = true;

        /// <summary>
        /// 点の配列を取得
        /// </summary>
        public Vector2[] GetPositions() => points;

        /// <summary>
        /// 指定したインデックスの点の位置を設定
        /// </summary>
        /// <param name="idx">点のインデックス</param>
        /// <param name="point">新しい位置</param>
        public void SetPosition(int idx, Vector2 point)
        {
            if (points != null && idx >= 0 && idx < points.Length)
            {
                points[idx] = point;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// 全ての点の位置を一度に設定
        /// </summary>
        /// <param name="newPoints">新しい点の配列</param>
        public void SetPositions(Vector2[] newPoints)
        {
            points = newPoints;
            SetVerticesDirty();
        }

        /// <summary>
        /// ラインの太さを設定
        /// </summary>
        /// <param name="newThickness">新しい太さ</param>
        public void SetThickness(float newThickness)
        {
            thickness = newThickness;
            SetVerticesDirty();
        }

        /// <summary>
        /// 2点間のラインセグメントを作成
        /// </summary>
        /// <param name="point1">始点</param>
        /// <param name="point2">終点</param>
        /// <param name="vh">頂点ヘルパー</param>
        /// <param name="u1">始点のU座標（0-1）</param>
        /// <param name="u2">終点のU座標（0-1）</param>
        private void CreateLineSegment(Vector3 point1, Vector3 point2, VertexHelper vh, float u1, float u2)
        {
            Vector3 offset = center ? (rectTransform.sizeDelta / 2) : Vector2.zero;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            // 始点の下端 (u1, 0)
            Quaternion point1Rotation = Quaternion.Euler(0, 0, RotatePointTowards(point1, point2) + 90);
            vertex.position = point1Rotation * new Vector3(-thickness / 2, 0);
            vertex.position += point1 - offset;
            vertex.uv0 = new Vector2(u1, 0f);
            vh.AddVert(vertex);

            // 始点の上端 (u1, 1)
            vertex.position = point1Rotation * new Vector3(thickness / 2, 0);
            vertex.position += point1 - offset;
            vertex.uv0 = new Vector2(u1, 1f);
            vh.AddVert(vertex);

            // 終点の下端 (u2, 0)
            Quaternion point2Rotation = Quaternion.Euler(0, 0, RotatePointTowards(point2, point1) - 90);
            vertex.position = point2Rotation * new Vector3(-thickness / 2, 0);
            vertex.position += point2 - offset;
            vertex.uv0 = new Vector2(u2, 0f);
            vh.AddVert(vertex);

            // 終点の上端 (u2, 1)
            vertex.position = point2Rotation * new Vector3(thickness / 2, 0);
            vertex.position += point2 - offset;
            vertex.uv0 = new Vector2(u2, 1f);
            vh.AddVert(vertex);

            // 終点の中央 (u2, 0.5)
            vertex.position = point2 - offset;
            vertex.uv0 = new Vector2(u2, 0.5f);
            vh.AddVert(vertex);
        }

        /// <summary>
        /// 頂点を目標方向に回転させる角度を取得
        /// </summary>
        private float RotatePointTowards(Vector2 vertex, Vector2 target)
        {
            return (float)(Mathf.Atan2(target.y - vertex.y, target.x - vertex.x) * (180 / Mathf.PI));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (points == null || points.Length < 2)
                return;

            // ライン全体の長さを計算
            float totalLength = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                totalLength += Vector2.Distance(points[i], points[i + 1]);
            }

            // 各セグメントのU座標を計算しながら描画
            float currentLength = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                float segmentLength = Vector2.Distance(points[i], points[i + 1]);
                float u1 = totalLength > 0 ? currentLength / totalLength : 0f;
                float u2 = totalLength > 0 ? (currentLength + segmentLength) / totalLength : 1f;

                CreateLineSegment(points[i], points[i + 1], vh, u1, u2);
                currentLength += segmentLength;

                int index = i * 5;

                vh.AddTriangle(index, index + 1, index + 3);
                vh.AddTriangle(index + 3, index + 2, index);

                if (i != 0)
                {
                    vh.AddTriangle(index, index - 1, index - 3);
                    vh.AddTriangle(index + 1, index - 1, index - 2);
                }
            }
        }
    }
}
