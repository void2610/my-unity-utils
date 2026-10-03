using NUnit.Framework;
using UnityEngine;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// SelectableTween の検証。生成した後で大きさを変えた要素が、選択を外したときに元の大きさへ戻らないことを固定する
    /// </summary>
    [TestFixture]
    public class SelectableTweenTest
    {
        private GameObject _gameObject;
        private SelectableTween _tween;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(SelectableTweenTest));
            _tween = _gameObject.AddComponent<SelectableTween>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_gameObject);

        [Test]
        public void SetDefaultScaleですぐにその大きさになる()
        {
            _tween.SetDefaultScale(2f);

            Assert.That(_gameObject.transform.localScale, Is.EqualTo(Vector3.one * 2f));
        }

        [Test]
        public void SetDefaultScaleの後は元に戻す先がその大きさになる()
        {
            _tween.SetDefaultScale(2f);
            _gameObject.transform.localScale = Vector3.one * 2.2f;

            // 無効にするとアニメーションなしで基準の大きさへ戻す
            _tween.SetTweenEnabled(false);

            Assert.That(_gameObject.transform.localScale, Is.EqualTo(Vector3.one * 2f));
        }
    }
}
