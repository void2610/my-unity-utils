using NUnit.Framework;
using Void2610.UnityTemplate.Steam;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// ISteamService を想定通りに使う側のロジックを検証するためのFake実装を提供し、
    /// その契約（呼び出し記録・統計値保持）が正しいことを検証する。
    ///
    /// 実SteamAPIへの依存を避けるため、SteamService本体のテストはここでは行わない
    /// （DISABLESTEAMWORKS側はすべてfalseを返すだけのスタブなので検証価値が低い）。
    /// 代わりに呼び出し側のPresenter等が ISteamService をどう使うかのテストで
    /// このFakeを活用する。
    /// </summary>
    [TestFixture]
    public class FakeSteamServiceTest
    {
        [Test]
        public void UnlockAchievement_呼び出しが記録される()
        {
            var fake = new FakeSteamService();

            var result = fake.UnlockAchievement("ACH_FIRST_BOOT");

            Assert.That(result, Is.True);
            Assert.That(fake.UnlockedAchievements, Is.EqualTo(new[] { "ACH_FIRST_BOOT" }));
        }

        [Test]
        public void UnlockAchievement_同じ実績を複数回解除しても履歴に全て残る()
        {
            var fake = new FakeSteamService();

            fake.UnlockAchievement("ACH_A");
            fake.UnlockAchievement("ACH_A");
            fake.UnlockAchievement("ACH_B");

            Assert.That(fake.UnlockedAchievements, Is.EqualTo(new[] { "ACH_A", "ACH_A", "ACH_B" }));
        }

        [Test]
        public void SetStat_int_値が保持されGetStatで取得できる()
        {
            var fake = new FakeSteamService();

            Assert.That(fake.SetStat("kills", 5), Is.True);

            Assert.That(fake.GetStat("kills", out int value), Is.True);
            Assert.That(value, Is.EqualTo(5));
        }

        [Test]
        public void SetStat_float_値が保持されGetStatで取得できる()
        {
            var fake = new FakeSteamService();

            Assert.That(fake.SetStat("accuracy", 0.75f), Is.True);

            Assert.That(fake.GetStat("accuracy", out float value), Is.True);
            Assert.That(value, Is.EqualTo(0.75f));
        }

        [Test]
        public void AddStat_int_既存値に加算される()
        {
            var fake = new FakeSteamService();
            fake.SetStat("kills", 3);

            Assert.That(fake.AddStat("kills", 2), Is.True);

            fake.GetStat("kills", out int value);
            Assert.That(value, Is.EqualTo(5));
        }

        [Test]
        public void AddStat_未登録キーに対しては0から加算される()
        {
            var fake = new FakeSteamService();

            Assert.That(fake.AddStat("new_stat", 7), Is.True);

            fake.GetStat("new_stat", out int value);
            Assert.That(value, Is.EqualTo(7));
        }

        [Test]
        public void AddStat_float_既存値に加算される()
        {
            var fake = new FakeSteamService();
            fake.SetStat("time", 1.0f);

            fake.AddStat("time", 0.5f);

            fake.GetStat("time", out float value);
            Assert.That(value, Is.EqualTo(1.5f));
        }

        [Test]
        public void GetStat_未登録キーはfalseを返しoutは0()
        {
            var fake = new FakeSteamService();

            var intResult = fake.GetStat("missing", out int intValue);
            var floatResult = fake.GetStat("missing", out float floatValue);

            Assert.That(intResult, Is.False);
            Assert.That(intValue, Is.EqualTo(0));
            Assert.That(floatResult, Is.False);
            Assert.That(floatValue, Is.EqualTo(0f));
        }

        [Test]
        public void ResetAllStats_統計値と実績が初期化される()
        {
            var fake = new FakeSteamService();
            fake.UnlockAchievement("ACH_A");
            fake.SetStat("kills", 10);
            fake.SetStat("accuracy", 0.5f);

            fake.ResetAllStats(achievementsToo: true);

            Assert.That(fake.UnlockedAchievements, Is.Empty);
            Assert.That(fake.GetStat("kills", out int _), Is.False);
            Assert.That(fake.GetStat("accuracy", out float _), Is.False);
        }

        [Test]
        public void ResetAllStats_achievementsTooがfalseなら実績は残る()
        {
            var fake = new FakeSteamService();
            fake.UnlockAchievement("ACH_A");
            fake.SetStat("kills", 10);

            fake.ResetAllStats(achievementsToo: false);

            Assert.That(fake.UnlockedAchievements, Is.EqualTo(new[] { "ACH_A" }));
            Assert.That(fake.GetStat("kills", out int _), Is.False);
        }

        [Test]
        public void IsInitialized_デフォルトはtrue_failNotInitializedでfalseをシミュレート可能()
        {
            var fake = new FakeSteamService();
            Assert.That(fake.IsInitialized, Is.True);
            Assert.That(fake.UnlockAchievement("A"), Is.True);

            fake.SimulateNotInitialized();

            Assert.That(fake.IsInitialized, Is.False);
            // 未初期化時は実SteamServiceと同じくfalseを返す
            Assert.That(fake.UnlockAchievement("B"), Is.False);
            Assert.That(fake.SetStat("s", 1), Is.False);
            Assert.That(fake.AddStat("s", 1), Is.False);
            Assert.That(fake.GetStat("s", out int _), Is.False);
            Assert.That(fake.ResetAllStats(), Is.False);
            // 初期化後の呼び出しのみ記録される（BはUnlockされない）
            Assert.That(fake.UnlockedAchievements, Is.EqualTo(new[] { "A" }));
        }
    }
}
