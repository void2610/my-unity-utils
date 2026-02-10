using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// セッション時間制限を管理するサービス
    /// 制限時間到達で指定シーンへ遷移する
    /// </summary>
    public class ExhibitSessionTimerService : IStartable, ITickable
    {
        private readonly ExhibitSettings _settings;
        private readonly IdleDetector _idleDetector;
        private readonly ISceneTransitionService _sceneTransitionService;

        private float _sessionStartTime;
        private bool _hasTriggered;

        public ExhibitSessionTimerService(
            ExhibitSettings settings,
            IdleDetector idleDetector,
            ISceneTransitionService sceneTransitionService)
        {
            _settings = settings;
            _idleDetector = idleDetector;
            _sceneTransitionService = sceneTransitionService;
        }

        public void Tick()
        {
            if (!_settings.EnableSessionTimeLimit || _hasTriggered) return;
            if (SceneManager.GetActiveScene().name == _settings.SessionLimitSceneName) return;
            if (_sceneTransitionService.IsFading) return;

            var elapsed = Time.realtimeSinceStartup - _sessionStartTime;
            if (elapsed >= _settings.SessionTimeLimitSeconds)
            {
                _hasTriggered = true;
                TransitionToSessionLimit().Forget();
            }
        }

        /// <summary>
        /// セッションタイマーをリセットする（感謝画面からタイトルに戻った際に使用）
        /// </summary>
        public void ResetSession()
        {
            _sessionStartTime = Time.realtimeSinceStartup;
            _hasTriggered = false;
        }

        private async UniTaskVoid TransitionToSessionLimit()
        {
            await _sceneTransitionService.TransitionToSceneWithFade(_settings.SessionLimitSceneName);
            _idleDetector.ResetIdleTimer();
        }

        public void Start() => _sessionStartTime = Time.realtimeSinceStartup;
    }
}
