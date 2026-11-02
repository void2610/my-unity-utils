using NUnit.Framework;
using UnityEngine;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// ExhibitSettings の実行中の上書きの検証。
    /// テストやデバッグがアセットを書き換えずに展示モードを切り替えられることを固定する
    /// </summary>
    [TestFixture]
    public class ExhibitSettingsTest
    {
        private ExhibitSettings _settings;

        [SetUp]
        public void SetUp()
        {
            ExhibitSettings.OverrideExhibitMode(null);
            // 新しく作った設定はアセットの値として展示モードが無効
            _settings = ScriptableObject.CreateInstance<ExhibitSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            ExhibitSettings.OverrideExhibitMode(null);
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void 上書きしなければアセットの値に従う()
        {
            Assert.That(_settings.EnableExhibitMode, Is.False);
            Assert.That(_settings.EnableIdlePauseAndReturn, Is.False);
            Assert.That(_settings.EnableSessionTimeLimit, Is.False);
            Assert.That(_settings.EnableTitleIdlePv, Is.False);
        }

        [Test]
        public void 有効に上書きすると展示モードの機能もすべて有効になる()
        {
            ExhibitSettings.OverrideExhibitMode(true);

            Assert.That(_settings.EnableExhibitMode, Is.True);
            Assert.That(_settings.EnableIdlePauseAndReturn, Is.True);
            Assert.That(_settings.EnableSessionTimeLimit, Is.True);
            Assert.That(_settings.EnableTitleIdlePv, Is.True);
        }

        [Test]
        public void 上書きを外すとアセットの値に戻る()
        {
            ExhibitSettings.OverrideExhibitMode(true);
            ExhibitSettings.OverrideExhibitMode(null);

            Assert.That(_settings.EnableExhibitMode, Is.False);
        }
    }
}
