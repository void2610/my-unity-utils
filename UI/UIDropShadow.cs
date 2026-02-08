using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/UI Drop Shadow", 14)]
public class UIDropShadow : BaseMeshEffect
{
    [SerializeField] private Color shadowColor = new(0f, 0f, 0f, 0.5f);
    [SerializeField] private Vector2 shadowDistance = new(1f, -1f);
    [SerializeField] private bool useGraphicAlpha = true;
    
    public Vector2 EffectDistance
    {
        set
        {
            shadowDistance = value;
            if (graphic) graphic.SetVerticesDirty();
        }
    }

    public int iterations = 5;
    public Vector2 shadowSpread = Vector2.one;

    private static readonly List<UIDropShadow> _tempShadowList = new();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        // 同じオブジェクト上の全UIDropShadowを取得
        GetComponents(_tempShadowList);

        // 自分より前にアクティブなUIDropShadowがあればスキップ
        foreach (var shadow in _tempShadowList)
        {
            if (shadow == this) break;
            if (shadow.IsActive())
            {
                _tempShadowList.Clear();
                return;
            }
        }

        var output = new List<UIVertex>();
        vh.GetUIVertexStream(output);
        var originalVerts = new List<UIVertex>(output);
        output.Clear();

        // 各UIDropShadowの影を追加（コンポーネント順 = 最初が最背面）
        foreach (var shadow in _tempShadowList)
        {
            if (!shadow.IsActive()) continue;
            shadow.AppendShadowVertices(originalVerts, output);
        }

        // オリジナル頂点を最前面に追加
        output.AddRange(originalVerts);

        vh.Clear();
        vh.AddUIVertexTriangleStream(output);
        _tempShadowList.Clear();
    }

    private void AppendShadowVertices(List<UIVertex> originalVerts, List<UIVertex> output)
    {
        var count = originalVerts.Count;
        if (count == 0) return;

        // バウンディングボックスの中心を計算
        var min = originalVerts[0].position;
        var max = min;
        for (var v = 1; v < count; v++)
        {
            var pos = originalVerts[v].position;
            if (pos.x < min.x) min.x = pos.x;
            if (pos.y < min.y) min.y = pos.y;
            if (pos.x > max.x) max.x = pos.x;
            if (pos.y > max.y) max.y = pos.y;
        }
        var center = (min + max) * 0.5f;

        for (var i = 0; i < iterations; i++)
        {
            for (var v = 0; v < count; v++)
            {
                var vt = originalVerts[v];
                var position = vt.position;
                var fac = i / (float)iterations;
                // 中心基準でスケーリング（位置に依存しない均一なspread）
                var scaleX = 1 + shadowSpread.x * fac * 0.01f;
                var scaleY = 1 + shadowSpread.y * fac * 0.01f;
                position.x = center.x + (position.x - center.x) * scaleX;
                position.y = center.y + (position.y - center.y) * scaleY;
                position.x += shadowDistance.x * fac;
                position.y += shadowDistance.y * fac;
                vt.position = position;
                var color = (Color32)shadowColor;
                color.a = (byte)(color.a / (float)iterations);
                vt.color = color;
                output.Add(vt);
            }
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        EffectDistance = shadowDistance;
        base.OnValidate();
    }
#endif
}