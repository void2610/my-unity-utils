#if CRIWARE_ENABLE
using System;
using System.Collections.Generic;
using System.Linq;
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
    [Tooltip("曲を切り替えるときに前の曲と重ねて入れ替える秒数。0 なら前の曲を止めてから次の曲を流す")]
    [SerializeField, Min(0f)] private float crossfadeDuration;

    /// <summary>曲を流していないときの CurrentBgmName</summary>
    public const string NO_BGM_NAME = "";

    /// <summary>登録済み BGM の名前一覧。シナリオ用の参照一覧等、外部ツールへ渡す用</summary>
    public IEnumerable<string> EnumerateBgmNames() => bgmList.Select(b => b.Name);

    public float CrossfadeDuration => crossfadeDuration;
    public bool IsInitialized { get; private set; }
    public bool HasCurrentPlayback { get; private set; }
    public string CurrentBgmName { get; private set; } = NO_BGM_NAME;
    public bool IsPlayingBGM1 { get; private set; } = true;

    private const string AISAC_CONTROL_NAME = "AisacControl_00";

    private CriSoundPlayer _player;
    private CriSoundPlayer.SimplePlayback _currentPlayback;
    // 切り替えで音量を下げきるまで鳴らしておく前の曲
    private CriSoundPlayer.SimplePlayback _outgoingPlayback;
    private bool _hasOutgoingPlayback;
    private CriAtomExAcb _acbAsset;

    private float _bgmVolume = 1.0f;
    private float _currentFadeVolume;
    private float _outgoingFadeVolume;
    private float _temporaryVolume = 1.0f;
    private float _aisacValue;
    private int _playRequestId;
    private MotionHandle _fadeHandle;
    private MotionHandle _outgoingFadeHandle;
    private MotionHandle _temporaryVolumeHandle;

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
            PlayerPrefs.SetFloat(AudioVolumePrefs.BgmVolumeKey, _bgmVolume);
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

    public void Resume()
    {
        if (HasCurrentPlayback) _currentPlayback.Resume();
        if (_hasOutgoingPlayback) _outgoingPlayback.Resume();
    }

    /// <summary>
    /// 次の再生ブロックインデックスを設定
    /// </summary>
    /// <param name="blockIndex">ブロックインデックス</param>
    public void SetNextBlockIndex(int blockIndex)
    {
        if (HasCurrentPlayback)
        {
            _currentPlayback.SetNextBlockIndex(blockIndex);
        }
    }

    /// <summary>
    /// 指定した名前のBGMを再生
    /// </summary>
    /// <param name="bgmName">BGMデータの名前</param>
    public void PlayBgm(string bgmName)
    {
        _playRequestId++;
        var bgmData = bgmList.Find(x => x.Name == bgmName);
        // ACB ロード失敗 (CI 環境) や不明な bgmName で bgmData/AcbAsset が無い場合は無音再生扱いにする。
        // 呼び出し側は再生成否を分岐しないので、no-op で通す方が失敗モードとして安全。
        // CriAtomCueReference は struct のため ?. が使えないので順に null チェック。
        if (bgmData == null || bgmData.CueReference.AcbAsset == null || !bgmData.CueReference.AcbAsset.Loaded)
        {
            CurrentBgmName = bgmName;
            return;
        }
        CurrentBgmName = bgmName;
        if (crossfadeDuration > 0f)
            CrossfadeTo(bgmData.CueReference, _playRequestId);
        else
            PlayBgmInternal(bgmData.CueReference, _playRequestId).Forget();
    }
    
    public async UniTaskVoid PlayBgm(string bgmName, int delayMilliseconds)
    {
        await UniTask.Delay(delayMilliseconds);
        PlayBgm(bgmName);
    }

    public void Stop()
    {
        _playRequestId++;
        CurrentBgmName = NO_BGM_NAME;
        _fadeHandle.TryCancel();
        StopOutgoing();
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
            HasCurrentPlayback = false;
        }
    }

    /// <summary>
    /// BGMをフェードアウトして停止。呼んだ時点で曲を流していない扱いになり、フェード中に次の曲を流せる
    /// </summary>
    /// <param name="duration">フェード時間（秒）</param>
    public async UniTask FadeOut(float duration = 0.5f)
    {
        _playRequestId++;
        CurrentBgmName = NO_BGM_NAME;
        if (!HasCurrentPlayback) return;

        ReleaseCurrentToOutgoing(duration);
        await _outgoingFadeHandle.ToUniTask().SuppressCancellationThrow();
    }


    /// <summary>
    /// BGM音量を一時的に変更（フェード付き）
    /// FadeOutとは別のフィールドで管理するため競合しない
    /// </summary>
    /// <param name="targetVolume">目標音量（0.0～1.0）</param>
    /// <param name="duration">フェード時間（秒）</param>
    public async UniTask SetTemporaryVolume(float targetVolume, float duration = 0.5f)
    {
        _temporaryVolumeHandle.TryCancel();
        _temporaryVolumeHandle = LMotion.Create(_temporaryVolume, targetVolume, duration)
            .WithEase(Ease.OutQuad)
            .Bind(x =>
            {
                _temporaryVolume = x;
                UpdateVolume();
            });
        await _temporaryVolumeHandle.ToUniTask();
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
        BgmVolume = PlayerPrefs.GetFloat(AudioVolumePrefs.BgmVolumeKey, 1.0f);
        _player = new CriSoundPlayer();
    }

    private void Start()
    {
        InitializeCri().Forget();
    }

    protected override void OnDestroy()
    {
        _fadeHandle.TryCancel();
        _temporaryVolumeHandle.TryCancel();
        StopOutgoing();
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
        }
        // CriSoundPlayerのネイティブリソースを解放
        _player.Dispose();
        base.OnDestroy();
    }

    /// <summary>
    /// CRIWAREの初期化待機
    /// キューシートのロードが完了するまで待機し、タイムアウトしても IsInitialized=true にして
    /// 呼び出し側 (TitlePresenter / GameInitializer の WaitUntil) をハングさせない。
    /// タイムアウト時は BGM 無しで進行する (CI 環境や ACB 未配置ビルドでの graceful degradation)。
    /// </summary>
    private async UniTaskVoid InitializeCri()
    {
        try
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
                    // ドメインリロード無効時、ACBアセットのLoadRequestedがtrueのまま残るため
                    // 強制的にUnloadして再ロード可能にする
                    if (acb.LoadRequested)
                    {
                        acb.Unload();
                    }

                    // ロードを要求
                    acb.LoadAsync();
#endif

                    // ACBのロード完了を待機
                    while (!acb.Loaded)
                    {
                        if (Time.realtimeSinceStartup - startTime > 10f)
                            throw new TimeoutException($"ACBロードタイムアウト: {bgmData.Name}");
                        await UniTask.Yield();
                    }
                }
            }

            _currentFadeVolume = 0f;
        }
        catch (TimeoutException ex)
        {
            // CI 環境 (self-hosted runner でオーディオデバイス無し等) や ACB 未配置ビルドで発生しうる。
            // ここで例外を握り潰さないと IsInitialized が永久に false になり、
            // TitlePresenter/GameInitializer の WaitUntil(IsInitialized) がハング → PlayMode テストが全滅する。
            // BGM 無しで進行させる方が失敗モードとして安全。
            Debug.LogWarning($"[CriBgmController] {ex.Message} — BGM 無しで進行します (CI / ACB 未配置環境の可能性)");
        }
        finally
        {
            IsInitialized = true;
        }
    }

    /// <summary>
    /// BGM再生の内部処理
    /// </summary>
    /// <param name="cueReference">再生するキュー参照</param>
    /// <param name="requestId">この再生を頼んだときの依頼番号</param>
    private async UniTaskVoid PlayBgmInternal(CriAtomCueReference cueReference, int requestId)
    {
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
            HasCurrentPlayback = false;
            await UniTask.Delay(50);
        }
        // 前の曲を止めて待つ間に Stop や別の PlayBgm が来たら、古い依頼の曲は鳴らさない
        if (requestId != _playRequestId) return;

        _currentPlayback = _player.StartPlayback(cueReference);
        // 再生開始直後に正しい音量を設定（一瞬大音量になるのを防ぐ）
        _currentFadeVolume = 1f;
        HasCurrentPlayback = true;
        UpdateVolume();

        ApplyAisacAfterStart(requestId).Forget();
    }

    /// <summary>
    /// 今の曲を前の曲として下げていきながら、次の曲を無音から上げて重ねる
    /// </summary>
    private void CrossfadeTo(CriAtomCueReference cueReference, int requestId)
    {
        ReleaseCurrentToOutgoing(crossfadeDuration);

        _currentPlayback = _player.StartPlayback(cueReference);
        _currentFadeVolume = 0f;
        HasCurrentPlayback = true;
        UpdateVolume();
        _fadeHandle = LMotion.Create(0f, 1f, crossfadeDuration)
            .Bind(x =>
            {
                _currentFadeVolume = x;
                UpdateVolume();
            });

        ApplyAisacAfterStart(requestId).Forget();
    }

    /// <summary>
    /// 今の曲を前の曲へ移し、duration かけて音量を下げきったら止める。下げている途中だった前の曲はすぐ止める
    /// </summary>
    private void ReleaseCurrentToOutgoing(float duration)
    {
        _fadeHandle.TryCancel();
        StopOutgoing();
        if (!HasCurrentPlayback) return;

        _outgoingPlayback = _currentPlayback;
        _outgoingFadeVolume = _currentFadeVolume;
        _hasOutgoingPlayback = true;
        HasCurrentPlayback = false;
        _outgoingFadeHandle = LMotion.Create(_outgoingFadeVolume, 0f, duration)
            .WithOnComplete(() =>
            {
                _outgoingPlayback.Stop();
                _hasOutgoingPlayback = false;
            })
            .Bind(x =>
            {
                _outgoingFadeVolume = x;
                UpdateVolume();
            });
    }

    private void StopOutgoing()
    {
        _outgoingFadeHandle.TryCancel();
        if (!_hasOutgoingPlayback) return;
        _outgoingPlayback.Stop();
        _hasOutgoingPlayback = false;
    }

    private async UniTaskVoid ApplyAisacAfterStart(int requestId)
    {
        await UniTask.Delay(100);
        if (requestId != _playRequestId || !HasCurrentPlayback) return;
        _currentPlayback.SetAisacControl(AISAC_CONTROL_NAME, _aisacValue);
    }

    /// <summary>
    /// 音量を更新（基礎音量 × マスターボリューム × フェード係数 × 一時音量係数）
    /// </summary>
    private void UpdateVolume()
    {
        var volume = baseVolume * _bgmVolume * _temporaryVolume * (IsEditorAudioMuted() ? 0f : 1f);
        if (HasCurrentPlayback)
            _currentPlayback.SetVolumeAndPitch(volume * _currentFadeVolume, 1.0f);
        if (_hasOutgoingPlayback)
            _outgoingPlayback.SetVolumeAndPitch(volume * _outgoingFadeVolume, 1.0f);
    }

    // CRI の再生は Unity の AudioListener を通らないため、エディタの消音 (Game ビューの Mute やテスト実行中の消音) を自前で反映する
    private static bool IsEditorAudioMuted()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.audioMasterMute;
#else
        return false;
#endif
    }

#if UNITY_EDITOR
    private bool _lastEditorAudioMuted;

    private void Update()
    {
        var muted = IsEditorAudioMuted();
        if (muted == _lastEditorAudioMuted) return;
        _lastEditorAudioMuted = muted;
        UpdateVolume();
    }
#endif

    /// <summary>
    /// 一時停止の内部処理
    /// </summary>
    private void PauseInternal()
    {
        if (HasCurrentPlayback)
            _currentPlayback.Pause();
        if (_hasOutgoingPlayback)
            _outgoingPlayback.Pause();
    }
}
#endif
