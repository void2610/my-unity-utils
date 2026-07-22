using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Image の透明部分をクリック判定から除外するコンポーネント
    /// テクスチャの Import Settings で Read/Write Enabled を ON にしておく必要がある
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class AlphaHitTestThreshold : MonoBehaviour
    {
        [Range(0f, 1f)]
        [SerializeField] private float threshold = 0.5f;

        private void Awake()
        {
            GetComponent<Image>().alphaHitTestMinimumThreshold = threshold;
        }
    }
}
