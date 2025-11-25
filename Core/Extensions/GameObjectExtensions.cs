using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// GameObject操作の拡張メソッド
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// GameObjectを安全に破棄（プレイモードとエディットモード両方に対応）
        /// </summary>
        public static void SafeDestroy(this GameObject gameObject)
        {
            if (gameObject == null) return;

            if (Application.isPlaying)
            {
                Object.Destroy(gameObject);
            }
            else
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// GameObjectからコンポーネントを取得または追加
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// GameObjectとその全ての子オブジェクトのレイヤーを設定
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (gameObject == null) return;

            gameObject.layer = layer;

            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
