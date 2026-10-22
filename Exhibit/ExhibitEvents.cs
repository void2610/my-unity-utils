using System;
using R3;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 展示モードの放置・時間切れを知らせる。シーン遷移の前に通知するので、遷移で破棄される側 (分析ログ等) も記録できる
    /// </summary>
    public class ExhibitEvents : IDisposable
    {
        /// <summary>
        /// 放置でオーバーレイを出した
        /// </summary>
        public Observable<Unit> OnIdlePaused => _onIdlePaused;

        /// <summary>
        /// オーバーレイを出した後に入力があり、プレイに戻った
        /// </summary>
        public Observable<Unit> OnIdleResumed => _onIdleResumed;

        /// <summary>
        /// 放置が続いたので、放置時の戻り先シーンへ遷移を始める
        /// </summary>
        public Observable<Unit> OnIdleReturning => _onIdleReturning;

        /// <summary>
        /// 制限時間に達したので、制限時間のシーンへ遷移を始める
        /// </summary>
        public Observable<Unit> OnSessionLimitReached => _onSessionLimitReached;

        private readonly Subject<Unit> _onIdlePaused = new();
        private readonly Subject<Unit> _onIdleResumed = new();
        private readonly Subject<Unit> _onIdleReturning = new();
        private readonly Subject<Unit> _onSessionLimitReached = new();

        internal void NotifyIdlePaused() => _onIdlePaused.OnNext(Unit.Default);
        internal void NotifyIdleResumed() => _onIdleResumed.OnNext(Unit.Default);
        internal void NotifyIdleReturning() => _onIdleReturning.OnNext(Unit.Default);
        internal void NotifySessionLimitReached() => _onSessionLimitReached.OnNext(Unit.Default);

        public void Dispose()
        {
            _onIdlePaused.Dispose();
            _onIdleResumed.Dispose();
            _onIdleReturning.Dispose();
            _onSessionLimitReached.Dispose();
        }
    }
}
