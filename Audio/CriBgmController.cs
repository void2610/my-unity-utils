#if CRIWARE_ENABLE
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
    [SerializeField, Range(0f, 1f)] private float baseVolume = 1.0f;

    public bool IsInitialized { get; private set; }
    public bool HasCurrentPlayback { get; private set; }
    public string CurrentBgmName { get; private set; } = string.Empty;
    public bool IsPlayingBGM1 { get; private set; } = true;

    private const string AISAC_CONTROL_NAME = "AisacControl_00";
    private const string BGM_VOLUME_KEY = "BgmVolume";

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
    public void Pause() => PauseInternal();
    public void Resume() => _currentPlayback.Resume();

    /// <summary>
    /// 指定した名前のBGMを再生
    /// </summary>
    /// <param name="bgmName">BGMデータの名前</param>
    public void PlayBgm(string bgmName)
    {
        var bgmData = bgmList.Find(x => x.Name == bgmName);
        CurrentBgmName = bgmName;
        PlayBgmInternal(bgmData.CueReference).Forget();
    }

    public void Stop()
    {
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
            HasCurrentPlayback = false;
            CurrentBgmName = string.Empty;
        }
    }

    /// <summary>
    /// BGMをフェードアウトして停止
    /// </summary>
    /// <param name="duration">フェード時間（秒）</param>
    public async UniTask FadeOut(float duration = 0.5f)
    {
        if (!HasCurrentPlayback) return;

        _fadeHandle.TryCancel();
        await LMotion.Create(_currentFadeVolume, 0f, duration)
            .WithEase(Ease.OutQuad)
            .Bind(x =>
            {
                _currentFadeVolume = x;
                UpdateVolume();
            })
            .ToUniTask();

        Stop();
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

    protected override void OnDestroy()
    {
        _fadeHandle.TryCancel();
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
        }
        // CriSoundPlayerのネイティブリソースを解放
        _player?.Dispose();
        base.OnDestroy();
    }

    /// <summary>
    /// CRIWAREの初期化待機
    /// キューシートのロードが完了するまで待機し、タイムアウト処理を行う
    /// </summary>
    private async UniTaskVoid InitializeCri()
    {
        var startTime = Time.realtimeSinceStartup;

        // CRIWARE全体の初期化（ACF登録含む）が完了するまで待機
        while (CriAtom.CueSheetsAreLoading)
        {
            if (Time.realtimeSinceStartup - startTime > 10f)
                throw new TimeoutException("CRI初期化タイムアウト");
            await UniTask.Yield();
        }

        // 全BGMのACBアセットがロードされるまで待機
        foreach (var bgmData in bgmList)
        {
            if (bgmData.CueReference.AcbAsset != null)
            {
                var acb = bgmData.CueReference.AcbAsset;

#if UNITY_EDITOR
                // ドメインリロード無効時、ACBアセットのLoadRequestedがtrueのまま残り
                // Handleがnullになるため、強制的にUnloadして再ロード可能にする
                if (acb.LoadRequested && acb.Handle == null)
                {
                    acb.Unload();
                }

                // Handle が null なら明示的に LoadAsync を呼び出す
                if (acb.Handle == null && !acb.LoadRequested)
                {
                    acb.LoadAsync();
                }
#endif

                // ACBのロード完了を待機
                while (!acb.Loaded)
                {
                    if (Time.realtimeSinceStartup - startTime > 10f)
                        throw new TimeoutException("ACBロードタイムアウト");
                    await UniTask.Yield();
                }
            }
        }

        _currentFadeVolume = 0f;
        IsInitialized = true;
    }

    /// <summary>
    /// BGM再生の内部処理
    /// </summary>
    /// <param name="cueReference">再生するキュー参照</param>
    private async UniTaskVoid PlayBgmInternal(CriAtomCueReference cueReference)
    {
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
            HasCurrentPlayback = false;
            await UniTask.Delay(50);
        }

        _currentPlayback = _player.StartPlayback(cueReference);
        // 再生開始直後に正しい音量を設定（一瞬大音量になるのを防ぐ）
        _currentFadeVolume = 1f;
        HasCurrentPlayback = true;
        UpdateVolume();

        await UniTask.Delay(100);

        _currentPlayback.SetAisacControl(AISAC_CONTROL_NAME, _aisacValue);
    }

    /// <summary>
    /// 音量を更新（基礎音量 × マスターボリューム × フェード係数）
    /// </summary>
    private void UpdateVolume()
    {
        if (HasCurrentPlayback)
        {
            _currentPlayback.SetVolumeAndPitch(baseVolume * _bgmVolume * _currentFadeVolume, 1.0f);
        }
    }

    /// <summary>
    /// 一時停止の内部処理
    /// </summary>
    private void PauseInternal()
    {
        if (HasCurrentPlayback)
            _currentPlayback.Pause();
    }
}
#endif
