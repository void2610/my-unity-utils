using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 拡張Buttonコンポーネント
    /// 独自の使用可否状態（IsAvailable）を持ち、常に選択可能でありながら
    /// 見た目だけDisabled状態にできる
    /// </summary>
    public class MyButton : Button
    {
        private bool _isAvailable = true;
        private bool _forceDisabledVisual = false;

        /// <summary>
        /// 独自の使用可否（interactableとは別に制御）
        /// </summary>
        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                if (_isAvailable == value) return;
                _isAvailable = value;
                UpdateVisualState();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            UpdateVisualState();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (!IsAvailable) return;
            base.OnSubmit(eventData);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!IsAvailable) return;
            base.OnPointerClick(eventData);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
        }

        /// <summary>
        /// 見た目を更新する
        /// </summary>
        private void UpdateVisualState()
        {
            base.interactable = true;

            _forceDisabledVisual = !IsAvailable;
            var state = _forceDisabledVisual ? SelectionState.Disabled : SelectionState.Normal;
            DoStateTransition(state, true);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (_forceDisabledVisual)
            {
                base.DoStateTransition(SelectionState.Disabled, instant);
                return;
            }

            base.DoStateTransition(state, instant);
        }
    }
}