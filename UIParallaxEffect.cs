using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// UI要素を入力座標に応じて視差移動させる。
    /// マウス入力または外部から与えた疑似スクリーン座標を基準に動作する。
    /// </summary>
    public class UIParallaxEffect : MonoBehaviour
    {
        [Serializable]
        public class ParallaxElement
        {
            public RectTransform target;
            public float intensity = 1f;
        }

        [SerializeField] private float multiplier = 1f;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool useMousePosition = true;
        [SerializeField] private List<ParallaxElement> elements = new();

        private List<Vector3> _basePositions = new();
        private bool _hasEmulatedScreenPosition;
        private Vector2 _emulatedScreenPosition;

        /// <summary>
        /// マウス位置を基準にするかどうかを切り替える。
        /// </summary>
        public void SetMousePositionEnabled(bool enabled) => useMousePosition = enabled;

        /// <summary>
        /// マウス位置の代わりに使用する疑似スクリーン座標を設定する。
        /// </summary>
        public void SetEmulatedScreenPosition(Vector2 screenPosition)
        {
            _emulatedScreenPosition = screenPosition;
            _hasEmulatedScreenPosition = true;
        }

        /// <summary>
        /// 疑似スクリーン座標の使用を解除する。
        /// </summary>
        public void ClearEmulatedScreenPosition() => _hasEmulatedScreenPosition = false;

        private void ApplyParallax()
        {
            // 参照するスクリーン座標を正規化（中心が0、範囲 [-0.5, +0.5]）
            var referenceScreenPosition = GetReferenceScreenPosition();
            var normalizedX = (referenceScreenPosition.x / Screen.width) - 0.5f;
            var normalizedY = (referenceScreenPosition.y / Screen.height) - 0.5f;

            // 参照座標と逆方向に動かすベクトル
            var offsetDirection = new Vector3(-normalizedX, -normalizedY, 0f);

            // 各要素をスムーズに移動
            for (var i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (!element.target) continue;

                // オフセット量 = 逆方向ベクトル × 強度 × 乗数
                var offset = offsetDirection * (element.intensity * multiplier);
                // 目標位置 = 初期位置 + オフセット
                var targetPosition = _basePositions[i] + offset;

                // 滑らかに補間
                element.target.localPosition = Vector3.Lerp(
                    element.target.localPosition,
                    targetPosition,
                    Time.deltaTime * smoothSpeed
                );
            }
        }

        private Vector2 GetReferenceScreenPosition()
        {
            // 疑似座標があれば最優先で使用
            if (_hasEmulatedScreenPosition)
            {
                return _emulatedScreenPosition;
            }

            // マウス入力が有効ならマウス位置を使用
            if (useMousePosition)
            {
                return Input.mousePosition;
            }

            // どちらも使わない場合は画面中心を基準にする
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private void Awake()
        {
            // 各要素の初期位置をキャッシュ
            _basePositions.Clear();
            foreach (var element in elements)
            {
                _basePositions.Add(element.target ? element.target.localPosition : Vector3.zero);
            }
        }

        private void Update()
        {
            ApplyParallax();
        }
    }
}
