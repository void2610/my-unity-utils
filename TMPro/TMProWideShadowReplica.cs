using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("")]
public sealed class TMProWideShadowReplica : RawImage
{
    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        maskable = false;
    }
}
