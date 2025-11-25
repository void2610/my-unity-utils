using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Transform操作の拡張メソッド
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// 名前で子オブジェクトを再帰的に検索
        /// </summary>
        public static Transform FindChildRecursive(this Transform parent, string name)
        {
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                var result = FindChildRecursive(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Transformをデフォルト値にリセット
        /// </summary>
        public static void ResetTransform(this Transform transform)
        {
            if (!transform) return;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
