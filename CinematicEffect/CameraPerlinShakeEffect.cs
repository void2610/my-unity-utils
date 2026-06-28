using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

/// <summary>
/// パーリンノイズを使ったゆっくりとした滑らかなカメラ揺れ演出。
/// </summary>
public sealed class CameraPerlinShakeEffect : ConfigurableCinematicEffectBase<CameraPerlinShakeConfig>
{
    public override string EffectName => "カメラ揺れ（パーリンノイズ）";

    private Vector3 _startPosition;
    private Camera _cam;

    public CameraPerlinShakeEffect() : base()
    {
        OnResetImmediate();
    }

    protected override async UniTask OnStopAsync(CancellationToken ct)
    {
        if (_cam == null) return;

        // 現在位置からスタート位置へスムーズに補間して戻す
        var currentPosition = _cam.transform.position;
        await AnimateBlendAsync(0f, 1f, CurrentConfig.ExitDuration, CurrentConfig.Ease, t =>
        {
            if (_cam != null)
                _cam.transform.position = Vector3.Lerp(currentPosition, _startPosition, t);
        }, ct);

        if (_cam != null) _cam.transform.position = _startPosition;
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        _cam = Camera.main;
        if (_cam == null) return;

        _startPosition = _cam.transform.position;
        // サンプリング開始位置をランダムにして単調にならないようにする
        var seed = Random.value * 1000f;
        // 開始時点のノイズ値を基準にすることで elapsed=0 のオフセットが必ず 0 になる
        var baseX = Mathf.PerlinNoise(seed, 0f);
        var baseY = Mathf.PerlinNoise(0f, seed);
        var elapsed = 0f;

        while (!ct.IsCancellationRequested)
        {
            var x = (Mathf.PerlinNoise(seed + elapsed * CurrentConfig.Speed, 0f) - baseX) * 2f * CurrentConfig.Magnitude;
            var y = (Mathf.PerlinNoise(0f, seed + elapsed * CurrentConfig.Speed) - baseY) * 2f * CurrentConfig.Magnitude;
            _cam.transform.position = _startPosition + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            await UniTask.Yield(ct);
        }
    }

    protected override void OnResetImmediate()
    {
        if (_cam != null) _cam.transform.position = _startPosition;
    }
}
