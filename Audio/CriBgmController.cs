#if CRIMW
using System;
using System.Collections.Generic;
using CriWare;
using CriWare.Assets;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LitMotion;
using Void2610.UnityTemplate;

/// <summary>
/// BGMデータクラス
/// CRIWAREのキュー参照と識別名を保持
/// </summary>
[Serializable]
public class BgmData
{
    [SerializeField] private string name;
    [SerializeField] private CriAtomCueReference cueReference;

    public string Name => name;
    public CriAtomCueReference CueReference => cueReference;
}

/// <summary>
/// CRIWAREを使用したBGMコントローラー
/// インタラクティブミュージック対応（AISAC制御）
/// Singletonパターンで実装されており、シーンをまたいで存続
/// </summary>
public class CriBgmController : SingletonMonoBehaviour<CriBgmController>
{
    [SerializeField] private List<BgmData> bgmList = new();

    public bool HasCurrentPlayback { get; private set; }
    public string CurrentBgmName { get; private set; } = string.Empty;
    public bool IsPlayingBGM1 { get; private set; } = true;

    private const string AISAC_CONTROL_NAME = "AisacControl_00";
    private const string BGM_VOLUME_KEY = "BgmVolume";
    private const float FADE_TIME = 1.0f;
    private const float CRI_INIT_TIMEOUT = 10.0f;

    private CriSoundPlayer _player;
    private CriSoundPlayer.SimplePlayback _currentPlayback;
    private CriAtomExAcb _acbAsset;

    private float _bgmVolume = 1.0f;
    private float _currentFadeVolume;
    private float _aisacValue;
    private MotionHandle _fadeHandle;

