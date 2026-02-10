using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 全入力を監視してアイドル時間を計測するサービス
    /// InputSystem.onEventを使用し、timeScale非依存で動作する
    /// </summary>
    public class IdleDetector : IDisposable
    {
        public float IdleSeconds => Time.realtimeSinceStartup - _lastInputTime;

        private float _lastInputTime;

        public IdleDetector()
        {
            _lastInputTime = Time.realtimeSinceStartup;
            InputSystem.onEvent += OnInputEvent;
        }

        public void ResetIdleTimer() => _lastInputTime = Time.realtimeSinceStartup;

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            // State/DeltaStateイベントのみ処理（デバイス設定変更等は無視）
            var eventType = eventPtr.type;
            if (eventType != StateEvent.Type && eventType != DeltaStateEvent.Type)
                return;

            // 実際に値が変化したコントロールがあるかチェック
            using var enumerator = eventPtr.EnumerateControls(
                InputControlExtensions.Enumerate.IncludeNonLeafControls |
                InputControlExtensions.Enumerate.IncludeSyntheticControls |
                InputControlExtensions.Enumerate.IgnoreControlsInCurrentState |
                InputControlExtensions.Enumerate.IgnoreControlsInDefaultState
            ).GetEnumerator();

            if (enumerator.MoveNext())
            {
                _lastInputTime = Time.realtimeSinceStartup;
            }
        }

        public void Dispose()
        {
            InputSystem.onEvent -= OnInputEvent;
        }
    }
}
