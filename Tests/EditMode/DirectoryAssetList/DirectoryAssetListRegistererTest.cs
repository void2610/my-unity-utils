using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// アセットの追加・削除・移動に合わせて、同じフォルダの [SameDirectoryAssets] を付けた一覧が登録し直されることを、一時フォルダの実アセットで確かめる
    /// </summary>
    [TestFixture]
    public class DirectoryAssetListRegistererTest
    {
        private const string ROOT = "Assets/DirectoryAssetListRegistererTestTemp";
        private const string FOLDER_A = ROOT + "/A";
        private const string FOLDER_B = ROOT + "/B";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(ROOT);
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(ROOT));
            AssetDatabase.CreateFolder(ROOT, "A");
            AssetDatabase.CreateFolder(ROOT, "B");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(ROOT);
        }

        [Test]
        public void 一覧を作るとフォルダにあるアセットが名前順に登録される()
        {
            var second = CreateEntry(FOLDER_A, "Second");
            var first = CreateEntry(FOLDER_A, "First");

            var list = CreateList(FOLDER_A);

            Assert.That(list.Entries, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void アセットを足すと登録され消すと外れる()
        {
            var list = CreateList(FOLDER_A);

            var entry = CreateEntry(FOLDER_A, "Added");
            Assert.That(list.Entries, Is.EqualTo(new[] { entry }), "足したアセットが登録される");

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(entry));
            Assert.That(list.Entries, Is.Empty, "消したアセットが外れる");
        }

        [Test]
        public void 一覧を別のフォルダへ移すと移動先のアセットで登録し直される()
        {
            CreateEntry(FOLDER_A, "InA");
            var inB = CreateEntry(FOLDER_B, "InB");
            var list = CreateList(FOLDER_A);

            Assert.That(AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(list), FOLDER_B + "/List.asset"), Is.Empty);

            Assert.That(list.Entries, Is.EqualTo(new[] { inB }));
        }

        [Test]
        public void 登録済みの一覧を読み込み直してもファイルが変わらない()
        {
            CreateEntry(FOLDER_A, "Second");
            CreateEntry(FOLDER_A, "First");
            var list = CreateList(FOLDER_A);
            var path = AssetDatabase.GetAssetPath(list);
            var before = File.ReadAllText(path);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            Assert.That(EditorUtility.IsDirty(list), Is.False);
        }

        private static DirectoryAssetListTestEntry CreateEntry(string folder, string name)
        {
            var entry = ScriptableObject.CreateInstance<DirectoryAssetListTestEntry>();
            AssetDatabase.CreateAsset(entry, $"{folder}/{name}.asset");
            return entry;
        }

        private static DirectoryAssetListTestList CreateList(string folder)
        {
            var list = ScriptableObject.CreateInstance<DirectoryAssetListTestList>();
            AssetDatabase.CreateAsset(list, $"{folder}/List.asset");
            return list;
        }
    }
}
