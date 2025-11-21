using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ボタンに効果音を自動追加するコンポーネント
    /// ホバー音とクリック音を設定して、統一された音響フィードバックを提供
    /// SeManagerを使用した統合音声管理バージョン
    /// </summary>
    public class ButtonSe : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, ISelectHandler, ISubmitHandler
    {
        [Header("効果音設定")]
        [SerializeField] private AudioClip hoverSe;
        [SerializeField] private float hoverVolume = 1.0f;
        [SerializeField] private AudioClip clickSe;
        [SerializeField] private float clickVolume = 1.0f;
        
        [Header("ピッチランダム化")]
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private bool isOnlyRandomizeOnHover = false; // ホバー音のみランダム化（クリック音は固定ピッチ）
        [SerializeField] private float minPitch = 0.9f;
        [SerializeField] private float maxPitch = 1.1f;
        
        [Header("クールダウン")]
        [SerializeField] private float hoverCooldown = 0.1f; // ホバー音のクールダウン時間
        
        [Header("重要度設定")]
        [SerializeField] private bool isImportantButton = false; // 重要なボタンの場合、音源が足りなくても強制再生
        
        private Button _button;
        private float _lastHoverTime;
        
        private void Awake()
        {
            _button = GetComponent<Button>();
        }
        
        /// <summary>
        /// ホバー効果音を再生
        /// </summary>
        private void PlayHoverSound()
        {
            // ボタンが無効またはクールダウン中の場合は再生しない
            if ((_button && !_button.interactable) ||
                Time.unscaledTime - _lastHoverTime < hoverCooldown ||
                !hoverSe)
                return;

            _lastHoverTime = Time.unscaledTime;
            PlaySound(hoverSe, hoverVolume, randomizePitch);
        }

        /// <summary>
        /// クリック効果音を再生
        /// </summary>
        private void PlayClickSound()
        {
            if ((_button && !_button.interactable) || clickSe == null)
                return;

            // isOnlyRandomizeOnHoverがtrueの場合、クリック音はランダム化しない
            PlaySound(clickSe, clickVolume, randomizePitch && !isOnlyRandomizeOnHover);
        }

        /// <summary>
        /// 効果音を再生する汎用メソッド（SeManager統合版）
        /// </summary>
        private void PlaySound(AudioClip clip, float volume, bool isRandomPitch)
        {
            if (!clip) return;

            // ピッチの計算
            var pitch = isRandomPitch ? Random.Range(minPitch, maxPitch) : 1.0f;

            // SeManagerを使用して再生
            SeManager.Instance.PlaySe(clip, volume, pitch, important: isImportantButton);
        }
        
        // EventSystemインターフェースの実装
        
        /// <summary>
        /// マウスホバー時
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHoverSound();
        }
        
        /// <summary>
        /// キーボード/コントローラー選択時
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            PlayHoverSound();
        }
        
        /// <summary>
        /// キーボード/コントローラー決定時
        /// </summary>
        public void OnSubmit(BaseEventData eventData)
        {
            PlayClickSound();
        }
        
        /// <summary>
        /// マウスクリック時
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            PlayClickSound();
        }
        
        /// <summary>
        /// 効果音を手動で再生
        /// </summary>
        public void PlayHoverSoundManual()
        {
            PlayHoverSound();
        }
        
        /// <summary>
        /// クリック音を手動で再生
        /// </summary>
        public void PlayClickSoundManual()
        {
            PlayClickSound();
        }
        
        /// <summary>
        /// 効果音の設定を更新
        /// </summary>
        public void UpdateAudioSettings(AudioClip newHoverSe = null, AudioClip newClickSe = null, 
                                       float newHoverVolume = -1f, float newClickVolume = -1f)
        {
            if (newHoverSe != null) hoverSe = newHoverSe;
            if (newClickSe != null) clickSe = newClickSe;
            if (newHoverVolume >= 0f) hoverVolume = newHoverVolume;
            if (newClickVolume >= 0f) clickVolume = newClickVolume;
        }
        
        /// <summary>
        /// ピッチランダム化の設定を更新
        /// </summary>
        public void UpdatePitchSettings(bool enableRandomPitch, float minPitchValue = 0.9f, float maxPitchValue = 1.1f)
        {
            randomizePitch = enableRandomPitch;
            minPitch = minPitchValue;
            maxPitch = maxPitchValue;
        }
        
        /// <summary>
        /// 重要度設定を更新
        /// </summary>
        public void SetImportant(bool important)
        {
            isImportantButton = important;
        }
        
        /// <summary>
        /// クールダウン設定を更新
        /// </summary>
        public void SetHoverCooldown(float cooldown)
        {
            hoverCooldown = Mathf.Max(0f, cooldown);
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタでのテスト再生
        /// </summary>
        [ContextMenu("Test Hover Sound")]
        private void TestHoverSound()
        {
            if (Application.isPlaying)
                PlayHoverSound();
        }
        
        [ContextMenu("Test Click Sound")]
        private void TestClickSound()
        {
            if (Application.isPlaying)
                PlayClickSound();
        }
#endif
    }
}