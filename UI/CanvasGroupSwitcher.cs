using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// CanvasGroupの表示/非表示を管理するクラス（LitMotionフェード付き）
    /// MVPパターンでのUI状態管理に最適
    /// </summary>
    public class CanvasGroupSwitcher
    {
        private readonly List<CanvasGroup> _canvasGroups;
        private readonly Dictionary<CanvasGroup, MotionHandle> _activeMotions = new();
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="canvasGroups">管理対象のCanvasGroupリスト</param>
        /// <param name="initialVisibleCanvas">初期表示するCanvasGroup名（nullの場合は全て非表示）</param>
        public CanvasGroupSwitcher(List<CanvasGroup> canvasGroups, string initialVisibleCanvas = null)
        {
            _canvasGroups = canvasGroups;
            
            foreach (var cg in _canvasGroups)
            {
                if (cg.name == initialVisibleCanvas)
                {
                    // 初期表示するキャンバスは即座に表示状態に設定
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                else
                {
                    EnableCanvasGroupAsync(cg.name, false).Forget();
                }
            }
        }
        
        /// <summary>
        /// 現在表示されているCanvasGroupのGameObjectを取得
        /// </summary>
        /// <returns>表示中のCanvasGroupのGameObject（なければnull）</returns>
        public GameObject GetTopCanvasGroup() => _canvasGroups.Find(c => c.alpha > 0)?.gameObject;
        
        /// <summary>
        /// CanvasGroupの表示状態を切り替え（同期）
        /// </summary>
        /// <param name="canvasName">対象CanvasGroup名</param>
        /// <param name="enable">表示するか</param>
        public void EnableCanvasGroup(string canvasName, bool enable) => EnableCanvasGroupAsync(canvasName, enable).Forget();
        
        /// <summary>
        /// CanvasGroupの表示状態を切り替え（非同期）
        /// </summary>
        /// <param name="canvasName">対象CanvasGroup名</param>
        /// <param name="enable">表示するか</param>
        /// <param name="duration">フェード時間</param>
        /// <returns>完了待機可能なUniTask</returns>
        public async UniTask EnableCanvasGroupAsync(string canvasName, bool enable, float duration = 0.15f)
        {
            var cg = _canvasGroups.Find(c => c.name == canvasName);
            if (!cg) return;
            
            // 進行中のアニメーションをキャンセル
            if (_activeMotions.TryGetValue(cg, out var existingMotion) && existingMotion.IsActive())
            {
                existingMotion.Cancel();
            }
            
            if (enable)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
                
                var motion = LMotion.Create(cg.alpha, 1f, duration)
                    .WithEase(Ease.OutQuad)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(alpha => cg.alpha = alpha);
                    
                _activeMotions[cg] = motion;
                await motion.ToUniTask();
            }
            else
            {
                var motion = LMotion.Create(cg.alpha, 0f, duration)
                    .WithEase(Ease.OutQuad)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(alpha => cg.alpha = alpha);
                    
                _activeMotions[cg] = motion;
                await motion.ToUniTask();
                
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            
            // アニメーション完了後、辞書から削除
            _activeMotions.Remove(cg);
        }
        
        /// <summary>
        /// 指定したCanvasGroup以外をすべて非表示にする
        /// </summary>
        /// <param name="canvasName">表示するCanvasGroup名</param>
        public void ShowOnly(string canvasName)
        {
            foreach (var cg in _canvasGroups)
            {
                EnableCanvasGroup(cg.name, cg.name == canvasName);
            }
        }
        
        /// <summary>
        /// 指定したCanvasGroup以外をすべて非表示にする（非同期）
        /// </summary>
        /// <param name="canvasName">表示するCanvasGroup名</param>
        /// <param name="duration">フェード時間</param>
        /// <returns>完了待機可能なUniTask</returns>
        public async UniTask ShowOnlyAsync(string canvasName, float duration = 0.15f)
        {
            var tasks = new List<UniTask>();
            foreach (var cg in _canvasGroups)
            {
                tasks.Add(EnableCanvasGroupAsync(cg.name, cg.name == canvasName, duration));
            }
            await UniTask.WhenAll(tasks);
        }
        
        /// <summary>
        /// すべてのCanvasGroupを非表示にする
        /// </summary>
        public void HideAll()
        {
            foreach (var cg in _canvasGroups)
            {
                EnableCanvasGroup(cg.name, false);
            }
        }
        
        /// <summary>
        /// すべてのCanvasGroupを非表示にする（非同期）
        /// </summary>
        /// <param name="duration">フェード時間</param>
        /// <returns>完了待機可能なUniTask</returns>
        public async UniTask HideAllAsync(float duration = 0.15f)
        {
            var tasks = new List<UniTask>();
            foreach (var cg in _canvasGroups)
            {
                tasks.Add(EnableCanvasGroupAsync(cg.name, false, duration));
            }
            await UniTask.WhenAll(tasks);
        }
        
        /// <summary>
        /// 指定したCanvasGroupが表示中かどうかを確認
        /// </summary>
        /// <param name="canvasName">確認するCanvasGroup名</param>
        /// <returns>表示中の場合true</returns>
        public bool IsVisible(string canvasName)
        {
            var cg = _canvasGroups.Find(c => c.name == canvasName);
            return cg != null && cg.alpha > 0;
        }
        
        /// <summary>
        /// すべてのアニメーションを停止し、リソースをクリーンアップする
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in _activeMotions)
            {
                if (kvp.Value.IsActive())
                {
                    kvp.Value.Cancel();
                }
            }
            _activeMotions.Clear();
        }
    }
}