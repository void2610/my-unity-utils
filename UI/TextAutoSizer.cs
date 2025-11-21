using UnityEngine;
using TMPro;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// TextMeshProの内容に応じてRectTransformのサイズを自動調整するユーティリティ
    /// 動的なテキスト表示（ライセンス、クレジット、ダイアログ等）で使用
    /// </summary>
    public static class TextAutoSizer
    {
        /// <summary>
        /// TextMeshProの推奨サイズに合わせてコンテンツサイズを更新
        /// 親要素のサイズも同時に更新
        /// </summary>
        /// <param name="text">対象のTextMeshProUGUI</param>
        public static void UpdateContentSize(TextMeshProUGUI text)
        {
            if (text == null) return;
            
            UpdateContentSize(text, true);
        }
        
        /// <summary>
        /// TextMeshProの推奨サイズに合わせてコンテンツサイズを更新
        /// </summary>
        /// <param name="text">対象のTextMeshProUGUI</param>
        /// <param name="updateParent">親要素のサイズも更新するか</param>
        public static void UpdateContentSize(TextMeshProUGUI text, bool updateParent)
        {
            if (text == null) return;
            
            var preferredHeight = text.GetPreferredValues().y;
            var content = text.GetComponent<RectTransform>();
            
            if (content == null) return;
            
            // テキスト要素のサイズを更新
            content.sizeDelta = new Vector2(content.sizeDelta.x, preferredHeight);
            
            // 親要素のサイズも更新
            if (updateParent && content.parent != null)
            {
                var parentRect = content.parent.GetComponent<RectTransform>();
                if (parentRect != null)
                {
                    parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, preferredHeight);
                }
            }
        }
        
        /// <summary>
        /// 指定したRectTransformをTextMeshProの内容にフィット
        /// </summary>
        /// <param name="rectTransform">調整対象のRectTransform</param>
        /// <param name="text">基準となるTextMeshProUGUI</param>
        public static void FitToContent(RectTransform rectTransform, TextMeshProUGUI text)
        {
            if (rectTransform == null || text == null) return;
            
            var preferredSize = text.GetPreferredValues();
            rectTransform.sizeDelta = preferredSize;
        }
        
        /// <summary>
        /// TextMeshProの幅を固定して高さを自動調整
        /// </summary>
        /// <param name="text">対象のTextMeshProUGUI</param>
        /// <param name="width">固定する幅</param>
        public static void UpdateHeightToFitWidth(TextMeshProUGUI text, float width)
        {
            if (text == null) return;
            
            var rectTransform = text.GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
            // 幅を設定
            rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
            
            // テキスト更新を強制
            text.ForceMeshUpdate();
            
            // 推奨高さを取得して設定
            var preferredHeight = text.GetPreferredValues(width, 0).y;
            rectTransform.sizeDelta = new Vector2(width, preferredHeight);
        }
        
        /// <summary>
        /// ScrollViewのContent領域をテキストサイズに合わせて調整
        /// </summary>
        /// <param name="scrollViewContent">ScrollViewのContent RectTransform</param>
        /// <param name="text">基準となるTextMeshProUGUI</param>
        /// <param name="padding">追加の余白</param>
        public static void UpdateScrollViewContent(RectTransform scrollViewContent, TextMeshProUGUI text, float padding = 0f)
        {
            if (scrollViewContent == null || text == null) return;
            
            var preferredHeight = text.GetPreferredValues().y + padding;
            scrollViewContent.sizeDelta = new Vector2(scrollViewContent.sizeDelta.x, preferredHeight);
        }
        
        /// <summary>
        /// 複数のTextMeshProを基準に最大サイズを適用
        /// </summary>
        /// <param name="targetRect">調整対象のRectTransform</param>
        /// <param name="texts">基準となるTextMeshProの配列</param>
        public static void FitToMaxContent(RectTransform targetRect, params TextMeshProUGUI[] texts)
        {
            if (targetRect == null || texts == null || texts.Length == 0) return;
            
            float maxWidth = 0f;
            float maxHeight = 0f;
            
            foreach (var text in texts)
            {
                if (text == null) continue;
                
                var preferredSize = text.GetPreferredValues();
                maxWidth = Mathf.Max(maxWidth, preferredSize.x);
                maxHeight = Mathf.Max(maxHeight, preferredSize.y);
            }
            
            targetRect.sizeDelta = new Vector2(maxWidth, maxHeight);
        }
    }
}