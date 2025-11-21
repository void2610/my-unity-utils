using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LitMotion;
using LitMotion.Extensions;
using System.Collections.Generic;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// LitMotionを使用した高機能ボタンアニメーション
    /// ポインターホバー、クリック時のスケールアニメーションを提供
    /// </summary>
    public class ButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Button Settings")]
        [SerializeField] private bool tweenByPointer = true; // ポインターでアニメーション
        [SerializeField] private bool tweenByClick = true; // クリック時にアニメーション

        [Header("Tween Settings")]
        [SerializeField] private float scale = 1.1f; // スケール倍率
        [SerializeField] private float duration = 0.5f; // アニメーション時間
        [SerializeField] private Ease easeType = Ease.OutElastic; // イージングタイプ

        [Header("Raycast Settings")]
        [SerializeField] private GraphicRaycaster raycaster;
        [SerializeField] private EventSystem eventSystem;

        private float _defaultScale = 1.0f;
        private readonly List<MotionHandle> _motionHandles = new();
        private Button _button;

        /// <summary>
        /// クリック時のアニメーション
        /// </summary>
        private void OnClick()
        {
            if (!tweenByClick) return;
            
            // 既存のモーションをキャンセル
            CancelAllMotions();
            
            // スケールアニメーション
            var handle = LMotion.Create(transform.localScale, Vector3.one * _defaultScale * scale, duration)
                .WithEase(easeType)
                .BindToLocalScale(transform);
            _motionHandles.Add(handle);
        }

        /// <summary>
        /// ポインターがボタンに入った時
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!tweenByPointer || (_button && !_button.interactable)) return;
            
            // 既存のモーションをキャンセル
            CancelAllMotions();
            
            // スケールアップアニメーション
            var handle = LMotion.Create(transform.localScale, Vector3.one * _defaultScale * scale, duration)
                .WithEase(easeType)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(transform);
            _motionHandles.Add(handle);
        }

        /// <summary>
        /// ポインターがボタンから出た時
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!tweenByPointer || (_button && !_button.interactable)) return;
            
            // 既存のモーションをキャンセル
            CancelAllMotions();
            
            // スケールダウンアニメーション
            var handle = LMotion.Create(transform.localScale, Vector3.one * _defaultScale, duration)
                .WithEase(easeType)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(transform);
            _motionHandles.Add(handle);
        }

        /// <summary>
        /// スケールを元に戻す
        /// </summary>
        public void ResetScale()
        {
            // 既存のモーションをキャンセル
            CancelAllMotions();
            
            // デフォルトスケールに戻すアニメーション
            var handle = LMotion.Create(transform.localScale, Vector3.one * _defaultScale, duration)
                .WithEase(easeType)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(transform);
            _motionHandles.Add(handle);
        }

        /// <summary>
        /// 即座にスケールを設定（アニメーションなし）
        /// </summary>
        /// <param name="scale">設定するスケール</param>
        public void SetScale(float scale)
        {
            CancelAllMotions();
            transform.localScale = Vector3.one * scale;
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
        /// すべてのモーションをキャンセル
        /// </summary>
        private void CancelAllMotions()
        {
            foreach (var handle in _motionHandles)
            {
                if (handle.IsActive())
                {
                    handle.Cancel();
                }
            }
            _motionHandles.Clear();
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _defaultScale = this.transform.localScale.x;
            
            if (tweenByClick && _button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        private void Start()
        {
            _defaultScale = this.transform.localScale.x;
        }

        private void OnDestroy()
        {
            CancelAllMotions();
            
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
            }
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
    }
}