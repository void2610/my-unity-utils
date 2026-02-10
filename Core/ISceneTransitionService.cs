using Cysharp.Threading.Tasks;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// シーン遷移サービスの抽象インターフェース
    /// </summary>
    public interface ISceneTransitionService
    {
        /// <summary>現在フェード中かどうか</summary>
        bool IsFading { get; }

        /// <summary>クロスフェード付きでシーンを遷移</summary>
        UniTask TransitionToSceneWithFade(string sceneName, float fadeDuration = 0.5f);
    }
}
