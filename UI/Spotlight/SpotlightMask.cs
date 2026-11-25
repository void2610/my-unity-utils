using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 全面の暗幕に、境界のぼけた穴を開けて見せたい場所を照らす (チュートリアルのクリック位置の誘導など)。
    /// 暗幕の Image に付け、SpotlightMask シェーダのマテリアルを当てる。穴の中のクリックは暗幕を素通りして下へ届く。
    /// hole の RectTransform の矩形がそのまま明るく抜け、その外側 edgeWidth の帯で暗幕へなだらかに戻る。回転・拡縮もそのまま反映する
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class SpotlightMask : MonoBehaviour, ICanvasRaycastFilter
    {
        [Tooltip("穴の位置と大きさを決める RectTransform。暗幕の子にしない (暗幕と一緒に動かさない) 方が扱いやすい")]
        [SerializeField] private RectTransform hole;
        [Tooltip("穴の外側に付くぼかしの幅 (穴のローカル座標)。マスク画像の 9 スライスのボーダーがこの幅に描かれる")]
        [SerializeField] private float edgeWidth = 45f;

        private static readonly int WORLD_TO_HOLE = Shader.PropertyToID("_WorldToHole");
        private static readonly int HOLE_RECT = Shader.PropertyToID("_HoleRect");
        private static readonly int HOLE_EDGE = Shader.PropertyToID("_HoleEdge");

        private Image _image;
        private Material _material;
        private MotionHandle _positionHandle;
        private MotionHandle _sizeHandle;

        /// <summary>穴が開いているか (閉じているときは全面を暗くする)</summary>
        public bool IsHoleOpen => hole.gameObject.activeSelf;

        public void OpenHole() => hole.gameObject.SetActive(true);

        public void CloseHole()
        {
            CancelMove();
            hole.gameObject.SetActive(false);
        }

        /// <summary>目標とその子をまとめて囲む穴を、目標と同じ向きで padding だけ広げて開ける。目標が動いている間は呼ばない (呼んだ時点の位置に開く)</summary>
        public void FitHole(RectTransform target, float padding)
        {
            CancelMove();
            var parent = (RectTransform)hole.parent;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(target);
            hole.localPosition = parent.InverseTransformPoint(target.TransformPoint(bounds.center));
            // 傾いた目標を軸に沿った矩形で囲むと角が欠けるので、目標と同じ向きに回す
            hole.localRotation = Quaternion.Inverse(parent.rotation) * target.rotation;
            var scale = target.lossyScale.x / parent.lossyScale.x;
            hole.sizeDelta = (Vector2)bounds.size * scale + Vector2.one * padding;
            OpenHole();
        }

        /// <summary>穴を anchoredPosition / sizeDelta の位置・大きさへ滑らかに動かして開ける。回転は戻す</summary>
        public void MoveHole(Vector2 anchoredPosition, Vector2 size, float duration, Ease ease = Ease.OutCubic)
        {
            CancelMove();
            hole.localRotation = Quaternion.identity;
            _positionHandle = LMotion.Create(hole.anchoredPosition, anchoredPosition, duration).WithEase(ease)
                .BindToAnchoredPosition(hole).AddTo(this);
            _sizeHandle = LMotion.Create(hole.sizeDelta, size, duration).WithEase(ease)
                .BindToSizeDelta(hole).AddTo(this);
            OpenHole();
        }

        // 穴の中だけクリックを下へ通す
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!IsHoleOpen) return true;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hole, screenPoint, eventCamera, out var local)) return true;
            return !hole.rect.Contains(local);
        }

        private void CancelMove()
        {
            _positionHandle.TryCancel();
            _sizeHandle.TryCancel();
        }

        private void Awake()
        {
            _image = GetComponent<Image>();
            // 共有マテリアルへ書くとアセットが汚れ、同じマテリアルの別の暗幕とも穴を取り合うので専用に複製する
            _material = new Material(_image.material);
            _image.material = _material;
        }

        // 穴は Tween やレイアウトで毎フレーム形が変わるので、描画直前に合わせる
        private void LateUpdate()
        {
            if (!IsHoleOpen)
            {
                _material.SetVector(HOLE_RECT, Vector4.zero);
                return;
            }
            // ぼかしを穴の外へ足す。内側に取ると、穴の大きさで指した場所の縁まで暗くなる
            var rect = hole.rect;
            var drawn = new Rect(rect.xMin - edgeWidth, rect.yMin - edgeWidth, rect.width + edgeWidth * 2f, rect.height + edgeWidth * 2f);
            _material.SetMatrix(WORLD_TO_HOLE, hole.worldToLocalMatrix);
            _material.SetVector(HOLE_RECT, new Vector4(drawn.xMin, drawn.yMin, drawn.width, drawn.height));
            _material.SetVector(HOLE_EDGE, new Vector4(edgeWidth / drawn.width, edgeWidth / drawn.height));
        }

        private void OnDestroy()
        {
            CancelMove();
            Destroy(_material);
        }
    }
}
