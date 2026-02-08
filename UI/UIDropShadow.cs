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

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;
    
        var output = new List<UIVertex>();
        vh.GetUIVertexStream(output);

        DropShadowEffect(output);

        vh.Clear();
        vh.AddUIVertexTriangleStream(output);
    }
    
    private void DropShadowEffect(List<UIVertex> verts)
    {
        var count = verts.Count;

        var vertsCopy = new List<UIVertex>(verts);
        verts.Clear();

        for(var i=0; i<iterations; i++)
        {
            for(var v=0; v<count; v++)
            {
                var vt = vertsCopy[v];
                var position = vt.position;
                var fac = i/(float)iterations;
                position.x *= (1 + shadowSpread.x*fac*0.01f);
                position.y *= (1 + shadowSpread.y*fac*0.01f);
                position.x += shadowDistance.x * fac;
                position.y += shadowDistance.y * fac;
                vt.position = position;
                Color32 color = shadowColor;
                color.a = (byte)(color.a /(float)iterations);
                vt.color = color;
                verts.Add(vt);
            }
        }

        verts.AddRange(vertsCopy);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        EffectDistance = shadowDistance;
        base.OnValidate();
    }
#endif
}