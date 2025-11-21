using System.Collections.Generic;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// スプライトシートを使用した2Dアニメーションコンポーネント
    /// 分割済みスプライトのリストを使用してフレームアニメーションを再生
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteSheetAnimator : MonoBehaviour
    {
        [Header("アニメーション設定")]
        [SerializeField] private List<Sprite> sprites = new List<Sprite>();
        [SerializeField] private float framesPerSecond = 10f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loopAnimation = true;
        
        [Header("制御設定")]
        [SerializeField] private bool randomStartFrame = false;
        [SerializeField] private int startFrame = 0;

        private float _timer;
        private int _currentFrame;
        private SpriteRenderer _spriteRenderer;
        private bool _isPlaying = false;
        private bool _isPaused = false;

        /// <summary>
        /// 現在再生中かどうか
        /// </summary>
        public bool IsPlaying => _isPlaying && !_isPaused;
        
        /// <summary>
        /// 現在のフレーム番号
        /// </summary>
        public int CurrentFrame => _currentFrame;
        
        /// <summary>
        /// 総フレーム数
        /// </summary>
        public int TotalFrames => sprites?.Count ?? 0;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (sprites == null || sprites.Count == 0)
            {
                Debug.LogWarning($"[SpriteSheetAnimator] {gameObject.name}: スプライトが設定されていません");
                return;
            }

            // ランダム開始フレーム
            if (randomStartFrame)
            {
                _currentFrame = Random.Range(0, sprites.Count);
            }
            else
            {
                _currentFrame = Mathf.Clamp(startFrame, 0, sprites.Count - 1);
            }

            // 初期スプライトを設定
            UpdateSprite();

            if (playOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!_isPlaying || _isPaused || sprites == null || sprites.Count == 0) return;

            _timer += Time.deltaTime;
            
            // フレーム更新のタイミングかチェック
            if (_timer >= 1f / framesPerSecond)
            {
                _timer -= 1f / framesPerSecond;
                NextFrame();
            }
        }

        /// <summary>
        /// 次のフレームに進む
        /// </summary>
        private void NextFrame()
        {
            _currentFrame++;
            
            // ループ処理
            if (_currentFrame >= sprites.Count)
            {
                if (loopAnimation)
                {
                    _currentFrame = 0;
                }
                else
                {
                    _currentFrame = sprites.Count - 1;
                    Stop();
                    return;
                }
            }

            UpdateSprite();
        }

        /// <summary>
        /// スプライトを更新
        /// </summary>
        private void UpdateSprite()
        {
            if (sprites != null && _currentFrame >= 0 && _currentFrame < sprites.Count)
            {
                _spriteRenderer.sprite = sprites[_currentFrame];
            }
        }

        /// <summary>
        /// アニメーションを開始
        /// </summary>
        public void Play()
        {
            if (sprites == null || sprites.Count == 0) return;
            
            _isPlaying = true;
            _isPaused = false;
            _timer = 0f;
        }

        /// <summary>
        /// アニメーションを停止
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
            _timer = 0f;
        }

        /// <summary>
        /// アニメーションを一時停止
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// 一時停止を解除
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// アニメーションをリセット（最初のフレームに戻る）
        /// </summary>
        public void Reset()
        {
            _currentFrame = 0;
            _timer = 0f;
            UpdateSprite();
        }

        /// <summary>
        /// 特定のフレームに移動
        /// </summary>
        public void SetFrame(int frameIndex)
        {
            if (sprites == null || frameIndex < 0 || frameIndex >= sprites.Count) return;
            
            _currentFrame = frameIndex;
            UpdateSprite();
        }

        /// <summary>
        /// スプライトリストとFPSを設定
        /// </summary>
        public void Setup(List<Sprite> newSprites, float fps)
        {
            sprites = newSprites;
            framesPerSecond = fps;
            
            if (sprites != null && sprites.Count > 0)
            {
                _currentFrame = 0;
                UpdateSprite();
            }
        }

        /// <summary>
        /// アニメーション設定を更新
        /// </summary>
        public void UpdateSettings(float newFps, bool newLoopAnimation, bool newPlayOnStart)
        {
            framesPerSecond = newFps;
            loopAnimation = newLoopAnimation;
            playOnStart = newPlayOnStart;
        }

        /// <summary>
        /// 指定したスプライトリストでアニメーションを変更
        /// </summary>
        public void ChangeAnimation(List<Sprite> newSprites, float newFps = -1f, bool autoPlay = true)
        {
            Stop();
            
            sprites = newSprites;
            if (newFps > 0) framesPerSecond = newFps;
            
            if (sprites != null && sprites.Count > 0)
            {
                Reset();
                if (autoPlay) Play();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタでの検証
        /// </summary>
        private void OnValidate()
        {
            framesPerSecond = Mathf.Max(0.1f, framesPerSecond);
            startFrame = Mathf.Max(0, startFrame);
        }

        /// <summary>
        /// エディタでのテスト再生
        /// </summary>
        [ContextMenu("Test Play Animation")]
        private void TestPlayAnimation()
        {
            if (Application.isPlaying)
            {
                Reset();
                Play();
            }
        }
#endif
    }
}