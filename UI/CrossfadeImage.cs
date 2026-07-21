using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 重ねた 2 枚の Image を交互に使い、スプライト差し替えをクロスフェードで行うコンポーネント。
    /// 前面が imageB のときは B をフェードアウトして背面 A の新絵を見せ、
    /// 前面が imageA のときは B に新絵を載せて上からフェードインする (実行時に sibling 順は変えない)。
    /// </summary>
    public sealed class CrossfadeImage : MonoBehaviour
    {
        [Tooltip("背面の Image (imageB の下に重ねる)")]
        [SerializeField] private Image imageA;

        [Tooltip("前面の Image (imageA の上に重ねる)")]
        [SerializeField] private Image imageB;

        [Tooltip("クロスフェードの所要時間 (秒)")]
        [SerializeField] private float duration = 0.25f;

        /// <summary>現在表示している (遷移中は遷移先の) スプライト</summary>
        public Sprite CurrentSprite { get; private set; }

        private bool _frontIsB;
        private MotionHandle _handle;

        /// <summary>フェードなしで即座に差し替える</summary>
        public void SetImmediate(Sprite sprite)
        {
            _handle.TryComplete();
            CurrentSprite = sprite;
            imageA.sprite = sprite;
            SetAlpha(imageA, 1f);
            SetAlpha(imageB, 0f);
            _frontIsB = false;
        }

        /// <summary>クロスフェードで差し替える。同一スプライトなら no-op</summary>
        public void Crossfade(Sprite sprite)
        {
            if (CurrentSprite == sprite) return;
            _handle.TryComplete();
            CurrentSprite = sprite;
            if (_frontIsB)
            {
                imageA.sprite = sprite;
                SetAlpha(imageA, 1f);
                _handle = imageB.FadeOut(duration, Ease.InOutSine);
                _frontIsB = false;
            }
            else
            {
                imageB.sprite = sprite;
                SetAlpha(imageB, 0f);
                _handle = imageB.FadeIn(duration, Ease.InOutSine);
                _frontIsB = true;
            }
        }

        private static void SetAlpha(Image img, float a)
        {
            var c = img.color;
            c.a = a;
            img.color = c;
        }

        private void Awake()
        {
            // Prefab の初期状態 (A 表示・B 透明) を現在値として引き継ぐ
            CurrentSprite = imageA.sprite;
        }

        private void OnDestroy()
        {
            _handle.TryCancel();
        }
    }
}
