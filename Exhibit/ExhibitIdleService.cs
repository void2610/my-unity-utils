using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// アイドル検知→オーバーレイ表示→指定シーン遷移を管理するサービス
    /// </summary>
    public class ExhibitIdleService : IStartable, ITickable, IDisposable
    {
        private enum State
        {
            Monitoring,
            OverlayShown,
            Transitioning
        }

        private readonly ExhibitSettings _settings;
        private readonly IdleDetector _idleDetector;
        private readonly ISceneTransitionService _sceneTransitionService;
        private readonly IExhibitOverlay _overlay;

        private State _state = State.Monitoring;
        private float _overlayShownTime;

        public ExhibitIdleService(
            ExhibitSettings settings,
            IdleDetector idleDetector,
            ISceneTransitionService sceneTransitionService,
            IExhibitOverlay overlay)
        {
            _settings = settings;
            _idleDetector = idleDetector;
            _sceneTransitionService = sceneTransitionService;
            _overlay = overlay;
        }

        public void Tick()
        {
            if (!_settings.EnableIdlePauseAndReturn) return;

            // スキップ対象のシーンではスキップ
            var currentSceneName = SceneManager.GetActiveScene().name;
            if (_settings.IdleSkipSceneNames.Contains(currentSceneName)) return;

            // フェード中はスキップ
            if (_sceneTransitionService.IsFading) return;

            switch (_state)
            {
                case State.Monitoring:
                    HandleMonitoring();
                    break;
                case State.OverlayShown:
                    HandleOverlayShown();
                    break;
            }
        }

        private void HandleMonitoring()
        {
            if (_idleDetector.IdleSeconds >= _settings.IdleToPauseSeconds)
            {
                _overlay.Show();
                _overlayShownTime = Time.realtimeSinceStartup;
                _state = State.OverlayShown;
            }
        }

        private void HandleOverlayShown()
        {
            // 入力があったらオーバーレイを閉じてMonitoringに戻る
            if (_idleDetector.IdleSeconds < 1f)
            {
                _overlay.Hide();
                _state = State.Monitoring;
                return;
            }

            // さらにpauseToTitleSeconds経過したら指定シーンへ
            var elapsed = Time.realtimeSinceStartup - _overlayShownTime;
            if (elapsed >= _settings.PauseToTitleSeconds)
            {
                _state = State.Transitioning;
                TransitionToIdleReturn().Forget();
            }
        }

        private async UniTaskVoid TransitionToIdleReturn()
        {
            _overlay.Hide();
            await _sceneTransitionService.TransitionToSceneWithFade(_settings.IdleReturnSceneName);
            _idleDetector.ResetIdleTimer();
            _state = State.Monitoring;
        }

        public void Start() { }

        public void Dispose() { }
    }
}
