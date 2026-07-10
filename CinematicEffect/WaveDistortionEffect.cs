using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

/// <summary>
/// フルスクリーン波打ち歪みシェーダーを LitMotion でフェードイン/アウトする演出。
/// <see cref="WaveDistortionConfig"/> で周波数・振幅・速度を設定する。
/// マテリアル (WaveDistortion.shader) はコンストラクター引数で受け取るか、未配線時は Resources から自己調達する。
/// </summary>
public sealed class WaveDistortionEffect : ConfigurableCinematicEffectBase<WaveDistortionConfig>
{
    public override string EffectName => "波打ち歪み";

    // シェーダープロパティ ID をキャッシュして毎フレームの文字列ルックアップを避ける
    private static readonly int FrequencyId = Shader.PropertyToID("_Frequency");
    private static readonly int AmplitudeId = Shader.PropertyToID("_Amplitude");
    private static readonly int SpeedId = Shader.PropertyToID("_Speed");

    // VisionWarp と違い同梱の default マテリアルが無いため、Resources から遅延ロードする
    private const string DefaultMaterialResourcePath = "WaveDistortion";

    private Material _waveDistortionMaterial;

    private float _currentAmplitude;

    // 未配線時は Resources から自己調達する。default アセットが無いため実際に再生されるまでロードは保留する
    public WaveDistortionEffect() : base() { }

    public WaveDistortionEffect(Material waveDistortionMaterial) : base()
    {
        _waveDistortionMaterial = waveDistortionMaterial;
        OnResetImmediate();
    }

    // 未配線構築時のマテリアル自己調達。パス設定ミスを後段の NRE ではなくロード時点で顕在化させる
    private void EnsureMaterial()
    {
        if (_waveDistortionMaterial != null) return;

        _waveDistortionMaterial = Resources.Load<Material>(DefaultMaterialResourcePath);
        if (_waveDistortionMaterial == null) throw new InvalidOperationException($"Resources から WaveDistortion マテリアルをロードできません: {DefaultMaterialResourcePath}");
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        EnsureMaterial();

        // 周波数・速度を即時反映し、振幅をフェードイン
        _waveDistortionMaterial.SetFloat(FrequencyId, CurrentConfig.Frequency);
        _waveDistortionMaterial.SetFloat(SpeedId, CurrentConfig.Speed);

        await AnimateAmplitudeAsync(0f, CurrentConfig.Amplitude, CurrentConfig.EnterDuration, CurrentConfig.Ease, ct);

        // キャンセルされるまで毎フレーム待機してループを維持
        while (!ct.IsCancellationRequested)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    protected override async UniTask OnStopAsync(CancellationToken ct)
    {
        // 一度も再生していなければマテリアル未ロード。止める対象が無いので何もしない
        if (_waveDistortionMaterial == null) return;

        await AnimateAmplitudeAsync(_currentAmplitude, 0f, CurrentConfig.ExitDuration, CurrentConfig.Ease, ct);
        _waveDistortionMaterial.SetFloat(AmplitudeId, 0f);
    }

    protected override void OnResetImmediate()
    {
        if (_waveDistortionMaterial == null) return;

        _currentAmplitude = 0f;
        _waveDistortionMaterial.SetFloat(AmplitudeId, 0f);
    }

    private async UniTask AnimateAmplitudeAsync(float from, float to, float duration, Ease ease, CancellationToken ct)
    {
        if (duration <= 0f)
        {
            _currentAmplitude = to;
            _waveDistortionMaterial.SetFloat(AmplitudeId, to);
            return;
        }

        var motion = LMotion.Create(from, to, duration)
            .WithEase(ease)
            .Bind(value =>
            {
                _currentAmplitude = value;
                _waveDistortionMaterial.SetFloat(AmplitudeId, value);
            });

        await motion.ToUniTask(cancellationToken: ct);
    }
}
