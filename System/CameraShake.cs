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
        private Vector3 _originalPosition;
        private MotionHandle _shakeHandle;
        
        protected override void Awake()
        {
            base.Awake();
            // DontDestroyOnLoadを解除してシーン固有で使用
            SceneManager.MoveGameObjectToScene(this.gameObject, SceneManager.GetActiveScene());
            _originalPosition = this.transform.position;
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
        /// カメラを一定時間揺らし、完了後に await できる
        /// </summary>
        /// <param name="magnitude">振動の強さ</param>
        /// <param name="duration">振動時間</param>
        /// <param name="frequency">振動の周波数</param>
        /// <param name="dampingRatio">減衰率</param>
        /// <returns>完了待機可能なUniTask</returns>
        public async UniTask ShakeCameraAsync(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f)
        {
            if(_shakeHandle.IsPlaying()) _shakeHandle.Complete();

            _shakeHandle = LMotion.Shake.Create(startValue: Vector3.zero, strength: Vector3.one * magnitude, duration: duration)
                .WithFrequency(frequency)
                .WithDampingRatio(dampingRatio)
                .Bind(v => this.transform.position = _originalPosition + v)
                .AddTo(this);

            await _shakeHandle.ToUniTask();
            this.transform.position = _originalPosition;
        }

        protected override void OnDestroy()
        {
            if (_shakeHandle.IsPlaying()) _shakeHandle.Cancel();
            base.OnDestroy();
        }
    }
}