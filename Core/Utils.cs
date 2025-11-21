using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ゲーム開発で頻繁に使用される汎用ユーティリティ関数
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// 指定時間待機するが、条件が満たされた場合は即座に終了
        /// </summary>
        /// <param name="delayTime">待機時間(ms)</param>
        /// <param name="condition">スキップ条件</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public static async UniTask WaitOrSkip(int delayTime, Func<bool> condition, CancellationToken cancellationToken = default)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var delayTask = UniTask.Delay(delayTime, cancellationToken: cts.Token);
                var conditionTask = UniTask.WaitUntil(condition, cancellationToken: cts.Token);
                
                var result = await UniTask.WhenAny(delayTask, conditionTask);

                if (result == 1) cts.Cancel();
            }
        }
        
        /// <summary>
        /// GameObjectにEventTriggerを動的に追加
        /// </summary>
        /// <param name="obj">対象オブジェクト</param>
        /// <param name="action">実行するアクション</param>
        /// <param name="type">イベントタイプ</param>
        /// <param name="removeExisting">既存の同タイプイベントを削除するか</param>
        public static void AddEventToObject(GameObject obj, System.Action action, EventTriggerType type, bool removeExisting = true)
        {
            var trigger = obj.GetComponent<EventTrigger>();
            if (!trigger)
            {
                trigger = obj.AddComponent<EventTrigger>();
            }
            
            if(removeExisting) trigger.triggers.RemoveAll(x => x.eventID == type);
            
            var entry = new EventTrigger.Entry {eventID = type};
            entry.callback.AddListener((data) => action());
            trigger.triggers.Add(entry);
        }
        
        /// <summary>
        /// GameObjectから全てのイベントを削除
        /// </summary>
        /// <param name="obj">対象オブジェクト</param>
        public static void RemoveAllEventFromObject(GameObject obj)
        {
            var trigger = obj.GetComponent<EventTrigger>();
            if (trigger)
                trigger.triggers.Clear();
            var button = obj.GetComponent<Button>();
            if (button)
                button.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// カスタムOutBounceイージング関数（最初の部分をゆっくりに調整）
        /// バウンドアニメーションで使用される、跳ねる動きを表現するイージング関数
        /// ExtendedMethods.CreateBounceMotionで使用されます
        /// </summary>
        public static float EaseOutBounce(float t)
        {
            if (t < 1f / 2.5f)
                return 6f * t * t;
            if (t < 2f / 2.5f)
                return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
            if (t < 2.25f / 2.5f)
                return 7.5625f * (t -= 2.125f / 2.75f) * t + 0.9375f;
            return 7.5625f * (t -= 2.4375f / 2.75f) * t + 0.984375f;
        }
    }
}