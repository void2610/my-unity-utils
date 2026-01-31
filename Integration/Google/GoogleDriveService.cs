#if GOOGLE_ENABLE
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;

namespace Void2610.UnityTemplate.Google
{
    /// <summary>
    /// Google Drive API サービス
    /// </summary>
    public static class GoogleDriveService
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveReadonly };

        private class PageToken
        {
            public string Token { get; set; } = "";
        }

        /// <summary>
        /// 指定フォルダ内のサブフォルダ一覧を取得する
        /// </summary>
        /// <param name="keyFileName">認証キーファイル名</param>
        /// <param name="applicationName">アプリケーション名</param>
        /// <param name="folderId">親フォルダID</param>
        /// <returns>フォルダ一覧</returns>
        public static async UniTask<List<File>> GetFolderList(string keyFileName, string applicationName, string folderId)
        {
            var credential = GoogleAuthService.GetCredential(keyFileName, Scopes);
            var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });

            var query = $"('{folderId}' in parents) and (mimeType = 'application/vnd.google-apps.folder') and (trashed = false)";
            var pageToken = new PageToken();
            var files = new List<File>();
            files.AddRange(await RequestFiles(driveService, query, pageToken));
            while (!string.IsNullOrEmpty(pageToken.Token))
            {
                files.AddRange(await RequestFiles(driveService, query, pageToken));
            }

            return files;
        }

        /// <summary>
        /// 指定フォルダ内のスプレッドシート一覧を取得する
        /// </summary>
        /// <param name="keyFileName">認証キーファイル名</param>
        /// <param name="applicationName">アプリケーション名</param>
        /// <param name="folderId">親フォルダID</param>
        /// <returns>スプレッドシート一覧（file.Id: スプレッドシートID, file.Name: 表示名）</returns>
        public static async UniTask<List<File>> GetSpreadSheetList(string keyFileName, string applicationName, string folderId)
        {
            var credential = GoogleAuthService.GetCredential(keyFileName, Scopes);
            var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });

            var query = $"('{folderId}' in parents) and (mimeType = 'application/vnd.google-apps.spreadsheet') and (trashed = false)";
            var pageToken = new PageToken();
            var files = new List<File>();
            files.AddRange(await RequestFiles(driveService, query, pageToken));
            while (!string.IsNullOrEmpty(pageToken.Token))
            {
                files.AddRange(await RequestFiles(driveService, query, pageToken));
            }

            return files;
        }

        private static async UniTask<IList<File>> RequestFiles(DriveService service, string query, PageToken pageToken)
        {
            var listRequest = service.Files.List();
            listRequest.PageSize = 200;
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.PageToken = pageToken.Token;
            listRequest.Q = query;

            var response = await listRequest.ExecuteAsync();
            if (response.Files != null && response.Files.Count > 0)
            {
                pageToken.Token = response.NextPageToken;
                return response.Files;
            }

            pageToken.Token = null;
            return new List<File>();
        }
    }
}
#endif
