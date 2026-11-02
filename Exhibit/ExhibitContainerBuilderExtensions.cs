using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 展示モード関連サービスのVContainer登録ヘルパー
    /// </summary>
    public static class ExhibitContainerBuilderExtensions
    {
        /// <summary>
        /// 展示モードに必要なサービス群を登録する（Overlayを型で解決する版）
        /// </summary>
        public static void RegisterExhibitServices<TSceneTransitionService, TExhibitOverlay>(
            this IContainerBuilder builder,
            ExhibitSettings settings)
            where TSceneTransitionService : class, ISceneTransitionService
            where TExhibitOverlay : class, IExhibitOverlay
        {
            RegisterSharedServices<TSceneTransitionService>(builder, settings);

            builder.Register<TExhibitOverlay>(Lifetime.Singleton);
            builder.Register<IExhibitOverlay>(resolver => new LazyExhibitOverlay(() => resolver.Resolve<TExhibitOverlay>()), Lifetime.Singleton);
            builder.RegisterEntryPoint<ExhibitIdleService>();
        }

        /// <summary>
        /// 展示モードに必要なサービス群を登録する（Overlayを外部生成する版）
        /// </summary>
        public static void RegisterExhibitServices<TSceneTransitionService>(
            this IContainerBuilder builder,
            ExhibitSettings settings,
            Func<IExhibitOverlay> overlayFactory)
            where TSceneTransitionService : class, ISceneTransitionService
        {
            RegisterSharedServices<TSceneTransitionService>(builder, settings);

            builder.RegisterInstance<IExhibitOverlay>(new LazyExhibitOverlay(overlayFactory));
            builder.RegisterEntryPoint<ExhibitIdleService>();
        }

        // 展示モードは実行中に切り替えられる (ExhibitSettings.OverrideExhibitMode) ので、無効でもサービスは登録しておく。
        // 各サービスは毎フレーム設定を見て、無効の間は何もしない。オーバーレイは初めて出すときに作る
        private static void RegisterSharedServices<TSceneTransitionService>(
            IContainerBuilder builder,
            ExhibitSettings settings)
            where TSceneTransitionService : class, ISceneTransitionService
        {
            var runtimeSettings = settings ? settings : ScriptableObject.CreateInstance<ExhibitSettings>();

            builder.RegisterInstance(runtimeSettings);
            builder.Register<IdleDetector>(Lifetime.Singleton);
            builder.Register<ExhibitEvents>(Lifetime.Singleton);
            builder.Register<TSceneTransitionService>(Lifetime.Singleton).As<ISceneTransitionService>();
            builder.RegisterEntryPoint<ExhibitSessionTimerService>();
        }
    }
}
