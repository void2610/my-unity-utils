using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// ExhibitInputBlocker の検証。
    /// 展示オーバーレイを閉じた押下がゲームに届かないことの根拠 (離されるまで止め続ける) を固定する
    /// </summary>
    [TestFixture]
    public class ExhibitInputBlockerTest
    {
        private InputAction _enabledAction;
        private InputAction _disabledAction;
        private bool _pressed;
        private ExhibitInputBlocker _blocker;

        [SetUp]
        public void SetUp()
        {
            _enabledAction = new InputAction("enabled", InputActionType.Button, "<Keyboard>/space");
            _enabledAction.Enable();
            _disabledAction = new InputAction("disabled", InputActionType.Button, "<Keyboard>/enter");
            _pressed = false;
            _blocker = new ExhibitInputBlocker(() => _pressed);
        }

        [TearDown]
        public void TearDown()
        {
            _blocker.ReleaseNow();
            _enabledAction.Dispose();
            _disabledAction.Dispose();
        }

        [Test]
        public void Blockで有効なアクションが止まりReleaseNowで戻る()
        {
            _blocker.Block();
            Assert.That(_enabledAction.enabled, Is.False);

            _blocker.ReleaseNow();
            Assert.That(_enabledAction.enabled, Is.True);
        }

        [Test]
        public void 止める前から無効だったアクションは戻さない()
        {
            _blocker.Block();
            _blocker.ReleaseNow();

            Assert.That(_disabledAction.enabled, Is.False);
        }

        [Test]
        public void 解除要求がなければTickで戻さない()
        {
            _blocker.Block();
            _blocker.Tick();

            Assert.That(_enabledAction.enabled, Is.False);
        }

        [Test]
        public void 解除要求後もボタンが押されている間は戻さず離すと戻す()
        {
            _blocker.Block();
            _pressed = true;
            _blocker.RequestRelease();

            _blocker.Tick();
            Assert.That(_enabledAction.enabled, Is.False);

            _pressed = false;
            _blocker.Tick();
            Assert.That(_enabledAction.enabled, Is.True);
        }
    }
}
