using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Random = UnityEngine.Random;

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
            // イントロ付きループのループ開始時間（秒、0 = 通常ループ）
            public float loopStartSeconds = 0f;
            // イントロ付きループのループ終端時間（秒、0 = クリップ末尾）
            public float loopEndSeconds = 0f;
        }

        [Header("設定")]
        [SerializeField] private bool playOnStart;
        [SerializeField] private AudioMixerGroup bgmMixerGroup;
        [SerializeField] private List<SoundData> bgmList = new();
        [SerializeField] private float fadeInTime = 1.0f;
        [SerializeField] private float fadeOutTime = 1.0f;
        [SerializeField, Range(0f, 1f)] private float projectMasterVolume = 1.0f;

        private AudioSource _audioSource;
        private bool _isPlaying;
        private float _bgmVolume = 1.0f;
        private SoundData _currentBGM;
        private MotionHandle _fadeHandle;
        private MotionHandle _duckingHandle;
        private float _originalVolume = 1.0f;
        private bool _currentLoop = true;
        // イントロループ検知用：ループ開始点を通過済みかどうか
        private bool _hasPassedLoopStart = false;

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

                ApplyMixerVolume();
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
            
            _fadeHandle.TryCancel();
            _fadeHandle = LMotion.Create(_audioSource.volume, _currentBGM.volume, fadeInTime)
                .WithEase(Ease.InQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
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
        public UniTask Stop()
        {
            return Stop(fadeOutTime);
        }

        /// <summary>
        /// フェード時間を指定して停止
        /// </summary>
        public async UniTask Stop(float fadeDuration)
        {
            _isPlaying = false;
            var resolvedFadeDuration = ResolveFadeOutDuration(fadeDuration);

            _fadeHandle.TryCancel();
            await LMotion.Create(_audioSource.volume, 0f, resolvedFadeDuration)
                .WithEase(Ease.InQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
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

            _duckingHandle.TryCancel();
            _duckingHandle = LMotion.Create(_audioSource.volume, _originalVolume * duckVolume, duckFadeTime)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
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
            _duckingHandle.TryCancel();
            _duckingHandle = LMotion.Create(_audioSource.volume, _originalVolume, restoreFadeTime)
                .WithEase(Ease.InQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
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
            PlayBGMInternal(bgmName, fadeOutTime, fadeInTime, true);
        }

        /// <summary>
        /// 名前を指定してBGMを再生
        /// </summary>
        /// <param name="bgmName">BGM名</param>
        /// <param name="fadeDuration">フェード時間</param>
        public void PlayBGM(string bgmName, float fadeDuration)
        {
            PlayBGMInternal(bgmName, fadeDuration, fadeDuration, true);
        }

        /// <summary>
        /// 名前を指定してBGMを再生
        /// </summary>
        /// <param name="bgmName">BGM名</param>
        /// <param name="fadeOutDuration">フェードアウト時間</param>
        /// <param name="fadeInDuration">フェードイン時間</param>
        public void PlayBGM(string bgmName, float fadeOutDuration, float fadeInDuration)
        {
            PlayBGMInternal(bgmName, fadeOutDuration, fadeInDuration, true);
        }

        public void PlayBGM(string bgmName, float fadeOutDuration, float fadeInDuration, bool loop)
        {
            PlayBGMInternal(bgmName, fadeOutDuration, fadeInDuration, loop);
        }

        private void PlayBGMInternal(string bgmName, float fadeOutDuration, float fadeInDuration, bool loop)
        {
            var data = bgmList.FirstOrDefault(t => t.name == bgmName);
            if (data == null)
            {
                Debug.LogError($"BGM '{bgmName}' が見つかりません。");
                return;
            }
            
            PlayBGMInternal(data, fadeOutDuration, fadeInDuration, loop).Forget();
        }

        /// <summary>
        /// カテゴリからランダムにBGMを再生
        /// </summary>
        /// <param name="bgmType">BGMカテゴリ</param>
        public void PlayRandomBGM(BgmType bgmType)
        {
            PlayRandomBGM(bgmType, fadeOutTime, fadeInTime);
        }

        /// <summary>
        /// カテゴリからランダムにBGMを再生
        /// </summary>
        /// <param name="bgmType">BGMカテゴリ</param>
        /// <param name="fadeDuration">フェード時間</param>
        public void PlayRandomBGM(BgmType bgmType, float fadeDuration)
        {
            PlayRandomBGM(bgmType, fadeDuration, fadeDuration);
        }

        /// <summary>
        /// カテゴリからランダムにBGMを再生
        /// </summary>
        /// <param name="bgmType">BGMカテゴリ</param>
        /// <param name="fadeOutDuration">フェードアウト時間</param>
        /// <param name="fadeInDuration">フェードイン時間</param>
        public void PlayRandomBGM(BgmType bgmType, float fadeOutDuration, float fadeInDuration)
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
            PlayBGMInternal(data, fadeOutDuration, fadeInDuration, true).Forget();
        }

        /// <summary>
        /// 全BGMからランダム再生
        /// </summary>
        public void PlayRandomBGM()
        {
            PlayRandomBGM(fadeOutTime, fadeInTime);
        }

        /// <summary>
        /// 全BGMからランダム再生
        /// </summary>
        /// <param name="fadeDuration">フェード時間</param>
        public void PlayRandomBGM(float fadeDuration)
        {
            PlayRandomBGM(fadeDuration, fadeDuration);
        }

        /// <summary>
        /// 全BGMからランダム再生
        /// </summary>
        /// <param name="fadeOutDuration">フェードアウト時間</param>
        /// <param name="fadeInDuration">フェードイン時間</param>
        public void PlayRandomBGM(float fadeOutDuration, float fadeInDuration)
        {
            if (bgmList.Count == 0) return;
            var data = bgmList[Random.Range(0, bgmList.Count)];
            PlayBGMInternal(data, fadeOutDuration, fadeInDuration, true).Forget();
        }

        /// <summary>
        /// BGM再生の内部処理
        /// </summary>
        private async UniTaskVoid PlayBGMInternal(SoundData data, float fadeOutDuration, float fadeInDuration, bool loop)
        {
            _isPlaying = true;
            _currentLoop = loop;
            var resolvedFadeOutDuration = ResolveFadeOutDuration(fadeOutDuration);
            var resolvedFadeInDuration = ResolveFadeInDuration(fadeInDuration);

            // 現在のBGMをフェードアウト
            if (_currentBGM != null)
            {
                _fadeHandle.TryCancel();
                await LMotion.Create(_audioSource.volume, 0f, resolvedFadeOutDuration)
                    .WithEase(Ease.InQuad)
                    .BindToVolume(_audioSource)
                    .ToUniTask();
                _audioSource.Stop();
            }

            _currentBGM = data;
            _audioSource.clip = _currentBGM.audioClip;
            _audioSource.loop = loop;
            _hasPassedLoopStart = false;
            _audioSource.volume = 0;
            _audioSource.Play();

            // フェードイン
            _fadeHandle = LMotion.Create(0f, _currentBGM.volume, resolvedFadeInDuration)
                .WithEase(Ease.InQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToVolume(_audioSource)
                .AddTo(this);

            await UniTask.Delay((int)(resolvedFadeInDuration * 1000), ignoreTimeScale: true);
        }

        /// <summary>
        /// 一時停止の内部処理
        /// </summary>
        private async UniTaskVoid PauseInternal()
        {
            _fadeHandle.TryCancel();
            await LMotion.Create(_audioSource.volume, 0f, fadeOutTime)
                .WithEase(Ease.InQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToVolume(_audioSource)
                .ToUniTask();
            
            _audioSource.Stop();
        }

        /// <summary>
        /// BGMをループさせる
        /// </summary>
        private async UniTaskVoid LoopBGM(float remainingFadeTime)
        {
            var bgmToLoop = _currentBGM;
            if (bgmToLoop == null) return;

            _fadeHandle.TryCancel();
            await LMotion.Create(_audioSource.volume, 0f, remainingFadeTime)
                .WithEase(Ease.InQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToVolume(_audioSource)
                .ToUniTask();

            if (!_isPlaying || _currentBGM != bgmToLoop) return;
            
            _audioSource.Stop();
            PlayBGM(bgmToLoop.name, fadeOutTime, fadeInTime, true);
        }

        private float ResolveFadeInDuration(float fadeDuration)
        {
            return fadeDuration > 0f ? fadeDuration : fadeInTime;
        }

        private float ResolveFadeOutDuration(float fadeDuration)
        {
            return fadeDuration > 0f ? fadeDuration : fadeOutTime;
        }

        private void ApplyMixerVolume()
        {
            var effectiveVolume = Mathf.Clamp01(_bgmVolume * projectMasterVolume);
            bgmMixerGroup.audioMixer.SetFloat("BgmVolume", Mathf.Log10(effectiveVolume) * 20);
        }

        protected override void Awake()
        {
            base.Awake();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.outputAudioMixerGroup = bgmMixerGroup;
            _audioSource.loop = true; // デフォルトはループ再生

            // 保存された音量を読み込み
            _bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1.0f);
            BgmVolume = _bgmVolume;
        }
        
        private void Start()
        {
            _audioSource.volume = _currentBGM == null ? 0 : _audioSource.volume;

            ApplyMixerVolume();

            if (playOnStart)
            {
                _isPlaying = true;
                PlayRandomBGM();
            }
        }
        
        private void Update()
        {
            if (!_isPlaying || _currentBGM == null) return;

            // イントロ付きループBGMの処理
            if (_currentBGM.loopStartSeconds > 0f)
            {
                var clip = _audioSource.clip;
                var loopStartSample = (int)(_currentBGM.loopStartSeconds * clip.frequency);

                // 一度ループ開始点を通過したか記録
                if (!_hasPassedLoopStart && _audioSource.timeSamples >= loopStartSample)
                {
                    _hasPassedLoopStart = true;
                }

                // ループ開始点通過後にtimeSamplesがloopStart未満になった = Unityのループが発生
                if (_hasPassedLoopStart && _audioSource.timeSamples < loopStartSample)
                {
                    _audioSource.Stop();
                    _audioSource.timeSamples = loopStartSample;
                    _audioSource.Play();
                }
                return;
            }

            // 通常ループBGMは曲末付近でフェードアウトして先頭から再生
            if (_currentLoop && _audioSource.isPlaying)
            {
                var remainingTime = _audioSource.clip.length - _audioSource.time;
                if (remainingTime <= fadeOutTime && !_fadeHandle.IsPlaying())
                {
                    LoopBGM(remainingTime).Forget();
                }
            }
        }

        protected override void OnDestroy()
        {
            _fadeHandle.TryCancel();
            _duckingHandle.TryCancel();
            base.OnDestroy();
        }
    }
}
