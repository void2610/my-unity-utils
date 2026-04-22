namespace Void2610.UnityTemplate.Steam
{
    /// <summary>
    /// Steam連携のインターフェース。
    /// テスト時のモック注入と、SteamSDKに依存しない呼び出し側のDIを可能にする。
    /// </summary>
    public interface ISteamService
    {
        /// <summary>現在のプロセスでSteamAPIの初期化に成功しているかを返す。</summary>
        bool IsInitialized { get; }

        /// <summary>実績を解除する。成功時true。</summary>
        bool UnlockAchievement(string achievementName);

        /// <summary>int型の統計値を上書きで設定する。成功時true。</summary>
        bool SetStat(string statName, int value);

        /// <summary>float型の統計値を上書きで設定する。成功時true。</summary>
        bool SetStat(string statName, float value);

        /// <summary>int型の統計値に加算する。成功時true。</summary>
        bool AddStat(string statName, int value);

        /// <summary>float型の統計値に加算する。成功時true。</summary>
        bool AddStat(string statName, float value);

        /// <summary>int型の統計値を取得する。</summary>
        bool GetStat(string statName, out int value);

        /// <summary>float型の統計値を取得する。</summary>
        bool GetStat(string statName, out float value);

        /// <summary>全統計値と（指定時は）全実績をリセットする。デバッグ用途。</summary>
        bool ResetAllStats(bool achievementsToo = true);
    }
}
