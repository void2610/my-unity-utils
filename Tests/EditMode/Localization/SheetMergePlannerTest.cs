using System.Collections.Generic;
using System.Linq;
using Void2610.UnityTemplate.Editor.Localization;
using NUnit.Framework;

namespace Void2610.UnityTemplate.Tests
{
    [TestFixture]
    public class SheetMergePlannerTest
    {
        private static readonly string[] Locales = { "ja", "en" };

        private static readonly HashSet<string> NoForce = new();

        [TestCase("a", "a", "x", SheetCellState.InSync)]
        [TestCase("b", "a", "a", SheetCellState.LocalChanged)]
        [TestCase("a", "b", "a", SheetCellState.DriveChanged)]
        [TestCase("b", "c", "a", SheetCellState.Conflict)]
        [TestCase("a", null, null, SheetCellState.LocalChanged)]
        [TestCase(null, "a", "a", SheetCellState.LocalChanged)]
        [TestCase(null, "a", null, SheetCellState.DriveChanged)]
        [TestCase("a", "b", null, SheetCellState.Conflict)]
        public void Classify_前回の同期時点と同じ側を変わっていない側とみなす(string local, string drive, string baseValue, SheetCellState expected)
        {
            var state = SheetMergePlanner.Classify(local, drive, baseValue);

            Assert.That(state, Is.EqualTo(expected));
        }

        [Test]
        public void PlanPush_ローカルで変えたセルだけを書き換える()
        {
            var baseRows = Rows(("K", "旧", "old"));
            var local = Rows(("K", "新", "old"));
            var drive = Rows(("K", "旧", "old"));

            var plan = SheetMergePlanner.PlanPush(Locales, local, drive, baseRows, NoForce);

            Assert.That(plan.Updates.Select(c => (c.Key, c.Locale, c.Local)), Is.EqualTo(new[] { ("K", "ja", "新") }));
        }

        [Test]
        public void PlanPush_Driveで変わったセルは書かずに残す()
        {
            var baseRows = Rows(("K", "旧", "old"));
            var local = Rows(("K", "旧", "old"));
            var drive = Rows(("K", "旧", "fixed"));

            var plan = SheetMergePlanner.PlanPush(Locales, local, drive, baseRows, NoForce);

            Assert.That(plan.Updates, Is.Empty);
            Assert.That(plan.Skipped.Select(c => (c.Key, c.Locale, c.State)), Is.EqualTo(new[] { ("K", "en", SheetCellState.DriveChanged) }));
        }

        [Test]
        public void PlanPush_衝突したセルはforceKeysに無ければ書かない()
        {
            var plan = SheetMergePlanner.PlanPush(Locales, Rows(("K", "ローカル", "x")), Rows(("K", "Drive", "x")), Rows(("K", "旧", "x")), NoForce);

            Assert.That(plan.Updates, Is.Empty);
            Assert.That(plan.Skipped.Single().State, Is.EqualTo(SheetCellState.Conflict));
        }

        [Test]
        public void PlanPush_衝突したセルはforceKeysに挙げるとローカルの値で書く()
        {
            var plan = SheetMergePlanner.PlanPush(Locales, Rows(("K", "ローカル", "x")), Rows(("K", "Drive", "x")), Rows(("K", "旧", "x")), new HashSet<string> { "K" });

            Assert.That(plan.Updates.Select(c => c.Local), Is.EqualTo(new[] { "ローカル" }));
            Assert.That(plan.Skipped, Is.Empty);
        }

        [Test]
        public void PlanPush_ローカルにだけあるキーを行として足す()
        {
            var plan = SheetMergePlanner.PlanPush(Locales, Rows(("A", "あ", "a"), ("NEW", "新", "new")), Rows(("A", "あ", "a")), Rows(), NoForce);

            Assert.That(plan.Appends, Is.EqualTo(new[] { "NEW" }));
        }

        [Test]
        public void PlanPush_ローカルで消したキーの行を消す()
        {
            var baseRows = Rows(("A", "あ", "a"), ("OLD", "旧", "old"));

            var plan = SheetMergePlanner.PlanPush(Locales, Rows(("A", "あ", "a")), baseRows, baseRows, NoForce);

            Assert.That(plan.Deletes, Is.EqualTo(new[] { "OLD" }));
        }

        [Test]
        public void PlanPush_Driveで消した行に書けないセルがあれば足し直さない()
        {
            var baseRows = Rows(("K", "あ", "old"));
            var local = Rows(("K", "あ", "new"));

            var plan = SheetMergePlanner.PlanPush(Locales, local, Rows(), baseRows, NoForce);

            Assert.That(plan.Appends, Is.Empty);
            Assert.That(plan.Skipped.Select(c => c.State), Is.EquivalentTo(new[] { SheetCellState.DriveChanged, SheetCellState.Conflict }));
        }

        [Test]
        public void PlanPush_書いたセルと一致したセルは次の基準をローカルの値にし書かなかったセルは前回の値を残す()
        {
            var baseRows = Rows(("K", "旧", "old"));
            var local = Rows(("K", "新", "old"));
            var drive = Rows(("K", "旧", "fixed"));

            var plan = SheetMergePlanner.PlanPush(Locales, local, drive, baseRows, NoForce);

            Assert.That(plan.NextBase.Get("K", "ja"), Is.EqualTo("新"));
            Assert.That(plan.NextBase.Get("K", "en"), Is.EqualTo("old"));
        }

        [Test]
        public void PlanPush_前回の同期で記録の無いロケールは次の基準にも残さない()
        {
            var baseRows = new TranslationRows();
            baseRows.Set("K", "en", "a");

            var plan = SheetMergePlanner.PlanPush(Locales, Rows(("K", "ローカル", "a")), Rows(("K", "Drive", "a")), baseRows, NoForce);

            Assert.That(plan.Skipped.Single().Base, Is.Null);
            Assert.That(plan.NextBase.Find("K", "ja"), Is.Null);
        }

        [Test]
        public void Compare_改行コードの違いだけなら一致とみなす()
        {
            var cells = SheetMergePlanner.Compare(Locales, Rows(("K", "一行目\n二行目", "a")), Rows(("K", "一行目\r\n二行目", "a")), Rows());

            Assert.That(cells.Select(c => c.State), Is.All.EqualTo(SheetCellState.InSync));
        }

        private static TranslationRows Rows(params (string Key, string Ja, string En)[] rows)
        {
            var result = new TranslationRows();
            foreach (var (key, ja, en) in rows)
            {
                result.Set(key, "ja", ja);
                result.Set(key, "en", en);
            }
            return result;
        }
    }
}
