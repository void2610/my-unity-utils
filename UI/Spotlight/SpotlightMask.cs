using System;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 全面の暗幕に、境界のぼけた穴を開けて見せたい場所を照らす (チュートリアルのクリック位置の誘導など)。
    /// 暗幕の Image に付け、SpotlightMask シェーダのマテリアルを当てる。穴の中のクリックは暗幕を素通りして下へ届く。
    /// hole の RectTransform の矩形がそのまま明るく抜け、その外側 edgeWidth の帯で暗幕へなだらかに戻る。回転・拡縮もそのまま反映する。
    /// 暗幕の出し入れと穴の開閉はフェードし、開いている穴は次の場所へ滑らせて動かす
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class SpotlightMask : MonoBehaviour, ICanvasRaycastFilter
    {
        [Tooltip("穴の位置と大きさを決める RectTransform。暗幕の子にしない (暗幕と一緒に消さない) こと")]
        [SerializeField] private RectTransform hole;
        [Tooltip("穴の外側に付くぼかしの幅 (穴のローカル座標)。この距離で暗幕の濃さに戻る")]
        [SerializeField] private float edgeWidth = 65f;
        [Tooltip("暗幕の出し入れと穴の開閉のフェードの秒数")]
        [SerializeField] private float fadeDuration = 0.25f;
        [Tooltip("開いている穴を次の場所へ動かす秒数")]
        [SerializeField] private float moveDuration = 0.25f;
        [SerializeField] private Ease ease = Ease.OutCubic;

        // 誘導は一時停止中 (timeScale=0) のメニューの上にも出すので、実時間で動かす
        private static readonly IMotionScheduler SCHEDULER = MotionScheduler.UpdateIgnoreTimeScale;
        private static readonly int WORLD_TO_HOLE = Shader.PropertyToID("_WorldToHole");
        private static readonly int HOLE_RECT = Shader.PropertyToID("_HoleRect");
        private static readonly int HOLE_EDGE = Shader.PropertyToID("_HoleEdge");
        private static readonly int HOLE_ALPHA = Shader.PropertyToID("_HoleAlpha");
        private static readonly int CURTAIN_ALPHA = Shader.PropertyToID("_CurtainAlpha");

        private Image _image;
        private Material _material;
        private float _curtainAlpha;
        private float _holeAlpha;
        private bool _isHoleOpen;
        // 穴の行き先。滑らせている間もクリックは行き先で通し、動き終わるまで目標を押せないことが無いようにする
        private bool _hasGoal;
        private Vector2 _goalPosition;
        private Quaternion _goalRotation;
        private Vector2 _goalSize;
        private MotionHandle _curtainHandle;
        private MotionHandle _holeAlphaHandle;
        private MotionHandle _positionHandle;
        private MotionHandle _rotationHandle;
        private MotionHandle _sizeHandle;

        /// <summary>穴が開いているか (閉じているときは全面を暗くし、クリックも全面で受け止める)</summary>
        public bool IsHoleOpen => _isHoleOpen;

        /// <summary>暗幕を有効にしてフェードインする</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            _curtainHandle.TryCancel();
            _curtainHandle = Fade(_curtainAlpha, 1f, v => _curtainAlpha = v);
        }

        /// <summary>暗幕をフェードアウトし、消え切ったら無効にする</summary>
        public void Hide()
        {
            _curtainHandle.TryCancel();
            if (!isActiveAndEnabled)
            {
                _curtainAlpha = 0f;
                gameObject.SetActive(false);
                return;
            }
            _curtainHandle = LMotion.Create(_curtainAlpha, 0f, fadeDuration).WithEase(ease).WithScheduler(SCHEDULER)
                .WithOnComplete(() => gameObject.SetActive(false))
                .Bind(v => _curtainAlpha = v).AddTo(this);
        }

        public void OpenHole()
        {
            _isHoleOpen = true;
            _holeAlphaHandle.TryCancel();
            _holeAlphaHandle = Fade(_holeAlpha, 1f, v => _holeAlpha = v);
        }

        public void CloseHole()
        {
            _isHoleOpen = false;
            CancelMove();
            _holeAlphaHandle.TryCancel();
            _holeAlphaHandle = Fade(_holeAlpha, 0f, v => _holeAlpha = v);
        }

        /// <summary>目標とその子をまとめて囲む穴を、目標と同じ向きで padding だけ広げて開ける。目標が動いている間は呼ばない (呼んだ時点の位置に開く)</summary>
        public void FitHole(RectTransform target, float padding)
        {
            var parent = (RectTransform)hole.parent;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(target);
            var position = parent.InverseTransformPoint(target.TransformPoint(bounds.center));
            // 傾いた目標を軸に沿った矩形で囲むと角が欠けるので、目標と同じ向きに回す
            var rotation = Quaternion.Inverse(parent.rotation) * target.rotation;
            var scale = target.lossyScale.x / parent.lossyScale.x;
            SetHole(position, rotation, (Vector2)bounds.size * scale + Vector2.one * padding);
        }

        /// <summary>穴を anchoredPosition / sizeDelta の位置・大きさへ動かして開ける。回転は戻す</summary>
        public void MoveHole(Vector2 anchoredPosition, Vector2 size)
        {
            // アンカーは変えないので、localPosition と anchoredPosition の差は一定
            var position = hole.localPosition + (Vector3)(anchoredPosition - hole.anchoredPosition);
            SetHole(position, Quaternion.identity, size);
        }

        // 穴の中だけクリックを下へ通す
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!_isHoleOpen) return true;
            if (!_hasGoal)
            {
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hole, screenPoint, eventCamera, out var current)) return true;
                return !hole.rect.Contains(current);
            }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hole.parent, screenPoint, eventCamera, out var point)) return true;
            Vector2 local = Quaternion.Inverse(_goalRotation) * (point - _goalPosition);
            local = new Vector2(local.x / hole.localScale.x, local.y / hole.localScale.y);
            return !new Rect(-Vector2.Scale(hole.pivot, _goalSize), _goalSize).Contains(local);
        }

        // 開いている穴は滑らせ、閉じている穴はその場に置いてからフェードインする (前の場所から飛んでくるように見せない)
        private void SetHole(Vector3 position, Quaternion rotation, Vector2 size)
        {
            CancelMove();
            _hasGoal = true;
            _goalPosition = position;
            _goalRotation = rotation;
            _goalSize = size;
            if (_isHoleOpen && isActiveAndEnabled)
            {
                _positionHandle = LMotion.Create(hole.localPosition, position, moveDuration).WithEase(ease).WithScheduler(SCHEDULER)
                    .BindToLocalPosition(hole).AddTo(this);
                _rotationHandle = LMotion.Create(hole.localRotation, rotation, moveDuration).WithEase(ease).WithScheduler(SCHEDULER)
                    .BindToLocalRotation(hole).AddTo(this);
                _sizeHandle = LMotion.Create(hole.sizeDelta, size, moveDuration).WithEase(ease).WithScheduler(SCHEDULER)
                    .BindToSizeDelta(hole).AddTo(this);
            }
            else
            {
                hole.localPosition = position;
                hole.localRotation = rotation;
                hole.sizeDelta = size;
            }
            OpenHole();
        }

        // 暗幕が無効の間は Tween を回さずその場で値を合わせる (一度も有効になっていないと破棄で Tween を止められない)
        private MotionHandle Fade(float from, float to, Action<float> setter)
        {
            if (!isActiveAndEnabled)
            {
                setter(to);
                return default;
            }
            return LMotion.Create(from, to, fadeDuration).WithEase(ease).WithScheduler(SCHEDULER).Bind(setter).AddTo(this);
        }

        private void CancelMove()
        {
            _positionHandle.TryCancel();
            _rotationHandle.TryCancel();
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
            _material.SetFloat(CURTAIN_ALPHA, _curtainAlpha);
            _material.SetFloat(HOLE_ALPHA, _holeAlpha);
            if (_holeAlpha <= 0f)
            {
                _material.SetVector(HOLE_RECT, Vector4.zero);
                return;
            }
            // ぼかしは穴の外へ付ける。内側に取ると、穴の大きさで指した場所の縁まで暗くなる
            var rect = hole.rect;
            _material.SetMatrix(WORLD_TO_HOLE, hole.worldToLocalMatrix);
            _material.SetVector(HOLE_RECT, new Vector4(rect.xMin, rect.yMin, rect.width, rect.height));
            _material.SetFloat(HOLE_EDGE, edgeWidth);
        }

        private void OnDestroy()
        {
            _curtainHandle.TryCancel();
            _holeAlphaHandle.TryCancel();
            CancelMove();
            Destroy(_material);
        }
    }
}
