using SyskenTLib.LicenseMaster;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// LicenseMasterと連携してライセンス情報を提供するサービス
    /// 使用ライブラリのライセンス表示機能を統一的に管理
    /// </summary>
    public class LicenseService
    {
        private readonly LicenseManager _licenseManager;
        
        public LicenseService(LicenseManager licenseManager)
        {
            _licenseManager = licenseManager;
        }
        
        /// <summary>
        /// 全てのライセンス情報をテキスト形式で取得
        /// </summary>
        /// <returns>ライセンステキスト</returns>
        public string GetLicenseText()
        {
            if (_licenseManager == null)
            {
                return "LicenseManagerが設定されていません。";
            }
            
            var licenses = _licenseManager.GetLicenseConfigsTxt();
            
            // ライセンステキストの先頭に余白を追加
            return "\n\n\n\n" + licenses;
        }
        
        /// <summary>
        /// 特定のライブラリのライセンス情報を取得
        /// </summary>
        /// <param name="libraryName">ライブラリ名</param>
        /// <returns>該当ライブラリのライセンス情報</returns>
        public string GetLibraryLicense(string libraryName)
        {
            if (_licenseManager == null)
            {
                return $"{libraryName}のライセンス情報が見つかりません。";
            }
            
            // 全ライセンスから特定ライブラリを検索
            var allLicenses = _licenseManager.GetLicenseConfigsTxt();
            var lines = allLicenses.Split('\n');
            
            bool isTargetLibrary = false;
            var result = new System.Text.StringBuilder();
            
            foreach (var line in lines)
            {
                if (line.Contains(libraryName))
                {
                    isTargetLibrary = true;
                }
                else if (isTargetLibrary && string.IsNullOrWhiteSpace(line))
                {
                    break;
                }
                
                if (isTargetLibrary)
                {
                    result.AppendLine(line);
                }
            }
            
            return result.Length > 0 ? result.ToString() : $"{libraryName}のライセンス情報が見つかりません。";
        }
    }
}