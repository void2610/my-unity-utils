using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

/// <summary>
/// 嘔吐・眩暈系の視界変化をまとめて扱う。
/// カメラの親にリグを差し込み、シェイクとは別レイヤーで視点の沈み込み・ズーム・ロール揺れを合成する。
/// </summary>
public sealed class CameraDisorientationEffect : ConfigurableCinematicEffectBase<CameraDisorientationConfig>
{
    public override string EffectName => "カメラ歪み";

    private Camera _targetCamera;
    private Transform _cameraTransform;
    private Transform _rigTransform;
    private Vector3 _defaultRigPosition;
    private Quaternion _defaultRigRotation;
    private Vector3 _defaultCameraLocalPosition;
    private Quaternion _defaultCameraLocalRotation;
    private Vector3 _defaultCameraLocalScale;
    private float _defaultOrthographicSize;
    private float _defaultFieldOfView;
    private float _currentBlend;

    public CameraDisorientationEffect() : base()
    {
        OnResetImmediate();
    }

    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        EnsureInitialized();
        CaptureDefaults();
        await AnimateBlendAsync(_currentBlend, 1f, CurrentConfig.EnterDuration, ct);

        while (!ct.IsCancellationRequested)
        {
            Apply(_currentBlend);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    protected override async UniTask OnStopAsync(CancellationToken ct)
    {
        EnsureInitialized();

        // 現在のリグ状態をキャプチャ
        // Sway を計算し続けると終了中に不規則な揺れが発生するため、
        // 現在位置からデフォルトへ直接補間する
        var startPosition = _rigTransform.localPosition;
        var startRotation = _rigTransform.localRotation;
        var startFov = _targetCamera.fieldOfView;
        var startSize = _targetCamera.orthographicSize;

        await AnimateBlendAsync(0f, 1f, CurrentConfig.ExitDuration, CurrentConfig.Ease, t =>
        {
            _currentBlend = 1f - t;
            _rigTransform.localPosition = Vector3.Lerp(startPosition, _defaultRigPosition, t);
            _rigTransform.localRotation = Quaternion.Slerp(startRotation, _defaultRigRotation, t);
            if (_targetCamera.orthographic)
                _targetCamera.orthographicSize = Mathf.Lerp(startSize, _defaultOrthographicSize, t);
            else
                _targetCamera.fieldOfView = Mathf.Lerp(startFov, _defaultFieldOfView, t);
        }, ct);

        RestoreDefaults();
    }

    protected override void OnResetImmediate()
    {
        if (_rigTransform == null || _targetCamera == null)
        {
            return;
        }

        _currentBlend = 0f;
        RestoreDefaults();
    }

    /// <summary>共通静的ヘルパーに委譲するインスタンスラッパー。</summary>
    private UniTask AnimateBlendAsync(float from, float to, float duration, CancellationToken ct)
    {
        return AnimateBlendAsync(from, to, duration, CurrentConfig.Ease, value =>
        {
            _currentBlend = value;
            Apply(value);
        }, ct);
    }

    private void Apply(float blend)
    {
        if (_rigTransform == null || _targetCamera == null)
        {
            return;
        }

        // 1周期を4クォーターに分割し、各クォーターにEaseを適用して滑らかな往復波を生成する
        // 各軸に異なる速度・位相オフセットを与えて単調にならないようにする
        var swayEase = CurrentConfig.SwayEase;
        var swayX = EvaluateSwayEase(Time.unscaledTime * CurrentConfig.SwayFrequency, swayEase) * CurrentConfig.SwayPositionAmplitude;
        var swayY = EvaluateSwayEase(Time.unscaledTime * CurrentConfig.SwayFrequency * 0.73f + 0.143f, swayEase) * (CurrentConfig.SwayPositionAmplitude * 0.6f);
        var swayZ = EvaluateSwayEase(Time.unscaledTime * CurrentConfig.SwayFrequency * 0.87f + 0.048f, swayEase) * CurrentConfig.SwayRotationAmplitude;

        var rigOffset = Vector3.Lerp(Vector3.zero, CurrentConfig.PositionOffset, blend);
        var swayOffset = new Vector3(swayX, swayY, 0f) * blend;
        _rigTransform.localPosition = _defaultRigPosition + rigOffset + swayOffset;

        var targetRotation = CurrentConfig.RotationZ * blend + swayZ * blend;
        _rigTransform.localRotation = _defaultRigRotation * Quaternion.Euler(0f, 0f, targetRotation);

        if (_targetCamera.orthographic)
        {
            var targetSize = _defaultOrthographicSize * CurrentConfig.OrthographicSizeScale;
            _targetCamera.orthographicSize = Mathf.Lerp(_defaultOrthographicSize, targetSize, blend);
        }
        else
        {
            var targetFov = _defaultFieldOfView + CurrentConfig.PerspectiveFovDelta;
            _targetCamera.fieldOfView = Mathf.Lerp(_defaultFieldOfView, targetFov, blend);
        }
    }

    /// <summary>
    /// 正規化位相（任意の実数）をEaseで整形した往復波（-1〜1）に変換する。
    /// 1周期を4クォーターに分け、各クォーターにEaseを適用して滑らかな往復を作る。
    ///   0→0.25 : 0→+1（正方向 上昇）
    ///   0.25→0.5: +1→0（正方向 下降）
    ///   0.5→0.75: 0→-1（負方向 下降）
    ///   0.75→1.0: -1→0（負方向 上昇）
    /// </summary>
    private static float EvaluateSwayEase(float phase, Ease ease)
    {
        var normalizedPhase = Mathf.Repeat(phase, 1f);
        var quarterIndex = Mathf.FloorToInt(normalizedPhase * 4f);
        var quarterPhase = (normalizedPhase * 4f) % 1f;
        var easedQuarter = EaseUtility.Evaluate(quarterPhase, ease);
        return quarterIndex switch
        {
            0 => easedQuarter,
            1 => 1f - easedQuarter,
            2 => -easedQuarter,
            _ => -(1f - easedQuarter),
        };
    }

    private void RestoreDefaults()
    {
        _rigTransform.localPosition = _defaultRigPosition;
        _rigTransform.localRotation = _defaultRigRotation;
        _cameraTransform.localPosition = _defaultCameraLocalPosition;
        _cameraTransform.localRotation = _defaultCameraLocalRotation;
        _cameraTransform.localScale = _defaultCameraLocalScale;
        _targetCamera.orthographicSize = _defaultOrthographicSize;
        _targetCamera.fieldOfView = _defaultFieldOfView;
    }

    private void EnsureInitialized()
    {
        if (_targetCamera != null && _rigTransform != null)
        {
            return;
        }

        _targetCamera = Camera.main;
        if (_targetCamera == null)
        {
            throw new MissingReferenceException("Main Camera was not found.");
        }

        _cameraTransform = _targetCamera.transform;
        _rigTransform = EnsureRig(_cameraTransform);
        CaptureDefaults();
    }

    private void CaptureDefaults()
    {
        _defaultRigPosition = _rigTransform.localPosition;
        _defaultRigRotation = _rigTransform.localRotation;
        _defaultCameraLocalPosition = _cameraTransform.localPosition;
        _defaultCameraLocalRotation = _cameraTransform.localRotation;
        _defaultCameraLocalScale = _cameraTransform.localScale;
        _defaultOrthographicSize = _targetCamera.orthographicSize;
        _defaultFieldOfView = _targetCamera.fieldOfView;
    }

    private static Transform EnsureRig(Transform cameraTransform)
    {
        if (cameraTransform.parent != null && cameraTransform.parent.name == "CinematicCameraRig")
        {
            return cameraTransform.parent;
        }

        var originalParent = cameraTransform.parent;
        var originalSiblingIndex = cameraTransform.GetSiblingIndex();
        var rig = new GameObject("CinematicCameraRig").transform;
        rig.SetParent(originalParent, false);
        rig.SetSiblingIndex(originalSiblingIndex);
        rig.SetPositionAndRotation(cameraTransform.position, cameraTransform.rotation);
        rig.localScale = Vector3.one;
        cameraTransform.SetParent(rig, true);
        return rig;
    }
}
