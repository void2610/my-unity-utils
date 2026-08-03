using VContainer;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// <see cref="IModule"/> の登録ヘルパー
    /// </summary>
    public static class ModuleContainerBuilderExtensions
    {
        /// <summary>モジュールを組み込む。登録順がそのまま VContainer の登録順になる (後勝ち)</summary>
        public static void Install<TModule>(this IContainerBuilder builder) where TModule : IModule, new()
            => new TModule().Install(builder);

        /// <summary>設定値などを渡す必要があるモジュールを組み込む</summary>
        public static void Install<TModule>(this IContainerBuilder builder, TModule module) where TModule : IModule
            => module.Install(builder);
    }
}