    /// <summary>
    /// BGMマスターボリューム（0.0～1.0）
    /// PlayerPrefsに自動保存される
    /// </summary>
    public float BgmVolume
    {
        get => _bgmVolume;
        set
        {
            _bgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, _bgmVolume);
            UpdateVolume();
        }
    }

    /// <summary>
    /// AISACコントロール値（インタラクティブミュージック用）
    /// 0.0～1.0の範囲でBGMの性質を動的に変更
    /// </summary>
    public float AisacValue
    {
        get => _aisacValue;
        set
        {
            _aisacValue = Mathf.Clamp01(value);
            if (HasCurrentPlayback)
            {
                _currentPlayback.SetAisacControl(AISAC_CONTROL_NAME, _aisacValue);
            }
        }
    }

    public bool IsPlaying => HasCurrentPlayback && _currentPlayback.IsPlaying();

    /// <summary>
    /// 指定した名前のBGMを再生
    /// </summary>
    /// <param name="bgmName">BGMデータの名前</param>
    /// <param name="fadeInTime">フェードイン時間（秒）</param>
    public void PlayBgm(string bgmName, float fadeInTime = FADE_TIME)
    {
        var bgmData = bgmList.Find(x => x.Name == bgmName);
        CurrentBgmName = bgmName;
        PlayBgmInternal(bgmData.CueReference, fadeInTime > 0f, fadeInTime).Forget();
    }

    /// <summary>
    /// BGM1を再生（後方互換性用）
    /// インスペクタでリストの0番目に設定されているBGMを再生
    /// </summary>
    public void PlayBGM1()
    {
        IsPlayingBGM1 = true;
        PlayBgm(bgmList[0].Name);
    }

    /// <summary>
    /// BGM2を再生（後方互換性用）
    /// インスペクタでリストの1番目に設定されているBGMを再生
    /// </summary>
    public void PlayBGM2()
    {
        IsPlayingBGM1 = false;
        PlayBgm(bgmList[1].Name);
    }

    /// <summary>
    /// ニアトゥルーエンドBGMを再生（後方互換性用）
    /// インスペクタでリストの2番目に設定されているBGMを再生
    /// </summary>
    public void PlayNearTrueEndBGM()
    {
        IsPlayingBGM1 = false;
        PlayBgm(bgmList[2].Name);
    }

    /// <summary>
    /// エンドBGMを再生（後方互換性用）
    /// インスペクタでリストの3番目に設定されているBGMを再生
    /// </summary>
    public void PlayEndBGM()
    {
        IsPlayingBGM1 = false;
        PlayBgm(bgmList[3].Name);
    }

    /// <summary>
    /// BGMを一時停止（フェードアウト後）
    /// </summary>
    public void Pause() => PauseInternal().Forget();

    /// <summary>
    /// BGMを停止（フェードアウト後）
    /// </summary>
    /// <param name="fadeOutTime">フェードアウト時間（秒）</param>
    public async UniTask Stop(float fadeOutTime = FADE_TIME)
    {
        await FadeOut(fadeOutTime);
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
            HasCurrentPlayback = false;
            CurrentBgmName = string.Empty;
        }
    }

    /// <summary>
    /// 一時停止したBGMを再開（フェードイン付き）
    /// </summary>
    public void Resume()
    {
        _currentPlayback.Resume();
        FadeIn().Forget();
    }

    /// <summary>
    /// AISACをスムーズに変更
    /// インタラクティブミュージックの性質を時間をかけて変化させる
    /// </summary>
    /// <param name="targetValue">目標AISAC値（0.0～1.0）</param>
    /// <param name="duration">変化にかける時間（秒）</param>
    public async UniTask SetAisacSmooth(float targetValue, float duration = 1f)
    {
        await LMotion.Create(_aisacValue, targetValue, duration)
            .WithEase(Ease.InOutQuad)
            .Bind(x => AisacValue = x)
            .ToUniTask();
    }

    protected override void Awake()
    {
        base.Awake();
        BgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1.0f);
        _player = new CriSoundPlayer();
    }

    private void Start()
    {
        InitializeCri().Forget();
    }

    private void OnEnable()
    {
        if (!HasCurrentPlayback && _acbAsset == null)
        {
            InitializeCri().Forget();
        }
    }

    protected override void OnDestroy()
    {
        _fadeHandle.TryCancel();
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
        }
        base.OnDestroy();
    }

    /// <summary>
    /// CRIWAREの初期化待機
    /// キューシートのロードが完了するまで待機し、タイムアウト処理を行う
    /// </summary>
    private async UniTaskVoid InitializeCri()
    {
        var startTime = Time.realtimeSinceStartup;

        while (CriAtom.CueSheetsAreLoading)
        {
            if (Time.realtimeSinceStartup - startTime > CRI_INIT_TIMEOUT)
            {
                throw new TimeoutException("CRI初期化タイムアウト");
            }
            await UniTask.Yield();
        }

        _currentFadeVolume = 0f;
    }

    /// <summary>
    /// BGM再生の内部処理
    /// </summary>
    /// <param name="cueReference">再生するキュー参照</param>
    /// <param name="useFadeIn">フェードインするかどうか</param>
    /// <param name="fadeInTime">フェードイン時間（秒）</param>
    private async UniTaskVoid PlayBgmInternal(CriAtomCueReference cueReference, bool useFadeIn = true, float fadeInTime = FADE_TIME)
    {
        if (HasCurrentPlayback)
        {
            await FadeOut();
            _currentPlayback.Stop();
            HasCurrentPlayback = false;
            await UniTask.Delay(50);
        }

        _currentPlayback = _player.StartPlayback(cueReference);
        HasCurrentPlayback = true;

        await UniTask.Delay(100);

        _currentFadeVolume = 0f;
        _currentPlayback.SetVolumeAndPitch(0f, 1.0f);

        _currentPlayback.SetAisacControl(AISAC_CONTROL_NAME, _aisacValue);

        if (useFadeIn)
        {
            await FadeIn(fadeInTime);
        }
        else
        {
            _currentFadeVolume = 1f;
            UpdateVolume();
        }
    }

    /// <summary>
    /// 音量を更新（マスターボリューム × フェード係数）
    /// </summary>
    private void UpdateVolume()
    {
        if (HasCurrentPlayback)
        {
            _currentPlayback.SetVolumeAndPitch(_bgmVolume * _currentFadeVolume, 1.0f);
        }
    }

    /// <summary>
    /// フェードイン処理
    /// </summary>
    /// <param name="fadeInTime">フェードイン時間（秒）</param>
    private async UniTask FadeIn(float fadeInTime = FADE_TIME)
    {
        _fadeHandle.TryCancel();

        _fadeHandle = LMotion.Create(0f, 1f, fadeInTime)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithEase(Ease.InQuad)
            .Bind(x => {
                _currentFadeVolume = x;
                UpdateVolume();
            })
            .AddTo(this);

        await _fadeHandle.ToUniTask();
    }

    /// <summary>
    /// フェードアウト処理
    /// </summary>
    /// <param name="fadeOutTime">フェードアウト時間（秒）</param>
    private async UniTask FadeOut(float fadeOutTime = FADE_TIME)
    {
        _fadeHandle.TryCancel();

        _fadeHandle = LMotion.Create(_currentFadeVolume, 0f, fadeOutTime)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithEase(Ease.InQuad)
            .Bind(x => {
                _currentFadeVolume = x;
                UpdateVolume();
            })
            .AddTo(this);

        await _fadeHandle.ToUniTask();
    }

    /// <summary>
    /// 一時停止の内部処理（フェードアウト後にポーズ）
    /// </summary>
    private async UniTaskVoid PauseInternal()
    {
        await FadeOut();
        if (HasCurrentPlayback)
        {
            _currentPlayback.Pause();
        }
    }
}
#endif
