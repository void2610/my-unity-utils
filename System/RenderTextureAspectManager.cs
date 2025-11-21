using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// RenderTextureを表示するRawImageのアスペクト比を動的に調整
    /// ポストエフェクトやセカンダリカメラの表示品質を維持
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class RenderTextureAspectManager : MonoBehaviour
    {
        [Header("アスペクト比設定")]
        [SerializeField] private Vector2 aspectRatio = new Vector2(16, 9);
        [SerializeField] private float defaultScale = 1.0f;
        
        private RawImage _renderTexture;
        private RectTransform _renderTextureRectTransform;
        private float _lastScreenWidth;
        private float _lastScreenHeight;

        private void Start()
        {
            _renderTexture = GetComponent<RawImage>();
            _renderTextureRectTransform = _renderTexture.GetComponent<RectTransform>();
            _renderTextureRectTransform.offsetMin = Vector2.zero;
            _renderTextureRectTransform.offsetMax = Vector2.zero;
            
            AdjustRenderTextureSize();
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private void Update()
        {
            // 画面サイズが変更された場合のみ調整
            if (Mathf.Abs(Screen.width - _lastScreenWidth) > 0.1f || 
                Mathf.Abs(Screen.height - _lastScreenHeight) > 0.1f)
            {
                AdjustRenderTextureSize();
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
            }
        }

        /// <summary>
        /// RenderTextureのアスペクト比を調整
        /// </summary>
        private void AdjustRenderTextureSize()
        {
            var screenAspect = (float)Screen.width / (float)Screen.height;
            var targetAspect = aspectRatio.x / aspectRatio.y;

            if (screenAspect > targetAspect)
            {
                // 横長の場合（ピラーボックス）
                var width = targetAspect / screenAspect;
                _renderTextureRectTransform.anchorMin = new Vector2((1 - width) / 2, 0);
                _renderTextureRectTransform.anchorMax = new Vector2((1 + width) / 2, 1);
                var scaleFactor = Screen.width / (Screen.height * targetAspect);
                
                _renderTextureRectTransform.localScale = new Vector3(scaleFactor * defaultScale, defaultScale, 1f);
            }
            else
            {
                // 縦長の場合（レターボックス）
                var height = screenAspect / targetAspect;
                _renderTextureRectTransform.anchorMin = new Vector2(0, (1 - height) / 2);
                _renderTextureRectTransform.anchorMax = new Vector2(1, (1 + height) / 2);
                
                var scaleFactor = Screen.height / (Screen.width / targetAspect);
                _renderTextureRectTransform.localScale = new Vector3(defaultScale, scaleFactor * defaultScale, 1f);
            }
        }
        
        /// <summary>
        /// ランタイムでアスペクト比を変更
        /// </summary>
        /// <param name="width">幅の比率</param>
        /// <param name="height">高さの比率</param>
        public void SetAspectRatio(float width, float height)
        {
            aspectRatio = new Vector2(width, height);
            AdjustRenderTextureSize();
        }
        
        /// <summary>
        /// スケール値を設定
        /// </summary>
        /// <param name="scale">スケール値</param>
        public void SetDefaultScale(float scale)
        {
            defaultScale = scale;
            AdjustRenderTextureSize();
        }
        
        /// <summary>
        /// 現在のアスペクト比を取得
        /// </summary>
        public float GetCurrentAspectRatio()
        {
            return aspectRatio.x / aspectRatio.y;
        }
    }
}