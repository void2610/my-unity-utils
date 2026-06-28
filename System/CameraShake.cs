using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// LitMotionを使用したシンプルなカメラ振動システム
    /// SingletonMonoBehaviourを継承してアクセスしやすくする
    ///
    /// 既定では <see cref="Camera.main"/> の transform を揺らすため、 シーンに本コンポーネントを
    /// 事前配置する必要はない (Singleton が自動生成し、 都度 Camera.main を解決する)。
    /// 特定カメラを揺らしたい場合は <see cref="TargetCamera"/> に明示セットするか、
    /// Inspector の <c>targetCamera</c> に割り当てる。
    /// </summary>
    public class CameraShake : SingletonMonoBehaviour<CameraShake>
    {
        [SerializeField] private Camera targetCamera;
        protected MotionHandle ShakeHandle;

        /// <summary>揺らす対象のカメラ。 未設定なら <see cref="Camera.main"/> を都度解決する。</summary>
        public Camera TargetCamera
        {
            get
            {
                if (targetCamera == null) targetCamera = Camera.main;
                return targetCamera;
            }
            set => targetCamera = value;
        }

        /// <summary>
        /// カメラを振動させる（非同期実行）
        /// </summary>
        public void ShakeCamera(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f) => ShakeCameraAsync(magnitude, duration, frequency, dampingRatio).Forget();

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
            var cam = TargetCamera;
            if (cam == null) return; // 揺らす対象が無ければ静かに無視する

            ShakeHandle.TryComplete();

            var camTransform = cam.transform;
            var startPosition = camTransform.position;

            ShakeHandle = LMotion.Shake.Create(startValue: Vector3.zero, strength: Vector3.one * magnitude, duration: duration)
                .WithFrequency(frequency)
                .WithDampingRatio(dampingRatio)
                .Bind(v => { if (camTransform != null) camTransform.position = startPosition + v; })
                .AddTo(this);

            await ShakeHandle.ToUniTask();
            if (camTransform != null) camTransform.position = startPosition;
        }

        protected override void Awake()
        {
            base.Awake();
            // DontDestroyOnLoadを解除してシーン固有で使用
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        }

        protected override void OnDestroy()
        {
            ShakeHandle.TryCancel();
            base.OnDestroy();
        }
    }
}
