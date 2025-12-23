using UnityEngine;
using LitMotion;
using Void2610.UnityTemplate;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// LitMotionを使用したシンプルなカメラ振動システム
    /// SingletonMonoBehaviourを継承してアクセスしやすくする
    /// </summary>
    public class CameraShake : SingletonMonoBehaviour<CameraShake>
    {
        private MotionHandle _shakeHandle;
        private float _continuousMagnitude;
        private Vector3? _basePosition;
        
        /// <summary>                                 
        /// 継続的な揺れの強度を設定（0で停止）       
        /// </summary> 
        public void SetContinuousMagnitude(float magnitude)
        {
            if (!_basePosition.HasValue) _basePosition = transform.position;
            
            _continuousMagnitude = magnitude;
            if (magnitude <= 0f) transform.position = _basePosition.Value;
        }

        /// <summary>
        /// カメラを振動させる（非同期実行）
        /// </summary>
        /// <param name="magnitude">振動の強さ</param>
        /// <param name="duration">振動時間</param>
        /// <param name="frequency">振動の周波数</param>
        /// <param name="dampingRatio">減衰率</param>
        public void ShakeCamera(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f) => 
            ShakeCameraAsync(magnitude, duration, frequency, dampingRatio).Forget();

        /// <summary>
        /// カメラの振動を止める
        /// </summary>
        public void StopShake() => _shakeHandle.TryComplete();

        /// <summary>
        /// カメラを一定時間揺らし、完了後に await できる
        /// </summary>
        /// <param name="magnitude">振動の強さ</param>
        /// <param name="duration">振動時間</param>
        /// <param name="frequency">振動の周波数</param>
        /// <param name="dampingRatio">減衰率</param>
        /// <returns>完了待機可能なUniTask</returns>
        public async UniTask ShakeCameraAsync(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f)
        {
            _shakeHandle.TryComplete();

            // ベースポジションから開始
            if (!_basePosition.HasValue) _basePosition = transform.position;
            var startPosition = _basePosition.Value;

            _shakeHandle = LMotion.Shake.Create(startValue: Vector3.zero, strength: Vector3.one * magnitude, duration: duration)
                .WithFrequency(frequency)
                .WithDampingRatio(dampingRatio)
                .Bind(v => this.transform.position = startPosition + v)
                .AddTo(this);

            await _shakeHandle.ToUniTask();
            this.transform.position = startPosition;
        }
        
        private void Update()
        {
            if (!_basePosition.HasValue) return;
            if (_continuousMagnitude <= 0f) return;
            // ShakeCamera中は継続的な揺れをスキップ
            if (_shakeHandle.IsActive()) return;

            // Perlin Noiseによる継続的な揺れ
            var time = Time.time * 10f;
            var offsetX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * _continuousMagnitude;
            var offsetY = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f * _continuousMagnitude;
            transform.position = _basePosition.Value + new Vector3(offsetX, offsetY, 0f);
        }

        protected override void Awake()
        {
            _basePosition = transform.position;
            base.Awake();
            // DontDestroyOnLoadを解除してシーン固有で使用
            SceneManager.MoveGameObjectToScene(this.gameObject, SceneManager.GetActiveScene());
        }

        protected override void OnDestroy()
        {
            _shakeHandle.TryCancel();
            base.OnDestroy();
        }
    }
}