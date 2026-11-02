using System;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 初めて表示するときに本物のオーバーレイを作る。展示モードを有効にしないまま終わるゲームでは作らない
    /// </summary>
    internal sealed class LazyExhibitOverlay : IExhibitOverlay
    {
        private readonly Func<IExhibitOverlay> _factory;
        private IExhibitOverlay _overlay;

        public bool IsVisible => _overlay is { IsVisible: true };

        public LazyExhibitOverlay(Func<IExhibitOverlay> factory)
        {
            _factory = factory;
        }

        public void Show() => GetOrCreate().Show();

        // まだ作っていなければ、出してもいないので閉じるものも無い
        public void Hide() => _overlay?.Hide();

        private IExhibitOverlay GetOrCreate()
        {
            _overlay ??= _factory?.Invoke() ?? throw new InvalidOperationException("Exhibit overlay factory returned null.");
            return _overlay;
        }
    }
}
