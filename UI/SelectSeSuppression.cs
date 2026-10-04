using System;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 選択時の効果音 (<see cref="ButtonSe"/> / <see cref="SelectableSe"/>) を鳴らさない条件。
    /// フォーカス管理が自動で選択し直したときなど、プレイヤーの操作ではない選択で鳴らさないよう利用側が設定する
    /// </summary>
    public static class SelectSeSuppression
    {
        /// <summary>true を返す間は選択時の効果音を鳴らさない。未設定なら常に鳴らす</summary>
        public static Func<bool> IsSuppressed { get; set; }

        internal static bool ShouldSuppress => IsSuppressed?.Invoke() == true;
    }
}
