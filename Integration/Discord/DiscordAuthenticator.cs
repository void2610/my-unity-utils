#if !UNITY_WEBGL || UNITY_EDITOR
using UnityEngine;
using Discord.Sdk;
using System;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Discord OAuth2認証を管理するクラス
    /// トークン管理、自動ログイン機能を提供
    /// </summary>
    public class DiscordAuthenticator
    {
        private const string REFRESH_TOKEN_KEY = "DiscordRefreshToken";
        private const string ACCESS_TOKEN_KEY = "DiscordAccessToken";
        private const string TOKEN_EXPIRY_KEY = "DiscordTokenExpiry";

        private readonly Client _client;
        private readonly ulong _clientId;
        private string _codeVerifier = "";

        public DiscordAuthenticator(Client client, ulong clientId)
        {
            _client = client;
            _clientId = clientId;
        }

        /// <summary>
        /// 認証を開始（自動ログイン→失敗時OAuth）
        /// </summary>
        public void StartAuthentication()
        {
            if (!TryAutoLogin())
                StartOAuthFlow(_clientId);
        }

        private void StartOAuthFlow(ulong clientId)
        {
            var authorizationVerifier = _client.CreateAuthorizationCodeVerifier();
            _codeVerifier = authorizationVerifier.Verifier();

            var args = new AuthorizationArgs();
            args.SetClientId(clientId);
            args.SetScopes(Client.GetDefaultPresenceScopes());
            args.SetCodeChallenge(authorizationVerifier.Challenge());
            _client.Authorize(args, OnAuthorizeResult);
        }

        private void OnAuthorizeResult(ClientResult result, string code, string redirectUri)
        {
            if (!result.Successful()) return;

            GetTokenFromCode(code, redirectUri);
        }

        /// <summary>
        /// 保存されたトークンを使用した自動ログインを試行
        /// </summary>
        public bool TryAutoLogin()
        {
            var refreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY, "");
            var accessToken = PlayerPrefs.GetString(ACCESS_TOKEN_KEY, "");
            var expiryTime = PlayerPrefs.GetString(TOKEN_EXPIRY_KEY, "");

            if (string.IsNullOrEmpty(refreshToken))
                return false;

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(expiryTime))
            {
                if (DateTime.TryParse(expiryTime, out var expiry) && DateTime.Now < expiry)
                {
                    _client.UpdateToken(AuthorizationTokenType.Bearer, accessToken, result => {
                        if (result.Successful()) _client.Connect();
                        else RefreshStoredToken(refreshToken);
                    });
                    return true;
                }
            }

            RefreshStoredToken(refreshToken);
            return true;
        }

        private void RefreshStoredToken(string refreshToken)
        {
            _client.RefreshToken(_clientId, refreshToken, (result, newAccessToken, newRefreshToken, _, expiresIn, _) => {
                if (result.Successful() && !string.IsNullOrEmpty(newAccessToken)) {
                    SaveTokens(newAccessToken, newRefreshToken, expiresIn);
                    OnReceivedToken(newAccessToken);
                } else ClearSavedTokens();
            });
        }

        private void GetTokenFromCode(string code, string redirectUri)
        {
            _client.GetToken(_clientId,
                code,
                _codeVerifier,
                redirectUri,
                (result, token, refreshToken, _, expiresIn, _) => {
                    if (result.Successful() && !string.IsNullOrEmpty(token)) {
                        SaveTokens(token, refreshToken, expiresIn);
                        OnReceivedToken(token);
                    }
                });
        }

        private void OnReceivedToken(string token)
        {
            _client.UpdateToken(AuthorizationTokenType.Bearer, token, result => {
                if (result.Successful()) _client.Connect();
            });
        }

        private void SaveTokens(string accessToken, string refreshToken, long expiresInSeconds)
        {
            PlayerPrefs.SetString(ACCESS_TOKEN_KEY, accessToken);
            PlayerPrefs.SetString(REFRESH_TOKEN_KEY, refreshToken);

            // 有効期限の5分前に更新するように設定
            var expiryTime = DateTime.Now.AddSeconds(expiresInSeconds - 300);
            PlayerPrefs.SetString(TOKEN_EXPIRY_KEY, expiryTime.ToString("O"));

            PlayerPrefs.Save();
        }

        /// <summary>
        /// 保存されたトークンをクリア
        /// </summary>
        public void ClearSavedTokens()
        {
            PlayerPrefs.DeleteKey(ACCESS_TOKEN_KEY);
            PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
            PlayerPrefs.DeleteKey(TOKEN_EXPIRY_KEY);
            PlayerPrefs.Save();
        }
    }
}
#endif
