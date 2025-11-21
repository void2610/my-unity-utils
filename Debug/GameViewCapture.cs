using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ゲームビューのスクリーンショットを撮影するコンポーネント
    /// エディタ実行中にゲーム画面のスクリーンショットを撮影して保存
    ///
    /// 使用例:
    /// - デバッグ時の画面記録
    /// - バグレポート用のスクリーンショット
    /// - プレイテスト時の画面キャプチャ
    /// - 開発ブログ用の画像素材作成
    ///
    /// 使い方:
    /// 1. 任意のGameObjectにアタッチ
    /// 2. Inspectorで保存先フォルダ名とファイル名プレフィックスを設定
    /// 3. ゲーム実行中にInspectorの「スクリーンショット撮影」ボタンをクリック
    /// 4. プロジェクトルート/Screenshots フォルダに画像が保存される
    /// </summary>
    public class GameViewCapture : MonoBehaviour
    {
        [Header("保存設定")]
        [SerializeField] private string folderName = "Screenshots";
        [SerializeField] private string fileNamePrefix = "Screenshot";

        [Header("撮影設定")]
        [SerializeField] private int superSize = 1;

        private string _screenshotPath;

        /// <summary>
        /// スクリーンショットを撮影して保存
        /// </summary>
        public void CaptureScreenshot()
        {
            // 保存先フォルダのパスを構築
            string folderPath = Path.Combine(Application.dataPath, "..", folderName);

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // ファイル名を生成（タイムスタンプ付き）
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{fileNamePrefix}_{timestamp}.png";
            _screenshotPath = Path.Combine(folderPath, fileName);

            // スクリーンショットを撮影
            ScreenCapture.CaptureScreenshot(_screenshotPath, superSize);

            UnityEngine.Debug.Log($"スクリーンショット保存: {_screenshotPath}");
        }

        /// <summary>
        /// 最後に撮影したスクリーンショットのフォルダを開く
        /// </summary>
        public void OpenScreenshotFolder()
        {
            string folderPath = Path.Combine(Application.dataPath, "..", folderName);
            if (Directory.Exists(folderPath))
            {
                Application.OpenURL($"file://{folderPath}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("スクリーンショットフォルダが存在しません");
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GameViewCapture))]
    public class GameViewCaptureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GameViewCapture capture = (GameViewCapture)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            // 実行中のみボタンを有効化
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("スクリーンショット撮影", GUILayout.Height(30)))
            {
                capture.CaptureScreenshot();
            }

            GUI.enabled = true;

            if (GUILayout.Button("スクリーンショットフォルダを開く"))
            {
                capture.OpenScreenshotFolder();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("スクリーンショット撮影はゲーム実行中のみ可能です", MessageType.Info);
            }
        }
    }
#endif
}
