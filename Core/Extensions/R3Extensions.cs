using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Void2610.UnityTemplate
{
#pragma warning disable VUA3001
    /// <summary>
    /// R3（Reactive Extensions）の拡張メソッド
    /// </summary>
    public static class R3Extensions
    {
        /// <summary>
        /// 次の 1 件を待つ。待っている間に source が完了したら (発行元の破棄など) キャンセルとして抜ける。
        /// R3 の FirstAsync は要素の無いまま完了すると InvalidOperationException を投げるため、シーン破棄と競合しうる待ちに使う
        /// </summary>
        public static async UniTask<T> NextOrCancelAsync<T>(this Observable<T> source, CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource<T>();
            using (source.Subscribe(value => tcs.TrySetResult(value), _ => tcs.TrySetCanceled()))
            {
                return await tcs.Task.AttachExternalCancellation(cancellationToken);
            }
        }

        /// <summary>
        /// ReactivePropertyの値が増加した時のみ発火するObservableを返す
        /// スコアやHP増加などの検知に便利
        /// </summary>
        public static Observable<T> WhereIncreased<T>(this ReadOnlyReactiveProperty<T> source) where T : IComparable<T>
        {
            return source
                .Pairwise()
                .Where(pair => pair.Current.CompareTo(pair.Previous) > 0)
                .Select(pair => pair.Current);
        }

        /// <summary>
        /// ReactivePropertyの値が増加した時のみ発火するObservableを返す（Unit型）
        /// 値自体は不要で、増加イベントのみを検知したい場合に使用
        /// </summary>
        public static Observable<Unit> OnIncreased<T>(this ReadOnlyReactiveProperty<T> source) where T : IComparable<T>
        {
            return source
                .Pairwise()
                .Where(pair => pair.Current.CompareTo(pair.Previous) > 0)
                .AsUnitObservable();
        }

        /// <summary>
        /// ReactivePropertyの値が減少した時のみ発火するObservableを返す
        /// HPやスタミナ減少などの検知に便利
        /// </summary>
        public static Observable<T> WhereDecreased<T>(this ReadOnlyReactiveProperty<T> source) where T : IComparable<T>
        {
            return source
                .Pairwise()
                .Where(pair => pair.Current.CompareTo(pair.Previous) < 0)
                .Select(pair => pair.Current);
        }

        /// <summary>
        /// ReactivePropertyの値が減少した時のみ発火するObservableを返す（Unit型）
        /// 値自体は不要で、減少イベントのみを検知したい場合に使用
        /// </summary>
        public static Observable<Unit> OnDecreased<T>(this ReadOnlyReactiveProperty<T> source) where T : IComparable<T>
        {
            return source
                .Pairwise()
                .Where(pair => pair.Current.CompareTo(pair.Previous) < 0)
                .AsUnitObservable();
        }
    }
#pragma warning restore VUA3001
}
