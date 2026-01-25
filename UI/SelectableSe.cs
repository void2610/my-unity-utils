using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Selectableに効果音を自動追加するコンポーネント
    /// EventSystemの選択・決定イベントに応じて効果音を再生
    /// </summary>
    public class SelectableSe : MonoBehaviour, ISelectHandler, ISubmitHandler
    {
        [Header("効果音設定")]
        [SerializeField] private AudioClip selectSe;
        [SerializeField] private float selectVolume = 1.0f;
        [SerializeField] private AudioClip submitSe;
        [SerializeField] private float submitVolume = 1.0f;

        [Header("ピッチランダム化")]
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private bool isOnlyRandomizeOnSelect;
        [SerializeField] private float minPitch = 0.9f;
        [SerializeField] private float maxPitch = 1.1f;

        [Header("クールダウン")]
        [SerializeField] private float selectCooldown = 0.1f;

        [Header("重要度設定")]
        [SerializeField] private bool isImportantButton;

        private float _lastSelectTime;

        /// <summary>
        /// 選択時
        /// </summary>
        public void OnSelect(BaseEventData eventData) => PlaySelectSound();

        /// <summary>
        /// 決定時
        /// </summary>
        public void OnSubmit(BaseEventData eventData) => PlaySubmitSound();

        /// <summary>
        /// 選択音を手動で再生
        /// </summary>
        public void PlaySelectSoundManual() => PlaySelectSound();

        /// <summary>
        /// 決定音を手動で再生
        /// </summary>
        public void PlaySubmitSoundManual() => PlaySubmitSound();

        /// <summary>
        /// 重要度設定を更新
        /// </summary>
        public void SetImportant(bool important) => isImportantButton = important;

        /// <summary>
        /// クールダウン設定を更新
        /// </summary>
        public void SetSelectCooldown(float cooldown) => selectCooldown = Mathf.Max(0f, cooldown);

        /// <summary>
        /// 効果音の設定を更新
        /// </summary>
        public void UpdateAudioSettings(AudioClip newSelectSe = null, AudioClip newSubmitSe = null,
                                       float newSelectVolume = -1f, float newSubmitVolume = -1f)
        {
            if (newSelectSe) selectSe = newSelectSe;
            if (newSubmitSe) submitSe = newSubmitSe;
            if (newSelectVolume >= 0f) selectVolume = newSelectVolume;
            if (newSubmitVolume >= 0f) submitVolume = newSubmitVolume;
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

        private void PlaySelectSound()
        {
            if (Time.unscaledTime - _lastSelectTime < selectCooldown || !selectSe)
                return;

            _lastSelectTime = Time.unscaledTime;
            PlaySound(selectSe, selectVolume, randomizePitch);
        }

        private void PlaySubmitSound()
        {
            if (!submitSe)
                return;

            PlaySound(submitSe, submitVolume, randomizePitch && !isOnlyRandomizeOnSelect);
        }

        private void PlaySound(AudioClip clip, float volume, bool isRandomPitch)
        {
            if (!clip) return;

            var pitch = isRandomPitch ? Random.Range(minPitch, maxPitch) : 1.0f;
            SeManager.Instance.PlaySe(clip, volume, pitch, important: isImportantButton);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Select Sound")]
        private void TestSelectSound()
        {
            if (Application.isPlaying)
                PlaySelectSound();
        }

        [ContextMenu("Test Submit Sound")]
        private void TestSubmitSound()
        {
            if (Application.isPlaying)
                PlaySubmitSound();
        }
#endif
    }
}
