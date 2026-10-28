using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 角度を補間せず一定間隔でパッと切り替える、パラパラ漫画風の揺れ (Z 回転)
    /// 有効化のたびに先頭の角度から再生し直すので、SetActive で使い回すオブジェクトにそのまま付けられる
    /// </summary>
    public class SteppedRotationAnimator : MonoBehaviour
    {
        [SerializeField] private float[] angles = { -2f, 2f }; // 配置時の回転からの差 (度)。この順に繰り返す
        [SerializeField, Min(0.1f)] private float stepsPerSecond = 4f;

        private Quaternion _baseRotation;
        private int _step;
        private float _timer;

        private void Awake()
        {
            _baseRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            _step = 0;
            _timer = 0f;
            ApplyStep();
        }

        private void Update()
        {
            var interval = 1f / stepsPerSecond;
            _timer += Time.deltaTime;
            if (_timer < interval) return;

            _timer -= interval;
            _step = (_step + 1) % angles.Length;
            ApplyStep();
        }

        private void ApplyStep()
        {
            transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angles[_step]);
        }
    }
}
