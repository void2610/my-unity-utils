using UnityEngine;
using Random = UnityEngine.Random;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// オブジェクトをふわふわと浮遊させるアニメーションコンポーネント
    /// UIエフェクトやゲームオブジェクトの演出に使用
    /// </summary>
    public class FloatMove : MonoBehaviour
    {
        [Header("フロート設定")]
        [SerializeField] private float moveDistance = 0.2f; // 移動距離
        [SerializeField] private float moveDuration = 1f; // 移動時間
        [SerializeField] private float startDelay; // 開始遅延
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 移動カーブ
        [SerializeField] private bool autoStart = true; // 自動開始
        [SerializeField] private bool useRandomOffset; // ランダムオフセット
        
        private Vector3 _originalPosition;
        private bool _isMovingUp = true;
        private float _currentTime;
        private bool _isActive;
        private RectTransform _rectTransform;
        private Transform _targetTransform;
        
        /// <summary>
        /// フロートアニメーションを停止
        /// </summary>
        public void StopFloating()
        {
            _isActive = false;
        }

        /// <summary>
        /// 元の位置に戻す
        /// </summary>
        public void ResetToOriginalPosition()
        {
            _isActive = false;
            _targetTransform.localPosition = _originalPosition;
        }

        /// <summary>
        /// 指定位置に移動してからフロートを開始
        /// </summary>
        public void MoveToAndFloat(Vector3 targetPosition, float moveDuration = 1f)
        {
            StopFloating();
            StartCoroutine(MoveToCoroutine(targetPosition, moveDuration));
        }

        /// <summary>
        /// フロートアニメーションを開始
        /// </summary>
        public void StartFloating()
        {
            _isActive = true;
            _currentTime = 0f;
            _isMovingUp = true;
        }

        private void Awake()
        {
            // RectTransformまたはTransformを取得
            _rectTransform = GetComponent<RectTransform>();
            _targetTransform = _rectTransform != null ? _rectTransform : transform;
            
            // 元の位置を保存
            _originalPosition = _targetTransform.localPosition;
            
            // ランダムオフセットを適用
            if (useRandomOffset)
            {
                startDelay += Random.Range(0f, moveDuration);
                _currentTime = Random.Range(0f, moveDuration);
            }
        }

        private void Start()
        {
            if (autoStart)
            {
                if (startDelay > 0)
                {
                    Invoke(nameof(StartFloating), startDelay);
                }
                else
                {
                    StartFloating();
                }
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            // 時間を更新
            _currentTime += Time.deltaTime;

            // ループ処理
            if (_currentTime >= moveDuration)
            {
                _currentTime = 0f;
                _isMovingUp = !_isMovingUp;
            }

            var curveValue = moveCurve.Evaluate(_currentTime / moveDuration);
            var yOffset = _isMovingUp ? Mathf.Lerp(0f, moveDistance, curveValue) : Mathf.Lerp(moveDistance, 0f, curveValue);

            // 位置を更新
            var newPosition = _originalPosition + Vector3.up * yOffset;
            _targetTransform.localPosition = newPosition;
        }

        /// <summary>
        /// 移動のコルーチン
        /// </summary>
        private System.Collections.IEnumerator MoveToCoroutine(Vector3 targetPosition, float duration)
        {
            var startPosition = _targetTransform.localPosition;
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var t = elapsedTime / duration;
                
                // スムーズな移動
                _targetTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, moveCurve.Evaluate(t));
                
                yield return null;
            }

            // 最終位置を設定
            _targetTransform.localPosition = targetPosition;
            _originalPosition = targetPosition;
            
            // フロートアニメーション開始
            StartFloating();
        }

        private void OnDisable()
        {
            StopFloating();
        }
        
        /// <summary>
        /// エディタでのギズモ表示
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;

            Gizmos.color = Color.yellow;
            var basePos = transform.position;
            
            // 移動範囲を表示
            Gizmos.DrawWireSphere(basePos, 0.1f);
            Gizmos.DrawWireSphere(basePos + Vector3.up * moveDistance, 0.1f);
            Gizmos.DrawLine(basePos, basePos + Vector3.up * moveDistance);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}