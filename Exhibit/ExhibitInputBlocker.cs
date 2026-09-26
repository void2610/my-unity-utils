using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 展示オーバーレイの表示中、ゲームへ入力が届かないよう有効な InputAction を一括で止める
    /// 閉じたあとも解除に使った押下が離されるまでは止めたままにし、その押下でゲームが動かないようにする
    /// </summary>
    public sealed class ExhibitInputBlocker
    {
        public bool IsBlocking { get; private set; }

        private readonly List<InputAction> _suspended = new();
        private readonly Func<bool> _isAnyButtonPressed;
        private bool _releaseRequested;

        public ExhibitInputBlocker() : this(IsAnyButtonPressed) { }

        /// <summary>押下中の判定を差し替える (テストで実デバイスの状態に依存しないため)</summary>
        public ExhibitInputBlocker(Func<bool> isAnyButtonPressed)
        {
            _isAnyButtonPressed = isAnyButtonPressed;
        }

        public void Block()
        {
            if (IsBlocking) return;

            IsBlocking = true;
            _releaseRequested = false;
            // uGUI の入力モジュールもアクション経由なので、ゲーム側の action map と合わせてここで止まる
            InputSystem.ListEnabledActions(_suspended);
            foreach (var action in _suspended) action.Disable();
        }

        /// <summary>押下中のボタンが無くなった時点で <see cref="Tick"/> が入力を戻す</summary>
        public void RequestRelease() => _releaseRequested = true;

        public void Tick()
        {
            if (!IsBlocking || !_releaseRequested) return;
            // 押下中に戻すと、initialStateCheck を持つアクション (uGUI の Click 等) が押下をそのまま拾う
            if (_isAnyButtonPressed()) return;

            ReleaseNow();
        }

        public void ReleaseNow()
        {
            if (!IsBlocking) return;

            foreach (var action in _suspended)
            {
                if (IsAlive(action)) action.Enable();
            }
            _suspended.Clear();
            IsBlocking = false;
            _releaseRequested = false;
        }

        // シーン遷移で破棄された入力モジュール等のアセットに属するアクションは戻さない
        private static bool IsAlive(InputAction action)
        {
            var asset = action.actionMap?.asset;
            return asset is null || asset;
        }

        private static bool IsAnyButtonPressed()
        {
            foreach (var device in InputSystem.devices)
            {
                foreach (var control in device.allControls)
                {
                    if (control is ButtonControl { isPressed: true }) return true;
                }
            }
            return false;
        }
    }
}
