using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LitMotion;
using Coffee.UIEffects;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// シンプルなフェードイン/フェードアウト機能を提供するView
    /// シーン遷移時の画面フェードに使用
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(UIEffect))]
    public class FadeImageView : MonoBehaviour
    {
        private UIEffect _transitionEffect;

        public async UniTask FadeIn(float duration = 0.5f)
        {
            _transitionEffect.transitionRate = 1f;
            var handle = LMotion.Create(1f, 0f, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(v => _transitionEffect.transitionRate = v)
                .AddTo(this);
            await handle.ToUniTask();
        }

        public async UniTask FadeOut(float duration = 0.5f)
        {
            _transitionEffect.transitionRate = 0f;
            var handle = LMotion.Create(0f, 1f, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(v => _transitionEffect.transitionRate = v)
                .AddTo(this);
            await handle.ToUniTask();
        }

        public void SetFadeIn() => _transitionEffect.transitionRate = 1f;

        public void SetFadeOut() => _transitionEffect.transitionRate = 0f;

        private void Awake()
        {
            _transitionEffect = GetComponent<UIEffect>();
        }
    }
}
