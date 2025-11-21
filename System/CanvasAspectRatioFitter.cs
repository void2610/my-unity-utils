using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Canvas用のアスペクト比調整コンポーネント
    /// UIが異なる画面サイズでも適切に表示されるよう調整
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasAspectRatioFitter : MonoBehaviour
    {
        [Header("アスペクト比設定")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float aspectWidth = 16.0f;
        [SerializeField] private float aspectHeight = 9.0f;
        
        private Canvas _canvas;
        private CanvasScaler _canvasScaler;
        private RectTransform _rectTransform;
        private float _targetAspect;
        private float _lastScreenWidth;
        private float _lastScreenHeight;
        
        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvasScaler = GetComponent<CanvasScaler>();
            _rectTransform = GetComponent<RectTransform>();
            _targetAspect = aspectWidth / aspectHeight;
            
            // カメラが未設定の場合は自動で検索
            if (targetCamera == null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetCamera = Camera.main;
            }
            
            AdjustCanvas();
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }
        
        private void Update()
        {
            // 画面サイズが変更されたかチェック
            if (Mathf.Abs(Screen.width - _lastScreenWidth) > 0.1f || 
                Mathf.Abs(Screen.height - _lastScreenHeight) > 0.1f)
            {
                AdjustCanvas();
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
            }
        }
        
        /// <summary>
        /// Canvasのサイズと位置を調整
        /// </summary>
        private void AdjustCanvas()
        {
            var windowAspect = (float)Screen.width / (float)Screen.height;
            var scaleHeight = windowAspect / _targetAspect;
            
            // Screen Space - Cameraモードの場合
            if (_canvas.renderMode == RenderMode.ScreenSpaceCamera && targetCamera != null)
            {
                AdjustForCameraMode(scaleHeight);
                return;
            }
            
            // Screen Space - Overlayモードの場合
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                AdjustForOverlayMode(scaleHeight);
            }
        }

        /// <summary>
        /// Screen Space - Cameraモード用の調整
        /// </summary>
        private void AdjustForCameraMode(float scaleHeight)
        {
            var cameraRect = targetCamera.rect;
            
            // デフォルト状態にリセット
            ResetRectTransform();
            
            // カメラのビューポートと同じオフセットを適用
            var xOffset = cameraRect.x * Screen.width;
            var yOffset = cameraRect.y * Screen.height;
            var widthScale = cameraRect.width;
            var heightScale = cameraRect.height;
            
            _rectTransform.offsetMin = new Vector2(xOffset, yOffset);
            _rectTransform.offsetMax = new Vector2(
                -(Screen.width * (1 - widthScale) - xOffset), 
                -(Screen.height * (1 - heightScale) - yOffset)
            );
            
            // CanvasScalerを調整
            UpdateCanvasScaler(scaleHeight);
        }

        /// <summary>
        /// Screen Space - Overlayモード用の調整
        /// </summary>
        private void AdjustForOverlayMode(float scaleHeight)
        {
            // デフォルト状態にリセット
            ResetRectTransform();
            
            if (scaleHeight < 1.0f)
            {
                // レターボックス（上下に黒い帯）
                var scaledHeight = Screen.height * scaleHeight;
                var yOffset = (Screen.height - scaledHeight) * 0.5f;
                
                _rectTransform.offsetMin = new Vector2(0, yOffset);
                _rectTransform.offsetMax = new Vector2(0, -yOffset);
            }
            else
            {
                // ピラーボックス（左右に黒い帯）
                var scaleWidth = 1.0f / scaleHeight;
                var scaledWidth = Screen.width * scaleWidth;
                var xOffset = (Screen.width - scaledWidth) * 0.5f;
                
                _rectTransform.offsetMin = new Vector2(xOffset, 0);
                _rectTransform.offsetMax = new Vector2(-xOffset, 0);
            }
            
            // CanvasScalerを調整
            UpdateCanvasScaler(scaleHeight);
        }

        /// <summary>
        /// RectTransformをデフォルト状態にリセット
        /// </summary>
        private void ResetRectTransform()
        {
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.sizeDelta = Vector2.zero;
        }

        /// <summary>
        /// CanvasScalerの設定を更新
        /// </summary>
        private void UpdateCanvasScaler(float scaleHeight)
        {
            if (_canvasScaler != null && _canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                // アスペクト比に基づいてmatchWidthOrHeightを調整
                if (scaleHeight < 1.0f)
                {
                    // レターボックス時は幅優先
                    _canvasScaler.matchWidthOrHeight = 0f;
                }
                else
                {
                    // ピラーボックス時は高さ優先
                    _canvasScaler.matchWidthOrHeight = 1f;
                }
            }
        }

        /// <summary>
        /// ランタイムで目標アスペクト比を変更
        /// </summary>
        public void SetAspectRatio(float width, float height)
        {
            aspectWidth = width;
            aspectHeight = height;
            _targetAspect = width / height;
            AdjustCanvas();
        }

        /// <summary>
        /// 現在の目標アスペクト比を取得
        /// </summary>
        public float GetTargetAspectRatio()
        {
            return _targetAspect;
        }
    }
}