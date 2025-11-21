#if UNITY_INCLUDE_TESTS || HAS_R3
using UnityEngine.InputSystem;
using R3;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// InputActionの拡張メソッド
    /// InputActionをR3のObservableに変換する機能を提供
    /// Unity Input SystemとR3の統合を簡単にする
    ///
    /// 使用例:
    /// - リアクティブプログラミングパターンでの入力処理
    /// - 複雑な入力シーケンスの実装
    /// - 入力イベントのフィルタリングや変換
    ///
    /// 使い方:
    /// // InputActionMapから取得
    /// var jumpAction = playerInput.actions["Jump"];
    ///
    /// // R3のObservableに変換して購読
    /// jumpAction.OnPerformedAsObservable()
    ///     .Subscribe(_ => Debug.Log("ジャンプ!"))
    ///     .AddTo(this);
    ///
    /// // 複数の入力を組み合わせる
    /// Observable.Merge(
    ///     jumpAction.OnPerformedAsObservable(),
    ///     altJumpAction.OnPerformedAsObservable()
    /// )
    /// .Subscribe(_ => Jump())
    /// .AddTo(this);
    ///
    /// 注意:
    /// - R3パッケージがインストールされている必要があります
    /// - Unity Input Systemパッケージがインストールされている必要があります
    /// - HAS_R3シンボルを定義するか、テストモードで使用してください
    /// </summary>
    public static class InputActionExtensions
    {
        /// <summary>
        /// InputAction.performedイベントをR3のObservableに変換
        /// アクションが完了した時（ボタンを押した/離した時など）に発火
        /// </summary>
        /// <param name="action">変換するInputAction</param>
        /// <returns>performedイベントのObservable</returns>
        public static Observable<Unit> OnPerformedAsObservable(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                h => action.performed += h,
                h => action.performed -= h
            ).Select(_ => Unit.Default);
        }

        /// <summary>
        /// InputAction.startedイベントをR3のObservableに変換
        /// アクションが開始された時（ボタンを押し始めた時など）に発火
        /// </summary>
        /// <param name="action">変換するInputAction</param>
        /// <returns>startedイベントのObservable</returns>
        public static Observable<Unit> OnStartedAsObservable(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                h => action.started += h,
                h => action.started -= h
            ).Select(_ => Unit.Default);
        }

        /// <summary>
        /// InputAction.canceledイベントをR3のObservableに変換
        /// アクションがキャンセルされた時（ボタンを離した時など）に発火
        /// </summary>
        /// <param name="action">変換するInputAction</param>
        /// <returns>canceledイベントのObservable</returns>
        public static Observable<Unit> OnCanceledAsObservable(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                h => action.canceled += h,
                h => action.canceled -= h
            ).Select(_ => Unit.Default);
        }

        /// <summary>
        /// InputAction.performedイベントをコンテキスト付きObservableに変換
        /// InputAction.CallbackContextを取得したい場合に使用
        /// </summary>
        /// <param name="action">変換するInputAction</param>
        /// <returns>performedイベントのObservable（コンテキスト付き）</returns>
        public static Observable<InputAction.CallbackContext> OnPerformedAsObservableWithContext(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                h => action.performed += h,
                h => action.performed -= h
            );
        }

        /// <summary>
        /// InputAction.startedイベントをコンテキスト付きObservableに変換
        /// InputAction.CallbackContextを取得したい場合に使用
        /// </summary>
        /// <param name="action">変換するInputAction</param>
        /// <returns>startedイベントのObservable（コンテキスト付き）</returns>
        public static Observable<InputAction.CallbackContext> OnStartedAsObservableWithContext(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                h => action.started += h,
                h => action.started -= h
            );
        }

        /// <summary>
        /// InputAction.canceledイベントをコンテキスト付きObservableに変換
        /// InputAction.CallbackContextを取得したい場合に使用
        /// </summary>
        /// <param name="action">変換するInputAction</param>
        /// <returns>canceledイベントのObservable（コンテキスト付き）</returns>
        public static Observable<InputAction.CallbackContext> OnCanceledAsObservableWithContext(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                h => action.canceled += h,
                h => action.canceled -= h
            );
        }
    }
}
#endif
