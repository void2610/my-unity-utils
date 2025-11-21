using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Unity開発に便利な拡張メソッドのコレクション
    ///
    /// 機能カテゴリ:
    /// - リスト操作: Print, ChooseByWeight
    /// - Enum操作: Toggle
    /// - UI Navigation: SetNavigation, SetGridNavigation
    /// - GameObject操作: SafeDestroy, GetOrAddComponent, SetLayerRecursively
    /// - Transform操作: FindChildRecursive, ResetTransform
    /// - Graphic操作: SetAlpha
    /// - LitMotionアニメーション: FadeIn/Out, MoveTo, ScaleTo, RotateTo, ColorTo, SizeTo
    /// - TextMeshPro拡張: TypewriterAnimation, MoveToX/Y
    /// - バウンドアニメーション: CreateBounceMotion
    /// - R3拡張: WhereIncreased/Decreased, OnIncreased/Decreased (R3がインストールされている場合のみ)
    /// </summary>
    public static class ExtendedMethods
    {


        /// <summary>
        /// リストの全要素をコンソールに出力
        /// </summary>
        public static void Print<T>(this List<T> list)
        {
            if (list == null)
            {
                Debug.Log("リストがnullです");
                return;
            }

            Debug.Log($"リストの内容 ({list.Count}個の要素):");
            for (int i = 0; i < list.Count; i++)
            {
                Debug.Log($"[{i}] {list[i]}");
            }
        }

        /// <summary>
        /// 重み付きリストから確率に基づいてランダムなアイテムを選択
        /// </summary>
        public static T ChooseByWeight<T>(this List<T> list, Func<T, float> weightSelector)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("リストが空またはnullです");

            float totalWeight = 0f;
            foreach (var item in list)
            {
                totalWeight += weightSelector(item);
            }

            if (totalWeight <= 0f)
                throw new ArgumentException("合計重みは0より大きい必要があります");

            var randomValue = UnityEngine.Random.Range(0f, totalWeight);
            var cumulativeWeight = 0f;

            foreach (var item in list)
            {
                cumulativeWeight += weightSelector(item);
                if (randomValue <= cumulativeWeight)
                    return item;
            }

            // フォールバック（浮動小数点精度の問題対策）
            return list[list.Count - 1];
        }





        /// <summary>
        /// enum値を次の値に順番に切り替える
        /// 最後の値の後は最初の値に戻る
        /// </summary>
        public static T Toggle<T>(this T current) where T : System.Enum
        {
            var values = (T[])System.Enum.GetValues(current.GetType());
            var index = System.Array.IndexOf(values, current);
            var nextIndex = (index + 1) % values.Length;
            return values[nextIndex];
        }





        /// <summary>
        /// UI Selectableのリストにナビゲーションを設定
        /// </summary>
        public static void SetNavigation(this List<Selectable> selectables, bool isHorizontal = true, bool wrapAround = false)
        {
            if (selectables == null || selectables.Count == 0)
                return;

            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                if (selectable == null) continue;

                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;

                if (isHorizontal)
                {
                    // 水平ナビゲーション
                    if (i > 0)
                        navigation.selectOnLeft = selectables[i - 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnLeft = selectables[selectables.Count - 1];

                    if (i < selectables.Count - 1)
                        navigation.selectOnRight = selectables[i + 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnRight = selectables[0];
                }
                else
                {
                    // 垂直ナビゲーション
                    if (i > 0)
                        navigation.selectOnUp = selectables[i - 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnUp = selectables[selectables.Count - 1];

                    if (i < selectables.Count - 1)
                        navigation.selectOnDown = selectables[i + 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnDown = selectables[0];
                }

                selectable.navigation = navigation;
            }
        }

        /// <summary>
        /// UI Selectableのリストにグリッド状のナビゲーションを設定
        /// グリッドレイアウトのUIに最適
        /// </summary>
        /// <param name="selectables">対象のSelectableリスト</param>
        /// <param name="columns">グリッドの列数</param>
        /// <param name="leftBoundary">左端列の左側に配置するSelectable（オプション）</param>
        /// <param name="rightBoundary">右端列の右側に配置するSelectable（オプション）</param>
        public static void SetGridNavigation(
            this List<Selectable> selectables,
            int columns,
            Selectable leftBoundary = null,
            Selectable rightBoundary = null)
        {
            if (selectables == null || selectables.Count == 0 || columns <= 0)
                return;

            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                if (selectable == null) continue;

                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;

                // 列と行のインデックスを計算
                var col = i % columns;
                var row = i / columns;

                // 左右のナビゲーション
                if (col > 0) // 左列以外
                {
                    navigation.selectOnLeft = selectables[i - 1];
                }
                else if (leftBoundary != null) // 左端列
                {
                    navigation.selectOnLeft = leftBoundary;
                }

                if (col < columns - 1 && i + 1 < selectables.Count) // 右列以外
                {
                    navigation.selectOnRight = selectables[i + 1];
                }
                else if (col == columns - 1 && rightBoundary != null) // 右端列
                {
                    navigation.selectOnRight = rightBoundary;
                }

                // 上下のナビゲーション
                if (row > 0) // 上に行がある
                {
                    var upIndex = i - columns;
                    if (upIndex >= 0)
                        navigation.selectOnUp = selectables[upIndex];
                }

                if (i + columns < selectables.Count) // 下に行がある
                {
                    navigation.selectOnDown = selectables[i + columns];
                }

                selectable.navigation = navigation;
            }
        }





        /// <summary>
        /// GameObjectを安全に破棄（プレイモードとエディットモード両方に対応）
        /// </summary>
        public static void SafeDestroy(this GameObject gameObject)
        {
            if (gameObject == null) return;

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// GameObjectからコンポーネントを取得または追加
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// GameObjectとその全ての子オブジェクトのレイヤーを設定
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (gameObject == null) return;

            gameObject.layer = layer;

            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }





        /// <summary>
        /// 名前で子オブジェクトを再帰的に検索
        /// </summary>
        public static Transform FindChildRecursive(this Transform parent, string name)
        {
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                var result = FindChildRecursive(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Transformをデフォルト値にリセット
        /// </summary>
        public static void ResetTransform(this Transform transform)
        {
            if (!transform) return;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }





        /// <summary>
        /// Graphic（UI要素）のアルファ値を設定
        /// </summary>
        public static void SetAlpha(this Graphic graphic, float alpha)
        {
            if (graphic == null) return;

            var color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }





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
            catch (OperationCanceledException)
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





#if UNITY_INCLUDE_TESTS || HAS_R3
        /// <summary>
        /// ReactivePropertyの値が増加した時のみ発火するObservableを返す
        /// スコアやHP増加などの検知に便利
        ///
        /// 注意: このメソッドはR3パッケージに依存しています
        /// R3がインストールされていない場合はコンパイルエラーになります
        /// </summary>
        public static R3.Observable<T> WhereIncreased<T>(this R3.ReadOnlyReactiveProperty<T> source) where T : IComparable<T>
        {
            return source
                .Pairwise()
                .Where(pair => pair.Current.CompareTo(pair.Previous) > 0)
                .Select(pair => pair.Current);
        }

        /// <summary>
        /// ReactivePropertyの値が増加した時のみ発火するObservableを返す（Unit型）
        /// 値自体は不要で、増加イベントのみを検知したい場合に使用
        ///
        /// 注意: このメソッドはR3パッケージに依存しています
        /// R3がインストールされていない場合はコンパイルエラーになります
        /// </summary>
        public static R3.Observable<R3.Unit> OnIncreased<T>(this R3.ReadOnlyReactiveProperty<T> source) where T : IComparable<T>
        {
            return source
                .Pairwise()
                .Where(pair => pair.Current.CompareTo(pair.Previous) > 0)
                .AsUnitObservable();
        }

        /// <summary>
        /// ReactivePropertyの値が減少した時のみ発火するObservableを返す
        /// HPやスタミナ減少などの検知に便利
        ///
        /// 注意: このメソッドはR3パッケージに依存しています
        /// R3がインストールされていない場合はコンパイルエラーになります
        /// </summary>
        public static R3.Observable<T> WhereDecreased<T>(this R3.ReadOnlyReactiveProperty<T> source) where T : IComparable<T>
        {
            return source
                .Pairwise()
                .Where(pair => pair.Current.CompareTo(pair.Previous) < 0)
                .Select(pair => pair.Current);
        }

        /// <summary>
        /// ReactivePropertyの値が減少した時のみ発火するObservableを返す（Unit型）
        /// 値自体は不要で、減少イベントのみを検知したい場合に使用
        ///
        /// 注意: このメソッドはR3パッケージに依存しています
        /// R3がインストールされていない場合はコンパイルエラーになります
        /// </summary>
        public static R3.Observable<R3.Unit> OnDecreased<T>(this R3.ReadOnlyReactiveProperty<T> source) where T : IComparable<T>
        {
            return source
                .Pairwise()
                .Where(pair => pair.Current.CompareTo(pair.Previous) < 0)
                .AsUnitObservable();
        }
#endif





#if UNITY_EDITOR
        /// <summary>
        /// 指定したScriptableObjectと同じディレクトリ内の指定した型のアセットを一括登録
        /// ScriptableObjectベースのデータ管理で便利
        /// </summary>
        /// <typeparam name="T">登録するScriptableObjectの型</typeparam>
        /// <param name="targetObject">検索基準となるScriptableObject</param>
        /// <param name="targetList">登録先のリスト</param>
        /// <param name="sortKeySelector">ソート用のキーセレクタ（nullの場合はソートしない）</param>
        public static void RegisterAssetsInSameDirectory<T>(
            this ScriptableObject targetObject,
            List<T> targetList,
            System.Func<T, string> sortKeySelector = null) where T : ScriptableObject
        {
            // このScriptableObjectと同じディレクトリパスを取得
            var path = AssetDatabase.GetAssetPath(targetObject);
            path = System.IO.Path.GetDirectoryName(path);

            // 指定ディレクトリ内の指定した型のアセットを検索
            var typeName = typeof(T).Name;
            var guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { path });

            // 検索結果をリストに追加
            targetList.Clear();
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset) targetList.Add(asset);
            }

            // ソートが指定されている場合はソート実行
            if (sortKeySelector != null)
            {
                targetList.Sort((a, b) => string.Compare(sortKeySelector(a), sortKeySelector(b)));
            }

            // ScriptableObjectを更新
            EditorUtility.SetDirty(targetObject);
        }
#endif


    }
}
