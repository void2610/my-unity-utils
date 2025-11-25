using System;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Enum操作の拡張メソッド
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// enum値を次の値に順番に切り替える
        /// 最後の値の後は最初の値に戻る
        /// </summary>
        public static T Toggle<T>(this T current) where T : Enum
        {
            var values = (T[])Enum.GetValues(current.GetType());
            var index = Array.IndexOf(values, current);
            var nextIndex = (index + 1) % values.Length;
            return values[nextIndex];
        }
    }
}
