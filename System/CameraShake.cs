using UnityEngine;
using LitMotion;
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
        protected MotionHandle ShakeHandle;

        protected override void Awake()
        {
            base.Awake();
            // DontDestroyOnLoadを解除してシーン固有で使用
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        }

        /// <summary>
        /// カメラを振動させる（非同期実行）
        /// </summary>
        public void ShakeCamera(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f) =>
            ShakeCameraAsync(magnitude, duration, frequency, dampingRatio).Forget();

        /// <summary>
        /// カメラの振動を止める
        /// </summary>
        public void StopShake() => ShakeHandle.TryComplete();

        /// <summary>
        /// シェイク中かどうか
        /// </summary>
        public bool IsShaking() => ShakeHandle.IsActive();

        /// <summary>
        /// カメラを一定時間揺らし、完了後に await できる
        /// </summary>
        public virtual async UniTask ShakeCameraAsync(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f)
        {
            ShakeHandle.TryComplete();

            var startPosition = transform.position;

            ShakeHandle = LMotion.Shake.Create(startValue: Vector3.zero, strength: Vector3.one * magnitude, duration: duration)
                .WithFrequency(frequency)
                .WithDampingRatio(dampingRatio)
                .Bind(v => transform.position = startPosition + v)
                .AddTo(this);

            await ShakeHandle.ToUniTask();
            transform.position = startPosition;
        }

        protected override void OnDestroy()
        {
            ShakeHandle.TryCancel();
            base.OnDestroy();
        }
    }
}
