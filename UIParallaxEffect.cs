using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
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
        [SerializeField] private List<ParallaxElement> elements = new();

        private List<Vector3> _basePositions = new();

        private void ApplyParallax()
        {
            // マウス位置を正規化（中心が0、範囲 [-0.5, +0.5]）
            var mousePosition = (Vector2)Input.mousePosition;
            var normalizedX = (mousePosition.x / Screen.width) - 0.5f;
            var normalizedY = (mousePosition.y / Screen.height) - 0.5f;

            // マウスと逆方向に動かすベクトル
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

        private void Awake()
        {
            // 各要素の初期位置をキャッシュ
            _basePositions.Clear();
            foreach (var element in elements)
                _basePositions.Add(element.target ? element.target.localPosition : Vector3.zero);
        }

        private void Update()
        {
            ApplyParallax();
        }
    }
}
