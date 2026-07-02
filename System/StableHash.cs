namespace Void2610.UnityTemplate
{
    /// <summary>
    /// プロセス・環境に依存しない安定した文字列ハッシュ。
    /// string.GetHashCode はランタイム実装やプロセスごとに値が変わりうるため、
    /// シード値のようにマシン・実行を跨いで再現が必要な用途はこちらを使う。
    /// </summary>
    public static class StableHash
    {
        /// <summary>
        /// FNV-1a (32bit) で文字列をハッシュ化する
        /// </summary>
        public static int Fnv1a(string text)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in text)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
                return (int)hash;
            }
        }
    }
}
