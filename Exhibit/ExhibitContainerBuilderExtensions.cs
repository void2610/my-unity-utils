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
            var runtimeSettings = RegisterSharedServices<TSceneTransitionService>(builder, settings);

            builder.Register<TExhibitOverlay>(Lifetime.Singleton).As<IExhibitOverlay>();

            if (runtimeSettings.EnableExhibitMode)
            {
                builder.RegisterEntryPoint<ExhibitIdleService>();
            }
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
            var runtimeSettings = RegisterSharedServices<TSceneTransitionService>(builder, settings);

            if (!runtimeSettings.EnableExhibitMode) return;

            var overlay = overlayFactory?.Invoke();
            if (overlay == null)
            {
                throw new InvalidOperationException("Exhibit overlay factory returned null.");
            }

            builder.RegisterInstance(overlay).As<IExhibitOverlay>();
            builder.RegisterEntryPoint<ExhibitIdleService>();
        }

        private static ExhibitSettings RegisterSharedServices<TSceneTransitionService>(
            IContainerBuilder builder,
            ExhibitSettings settings)
            where TSceneTransitionService : class, ISceneTransitionService
        {
            var runtimeSettings = settings ? settings : ScriptableObject.CreateInstance<ExhibitSettings>();

            builder.RegisterInstance(runtimeSettings);
            builder.Register<IdleDetector>(Lifetime.Singleton);
            builder.Register<TSceneTransitionService>(Lifetime.Singleton).As<ISceneTransitionService>();
            builder.RegisterEntryPoint<ExhibitSessionTimerService>();
            return runtimeSettings;
        }
    }
}
