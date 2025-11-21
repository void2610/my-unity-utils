using UnityEngine;

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
        [SerializeField] private float startDelay = 0f; // 開始遅延
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 移動カーブ
        [SerializeField] private bool autoStart = true; // 自動開始
        [SerializeField] private bool useRandomOffset = false; // ランダムオフセット
        
        private Vector3 _originalPosition;
        private bool _isMovingUp = true;
        private float _currentTime = 0f;
        private bool _isActive = false;
        private RectTransform _rectTransform;
        private Transform _targetTransform;

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

            // 正規化された時間（0-1）
            float normalizedTime = _currentTime / moveDuration;
            
            // アニメーションカーブを適用
            float curveValue = moveCurve.Evaluate(normalizedTime);
            
            // Y座標を計算
            float yOffset = _isMovingUp ? 
                Mathf.Lerp(0f, moveDistance, curveValue) : 
                Mathf.Lerp(moveDistance, 0f, curveValue);

            // 位置を更新
            Vector3 newPosition = _originalPosition + Vector3.up * yOffset;
            _targetTransform.localPosition = newPosition;
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
        /// 移動のコルーチン
        /// </summary>
        private System.Collections.IEnumerator MoveToCoroutine(Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = _targetTransform.localPosition;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                
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

        /// <summary>
        /// 設定を更新
        /// </summary>
        public void UpdateSettings(float newMoveDistance, float newMoveDuration)
        {
            moveDistance = newMoveDistance;
            moveDuration = newMoveDuration;
        }

        /// <summary>
        /// 現在のフロート状態を取得
        /// </summary>
        public bool IsFloating => _isActive;

        private void OnDisable()
        {
            StopFloating();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// エディタでのギズモ表示
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;

            Gizmos.color = Color.yellow;
            Vector3 basePos = transform.position;
            
            // 移動範囲を表示
            Gizmos.DrawWireSphere(basePos, 0.1f);
            Gizmos.DrawWireSphere(basePos + Vector3.up * moveDistance, 0.1f);
            Gizmos.DrawLine(basePos, basePos + Vector3.up * moveDistance);
        }
    }
}