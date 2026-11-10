using System;
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
    /// 絵は再生のたびに掴む対象から写すので、画面ごとの絵は要らない。写し先の Image / TMP を空の子として必要な数だけ用意する
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DragHintGhost : MonoBehaviour
    {
        [Header("写し先 (掴む対象の描画順に使う)")]
        [SerializeField] private Image[] imageLayers;
        [SerializeField] private TMP_Text[] textLayers;

        [Header("動き")]
        [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.5f;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float slideDuration = 1.6f;
        // 軌道の膨らみ (親のローカル単位)。正で進行方向の左側、負で右側へ曲げる
        [SerializeField] private float arcHeight = 100f;
        [SerializeField] private float holdDuration = 0.25f;
        [SerializeField] private float intervalDuration = 0.6f;

        private CanvasGroup _group;
        private RectTransform _rect;
        private CancellationTokenSource _cts;
        private int _remainingCycles;

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
            for (var i = 0; i < imageLayers.Length; i++) imageLayers[i].gameObject.SetActive(i < images.Length);
            for (var i = 0; i < images.Length; i++) CopyImage(source, images[i], imageLayers[i]);

            var texts = source.GetComponentsInChildren<TMP_Text>();
            for (var i = 0; i < textLayers.Length; i++) textLayers[i].gameObject.SetActive(i < texts.Length);
            for (var i = 0; i < texts.Length; i++) CopyText(source, texts[i], textLayers[i]);
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

        private static void CopyText(RectTransform source, TMP_Text from, TMP_Text to)
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
            await LMotion.Create(0f, ghostAlpha, fadeDuration).WithEase(Ease.OutQuad)
                .Bind(_group, (v, g) => g.alpha = v).AddTo(gameObject).ToUniTask(ct);
            var parent = _rect.parent;
            var localFrom = parent.InverseTransformPoint(from);
            var localTo = parent.InverseTransformPoint(to);
            var control = ArcControlPoint(localFrom, localTo);
            await LMotion.Create(0f, 1f, slideDuration).WithEase(Ease.InOutCubic)
                .Bind(t => _rect.localPosition = QuadraticBezier(localFrom, control, localTo, t)).AddTo(gameObject).ToUniTask(ct);
            await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: ct);
            await LMotion.Create(ghostAlpha, 0f, fadeDuration).WithEase(Ease.InQuad)
                .Bind(_group, (v, g) => g.alpha = v).AddTo(gameObject).ToUniTask(ct);
            await UniTask.Delay(TimeSpan.FromSeconds(intervalDuration), cancellationToken: ct);
        }

        private Vector3 ArcControlPoint(Vector3 from, Vector3 to)
        {
            var direction = (to - from).normalized;
            var normal = new Vector3(-direction.y, direction.x, 0f);
            return (from + to) * 0.5f + normal * arcHeight;
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
