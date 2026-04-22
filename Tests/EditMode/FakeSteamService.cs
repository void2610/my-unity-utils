using System.Collections.Generic;
using Void2610.UnityTemplate.Steam;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// テスト用の ISteamService 実装。
    /// 実績解除の履歴と統計値をメモリ上に保持し、テストから検証できるようにする。
    /// 未初期化状態をシミュレートする <see cref="SimulateNotInitialized"/> を提供する。
    /// </summary>
    public class FakeSteamService : ISteamService
    {
        private readonly List<string> _unlockedAchievements = new();
        private readonly Dictionary<string, int> _intStats = new();
        private readonly Dictionary<string, float> _floatStats = new();

        public bool IsInitialized { get; private set; } = true;

        /// <summary>実績解除が呼ばれた順で履歴を返す（重複あり）。</summary>
        public IReadOnlyList<string> UnlockedAchievements => _unlockedAchievements;

        /// <summary>未初期化状態をシミュレートする。以降のAPI呼び出しは全てfalseを返す。</summary>
        public void SimulateNotInitialized() => IsInitialized = false;

        public bool UnlockAchievement(string achievementName)
        {
            if (!IsInitialized) return false;
            _unlockedAchievements.Add(achievementName);
            return true;
        }

        public bool SetStat(string statName, int value)
        {
            if (!IsInitialized) return false;
            _intStats[statName] = value;
            return true;
        }

        public bool SetStat(string statName, float value)
        {
            if (!IsInitialized) return false;
            _floatStats[statName] = value;
            return true;
        }

        public bool AddStat(string statName, int value)
        {
            if (!IsInitialized) return false;
            _intStats.TryGetValue(statName, out var current);
            _intStats[statName] = current + value;
            return true;
        }

        public bool AddStat(string statName, float value)
        {
            if (!IsInitialized) return false;
            _floatStats.TryGetValue(statName, out var current);
            _floatStats[statName] = current + value;
            return true;
        }

        public bool GetStat(string statName, out int value)
        {
            if (!IsInitialized || !_intStats.TryGetValue(statName, out value))
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool GetStat(string statName, out float value)
        {
            if (!IsInitialized || !_floatStats.TryGetValue(statName, out value))
            {
                value = 0f;
                return false;
            }
            return true;
        }

        public bool ResetAllStats(bool achievementsToo = true)
        {
            if (!IsInitialized) return false;
            _intStats.Clear();
            _floatStats.Clear();
            if (achievementsToo) _unlockedAchievements.Clear();
            return true;
        }
    }
}
