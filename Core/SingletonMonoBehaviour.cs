using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 汎用シングルトンMonoBehaviour実装
    /// インスタンスが存在しない場合は自動作成し、重複を防ぐ
    /// </summary>
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
        private static T _instance;
        
        public static T Instance
        {
            get
            {
                // 既存のインスタンスがあれば返す
                if (_instance != null)
                    return _instance;

                // シーン内の既存インスタンスを検索
                _instance = FindFirstObjectByType<T>();
                if (_instance != null)
                    return _instance;

                // インスタンスが存在しない場合は自動作成
                var singletonObject = new GameObject(typeof(T).Name);
                _instance = singletonObject.AddComponent<T>();
                
                return _instance;
            }
        }

        /// <summary>
        /// インスタンスが存在するかどうかを返す（作成はしない）
        /// </summary>
        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            // 初回のインスタンスならシングルトンとして登録
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            // 既に別のインスタンスが存在する場合は自分を破棄
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            // このオブジェクトが破棄される際にインスタンス参照をクリア
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}