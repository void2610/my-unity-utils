using UnityEditor;
using UnityEngine.UI;

namespace Void2610.UnityTemplate.Editor
{
    [CustomEditor(typeof(AlphaHitTestThreshold))]
    public class AlphaHitTestThresholdEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var image = ((AlphaHitTestThreshold)target).GetComponent<Image>();
            if (image.sprite && !image.sprite.texture.isReadable)
            {
                EditorGUILayout.HelpBox(
                    "テクスチャの Read/Write Enabled が OFF のため alphaHitTest が動作しません。Import Settings で有効にしてください。",
                    MessageType.Warning);
            }
        }
    }
}
