using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// クレジット情報を管理するサービス
    /// TextAssetからクレジットテキストを読み込み、URL自動リンク化機能を提供
    /// </summary>
    public class CreditService
    {
        private readonly TextAsset _textAsset;
        
        public CreditService(TextAsset textAsset)
        {
            _textAsset = textAsset;
        }
        
        /// <summary>
        /// クレジットテキストを取得し、URL自動リンク化を適用
        /// </summary>
        /// <returns>リンク化されたクレジットテキスト</returns>
        public string GetCreditText()
        {
            if (_textAsset == null)
            {
                return "クレジット情報が設定されていません。";
            }
            
            var textData = _textAsset.text;
            return ConvertUrlsToLinks(textData);
        }
        
        /// <summary>
        /// テキスト内のURLを自動的にリンク形式に変換
        /// TextMeshProのリンク機能に対応
        /// </summary>
        /// <param name="text">変換対象のテキスト</param>
        /// <returns>リンク化されたテキスト</returns>
        public string ConvertUrlsToLinks(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            
            return System.Text.RegularExpressions.Regex.Replace(
                text,
                @"(http[s]?:\/\/[^\s]+)",
                "<link=\"$1\"><u>$1</u></link>"
            );
        }
        
        /// <summary>
        /// 生のクレジットテキストを取得（リンク化なし）
        /// </summary>
        /// <returns>生のクレジットテキスト</returns>
        public string GetRawCreditText()
        {
            if (_textAsset == null)
            {
                return "クレジット情報が設定されていません。";
            }
            
            return _textAsset.text;
        }
        
        /// <summary>
        /// TextAssetを動的に設定
        /// </summary>
        /// <param name="newTextAsset">新しいTextAsset</param>
        public void SetTextAsset(TextAsset newTextAsset)
        {
            // Note: このクラスはreadonlyフィールドを使用しているため、
            // 動的設定が必要な場合は新しいインスタンスを作成することを推奨
        }
    }
}