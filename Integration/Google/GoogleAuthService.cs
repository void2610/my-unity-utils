#if GOOGLE_ENABLE
using System.IO;
using Google.Apis.Auth.OAuth2;
using UnityEngine;

namespace Void2610.UnityTemplate.Google
{
    /// <summary>
    /// Google API 認証サービス
    /// </summary>
    public static class GoogleAuthService
    {
        /// <summary>
        /// 指定されたスコープで認証情報を取得する
        /// </summary>
        /// <param name="keyFileName">StreamingAssetsフォルダ内の認証キーファイル名</param>
        /// <param name="scopes">必要な API スコープ</param>
        /// <returns>認証情報、失敗時は null</returns>
        public static ICredential GetCredential(string keyFileName, string[] scopes)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL環境ではGoogle APIが動作しないためnullを返す
            return null;
#else
            var keyPath = Path.Combine(Application.streamingAssetsPath, keyFileName);
            try
            {
                using (var stream = new FileStream(keyPath, FileMode.Open, FileAccess.Read))
                {
                    return GoogleCredential.FromStream(stream)
                        .CreateScoped(scopes).UnderlyingCredential;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"GoogleAuthService: 認証失敗 - {e.Message}");
                return null;
            }
#endif
        }
    }
}
#endif
