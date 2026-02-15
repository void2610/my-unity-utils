#if !STEAMWORKS_NET || (UNITY_WEBGL && !UNITY_EDITOR)
#define DISABLESTEAMWORKS
#elif !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS
using System;
using Steamworks;
using UnityEngine;
using VContainer.Unity;

namespace Void2610.UnityTemplate.Steam
{
    public class SteamService : IDisposable, ITickable
    {
        private static bool _everInitialized;
        private bool _initialized;

        public static bool Initialized => _everInitialized;

        [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
        private static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
        {
            Debug.LogWarning(pchDebugText);
        }

        public SteamService(int appId)
        {
            Init(appId);
            if (_initialized)
            {
                // SteamAPIの警告メッセージをUnityのコンソールに出力する（起動時引数に-debug_steamapiが含まれている場合にのみ有効）
                var steamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
                SteamClient.SetWarningMessageHook(steamAPIWarningMessageHook);
            }
        }

        public bool UnlockAchievement(string achievementName)
        {
            if (!_initialized) return false;
            if (!SteamUserStats.SetAchievement(achievementName)) return false;
            if (!SteamUserStats.StoreStats()) return false;
            return true;
        }

        public bool SetStat(string statName, int value)
        {
            if (!_initialized) return false;
            if (!SteamUserStats.SetStat(statName, value)) return false;
            if (!SteamUserStats.StoreStats()) return false;
            return true;
        }

        public bool SetStat(string statName, float value)
        {
            if (!_initialized) return false;
            if (!SteamUserStats.SetStat(statName, value)) return false;
            if (!SteamUserStats.StoreStats()) return false;
            return true;
        }

        public bool AddStat(string statName, int value)
        {
            if (!_initialized) return false;
            if (!SteamUserStats.GetStat(statName, out int currentValue)) return false;
            currentValue += value;
            if (!SteamUserStats.SetStat(statName, currentValue)) return false;
            if (!SteamUserStats.StoreStats()) return false;
            return true;
        }

        public bool AddStat(string statName, float value)
        {
            if (!_initialized) return false;
            if (!SteamUserStats.GetStat(statName, out float currentValue)) return false;
            currentValue += value;
            if (!SteamUserStats.SetStat(statName, currentValue)) return false;
            if (!SteamUserStats.StoreStats()) return false;
            return true;
        }

        public bool GetStat(string statName, out int value)
        {
            value = 0;
            if (!_initialized) return false;
            return SteamUserStats.GetStat(statName, out value);
        }

        public bool GetStat(string statName, out float value)
        {
            value = 0f;
            if (!_initialized) return false;
            return SteamUserStats.GetStat(statName, out value);
        }

        private void Init(int appId)
        {
            if (_everInitialized) throw new Exception("Tried to Initialize the SteamAPI twice in one session!");
            if (!Packsize.Test()) Debug.LogError("[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.");
            if (!DllCheck.Test()) Debug.LogError("[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.");

            try
            {
                #if ENABLE_STEAMWORKS_RESTARTAPPIFNECESSARY
                // Steam以外で起動された場合、Steamクライアントを起動しゲームを再度Steam経由で起動する
                if (SteamAPI.RestartAppIfNecessary(new AppId_t((uint)appId)))
                {
                    Debug.Log("[Steamworks.NET] Shutting down because RestartAppIfNecessary returned true. Steam will restart the application.");
                    Application.Quit();
                    return;
                }
                #endif
            }
            catch (DllNotFoundException e)
            {
                Debug.LogError("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e);
                Application.Quit();
                return;
            }

            // SteamworksAPIを初期化
            _initialized = SteamAPI.Init();
            if (!_initialized)
            {
                Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.");
                return;
            }

            _everInitialized = true;
        }

        public void Tick()
        {
            if (!_initialized) return;
            SteamAPI.RunCallbacks();
        }

        public void Dispose()
        {
            if (!_initialized) return;
            SteamAPI.Shutdown();
            _initialized = false;
            _everInitialized = false;
        }

        // [デバッグ用] 全ての実績と統計情報をリセットする
        public bool ResetAllStats(bool achievementsToo = true)
        {
            if (!_initialized) return false;
            return SteamUserStats.ResetAllStats(achievementsToo);
        }
    }
}
#else
using System;
using VContainer.Unity;

namespace Void2610.UnityTemplate.Steam
{
    public class SteamService : IDisposable, ITickable
    {
        public static bool Initialized => false;
        public SteamService() { }
        public bool UnlockAchievement(string achievementName) => false;
        public bool SetStat(string statName, int value) => false;
        public bool SetStat(string statName, float value) => false;
        public bool AddStat(string statName, int value) => false;
        public bool AddStat(string statName, float value) => false;
        public bool GetStat(string statName, out int value) { value = 0; return false; }
        public bool GetStat(string statName, out float value) { value = 0f; return false; }
        public bool ResetAllStats(bool achievementsToo = true) => false;
        public void Tick() { }
        public void Dispose() { }
    }
}
#endif
