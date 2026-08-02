using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Addressables のプレハブをシーン開始時に読み込み、Canvas 配下へ生成して DI から使えるようにする。
    /// 生成物はスコープと同じ寿命で、破棄時にインスタンスとハンドルを解放する
    /// </summary>
    public sealed class AsyncPrefabHost<T> : IAsyncStartable, IDisposable where T : Component
    {
        /// <summary>生成完了を待つ。失敗・中断時も完了するため、待機側が取り残されることはない</summary>
        public UniTask ReadyAsync => _ready.Task;

        /// <summary>生成が終わっているか</summary>
        public bool IsReady => _instance;

        public T Instance => _instance;

        private readonly string _address;
        private readonly Func<Canvas> _canvasSelector;
        private readonly Action<T> _onInstantiated;
        private readonly UniTaskCompletionSource _ready = new();

        private T _instance;
#if ADDRESSABLES
        private AsyncOperationHandle<GameObject> _handle;
#endif

        public AsyncPrefabHost(string address, Func<Canvas> canvasSelector, Action<T> onInstantiated = null)
        {
            _address = address;
            _canvasSelector = canvasSelector;
            _onInstantiated = onInstantiated;
        }

        public async UniTask StartAsync(CancellationToken ct)
        {
            try
            {
#if ADDRESSABLES
                _handle = Addressables.LoadAssetAsync<GameObject>(_address);
                var prefab = await _handle.ToUniTask(cancellationToken: ct);
                _instance = Object.Instantiate(prefab, _canvasSelector().transform, false).GetComponent<T>();
                _instance.transform.SetAsLastSibling();
                _onInstantiated?.Invoke(_instance);
                _ready.TrySetResult();
#else
                await UniTask.CompletedTask;
                throw new NotSupportedException($"{nameof(AsyncPrefabHost<T>)} には Addressables が必要です");
#endif
            }
            catch (OperationCanceledException)
            {
                // 生成待ちが永久に解けないのを避けるため、失敗も待機側へ伝える
                _ready.TrySetCanceled();
                throw;
            }
            catch (Exception e)
            {
                _ready.TrySetException(e);
                throw;
            }
        }

        public void Dispose()
        {
            _ready.TrySetCanceled();
            if (_instance) Object.Destroy(_instance.gameObject);
#if ADDRESSABLES
            if (_handle.IsValid()) Addressables.Release(_handle);
#endif
        }
    }
}
