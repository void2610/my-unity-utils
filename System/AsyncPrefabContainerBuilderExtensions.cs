using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Addressables から非同期生成するプレハブのVContainer登録ヘルパー
    /// </summary>
    public static class AsyncPrefabContainerBuilderExtensions
    {
        /// <summary>
        /// プレハブをシーン開始時に生成するホストを登録する。生成物は <see cref="AsyncPrefabRegistration{T}.Expose{TComponent}"/> で公開する
        /// </summary>
        public static AsyncPrefabRegistration<T> RegisterAsyncPrefab<T>(
            this IContainerBuilder builder,
            string address,
            Canvas canvas,
            Action<T> onInstantiated = null) where T : Component
            => builder.RegisterAsyncPrefab(address, () => canvas, onInstantiated);

        /// <summary>
        /// 生成先の Canvas を生成時に決めたい場合に使う
        /// </summary>
        public static AsyncPrefabRegistration<T> RegisterAsyncPrefab<T>(
            this IContainerBuilder builder,
            string address,
            Func<Canvas> canvasSelector,
            Action<T> onInstantiated = null) where T : Component
        {
            builder.Register(_ => new AsyncPrefabHost<T>(address, canvasSelector, onInstantiated), Lifetime.Singleton)
                .AsSelf().As<IAsyncStartable>();
            return new AsyncPrefabRegistration<T>(builder);
        }
    }

    /// <summary>
    /// 生成されたプレハブ内の部品を DI へ公開する。実体はホストの生成後に解決される
    /// </summary>
    public readonly struct AsyncPrefabRegistration<T> where T : Component
    {
        private readonly IContainerBuilder _builder;

        public AsyncPrefabRegistration(IContainerBuilder builder)
        {
            _builder = builder;
        }

        public AsyncPrefabRegistration<T> Expose<TComponent>(
            Func<T, TComponent> selector, Action<RegistrationBuilder> configure = null) where TComponent : class
        {
            var registration = _builder.Register(c => selector(c.Resolve<AsyncPrefabHost<T>>().Instance), Lifetime.Singleton);
            configure?.Invoke(registration);
            return this;
        }
    }
}
