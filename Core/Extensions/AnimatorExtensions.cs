using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Animator の状態遷移待機をまとめる拡張メソッド。
    /// </summary>
    public static class AnimatorExtensions
    {
        public static async UniTask WaitForStateAsync(this Animator animator, string stateName, int layer, CancellationToken cancellationToken)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            while (!animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            while (animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}
