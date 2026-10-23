using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// StaggeredSlideInGroup が並べる子の選び方を固定する
    /// </summary>
    [TestFixture]
    public class StaggeredSlideInGroupTest
    {
        private GameObject _root;
        private StaggeredSlideInGroup _group;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(StaggeredSlideInGroupTest), typeof(RectTransform));
            ((RectTransform)_root.transform).sizeDelta = new Vector2(300f, 100f);
            _group = _root.AddComponent<StaggeredSlideInGroup>();
            var serialized = new SerializedObject(_group);
            serialized.FindProperty("layoutMode").enumValueIndex = (int)StaggeredSlideInGroup.LayoutMode.Horizontal;
            serialized.FindProperty("spacing").floatValue = 20f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void レイアウトから外す指定の子は並べず動かさない()
        {
            var item = CreateChild("Item");
            item.sizeDelta = new Vector2(100f, 100f);
            _group.ApplyLayout();
            var expected = item.anchoredPosition;

            var ignored = CreateChild("Ignored");
            ignored.anchoredPosition = new Vector2(7f, 9f);
            ignored.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _group.ApplyLayout();

            Assert.That(item.anchoredPosition, Is.EqualTo(expected));
            Assert.That(ignored.anchoredPosition, Is.EqualTo(new Vector2(7f, 9f)));
        }

        private RectTransform CreateChild(string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(_root.transform, false);
            return (RectTransform)child.transform;
        }
    }
}
