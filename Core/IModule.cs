using VContainer;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// シーンの DI へまとめて組み込める機能単位。
    /// LifetimeScope 側は <c>builder.Install&lt;TModule&gt;()</c> で積む
    /// </summary>
    public interface IModule
    {
        void Install(IContainerBuilder builder);
    }
}
