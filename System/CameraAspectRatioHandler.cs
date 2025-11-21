using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// カメラの固定アスペクト比を維持し、レターボックス/ピラーボックスで調整
    /// 異なる画面サイズやデバイスでの一貫した表示に便利
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraAspectRatioHandler : MonoBehaviour
    {
        [Header("目標アスペクト比")]
        [SerializeField] private float aspectWidth = 16.0f;
        [SerializeField] private float aspectHeight = 9.0f;
        
        [Header("レターボックス色")]
        [SerializeField] private Color letterboxColor = Color.black;
        
        private float _targetAspect;
        private Camera _camera;
        private float _lastScreenWidth;
        private float _lastScreenHeight;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _targetAspect = aspectWidth / aspectHeight;
            
            // 初期調整
            AdjustCameraViewport();
            
            // 現在の画面サイズを保存
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private void Update()
        {
            // 画面サイズが変更されたかチェック
            if (Mathf.Abs(Screen.width - _lastScreenWidth) > 0.1f || 
                Mathf.Abs(Screen.height - _lastScreenHeight) > 0.1f)
            {
                AdjustCameraViewport();
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
            }
        }

        /// <summary>
        /// 目標アスペクト比を維持するためにカメラのビューポートを調整
        /// </summary>
        private void AdjustCameraViewport()
        {
            var windowAspect = (float)Screen.width / (float)Screen.height;
            var scaleHeight = windowAspect / _targetAspect;

            var rect = _camera.rect;

            if (scaleHeight < 1.0f)
            {
                // レターボックス（上下に黒い帯）
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f;
            }
            else
            {
                // ピラーボックス（左右に黒い帯）
                var scaleWidth = 1.0f / scaleHeight;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f;
                rect.y = 0;
            }

            _camera.rect = rect;
        }

        /// <summary>
        /// レンダリング前にレターボックス色で背景をクリア
        /// </summary>
        private void OnPreCull()
        {
            GL.Clear(true, true, letterboxColor);
        }

        /// <summary>
        /// ランタイムで目標アスペクト比を設定
        /// </summary>
        public void SetAspectRatio(float width, float height)
        {
            aspectWidth = width;
            aspectHeight = height;
            _targetAspect = width / height;
            AdjustCameraViewport();
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