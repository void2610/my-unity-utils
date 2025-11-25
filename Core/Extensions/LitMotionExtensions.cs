using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// LitMotionアニメーションの拡張メソッド
    /// </summary>
    public static class LitMotionExtensions
    {
        /// <summary>
        /// Imageの透明度をLitMotionでフェードインさせる
        /// </summary>
        public static MotionHandle FadeIn(this Image image, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(image.color.a, 1f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColorA(image)
                .AddTo(image.gameObject);
        }

        /// <summary>
        /// Imageの透明度をLitMotionでフェードアウトさせる
        /// </summary>
        public static MotionHandle FadeOut(this Image image, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(image.color.a, 0f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColorA(image)
                .AddTo(image.gameObject);
        }

        /// <summary>
        /// 2つのImageの透明度をLitMotionでクロスフェード
        /// シーン遷移や画像切り替えに便利
        /// </summary>
        public static (MotionHandle fadeOutHandle, MotionHandle fadeInHandle) CrossFade(
            this Image fromImage,
            Image toImage,
            float duration,
            Ease ease = Ease.Linear,
            bool ignoreTimeScale = false)
        {
            var fadeOutHandle = LMotion.Create(fromImage.color.a, 0f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColorA(fromImage)
                .AddTo(fromImage.gameObject);
            var fadeInHandle = LMotion.Create(toImage.color.a, 1f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColorA(toImage)
                .AddTo(toImage.gameObject);
            return (fadeOutHandle, fadeInHandle);
        }

        /// <summary>
        /// Imageの色をLitMotionで変更する
        /// </summary>
        public static MotionHandle ColorTo(this Image image, Color targetColor, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(image.color, targetColor, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColor(image)
                .AddTo(image.gameObject);
        }

        /// <summary>
        /// CanvasGroupの透明度をLitMotionでフェードインさせる
        /// </summary>
        public static MotionHandle FadeIn(this CanvasGroup canvasGroup, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(0f, 1f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToAlpha(canvasGroup)
                .AddTo(canvasGroup.gameObject);
        }

        /// <summary>
        /// CanvasGroupの透明度をLitMotionでフェードアウトさせる
        /// </summary>
        public static MotionHandle FadeOut(this CanvasGroup canvasGroup, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(1f, 0f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToAlpha(canvasGroup)
                .AddTo(canvasGroup.gameObject);
        }

        /// <summary>
        /// TextMeshProUGUIの色をLitMotionで変更
        /// </summary>
        public static MotionHandle ColorTo(this TextMeshProUGUI text, Color targetColor, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(text.color, targetColor, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColor(text)
                .AddTo(text.gameObject);
        }

        /// <summary>
        /// TextMeshProUGUIの透明度をLitMotionでフェードイン
        /// </summary>
        public static MotionHandle FadeIn(this TextMeshProUGUI text, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(0f, 1f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColorA(text)
                .AddTo(text.gameObject);
        }

        /// <summary>
        /// TextMeshProUGUIの透明度をLitMotionでフェードアウト
        /// </summary>
        public static MotionHandle FadeOut(this TextMeshProUGUI text, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(1f, 0f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColorA(text)
                .AddTo(text.gameObject);
        }

        /// <summary>
        /// TextMeshProUGUIにタイプライター効果を適用（1文字ずつ表示）
        /// ノベルゲームやチュートリアル表示に最適
        /// </summary>
        /// <param name="text">対象のTextMeshProUGUI</param>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="charSpeed">1文字あたりの表示時間（秒）</param>
        /// <param name="skipOnClick">クリックでスキップ可能にするか</param>
        /// <param name="cancellationToken">キャンセル用トークン</param>
        public static async UniTask TypewriterAnimation(
            this TextMeshProUGUI text,
            string message,
            float charSpeed = 0.05f,
            bool skipOnClick = true,
            CancellationToken cancellationToken = default
            )
        {
            if (!text || string.IsNullOrEmpty(message)) return;

            // 初期状態でテキストをクリア
            text.text = "";

            // LitMotionで文字数を増やすアニメーション
            var totalDuration = charSpeed * message.Length;
            var currentLength = 0;

            var motion = LMotion.Create(0f, message.Length, totalDuration)
                .WithEase(Ease.Linear)
                .Bind(value =>
                {
                    var newLength = Mathf.FloorToInt(value);
                    if (newLength != currentLength && text)
                    {
                        currentLength = newLength;
                        text.text = message.Substring(0, currentLength);
                    }
                })
                .AddTo(text.gameObject);

            try
            {
                if (skipOnClick)
                {
                    // クリック検知タスクとアニメーションタスクを並行実行
                    var animationTask = motion.ToUniTask(cancellationToken);
                    var clickTask = UniTask.WaitUntil(() => Input.GetMouseButtonUp(0), cancellationToken: cancellationToken);

                    // どちらかが完了したら処理を進める
                    var winIndex = await UniTask.WhenAny(animationTask, clickTask);

                    // クリックされた場合はアニメーションをキャンセル
                    if (winIndex == 1) // clickTaskが完了
                    {
                        motion.TryCancel();
                    }
                }
                else
                {
                    // 通常のアニメーション実行
                    await motion.ToUniTask(cancellationToken);
                }
            }
            catch (System.OperationCanceledException)
            {
                // キャンセル時は完全なテキストを表示
                if (text) text.text = message;
                throw;
            }

            // 最終的に完全なテキストを表示（念のため）
            if (text) text.text = message;
        }

        /// <summary>
        /// TransformをLitMotionで指定位置に移動
        /// </summary>
        public static MotionHandle MoveTo(this Transform transform, Vector3 targetPosition, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(transform.localPosition, targetPosition, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToLocalPosition(transform)
                .AddTo(transform.gameObject);
        }

        /// <summary>
        /// TransformをLitMotionで指定スケールに変更
        /// </summary>
        public static MotionHandle ScaleTo(this Transform transform, Vector3 targetScale, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(transform.localScale, targetScale, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToLocalScale(transform)
                .AddTo(transform.gameObject);
        }

        /// <summary>
        /// TransformをLitMotionで指定回転に変更
        /// </summary>
        public static MotionHandle RotateTo(this Transform transform, Quaternion targetRotation, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(transform.rotation, targetRotation, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToRotation(transform)
                .AddTo(transform.gameObject);
        }

        /// <summary>
        /// バウンドアニメーション（Transform版）
        /// オブジェクトが跳ねるような動きを実現
        ///
        /// 注意: このメソッドはUtils.EaseOutBounce関数に依存しています
        /// Utils.csがプロジェクトに含まれていることを確認してください
        /// </summary>
        /// <param name="trans">対象のTransform</param>
        /// <param name="targetPos">目標位置</param>
        /// <param name="duration">アニメーション時間</param>
        /// <returns>アニメーションハンドル</returns>
        public static MotionHandle CreateBounceMotion(this Transform trans, Vector3 targetPos, float duration)
        {
            var startPos = trans.position;
            return LMotion.Create(0f, 1f, duration)
                .WithEase(Ease.Linear)
                .WithOnComplete(() => { })
                .Bind(t =>
                {
                    var bounceValue = Utils.EaseOutBounce(t);
                    var y = Mathf.Lerp(startPos.y, targetPos.y, bounceValue);
                    var x = Mathf.Lerp(startPos.x, targetPos.x, t);
                    var z = Mathf.Lerp(startPos.z, targetPos.z, t);
                    trans.position = new Vector3(x, y, z);
                })
                .AddTo(trans.gameObject);
        }

        /// <summary>
        /// RectTransformをLitMotionで指定位置に移動（anchoredPosition）
        /// </summary>
        public static MotionHandle MoveToAnchored(this RectTransform rectTransform, Vector2 targetPosition, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(rectTransform.anchoredPosition, targetPosition, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToAnchoredPosition(rectTransform)
                .AddTo(rectTransform.gameObject);
        }

        /// <summary>
        /// RectTransformのX座標のみをLitMotionで移動
        /// </summary>
        public static MotionHandle MoveToX(this RectTransform rectTransform, float targetX, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(rectTransform.anchoredPosition.x, targetX, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .Bind(x => rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y))
                .AddTo(rectTransform.gameObject);
        }

        /// <summary>
        /// RectTransformのY座標のみをLitMotionで移動
        /// </summary>
        public static MotionHandle MoveToY(this RectTransform rectTransform, float targetY, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(rectTransform.anchoredPosition.y, targetY, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .Bind(y => rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y))
                .AddTo(rectTransform.gameObject);
        }

        /// <summary>
        /// RectTransformをLitMotionで指定サイズに変更（sizeDelta）
        /// </summary>
        public static MotionHandle SizeTo(this RectTransform rectTransform, Vector2 targetSize, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(rectTransform.sizeDelta, targetSize, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToSizeDelta(rectTransform)
                .AddTo(rectTransform.gameObject);
        }

        /// <summary>
        /// バウンドアニメーション（RectTransform版・anchoredPosition用）
        /// UI要素が跳ねるような動きを実現
        ///
        /// 注意: このメソッドはUtils.EaseOutBounce関数に依存しています
        /// Utils.csがプロジェクトに含まれていることを確認してください
        /// </summary>
        /// <param name="rectTrans">対象のRectTransform</param>
        /// <param name="targetPos">目標位置</param>
        /// <param name="duration">アニメーション時間</param>
        /// <returns>アニメーションハンドル</returns>
        public static MotionHandle CreateBounceMotion(this RectTransform rectTrans, Vector2 targetPos, float duration)
        {
            var startPos = rectTrans.anchoredPosition;
            return LMotion.Create(0f, 1f, duration)
                .WithEase(Ease.Linear)
                .WithOnComplete(() => { })
                .Bind(t =>
                {
                    var bounceValue = Utils.EaseOutBounce(t);
                    var y = Mathf.Lerp(startPos.y, targetPos.y, bounceValue);
                    var x = Mathf.Lerp(startPos.x, targetPos.x, t);
                    rectTrans.anchoredPosition = new Vector2(x, y);
                })
                .AddTo(rectTrans.gameObject);
        }
    }
}
