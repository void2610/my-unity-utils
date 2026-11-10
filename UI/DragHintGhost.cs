using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ドラッグ操作のお手本。掴む対象の見た目を写した半透明の複製が、ドロップ先まで滑って消える動きを繰り返す
    /// 見た目は掴む対象によって決まるので Prefab を持たず、<see cref="Create"/> で作って再生のたびに掴む対象から写す
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DragHintGhost : MonoBehaviour
    {
        private const float FADE_DURATION = 0.25f;
        private const float HOLD_DURATION = 0.25f;
        private const float INTERVAL_DURATION = 0.6f;

        private readonly List<Image> _imageLayers = new();
        private readonly List<TextMeshProUGUI> _textLayers = new();
        private float _ghostAlpha;
        private float _slideDuration;
        private float _arcHeight;
        private CanvasGroup _group;
        private RectTransform _rect;
        private CancellationTokenSource _cts;
        private int _remainingCycles;

        /// <summary>parent の最前面に作る。arcHeight は軌道の膨らみ (親のローカル単位) で、正なら進行方向の左側、負なら右側へ曲げる</summary>
        public static DragHintGhost Create(Transform parent, float ghostAlpha = 0.5f, float slideDuration = 1.6f, float arcHeight = 100f)
        {
            var go = new GameObject(nameof(DragHintGhost), typeof(RectTransform), typeof(CanvasGroup), typeof(DragHintGhost)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            var ghost = go.GetComponent<DragHintGhost>();
            ghost._ghostAlpha = ghostAlpha;
            ghost._slideDuration = slideDuration;
            ghost._arcHeight = arcHeight;
            return ghost;
        }

        /// <summary>掴む対象の見た目と位置から、止めるまで繰り返す。再呼び出しは前の再生を打ち切ってやり直す</summary>
        public void Play(RectTransform source, Vector3 toWorldPosition)
        {
            Stop();
            _remainingCycles = int.MaxValue;
            CopyVisual(source);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            LoopAsync(source.position, toWorldPosition, _cts.Token).Forget();
        }

        /// <summary>今の周を 1 回目として数え、あと cycles 回目の周を終えたら止める</summary>
        public void FinishAfter(int cycles) => _remainingCycles = Math.Max(cycles, 1);

        public void Stop()
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
            _group.alpha = 0f;
        }

        private void CopyVisual(RectTransform source)
        {
            _rect.pivot = source.pivot;
            _rect.sizeDelta = source.rect.size;
            _rect.localScale = source.localScale;

            var images = source.GetComponentsInChildren<Image>();
            PrepareLayers(_imageLayers, images.Length);
            for (var i = 0; i < images.Length; i++) CopyImage(source, images[i], _imageLayers[i]);

            var texts = source.GetComponentsInChildren<TextMeshProUGUI>();
            PrepareLayers(_textLayers, texts.Length);
            for (var i = 0; i < texts.Length; i++) CopyText(source, texts[i], _textLayers[i]);
        }

        // 写す数は掴む対象ごとに違うので、足りない分だけ作って使い回し、余りは隠す
        private void PrepareLayers<T>(List<T> layers, int count) where T : Graphic
        {
            while (layers.Count < count)
            {
                var go = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(T)) { layer = gameObject.layer };
                go.transform.SetParent(transform, false);
                layers.Add(go.GetComponent<T>());
            }
            for (var i = 0; i < layers.Count; i++) layers[i].gameObject.SetActive(i < count);
        }

        private static void CopyImage(RectTransform source, Image from, Image to)
        {
            to.enabled = from.enabled;
            to.sprite = from.sprite;
            to.color = from.color;
            to.material = from.material;
            to.type = from.type;
            to.preserveAspect = from.preserveAspect;
            to.pixelsPerUnitMultiplier = from.pixelsPerUnitMultiplier;
            Place(source, from.rectTransform, to.rectTransform);
        }

        private static void CopyText(RectTransform source, TextMeshProUGUI from, TextMeshProUGUI to)
        {
            to.enabled = from.enabled;
            to.font = from.font;
            to.fontSharedMaterial = from.fontSharedMaterial;
            to.enableAutoSizing = false;
            to.fontSize = from.fontSize;
            to.fontStyle = from.fontStyle;
            to.alignment = from.alignment;
            to.textWrappingMode = from.textWrappingMode;
            to.color = from.color;
            to.text = from.text;
            Place(source, from.rectTransform, to.rectTransform);
        }

        // 写し先はゴースト直下に並ぶので、掴む対象の中での入れ子を掴む対象からの相対に畳んで置く
        private static void Place(RectTransform source, RectTransform from, RectTransform to)
        {
            to.pivot = from.pivot;
            to.anchorMin = to.anchorMax = new Vector2(0.5f, 0.5f);
            to.sizeDelta = from.rect.size;
            to.localPosition = source.InverseTransformPoint(from.position);
            to.localRotation = Quaternion.Inverse(source.rotation) * from.rotation;
            var sourceScale = source.lossyScale;
            var fromScale = from.lossyScale;
            to.localScale = new Vector3(fromScale.x / sourceScale.x, fromScale.y / sourceScale.y, 1f);
        }

        private async UniTaskVoid LoopAsync(Vector3 from, Vector3 to, CancellationToken ct)
        {
            do
            {
                _rect.position = from;
                var cancelled = await RunCycleAsync(from, to, ct).SuppressCancellationThrow();
                if (cancelled) return;
            } while (--_remainingCycles > 0);

            // 自然終了。Stop が呼ぶ Cancel は自分自身を打ち切らない
            if (ct.IsCancellationRequested) return;
            _cts.Dispose();
            _cts = null;
        }

        private async UniTask RunCycleAsync(Vector3 from, Vector3 to, CancellationToken ct)
        {
            await LMotion.Create(0f, _ghostAlpha, FADE_DURATION).WithEase(Ease.OutQuad)
                .Bind(_group, (v, g) => g.alpha = v).AddTo(gameObject).ToUniTask(ct);
            var parent = _rect.parent;
            var localFrom = parent.InverseTransformPoint(from);
            var localTo = parent.InverseTransformPoint(to);
            var control = ArcControlPoint(localFrom, localTo);
            await LMotion.Create(0f, 1f, _slideDuration).WithEase(Ease.InOutCubic)
                .Bind(t => _rect.localPosition = QuadraticBezier(localFrom, control, localTo, t)).AddTo(gameObject).ToUniTask(ct);
            await UniTask.Delay(TimeSpan.FromSeconds(HOLD_DURATION), cancellationToken: ct);
            await LMotion.Create(_ghostAlpha, 0f, FADE_DURATION).WithEase(Ease.InQuad)
                .Bind(_group, (v, g) => g.alpha = v).AddTo(gameObject).ToUniTask(ct);
            await UniTask.Delay(TimeSpan.FromSeconds(INTERVAL_DURATION), cancellationToken: ct);
        }

        private Vector3 ArcControlPoint(Vector3 from, Vector3 to)
        {
            var direction = (to - from).normalized;
            var normal = new Vector3(-direction.y, direction.x, 0f);
            return (from + to) * 0.5f + normal * _arcHeight;
        }

        private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            var u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _rect = (RectTransform)transform;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void OnDestroy()
        {
            _cts?.Dispose();
        }
    }
}
