using System.Linq;
using NUnit.Framework;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// System 版 RandomManager (Void2610.UnityTemplate) のシード再現性の検証。
    /// Core 版 (グローバル名前空間、ILogger 依存) も同じ StableHash + System.Random 構成のため、
    /// シード化の契約はここで代表して固定する。
    /// </summary>
    [TestFixture]
    public class RandomManagerTest
    {
        [Test]
        public void 同一シードで乱数列が再現する()
        {
            RandomManager.Instance.Initialize("testSeed");
            var first = Enumerable.Range(0, 10).Select(_ => RandomManager.Instance.RandomInt(0, 1000)).ToArray();
            RandomManager.Instance.Initialize("testSeed");
            var second = Enumerable.Range(0, 10).Select(_ => RandomManager.Instance.RandomInt(0, 1000)).ToArray();
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void 異なるシードで乱数列が変わる()
        {
            RandomManager.Instance.Initialize("seedA");
            var a = Enumerable.Range(0, 10).Select(_ => RandomManager.Instance.RandomInt(0, 1000)).ToArray();
            RandomManager.Instance.Initialize("seedB");
            var b = Enumerable.Range(0, 10).Select(_ => RandomManager.Instance.RandomInt(0, 1000)).ToArray();
            Assert.That(b, Is.Not.EqualTo(a));
        }

        [Test]
        public void RandomIntは指定範囲内の値を返す()
        {
            RandomManager.Instance.Initialize("rangeSeed");
            for (var i = 0; i < 100; i++)
            {
                var v = RandomManager.Instance.RandomInt(3, 7);
                Assert.That(v, Is.InRange(3, 6), "max は exclusive");
            }
        }

        [Test]
        public void Chanceは0で常にfalse_1で常にtrue()
        {
            RandomManager.Instance.Initialize("chanceSeed");
            for (var i = 0; i < 50; i++)
            {
                Assert.That(RandomManager.Instance.Chance(0f), Is.False);
                Assert.That(RandomManager.Instance.Chance(1f), Is.True);
            }
        }
    }
}
