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
        /// 文字列を UTF-8 バイト列として FNV-1a (32bit) でハッシュ化する
        /// </summary>
        public static int Fnv1a(string text)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var b in System.Text.Encoding.UTF8.GetBytes(text))
                {
                    hash ^= b;
                    hash *= 16777619u;
                }
                return (int)hash;
            }
        }
    }
}
