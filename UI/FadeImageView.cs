using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LitMotion;
using LitMotion.Extensions;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// シンプルなフェードイン/フェードアウト機能を提供するView
    /// シーン遷移時の画面フェードに使用
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FadeImageView : MonoBehaviour
    {
        private Image _image;
        
        /// <summary>
        /// フェードイン（透明→不透明）
        /// </summary>
        /// <param name="duration">フェード時間</param>
        /// <returns>完了待機可能なUniTask</returns>
        public async UniTask FadeIn(float duration = 0.5f)
        {
            var handle = LMotion.Create(0f, 1f, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToColorA(_image)
                .AddTo(this);
            await handle.ToUniTask();
        }
        
        /// <summary>
        /// フェードアウト（不透明→透明）
        /// </summary>
        /// <param name="duration">フェード時間</param>
        /// <returns>完了待機可能なUniTask</returns>
        public async UniTask FadeOut(float duration = 0.5f)
        {
            var handle = LMotion.Create(1f, 0f, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToColorA(_image)
                .AddTo(this);
            await handle.ToUniTask();
        }
        
        /// <summary>
        /// 即座にフェードイン状態にする（アニメーションなし）
        /// </summary>
        public void SetFadeIn()
        {
            if (_image)
            {
                var color = _image.color;
                color.a = 1f;
                _image.color = color;
            }
        }
        
        /// <summary>
        /// 即座にフェードアウト状態にする（アニメーションなし）
        /// </summary>
        public void SetFadeOut()
        {
            if (_image)
            {
                var color = _image.color;
                color.a = 0f;
                _image.color = color;
            }
        }
        
        /// <summary>
        /// フェード色を設定
        /// </summary>
        /// <param name="color">フェード色（アルファ値は現在の値を保持）</param>
        public void SetFadeColor(Color color)
        {
            if (_image)
            {
                color.a = _image.color.a;
                _image.color = color;
            }
        }

        private void Awake()
        {
            _image = this.GetComponent<Image>();
            if (_image)
            {
                // 初期状態は完全に黒（フェードイン状態）
                _image.color = new Color(0f, 0f, 0f, 1f);
            }
        }
    }
}