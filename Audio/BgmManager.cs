using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// BGM再生を管理するシングルトンクラス
    /// LitMotionを使用したフェード機能付き
    /// カテゴリ別・名前指定両対応
    /// </summary>
    public class BgmManager : SingletonMonoBehaviour<BgmManager>
    {
        /// <summary>
        /// BGMカテゴリ列挙型
        /// プロジェクトに応じて追加・変更してください
        /// </summary>
        public enum BgmType
        {
            Default,
            Menu,
            Battle,
            Victory,
            GameOver,
            Peaceful,
            Tension
        }

        [System.Serializable]
        public class SoundData
        {
            public string name;
            public AudioClip audioClip;
            public float volume = 1.0f;
            public BgmType bgmType = BgmType.Default;
        }

        [Header("設定")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private AudioMixerGroup bgmMixerGroup;
        [SerializeField] private List<SoundData> bgmList = new List<SoundData>();
        [SerializeField] private float fadeTime = 1.0f;

        private AudioSource _audioSource;
        private bool _isPlaying;
        private float _bgmVolume = 1.0f;
        private bool _isFading;
        private SoundData _currentBGM;
        private MotionHandle _fadeHandle;
        private MotionHandle _duckingHandle;
        private float _originalVolume = 1.0f;

        // PlayerPrefsキー
        private const string BGM_VOLUME_KEY = "BgmVolume";

        /// <summary>
        /// BGM音量プロパティ（0.0f～1.0f）
        /// </summary>
        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                if (_bgmVolume <= 0.0f) _bgmVolume = 0.0001f;
                
                bgmMixerGroup.audioMixer.SetFloat("BgmVolume", Mathf.Log10(_bgmVolume) * 20);
                PlayerPrefs.SetFloat(BGM_VOLUME_KEY, _bgmVolume);
            }
        }

        /// <summary>
        /// 現在再生中のBGM名を取得
        /// </summary>
        public string CurrentBgmName => _currentBGM?.name ?? "";

        /// <summary>
        /// 再生中かどうか
        /// </summary>
        public bool IsPlaying => _isPlaying && _audioSource && _audioSource.isPlaying;

        /// <summary>
        /// 再生を再開
        /// </summary>
        public void Resume()
        {
            if (_currentBGM == null) return;

            _isPlaying = true;
            _audioSource.Play();
            
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(_audioSource.volume, _currentBGM.volume, fadeTime)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .AddTo(this);
        }

        /// <summary>
        /// 一時停止
        /// </summary>
        public void Pause()
        {
            _isPlaying = false;
            PauseInternal().Forget();
        }

        /// <summary>
        /// 停止
        /// </summary>
        public async UniTask Stop()
        {
            _isPlaying = false;

            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            await LMotion.Create(_audioSource.volume, 0f, fadeTime)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .ToUniTask();

            _audioSource.Stop();
            _currentBGM = null;
        }

        /// <summary>
        /// BGMボリュームを一時的に下げる（SE再生時のダッキング用）
        /// 重要なボイスやSEの際にBGMを目立たなくする効果
        /// </summary>
        /// <param name="duckVolume">下げる先のボリューム倍率（0.0f～1.0f、通常は0.2f程度）</param>
        /// <param name="duckFadeTime">フェード時間（秒）</param>
        public async UniTask DuckVolume(float duckVolume = 0.2f, float duckFadeTime = 0.5f)
        {
            _originalVolume = _audioSource.volume;

            if (_duckingHandle.IsActive()) _duckingHandle.Cancel();
            _duckingHandle = LMotion.Create(_audioSource.volume, _originalVolume * duckVolume, duckFadeTime)
                .WithEase(Ease.OutQuad)
                .BindToVolume(_audioSource)
                .AddTo(this);

            await _duckingHandle.ToUniTask();
        }

        /// <summary>
        /// ダッキングしたボリュームを元に戻す
        /// DuckVolumeと対で使用します
        /// </summary>
        /// <param name="restoreFadeTime">フェード時間（秒）</param>
        public async UniTask RestoreVolume(float restoreFadeTime = 0.5f)
        {
            if (_duckingHandle.IsActive()) _duckingHandle.Cancel();
            _duckingHandle = LMotion.Create(_audioSource.volume, _originalVolume, restoreFadeTime)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .AddTo(this);

            await _duckingHandle.ToUniTask();
        }

        /// <summary>
        /// 名前を指定してBGMを再生
        /// </summary>
        /// <param name="bgmName">BGM名</param>
        public void PlayBGM(string bgmName)
        {
            var data = bgmList.FirstOrDefault(t => t.name == bgmName);
            if (data == null)
            {
                Debug.LogError($"BGM '{bgmName}' が見つかりません。");
                return;
            }
            
            PlayBGMInternal(data).Forget();
        }

        /// <summary>
        /// カテゴリからランダムにBGMを再生
        /// </summary>
        /// <param name="bgmType">BGMカテゴリ</param>
        public void PlayRandomBGM(BgmType bgmType = BgmType.Default)
        {
            if (bgmList.Count == 0) return;
            
            var targetBgmList = bgmList.FindAll(x => x.bgmType == bgmType);
            if (targetBgmList.Count == 0) 
            {
                Debug.LogWarning($"BGMタイプ '{bgmType}' のBGMが見つかりません。");
                return;
            }
            
            // 同じタイプで同じBGMが再生中の場合はスキップ
            if (_currentBGM != null && _currentBGM.bgmType == bgmType) return;
            
            var data = targetBgmList[Random.Range(0, targetBgmList.Count)];
            PlayBGMInternal(data).Forget();
        }

        /// <summary>
        /// 全BGMからランダム再生
        /// </summary>
        public void PlayRandomBGM()
        {
            if (bgmList.Count == 0) return;
            var data = bgmList[Random.Range(0, bgmList.Count)];
            PlayBGMInternal(data).Forget();
        }

        /// <summary>
        /// BGM再生の内部処理
        /// </summary>
        private async UniTaskVoid PlayBGMInternal(SoundData data)
        {
            // 現在のBGMをフェードアウト
            if (_currentBGM != null)
            {
                if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
                await LMotion.Create(_audioSource.volume, 0f, fadeTime)
                    .WithEase(Ease.InQuad)
                    .BindToVolume(_audioSource)
                    .ToUniTask();
                _audioSource.Stop();
            }

            _currentBGM = data;
            _audioSource.clip = _currentBGM.audioClip;
            _audioSource.volume = 0;
            _audioSource.Play();

            // フェードイン
            _isFading = true;
            _fadeHandle = LMotion.Create(0f, _currentBGM.volume, fadeTime)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .AddTo(this);
            
            await UniTask.Delay((int)(fadeTime * 1000));
            _isFading = false;
        }

        /// <summary>
        /// 一時停止の内部処理
        /// </summary>
        private async UniTaskVoid PauseInternal()
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            await LMotion.Create(_audioSource.volume, 0f, fadeTime)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .ToUniTask();
            
            _audioSource.Stop();
        }

        /// <summary>
        /// 次のBGMへのループ処理
        /// </summary>
        private async UniTaskVoid LoopToNextBGM(float remainingFadeTime)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            await LMotion.Create(_audioSource.volume, 0f, remainingFadeTime)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .ToUniTask();
            
            // 同じタイプからランダム選択
            if (_currentBGM != null)
            {
                PlayRandomBGM(_currentBGM.bgmType);
            }
            else
            {
                PlayRandomBGM();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.outputAudioMixerGroup = bgmMixerGroup;
            _audioSource.loop = false; // ループは手動で管理
            
            // 保存された音量を読み込み
            _bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1.0f);
            BgmVolume = _bgmVolume;
        }
        
        private void Start()
        {
            _currentBGM = null;
            _audioSource.volume = 0;
            
            if (playOnStart)
            {
                _isPlaying = true;
                PlayRandomBGM();
            }
        }

        private void Update()
        {
            // 自動ループ処理
            if (_isPlaying && _audioSource.clip && !_isFading)
            {
                var remainingTime = _audioSource.clip.length - _audioSource.time;
                if (remainingTime <= fadeTime)
                {
                    _isFading = true;
                    LoopToNextBGM(remainingTime).Forget();
                }
            }
        }

        protected override void OnDestroy()
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            if (_duckingHandle.IsActive()) _duckingHandle.Cancel();
            base.OnDestroy();
        }
    }
}