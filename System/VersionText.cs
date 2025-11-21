using TMPro;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// アプリケーションのバージョン情報をTextMeshProに表示するコンポーネント
    /// 起動時に自動的にApplication.versionから取得して表示
    ///
    /// 使用例:
    /// - タイトル画面のバージョン表示
    /// - 設定画面のアプリ情報
    /// - デバッグメニューのバージョン確認
    /// - ビルド識別用の情報表示
    ///
    /// 使い方:
    /// 1. TextMeshProUGUIコンポーネントを持つGameObjectにアタッチ
    /// 2. Inspectorでバージョンサフィックスを設定（Alpha、Beta、など）
    /// 3. 実行時に自動的に「Ver. x.x.x (サフィックス)」形式で表示
    ///
    /// バージョン番号の設定:
    /// - Unity Editor: Project Settings > Player > Version
    /// - 形式: major.minor.patch (例: 1.0.0)
    ///
    /// 注意:
    /// - TextMeshProUGUIコンポーネントが必須です
    /// - Application.versionはProject Settingsで設定したバージョンを返します
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class VersionText : MonoBehaviour
    {
        [Header("バージョン設定")]
        [SerializeField] private string versionSuffix = ""; // 例: "Alpha", "Beta", "Release"
        [SerializeField] private string versionFormat = "Ver. {0}"; // {0}がバージョン番号に置き換わる

        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            UpdateVersionText();
        }

        /// <summary>
        /// バージョンテキストを更新
        /// </summary>
        private void UpdateVersionText()
        {
            var version = Application.version;

            // サフィックスがある場合は追加
            if (!string.IsNullOrEmpty(versionSuffix))
            {
                version = $"{version} ({versionSuffix})";
            }

            // フォーマットに従って表示
            _text.text = string.Format(versionFormat, version);
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタでの値変更時にも反映
        /// </summary>
        private void OnValidate()
        {
            if (_text == null)
            {
                _text = GetComponent<TextMeshProUGUI>();
            }

            if (_text != null && Application.isPlaying)
            {
                UpdateVersionText();
            }
        }
#endif
    }
}
