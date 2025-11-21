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
        /// 点の配列を取得
        /// </summary>
        public Vector2[] GetPositions()
        {
            return points;
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

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (points == null || points.Length < 2)
                return;

            for (int i = 0; i < points.Length - 1; i++)
            {
                CreateLineSegment(points[i], points[i + 1], vh);

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

        /// <summary>
        /// 2点間のラインセグメントを作成
        /// </summary>
        private void CreateLineSegment(Vector3 point1, Vector3 point2, VertexHelper vh)
        {
            Vector3 offset = center ? (rectTransform.sizeDelta / 2) : Vector2.zero;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            Quaternion point1Rotation = Quaternion.Euler(0, 0, RotatePointTowards(point1, point2) + 90);
            vertex.position = point1Rotation * new Vector3(-thickness / 2, 0);
            vertex.position += point1 - offset;
            vh.AddVert(vertex);
            vertex.position = point1Rotation * new Vector3(thickness / 2, 0);
            vertex.position += point1 - offset;
            vh.AddVert(vertex);

            Quaternion point2Rotation = Quaternion.Euler(0, 0, RotatePointTowards(point2, point1) - 90);
            vertex.position = point2Rotation * new Vector3(-thickness / 2, 0);
            vertex.position += point2 - offset;
            vh.AddVert(vertex);
            vertex.position = point2Rotation * new Vector3(thickness / 2, 0);
            vertex.position += point2 - offset;
            vh.AddVert(vertex);

            vertex.position = point2 - offset;
            vh.AddVert(vertex);
        }

        /// <summary>
        /// 頂点を目標方向に回転させる角度を取得
        /// </summary>
        private float RotatePointTowards(Vector2 vertex, Vector2 target)
        {
            return (float)(Mathf.Atan2(target.y - vertex.y, target.x - vertex.x) * (180 / Mathf.PI));
        }
    }
}