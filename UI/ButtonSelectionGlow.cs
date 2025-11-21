using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LitMotion;
using LitMotion.Extensions;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ボタン選択/ホバー時の背景グロー効果を管理するコンポーネント
    /// キーボード/ゲームパッド選択とマウスホバーの両方に対応
    ///
    /// 使用例:
    /// - メニューボタンの選択フィードバック
    /// - ホバー時のビジュアルハイライト
    /// - クロスプラットフォーム対応のUI演出
    ///
    /// 使い方:
    /// 1. Buttonコンポーネントを持つGameObjectにアタッチ
    /// 2. Glow Imageに光らせたいImageコンポーネントをアサイン
    /// 3. 選択/ホバー時に自動的にフェードイン/アウト
    ///
    /// 注意:
    /// - LitMotion拡張メソッド（FadeIn/FadeOut）が必要です
    /// - ExtendedMethodsのFadeIn/FadeOut実装を使用します
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class ButtonSelectionGlow : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("グロー設定")]
        [SerializeField] private Image glowImage;

        [Header("フェード設定")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private Ease fadeEase = Ease.OutCubic;
        [SerializeField] private bool ignoreTimeScale = true;

        private MotionHandle _currentMotion;
        private bool _isSelected;
        private bool _isHovered;

        private void Awake()
        {
            // 初期状態では非表示
            if (glowImage != null)
            {
                glowImage.SetAlpha(0f);
            }
        }

        /// <summary>
        /// Selectableが選択された時の処理（キーボード/ゲームパッド）
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            _isSelected = true;
            ShowGlow();
        }

        /// <summary>
        /// Selectableの選択が解除された時の処理（キーボード/ゲームパッド）
        /// </summary>
        public void OnDeselect(BaseEventData eventData)
        {
            _isSelected = false;
            // ホバー中でなければ非表示
            if (!_isHovered)
            {
                HideGlow();
            }
        }

        /// <summary>
        /// マウスがボタン上に入った時の処理
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            ShowGlow();
        }

        /// <summary>
        /// マウスがボタンから出た時の処理
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            // 選択中でなければ非表示
            if (!_isSelected)
            {
                HideGlow();
            }
        }

        /// <summary>
        /// グローを表示
        /// </summary>
        private void ShowGlow()
        {
            if (glowImage == null) return;

            _currentMotion.TryCancel();
            _currentMotion = glowImage.FadeIn(fadeDuration, fadeEase, ignoreTimeScale: ignoreTimeScale);
        }

        /// <summary>
        /// グローを非表示
        /// </summary>
        private void HideGlow()
        {
            if (glowImage == null) return;

            _currentMotion.TryCancel();
            _currentMotion = glowImage.FadeOut(fadeDuration, fadeEase, ignoreTimeScale: ignoreTimeScale);
        }

        private void OnDestroy()
        {
            _currentMotion.TryCancel();
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタでの検証
        /// </summary>
        private void OnValidate()
        {
            if (glowImage == null)
            {
                Debug.LogWarning($"ButtonSelectionGlow: Glow Imageが設定されていません ({gameObject.name})", this);
            }
        }
#endif
    }
}
