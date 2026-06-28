using System.Threading;
using Cysharp.Threading.Tasks;
using Void2610.UnityTemplate;

/// <summary>
/// <see cref="CameraShake"/> シングルトンに委譲するシェイク演出。
/// </summary>
public sealed class CameraShakeEffect : ConfigurableCinematicEffectBase<CameraShakeConfig>
{
    public override string EffectName => "カメラ振動";

    public CameraShakeEffect() : base()
    {
        OnResetImmediate();
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        if (CurrentConfig.Loop)
        {
            while (!ct.IsCancellationRequested)
            {
                await CameraShake.Instance.ShakeCameraAsync(CurrentConfig.Magnitude, CurrentConfig.Duration, CurrentConfig.Frequency);
            }
        }
        else
        {
            await CameraShake.Instance.ShakeCameraAsync(CurrentConfig.Magnitude, CurrentConfig.Duration, CurrentConfig.Frequency);
        }
    }

    protected override UniTask OnStopAsync(CancellationToken ct)
    {
        CameraShake.Instance.StopShake();
        return UniTask.CompletedTask;
    }

    protected override void OnResetImmediate()
    {
        if (CameraShake.HasInstance) CameraShake.Instance.StopShake();
    }
}
