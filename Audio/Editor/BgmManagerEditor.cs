using UnityEditor;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// BgmManagerのカスタムエディタ
    /// 再生位置をプログレスバーで表示、クリックでシーク
    /// </summary>
    [CustomEditor(typeof(BgmManager))]
    public class BgmManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            var bgmManager = (BgmManager)target;
            var audioSource = bgmManager.GetComponent<AudioSource>();
            if (audioSource == null || audioSource.clip == null) return;

            var currentTime = audioSource.time;
            var totalLength = audioSource.clip.length;

            EditorGUILayout.Space(4);

            // プログレスバー描画
            var progressRect = EditorGUILayout.GetControlRect(false, 20f);
            var progress = totalLength > 0f ? currentTime / totalLength : 0f;

            EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.2f, 0.2f));
            EditorGUI.DrawRect(new Rect(progressRect.x, progressRect.y, progressRect.width * progress, progressRect.height), new Color(0.3f, 0.7f, 1.0f));
            EditorGUI.LabelField(progressRect, $"  {currentTime:F2}s / {totalLength:F2}s", EditorStyles.whiteLabel);

            // クリックでシーク
            var e = Event.current;
            if (e.type == EventType.MouseDown && progressRect.Contains(e.mousePosition))
            {
                audioSource.time = (e.mousePosition.x - progressRect.x) / progressRect.width * totalLength;
                e.Use();
            }

            Repaint();
        }
    }
}
