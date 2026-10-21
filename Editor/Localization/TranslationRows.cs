using System.Collections.Generic;

namespace Void2610.UnityTemplate.Editor.Localization
{
    /// <summary>
    /// 1 つのテーブルの訳文を、キーとロケールで引ける形で持つ
    /// </summary>
    public sealed class TranslationRows
    {
        public IReadOnlyList<string> Keys => _keys;
        private readonly List<string> _keys = new();
        private readonly Dictionary<string, Dictionary<string, string>> _rows = new();

        public bool HasKey(string key) => _rows.ContainsKey(key);

        public IReadOnlyDictionary<string, string> GetRow(string key) => _rows[key];

        /// <summary>
        /// 記録が無ければ null を返す。前回の同期時点の値のように、記録の無さを空文字と区別したいときに使う
        /// </summary>
        public string Find(string key, string locale) => _rows.TryGetValue(key, out var row) && row.TryGetValue(locale, out var value) ? value : null;

        public void AddKey(string key)
        {
            if (_rows.ContainsKey(key)) return;
            _keys.Add(key);
            _rows.Add(key, new Dictionary<string, string>());
        }

        public void Set(string key, string locale, string value)
        {
            AddKey(key);
            _rows[key][locale] = Normalize(value);
        }

        /// <summary>
        /// キーが無ければ null、キーはあるが訳が無ければ空文字を返す
        /// </summary>
        public string Get(string key, string locale)
        {
            if (!_rows.TryGetValue(key, out var row)) return null;
            return row.TryGetValue(locale, out var value) ? value : "";
        }

        // スプレッドシートで改行を打つと CRLF になることがあり、StringTable の LF と同じ文として扱う
        private static string Normalize(string value)
        {
            return (value ?? "").Replace("\r\n", "\n");
        }
    }
}
