using System;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ドラッグ中の見た目を受け持つ。掴んだら持ち上げてポインタに付いていき、置き損ねたら元の場所へすべって戻る
    /// ドラッグする要素そのもの、または指に付いてくるゴーストに付け、ドラッグのイベントを受けた側から呼ぶ
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class DragFollower : MonoBehaviour
    {
        [Header("掴んでいる間の見た目")]
        [SerializeField] private float holdScale = 1f; // 掴む前の大きさに対する倍率
        [SerializeField] private float holdScaleDuration; // 0 なら掴んだ瞬間にその大きさにする
        [SerializeField] private Ease holdScaleEase = Ease.OutQuad;
        [SerializeField, Range(0f, 1f)] private float holdAlpha = 1f; // 置き場所に戻るまで続く
        // 親の外まで運ぶ要素は、ルート Canvas 直下へ移して他の UI より手前に描く
        [SerializeField] private bool liftToRootCanvas;

        [Header("置き損ねたときの戻り")]
        [SerializeField] private float returnDuration = 0.25f;
        [SerializeField] private Ease returnEase = Ease.OutCubic;

        /// <summary>掴んでから離すまでの間か</summary>
        public bool IsHeld { get; private set; }

        private RectTransform _rect;
        private CanvasGroup _group;
        private Transform _homeParent;
        private int _homeSiblingIndex;
        private Vector2 _homeAnchoredPosition;
        // ルート Canvas 直下へ移した後も同じ場所を指せるようワールド座標でも持つ
        private Vector3 _homePosition;
        private Vector3 _homeScale;
        private float _homeAlpha;
        // 掴んでからホームへ戻りきるまでの間か
        private bool _away;
        // 掴んだときの大きさの変化と離したときの戻りは同時に走らないため、1 本のハンドルで持つ
        private MotionHandle _motion;

        /// <summary>掴む。持ち上げた見た目にして、下の受け取り先へレイキャストを通す</summary>
        public void PickUp(PointerEventData eventData)
        {
            _rect = (RectTransform)transform;
            _group = GetComponent<CanvasGroup>();
            _motion.TryCancel();
            // 戻りの途中で掴み直したら (ゴーストの使い回し等)、戻る前のホームを基準にし続ける
            if (!_away) RememberHome();
            _away = true;
            IsHeld = true;
            if (liftToRootCanvas) transform.SetParent(GetComponentInParent<Canvas>().rootCanvas.transform, true);
            transform.SetAsLastSibling();
            _group.alpha = holdAlpha;
            _group.blocksRaycasts = false;
            var target = _homeScale * holdScale;
            if (holdScaleDuration > 0f)
            {
                _motion = LMotion.Create(transform.localScale, target, holdScaleDuration)
                    .WithEase(holdScaleEase)
                    .BindToLocalScale(transform)
                    .AddTo(gameObject);
            }
            else
            {
                transform.localScale = target;
            }
            Follow(eventData);
        }

        /// <summary>掴んでいる間、ポインタの位置へ付いていく</summary>
        public void Follow(PointerEventData eventData)
        {
            if (!IsHeld) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, eventData.position, eventData.pressEventCamera, out var local))
            {
                _rect.localPosition = local;
            }
        }

        /// <summary>受け取り先に置けたとき。戻る動きは置き損ねに見えるため見せず、その場でホームへ戻して再び掴めるようにする</summary>
        public void Drop()
        {
            if (!IsHeld) return;
            _motion.TryCancel();
            Land();
        }

        /// <summary>置き損ねたとき。離した位置から掴んだ場所へすべらせて戻し、着いたら再び掴めるようにする</summary>
        public void Return(Action onLanded = null) => ReturnTo(_homePosition, onLanded);

        /// <summary>置き損ねた要素を、掴んだ場所とは別の位置 (ゴーストなら語の元のボタン等) へすべらせて戻す</summary>
        public void ReturnTo(Vector3 worldPosition, Action onLanded = null)
        {
            if (!IsHeld) return;
            IsHeld = false;
            _motion.TryCancel();
            var startPosition = transform.position;
            var startScale = transform.localScale;
            // 戻りきるまではレイキャストを通したままにして、戻り途中の要素を掴ませない
            _motion = LMotion.Create(0f, 1f, returnDuration)
                .WithEase(returnEase)
                .WithOnComplete(() =>
                {
                    Land();
                    onLanded?.Invoke();
                })
                .Bind(t =>
                {
                    transform.position = Vector3.LerpUnclamped(startPosition, worldPosition, t);
                    transform.localScale = Vector3.LerpUnclamped(startScale, _homeScale, t);
                })
                .AddTo(gameObject);
        }

        /// <summary>置き損ねを呼び出し側の演出で見せるとき。持ち上げた大きさだけ戻してその場に残す。演出の後に <see cref="SnapHome"/> で戻す</summary>
        public void Release()
        {
            if (!IsHeld) return;
            IsHeld = false;
            _motion.TryCancel();
            transform.localScale = _homeScale;
        }

        /// <summary>途中の動きを打ち切り、その場でホームへ置く。打ち切りは呼び出し側の都合で起きるため、入力の可否は呼び出し側が決める</summary>
        public void SnapHome()
        {
            _motion.TryCancel();
            IsHeld = false;
            _away = false;
            // ホームへ置いた後に呼び出し側の演出で動かされることもあるため、離れているかに関わらず置き直す
            if (_homeParent != null) PlaceHome();
        }

        private void RememberHome()
        {
            _homeParent = transform.parent;
            _homeSiblingIndex = transform.GetSiblingIndex();
            _homeAnchoredPosition = _rect.anchoredPosition;
            _homePosition = transform.position;
            _homeScale = transform.localScale;
            _homeAlpha = _group.alpha;
        }

        private void Land()
        {
            IsHeld = false;
            _away = false;
            PlaceHome();
            _group.blocksRaycasts = true;
        }

        private void PlaceHome()
        {
            if (transform.parent != _homeParent)
            {
                transform.SetParent(_homeParent, false);
                transform.SetSiblingIndex(_homeSiblingIndex);
            }
            _rect.anchoredPosition = _homeAnchoredPosition;
            transform.localScale = _homeScale;
            _group.alpha = _homeAlpha;
        }
    }
}
