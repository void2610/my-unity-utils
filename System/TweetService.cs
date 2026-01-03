using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Void2610.UnityTemplate
{
    public static class TweetService
    {
        private const string TWEET_URL = "https://x.com/intent/tweet";
        private const string IMGBB_UPLOAD_URL = "https://api.imgbb.com/1/upload";

        public static void OpenTweet(string tweetText)
        {
            var encodedText = Uri.EscapeDataString(tweetText);
            var url = $"{TWEET_URL}?text={encodedText}";
            Application.OpenURL(url);
        }

        public static void OpenTweet(string tweetText, string linkUrl)
        {
            var encodedText = Uri.EscapeDataString(tweetText);
            var encodedUrl = Uri.EscapeDataString(linkUrl);
            var url = $"{TWEET_URL}?text={encodedText}&url={encodedUrl}";
            Application.OpenURL(url);
        }

        public static void OpenTweet()
        {
            Application.OpenURL(TWEET_URL);
        }

        public static async UniTask OpenTweetWithScreenshotAsync(string tweetText, string imgBBApiKey)
        {
            // スクリーンショット撮影
            await UniTask.WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            var bytes = texture.EncodeToPNG();
            UnityEngine.Object.Destroy(texture);

            // ImgBBにアップロード
            var imageUrl = await UploadToImgBBAsync(bytes, imgBBApiKey);

            if (!string.IsNullOrEmpty(imageUrl))
            {
                OpenTweet(tweetText, imageUrl);
            }
            else
            {
                OpenTweet(tweetText);
            }
        }

        private static async UniTask<string> UploadToImgBBAsync(byte[] imageBytes, string apiKey)
        {
            var base64 = Convert.ToBase64String(imageBytes);

            var form = new WWWForm();
            form.AddField("key", apiKey);
            form.AddField("image", base64);

            using var request = UnityWebRequest.Post(IMGBB_UPLOAD_URL, form);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[TweetService] ImgBBへのアップロードに失敗しました: {request.error}");
                return null;
            }

            var json = request.downloadHandler.text;

            try
            {
                var response = JsonUtility.FromJson<ImgBBResponse>(json);
                if (response?.data == null)
                {
                    Debug.LogError("[TweetService] ImgBBレスポンスのパースに失敗しました: dataがnullです");
                    return null;
                }
                return response.data.url_viewer;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TweetService] ImgBBレスポンスのパースに失敗しました: {e.Message}");
                return null;
            }
        }

        [Serializable]
        private class ImgBBResponse
        {
            public ImgBBData data;
        }

        [Serializable]
        private class ImgBBData
        {
            // ReSharper disable once InconsistentNaming
            public string url_viewer;
        }
    }
}
