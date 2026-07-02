using NUnit.Framework;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// StableHash (FNV-1a) の検証。
    /// シード値の再現性の根幹なので、既知値との一致でアルゴリズム自体を固定する
    /// (実装を変えるとマシン・バージョンを跨いだセーブ/リプレイ/テスト期待値が全て壊れるため)。
    /// </summary>
    [TestFixture]
    public class StableHashTest
    {
        [Test]
        public void 既知値と一致する()
        {
            // FNV-1a 32bit の公開テストベクタ (http://www.isthe.com/chongo/tech/comp/fnv/)
            Assert.That(StableHash.Fnv1a(""), Is.EqualTo(unchecked((int)0x811c9dc5)));
            Assert.That(StableHash.Fnv1a("a"), Is.EqualTo(unchecked((int)0xe40c292c)));
            Assert.That(StableHash.Fnv1a("foobar"), Is.EqualTo(unchecked((int)0xbf9cf968)));
        }

        [Test]
        public void 非ASCII文字列もUTF8バイト列の既知値と一致する()
        {
            // UTF-8 バイト列に対する FNV-1a 32bit (外部計算による参照値)
            Assert.That(StableHash.Fnv1a("あ"), Is.EqualTo(unchecked((int)0xe02c1bb1)));
            Assert.That(StableHash.Fnv1a("シード値"), Is.EqualTo(unchecked((int)0xe19512ed)));
        }

        [Test]
        public void 同一文字列は常に同じハッシュになる()
        {
            Assert.That(StableHash.Fnv1a("e2e_fixed_001"), Is.EqualTo(StableHash.Fnv1a("e2e_fixed_001")));
        }

        [Test]
        public void 異なる文字列は異なるハッシュになる()
        {
            Assert.That(StableHash.Fnv1a("seed1"), Is.Not.EqualTo(StableHash.Fnv1a("seed2")));
        }
    }
}
