using System;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ツイート機能を提供するシンプルなサービスクラス
    /// 基本的なツイート画面を開く機能を提供
    /// </summary>
    public static class TweetService
    {
        private const string TWEET_URL = "https://x.com/intent/tweet";
        
        /// <summary>
        /// 指定したテキストでツイート画面を開く
        /// </summary>
        /// <param name="tweetText">ツイートする内容</param>
        public static void OpenTweet(string tweetText)
        {
            var encodedText = Uri.EscapeDataString(tweetText);
            var url = $"{TWEET_URL}?text={encodedText}";
            Application.OpenURL(url);
        }
        
        /// <summary>
        /// 空のツイート画面を開く
        /// </summary>
        public static void OpenTweet()
        {
            Application.OpenURL(TWEET_URL);
        }
    }
}