#if GOOGLE_ENABLE
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace Void2610.UnityTemplate.Google
{
    /// <summary>
    /// Google Sheets API サービス
    /// </summary>
    public static class GoogleSpreadSheetService
    {
        private static readonly string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };

        /// <summary>
        /// スプレッドシートのシートデータを取得する
        /// </summary>
        /// <param name="keyFileName">認証キーファイル名</param>
        /// <param name="sheetId">スプレッドシートID</param>
        /// <param name="sheetName">シート名</param>
        /// <returns>シートデータ、失敗時は null</returns>
        public static async UniTask<IList<IList<object>>> GetSheet(string keyFileName, string sheetId, string sheetName)
        {
            var credential = GoogleAuthService.GetCredential(keyFileName, Scopes);
            if (credential == null) return null;

            var sheetService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });

            // シート全体のデータを取得
            var range = $"{sheetName}";
            var result = await sheetService.Spreadsheets.Values.Get(sheetId, range).ExecuteAsync();
            return result.Values;
        }
    }
}
#endif
