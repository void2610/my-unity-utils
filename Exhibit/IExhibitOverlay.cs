namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 展示モードオーバーレイの抽象インターフェース
    /// </summary>
    public interface IExhibitOverlay
    {
        bool IsVisible { get; }
        void Show();
        void Hide();
    }
}
