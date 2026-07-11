using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// TextMeshPro のテキストを LitMotion で 1 文字ずつ順次表示するだけの汎用コンポーネント。
    /// 頂点カラーのアルファを文字ごとに上げるため、出現時に 1 文字ずつフェードインする (リッチテキストのタグも壊さない)。
    /// 改行位置では <see cref="pauseAtNewline"/> 秒だけ送りを止める。
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TMProSequentialReveal : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float secondsPerChar = 0.03f;
        [SerializeField] private Ease ease = Ease.Linear;
        [SerializeField] private bool ignoreTimeScale;

        /// <summary>1 文字が透明→不透明になりきるまでにかかる「文字数ぶん」の幅。大きいほどふわっと重なって出る。</summary>
        [SerializeField] private float fadeCharSpan = 4f;

        /// <summary>改行文字を送り終えたときに止まる秒数。</summary>
        [SerializeField] private float pauseAtNewline = 0.4f;

        /// <summary>1 文字ぶんの出現アニメを表す。<paramref name="t"/> はその文字の表示進捗 (0=未表示, 1=表示完了)。頂点カラー/座標を書き換える。</summary>
        public delegate void CharacterTween(TextMeshProUGUI text, int charIndex, float t);

        /// <summary>表示対象の TextMeshPro (呼び出し側が全文表示状態を読みたいとき用)</summary>
        public TextMeshProUGUI Text => text;

        /// <summary>各文字の出現アニメ。未設定ならアルファフェード (<see cref="AlphaFade"/>) を使う。外部から差し替え可能。</summary>
        public CharacterTween Tween { get; set; }

        private readonly List<int> _newlineIndices = new();
        private CancellationTokenSource _cts;

        /// <summary>既定の出現アニメ: 頂点アルファを 0→255 に補間する。</summary>
        public static void AlphaFade(TextMeshProUGUI text, int charIndex, float t)
        {
            var info = text.textInfo;
            var ci = info.characterInfo[charIndex];
            var alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(t) * byte.MaxValue);
            var colors = info.meshInfo[ci.materialReferenceIndex].colors32;
            for (var v = 0; v < 4; v++) colors[ci.vertexIndex + v].a = alpha;
        }

        /// <summary>プリセット: 文字中心から t 倍にスケールしつつフェードして出す (ポンっと弾ける)。</summary>
        public static void ScalePop(TextMeshProUGUI text, int charIndex, float t)
        {
            var info = text.textInfo;
            var ci = info.characterInfo[charIndex];
            var verts = info.meshInfo[ci.materialReferenceIndex].vertices;
            var vi = ci.vertexIndex;
            var center = (ci.bottomLeft + ci.topRight) * 0.5f;
            var s = Mathf.Clamp01(t);
            verts[vi + 0] = center + (ci.bottomLeft - center) * s;
            verts[vi + 1] = center + (ci.topLeft - center) * s;
            verts[vi + 2] = center + (ci.topRight - center) * s;
            verts[vi + 3] = center + (ci.bottomRight - center) * s;
            AlphaFade(text, charIndex, t);
        }

        /// <summary>プリセット生成: 下から <paramref name="rise"/> px せり上がりつつフェードして出す tween を返す。</summary>
        public static CharacterTween SlideUp(float rise = 40f)
        {
            return (text, charIndex, t) =>
            {
                var info = text.textInfo;
                var ci = info.characterInfo[charIndex];
                var verts = info.meshInfo[ci.materialReferenceIndex].vertices;
                var vi = ci.vertexIndex;
                var offset = Vector3.up * (-rise * (1f - Mathf.Clamp01(t))); // t=0 で下、t=1 で定位置
                verts[vi + 0] = ci.bottomLeft + offset;
                verts[vi + 1] = ci.topLeft + offset;
                verts[vi + 2] = ci.topRight + offset;
                verts[vi + 3] = ci.bottomRight + offset;
                AlphaFade(text, charIndex, t);
            };
        }

        /// <summary>指定テキストを先頭から順次フェード表示する。進行中の演出があれば打ち切って上書きする。</summary>
        public void Play(string message) => StartReveal(message).Forget();

        /// <summary>順次表示の完了を待つ (キャンセル時は最後まで表示して抜ける)。</summary>
        public async UniTask PlayAsync(string message, CancellationToken ct)
        {
            var token = ResetToken(ct);
            try
            {
                await RevealCore(message, token);
            }
            catch (OperationCanceledException)
            {
                Complete();
                throw;
            }
        }

        /// <summary>進行中の演出を打ち切り、全文を即座に不透明で表示する。</summary>
        public void Complete()
        {
            _cts?.Cancel();
            ApplyReveal(text.textInfo.characterCount + Mathf.Max(0.01f, fadeCharSpan));
        }

        private async UniTaskVoid StartReveal(string message)
        {
            var token = ResetToken(CancellationToken.None);
            try
            {
                await RevealCore(message, token);
            }
            catch (OperationCanceledException)
            {
                // 上書き再生や破棄によるキャンセルは正常系
            }
        }

        // 改行区切りでヘッドを進め、各改行後に pauseAtNewline 秒止める
        private async UniTask RevealCore(string message, CancellationToken ct)
        {
            text.maxVisibleCharacters = int.MaxValue; // 可視制御はアルファで行うため字形は全部出す
            text.text = message;
            text.ForceMeshUpdate();
            var total = text.textInfo.characterCount;
            if (total <= 0) return;

            ApplyReveal(0f); // 開始時は全文字を透明に
            var span = Mathf.Max(0.01f, fadeCharSpan);

            CollectNewlineIndices(total);
            var head = 0f;
            foreach (var stop in _newlineIndices)
            {
                var target = stop + 1; // 改行文字を通過するところまで送る
                await AnimateHead(head, target, ct);
                head = target;
                if (pauseAtNewline > 0f)
                {
                    var delayType = ignoreTimeScale ? Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime : Cysharp.Threading.Tasks.DelayType.DeltaTime;
                    await UniTask.Delay(TimeSpan.FromSeconds(pauseAtNewline), delayType, PlayerLoopTiming.Update, ct);
                }
            }
            // 末尾の文字もフェードしきるよう total+span まで送る
            await AnimateHead(head, total + span, ct);
        }

        private UniTask AnimateHead(float from, float to, CancellationToken ct)
        {
            if (to <= from) return UniTask.CompletedTask;
            return LMotion.Create(from, to, secondsPerChar * (to - from))
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .Bind(ApplyReveal)
                .AddTo(gameObject)
                .ToUniTask(cancellationToken: ct);
        }

        private void CollectNewlineIndices(int total)
        {
            _newlineIndices.Clear();
            var info = text.textInfo;
            for (var i = 0; i < total; i++)
            {
                if (info.characterInfo[i].character == '\n') _newlineIndices.Add(i);
            }
        }

        // ヘッド位置に応じて各文字の出現アニメを適用する。head を越えた手前 span 文字が t=0→1 になる
        private void ApplyReveal(float head)
        {
            var span = Mathf.Max(0.01f, fadeCharSpan);
            var tween = Tween ?? AlphaFade;
            var info = text.textInfo;
            for (var i = 0; i < info.characterCount; i++)
            {
                if (!info.characterInfo[i].isVisible) continue;
                tween(text, i, Mathf.Clamp01((head - i) / span));
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.All); // 差し込み tween が座標も動かせるよう全頂点データを反映する
        }

        private CancellationToken ResetToken(CancellationToken external)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy(), external);
            return _cts.Token;
        }

        private void Reset() => text = GetComponent<TextMeshProUGUI>();
    }
}
