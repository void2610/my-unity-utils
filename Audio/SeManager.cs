using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 効果音再生を管理するシングルトンクラス
    /// 20チャンネル対応、重要度制御、ランダムピッチ機能付き
    /// </summary>
    public class SeManager : SingletonMonoBehaviour<SeManager>
    {
        [System.Serializable]
        public class SoundData
        {
            public string name;
            public AudioClip audioClip;
            public float volume = 1.0f;
        }

        [Header("設定")]
        [SerializeField] private AudioMixerGroup seMixerGroup;
        [SerializeField] private SoundData[] soundData;
        [SerializeField] private int maxChannels = 20;

        private AudioSource[] _seAudioSourceList;
        private float _seVolume = 0.5f;

        // PlayerPrefsキー
        private const string SE_VOLUME_KEY = "SeVolume";

        /// <summary>
        /// SE音量プロパティ（0.0f～1.0f）
        /// </summary>
        public float SeVolume
        {
            get => _seVolume;
            set
            {
                _seVolume = Mathf.Clamp01(value);
                if (_seVolume <= 0.0f) _seVolume = 0.0001f;
                
                seMixerGroup.audioMixer.SetFloat("SeVolume", Mathf.Log10(_seVolume) * 20);
                PlayerPrefs.SetFloat(SE_VOLUME_KEY, _seVolume);
            }
        }

        /// <summary>
        /// AudioClipを直接指定してSEを再生
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="volume">音量倍率</param>
        /// <param name="pitch">ピッチ</param>
        /// <param name="important">重要なSEフラグ（チャンネル不足時に強制再生）</param>
        public void PlaySe(AudioClip clip, float volume = 1.0f, float pitch = 1.0f, bool important = false)
        {
            var audioSource = GetAvailableAudioSource(important);
            if (!clip)
            {
                Debug.LogError("AudioClip could not be found.");
                return;
            }
            if (!audioSource)
            {
                Debug.LogWarning("There is no available AudioSource.");
                return;
            }

            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.Play();
        }

        /// <summary>
        /// 名前を指定してSEを再生
        /// </summary>
        /// <param name="seName">SE名</param>
        /// <param name="volume">音量倍率</param>
        /// <param name="pitch">ピッチ（-1でランダム）</param>
        /// <param name="important">重要なSEフラグ</param>
        public void PlaySe(string seName, float volume = 1.0f, float pitch = -1.0f, bool important = false)
        {
            var data = soundData.FirstOrDefault(t => t.name == seName);
            var audioSource = GetAvailableAudioSource(important);
            
            if (data == null) 
            {
                Debug.LogWarning($"SE '{seName}' が見つかりません。");
                return;
            }
            if (!audioSource) return;

            audioSource.clip = data.audioClip;
            audioSource.volume = data.volume * volume;
            
            // ピッチがマイナスの場合はランダム化
            audioSource.pitch = pitch < 0.0f ? Random.Range(0.8f, 1.2f) : pitch;
            audioSource.Play();
        }

        /// <summary>
        /// 遅延してSEを再生
        /// </summary>
        /// <param name="seName">SE名</param>
        /// <param name="delayTime">遅延時間（秒）</param>
        /// <param name="volume">音量倍率</param>
        /// <param name="pitch">ピッチ</param>
        /// <param name="important">重要なSEフラグ</param>
        public void WaitAndPlaySe(string seName, float delayTime, float volume = 1.0f, float pitch = 1.0f, bool important = false)
        {
            WaitAndPlaySeAsync(seName, delayTime, volume, pitch, important).Forget();
        }

        /// <summary>
        /// 遅延してAudioClipを再生
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="delayTime">遅延時間（秒）</param>
        /// <param name="volume">音量倍率</param>
        /// <param name="pitch">ピッチ</param>
        /// <param name="important">重要なSEフラグ</param>
        public void WaitAndPlaySe(AudioClip clip, float delayTime, float volume = 1.0f, float pitch = 1.0f, bool important = false)
        {
            WaitAndPlaySeAsync(clip, delayTime, volume, pitch, important).Forget();
        }

        /// <summary>
        /// すべてのSE再生を停止
        /// </summary>
        public void StopAllSe()
        {
            foreach (var audioSource in _seAudioSourceList)
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }

        /// <summary>
        /// 指定したSEを停止
        /// </summary>
        /// <param name="clip">停止するAudioClip</param>
        public void StopSe(AudioClip clip)
        {
            if (!clip) return;
            
            foreach (var audioSource in _seAudioSourceList)
            {
                if (audioSource.isPlaying && audioSource.clip == clip)
                {
                    audioSource.Stop();
                }
            }
        }

        /// <summary>
        /// 現在再生中のSE数を取得
        /// </summary>
        /// <returns>再生中のSE数</returns>
        public int GetPlayingSeCount()
        {
            return _seAudioSourceList.Count(audioSource => audioSource.isPlaying);
        }

        /// <summary>
        /// 名前を指定してSEをループ再生する（CancellationTokenでキャンセル可能）
        /// 足音や環境音など、継続的に再生したいSEに最適
        /// </summary>
        /// <param name="seName">SE名</param>
        /// <param name="interval">再生間隔（秒）</param>
        /// <param name="volume">音量倍率</param>
        /// <param name="pitchMin">ピッチの最小値</param>
        /// <param name="pitchMax">ピッチの最大値</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async UniTaskVoid PlaySeLoop(string seName, float interval = 0.08f, float volume = 1.0f, float pitchMin = 0.95f, float pitchMax = 1.05f, CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    PlaySe(seName, volume, UnityEngine.Random.Range(pitchMin, pitchMax));
                    await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// 遅延SE再生の内部処理（名前指定）
        /// </summary>
        private async UniTaskVoid WaitAndPlaySeAsync(string seName, float delayTime, float volume, float pitch, bool important)
        {
            await UniTask.Delay((int)(delayTime * 1000));
            PlaySe(seName, volume, pitch, important);
        }

        /// <summary>
        /// 遅延SE再生の内部処理（AudioClip指定）
        /// </summary>
        private async UniTaskVoid WaitAndPlaySeAsync(AudioClip clip, float delayTime, float volume, float pitch, bool important)
        {
            await UniTask.Delay((int)(delayTime * 1000));
            PlaySe(clip, volume, pitch, important);
        }

        /// <summary>
        /// 利用可能なAudioSourceを取得する
        /// </summary>
        /// <param name="important">重要な効果音フラグ</param>
        /// <returns>利用可能なAudioSource</returns>
        private AudioSource GetAvailableAudioSource(bool important = false)
        {
            // 使用中でないAudioSourceを探す
            var unusedAudioSource = _seAudioSourceList.FirstOrDefault(t => !t.isPlaying);
            if (unusedAudioSource) return unusedAudioSource;
            
            // importantフラグがtrueの場合、強制的に最初のAudioSourceを使用
            if (important)
            {
                var forcedAudioSource = _seAudioSourceList[0];
                forcedAudioSource.Stop(); // 現在の再生を停止
                return forcedAudioSource;
            }
            
            // 利用可能なAudioSourceがない場合はnullを返す
            return null;
        }

        protected override void Awake()
        {
            base.Awake();
            
            // AudioSourceリストを初期化
            _seAudioSourceList = new AudioSource[maxChannels];
            for (var i = 0; i < _seAudioSourceList.Length; ++i)
            {
                _seAudioSourceList[i] = gameObject.AddComponent<AudioSource>();
                _seAudioSourceList[i].outputAudioMixerGroup = seMixerGroup;
                _seAudioSourceList[i].playOnAwake = false;
            }
            
            // 保存された音量を読み込み
            _seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);
            SeVolume = _seVolume;
        }
        
        private void Start()
        {
            // AudioMixerの初期設定
            if (seMixerGroup && seMixerGroup.audioMixer)
            {
                seMixerGroup.audioMixer.SetFloat("SeVolume", Mathf.Log10(_seVolume) * 20);
            }
        }
    }
}