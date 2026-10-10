using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// DragFollower の離し方ごとの戻り方を固定する
    /// </summary>
    [TestFixture]
    public class DragFollowerTest
    {
        private static readonly Vector2 HOME_POSITION = new(10f, 20f);
        private static readonly Vector2 RELEASE_POSITION = new(300f, -150f);
        private const float RETURN_TIMEOUT_SECONDS = 2f;

        private GameObject _parent;
        private DragFollower _follower;
        private CanvasGroup _group;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject(nameof(DragFollowerTest), typeof(RectTransform));
            var element = new GameObject("Element", typeof(RectTransform), typeof(CanvasGroup));
            element.transform.SetParent(_parent.transform, false);
            ((RectTransform)element.transform).anchoredPosition = HOME_POSITION;
            _group = element.GetComponent<CanvasGroup>();
            _follower = element.AddComponent<DragFollower>();
            _follower.PickUp(new PointerEventData(null) { position = RELEASE_POSITION });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_parent);

        [Test]
        public void 掴むとポインタに付いていきレイキャストを通す()
        {
            Assert.That(AnchoredPosition, Is.EqualTo(RELEASE_POSITION));
            Assert.That(_group.blocksRaycasts, Is.False);
        }

        [Test]
        public void 受け取られるとその場でホームへ戻り再び掴める()
        {
            _follower.Drop();

            Assert.That(AnchoredPosition, Is.EqualTo(HOME_POSITION));
            Assert.That(_group.blocksRaycasts, Is.True);
        }

        [UnityTest]
        public IEnumerator 置き損ねると掴んだ場所へすべって戻り着いてから再び掴める()
        {
            var landed = false;
            _follower.Return(() => landed = true);

            // 戻りの途中は掴ませない
            Assert.That(_group.blocksRaycasts, Is.False);
            var deadline = Time.realtimeSinceStartup + RETURN_TIMEOUT_SECONDS;
            while (!landed && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.That(landed, Is.True);
            Assert.That(AnchoredPosition, Is.EqualTo(HOME_POSITION));
            Assert.That(_group.blocksRaycasts, Is.True);
        }

        [Test]
        public void 打ち切るとホームへ戻すが入力の可否は呼び出し側に任せる()
        {
            _follower.SnapHome();

            Assert.That(AnchoredPosition, Is.EqualTo(HOME_POSITION));
            Assert.That(_group.blocksRaycasts, Is.False);
        }

        [Test]
        public void ホームへ置いた後に動かされても打ち切りでホームへ置き直す()
        {
            // 次の出題で打ち切られた後に、前の問題の落下演出がホームから動かす順序
            _follower.SnapHome();
            ((RectTransform)_follower.transform).anchoredPosition = RELEASE_POSITION;

            _follower.SnapHome();

            Assert.That(AnchoredPosition, Is.EqualTo(HOME_POSITION));
        }

        private Vector2 AnchoredPosition => ((RectTransform)_follower.transform).anchoredPosition;
    }
}
