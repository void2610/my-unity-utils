using System;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// デバイス上でデバッグログを画面に表示するコンポーネント
    /// ビルド後の端末でUnityコンソールにアクセスできない時に便利
    /// </summary>
    public class DebugLogDisplay : MonoBehaviour
    {
        [Header("表示設定")]
        [SerializeField] private int maxLogLines = 50; // 表示するログの最大行数
        [SerializeField] private int fontSize = 15; // フォントサイズ
        [SerializeField] private Color textColor = Color.white; // テキスト色
        [SerializeField] private Vector2 displayPosition = new Vector2(10, 50); // 表示位置
        [SerializeField] private float autoDeleteInterval = 5f; // 自動削除間隔（秒）
        [SerializeField] private bool showWarnings = true; // 警告を表示するか
        [SerializeField] private bool showErrors = true; // エラーを表示するか
        [SerializeField] private bool showLogs = true; // 通常ログを表示するか

        private string _logText = "";
        private GUIStyle _guiStyle;
        private float _lastDeleteTime;

        private void Awake()
        {
            // GUIスタイルを初期化
            _guiStyle = new GUIStyle
            {
                fontSize = fontSize,
                normal = { textColor = textColor }
            };
            
            _lastDeleteTime = Time.time;
        }

        private void Update()
        {
            // 一定間隔で古いログを削除
            if (Time.time - _lastDeleteTime >= autoDeleteInterval)
            {
                DeleteFirstLine();
                _lastDeleteTime = Time.time;
            }
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(_logText))
            {
                GUI.Label(new Rect(displayPosition.x, displayPosition.y, Screen.width - displayPosition.x, Screen.height - displayPosition.y), 
                         _logText, _guiStyle);
            }
        }

        private void OnEnable()
        {
            // デバッグログを表示するためのイベントハンドラを登録
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            // イベントハンドラを解除
            Application.logMessageReceived -= HandleLog;
        }
        
        /// <summary>
        /// ログメッセージを処理
        /// </summary>
        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // ログタイプに応じてフィルタリング
            string formattedLog = type switch
            {
                LogType.Error or LogType.Exception when showErrors => $"<color=red>[Error] {logString}</color>\n",
                LogType.Warning when showWarnings => $"<color=yellow>[Warning] {logString}</color>\n",
                LogType.Log when showLogs => $"{logString}\n",
                _ => null
            };

            if (string.IsNullOrEmpty(formattedLog)) return;

            _logText += formattedLog;

            // 表示するログの行数が上限を超えたら古いログを削除
            LimitLogLines();
        }

        /// <summary>
        /// ログの行数を制限
        /// </summary>
        private void LimitLogLines()
        {
            var logLines = _logText.Split('\n');
            if (logLines.Length > maxLogLines)
            {
                var linesToKeep = maxLogLines - 1; // 改行を考慮
                _logText = string.Join("\n", logLines, logLines.Length - linesToKeep, linesToKeep);
            }
        }

        /// <summary>
        /// 最初の行を削除
        /// </summary>
        private void DeleteFirstLine()
        {
            if (string.IsNullOrEmpty(_logText)) return;

            var firstNewlineIndex = _logText.IndexOf('\n');
            if (firstNewlineIndex > 0 && firstNewlineIndex < _logText.Length - 1)
            {
                _logText = _logText.Substring(firstNewlineIndex + 1);
            }
        }

        /// <summary>
        /// ログをクリア
        /// </summary>
        public void ClearLogs()
        {
            _logText = "";
        }

        /// <summary>
        /// 表示設定を更新
        /// </summary>
        public void UpdateDisplaySettings(int newFontSize, Color newTextColor)
        {
            fontSize = newFontSize;
            textColor = newTextColor;
            _guiStyle.fontSize = fontSize;
            _guiStyle.normal.textColor = textColor;
        }
    }
}