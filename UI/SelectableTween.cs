using UnityEngine;
using UnityEngine.EventSystems;
using LitMotion;
using System.Collections.Generic;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// LitMotionを使用した選択状態アニメーション
    /// EventSystemの選択状態に応じたスケールアニメーションを提供
    /// </summary>
    public class SelectableTween : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [Header("トゥイーン設定")]
        [SerializeField] private float scale = 1.1f;
        [SerializeField] private float duration = 0.1f;
        [SerializeField] private Ease easeType = Ease.OutQuad;

        private float _defaultScale = 1.0f;
        private readonly List<MotionHandle> _motionHandles = new();

        /// <summary>
        /// 選択時のアニメーション
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            CancelAllMotions();
            var handle = transform.ScaleTo(Vector3.one * _defaultScale * scale, duration, easeType, ignoreTimeScale: true);
            _motionHandles.Add(handle);
        }

        /// <summary>
        /// 選択解除時のアニメーション
        /// </summary>
        public void OnDeselect(BaseEventData eventData)
        {
            CancelAllMotions();
            var handle = transform.ScaleTo(Vector3.one * _defaultScale, duration, easeType, ignoreTimeScale: true);
            _motionHandles.Add(handle);
        }

        /// <summary>
        /// スケールを元に戻す
        /// </summary>
        public void ResetScale()
        {
            CancelAllMotions();
            var handle = transform.ScaleTo(Vector3.one * _defaultScale, duration, easeType, ignoreTimeScale: true);
            _motionHandles.Add(handle);
        }

        /// <summary>
        /// 即座にスケールを設定（アニメーションなし）
        /// </summary>
        /// <param name="newScale">設定するスケール</param>
        public void SetScale(float newScale)
        {
            CancelAllMotions();
            transform.localScale = Vector3.one * newScale;
        }

        /// <summary>
        /// アニメーション設定を更新
        /// </summary>
        /// <param name="newScale">新しいスケール値</param>
        /// <param name="newDuration">新しいアニメーション時間</param>
        /// <param name="newEase">新しいイージングタイプ</param>
        public void UpdateTweenSettings(float newScale = -1f, float newDuration = -1f, Ease newEase = Ease.Linear)
        {
            if (newScale > 0) scale = newScale;
            if (newDuration > 0) duration = newDuration;
            if (newEase != Ease.Linear) easeType = newEase;
        }

        /// <summary>
        /// 有効/無効を設定
        /// </summary>
        /// <param name="enabled">有効にするかどうか</param>
        public void SetTweenEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                CancelAllMotions();
                transform.localScale = Vector3.one * _defaultScale;
            }
        }

        private void CancelAllMotions()
        {
            foreach (var handle in _motionHandles)
            {
                handle.TryCancel();
            }
            _motionHandles.Clear();
        }

        private void Awake()
        {
            _defaultScale = transform.localScale.x;
        }

        private void OnDestroy()
        {
            CancelAllMotions();
        }
    }
}
