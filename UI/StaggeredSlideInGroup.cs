using System.Collections.Generic;
using LitMotion;
using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// 子要素のレイアウト計算とスタッガーアニメーションを統合したコンポーネント
/// </summary>
public class StaggeredSlideInGroup : MonoBehaviour
{
    public enum LayoutMode { None, Horizontal, Vertical, Grid }

    [Header("レイアウト設定")]
    [SerializeField] private LayoutMode layoutMode = LayoutMode.Horizontal;
    [SerializeField] private float spacing = 10f;
    [SerializeField] private TextAnchor alignment = TextAnchor.MiddleCenter;
    [SerializeField] private int gridConstraintCount = 2;

    [Header("スライド設定")]
    [SerializeField] private Vector2 slideOffset = new(0, 50f);
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private float staggerDelay = 0.05f;

    [Header("イージング設定")]
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private bool enableFade = true;
    [SerializeField] private Ease fadeEase = Ease.OutCubic;

    [Header("オプション")]
    [SerializeField] private bool ignoreTimeScale;
    [SerializeField] private bool activeOnly = true;

    private readonly List<MotionHandle> _animHandles = new();

    // Noneモード用: 子要素の初期位置キャッシュ
    private readonly Dictionary<RectTransform, Vector2> _cachedPositions = new();

    /// <summary>
    /// レイアウト計算後、スタッガーアニメーションを実行
    /// </summary>
    public void Play()
    {
        _animHandles.CancelAll();
        Canvas.ForceUpdateCanvases();

        var children = GetTargetChildren();
        if (children.Count == 0) return;

        if (layoutMode == LayoutMode.None)
        {
            // キャッシュ位置に復元し、新規子要素をキャッシュ
            RestoreCachedPositions(children);
            CacheNewChildren(children);
        }
        else
        {
            // レイアウト計算して目標位置を適用
            var positions = CalculatePositions(children);
            ApplyPositions(children, positions);
        }

        // スタッガーアニメーション実行
        var targets = BuildTargets(children);
        targets.StaggeredSlideIn(
            slideOffset, duration, staggerDelay, _animHandles,
            moveEase, enableFade ? fadeEase : null, ignoreTimeScale);
    }

    /// <summary>
    /// アニメーション停止＋目標位置に復元
    /// </summary>
    public void Cancel()
    {
        _animHandles.CancelAll();

        var children = GetTargetChildren();
        if (children.Count == 0) return;

        if (layoutMode == LayoutMode.None)
        {
            RestoreCachedPositions(children);
        }
        else
        {
            var positions = CalculatePositions(children);
            ApplyPositions(children, positions);
        }

        // フェード使用時はalphaを復元
        if (enableFade)
        {
            foreach (var child in children)
            {
                var canvasGroup = child.GetComponent<CanvasGroup>();
                if (canvasGroup) canvasGroup.alpha = 1f;
            }
        }
    }

    /// <summary>
    /// レイアウトを即座に適用（アニメーションなし）
    /// </summary>
    public void ApplyLayout()
    {
        if (layoutMode == LayoutMode.None) return;

        var children = GetTargetChildren();
        if (children.Count == 0) return;

        var positions = CalculatePositions(children);
        ApplyPositions(children, positions);
    }

    private List<RectTransform> GetTargetChildren()
    {
        var children = new List<RectTransform>();
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i) as RectTransform;
            if (!child) continue;
            if (activeOnly && !child.gameObject.activeSelf) continue;
            children.Add(child);
        }
        return children;
    }

    private List<(RectTransform rect, CanvasGroup canvasGroup)> BuildTargets(List<RectTransform> children)
    {
        var targets = new List<(RectTransform, CanvasGroup)>();
        foreach (var child in children)
        {
            var canvasGroup = enableFade ? child.gameObject.GetOrAddComponent<CanvasGroup>() : null;
            targets.Add((child, canvasGroup));
        }
        return targets;
    }

    private void RestoreCachedPositions(List<RectTransform> children)
    {
        foreach (var child in children)
        {
            if (_cachedPositions.TryGetValue(child, out var pos))
                child.anchoredPosition = pos;
        }
    }

    private void CacheNewChildren(List<RectTransform> children)
    {
        foreach (var child in children)
        {
            _cachedPositions.TryAdd(child, child.anchoredPosition);
        }
    }

    private List<Vector2> CalculatePositions(List<RectTransform> children)
    {
        return layoutMode switch
        {
            LayoutMode.Horizontal => CalculateLinearPositions(children, isHorizontal: true),
            LayoutMode.Vertical => CalculateLinearPositions(children, isHorizontal: false),
            LayoutMode.Grid => CalculateGridPositions(children),
            _ => new List<Vector2>()
        };
    }

    private List<Vector2> CalculateLinearPositions(List<RectTransform> children, bool isHorizontal)
    {
        var positions = new List<Vector2>();
        var parentRect = transform as RectTransform;
        var parentSize = parentRect.rect.size;

        // 子要素の合計サイズを計算
        var totalSize = 0f;
        foreach (var child in children)
        {
            totalSize += isHorizontal ? child.sizeDelta.x : child.sizeDelta.y;
        }
        totalSize += spacing * (children.Count - 1);

        // alignmentに基づく開始位置
        var startOffset = CalculateStartOffset(totalSize, parentSize, isHorizontal);
        var cursor = isHorizontal ? startOffset.x : startOffset.y;

        foreach (var child in children)
        {
            var childSize = isHorizontal ? child.sizeDelta.x : child.sizeDelta.y;
            var crossSize = isHorizontal ? child.sizeDelta.y : child.sizeDelta.x;
            var crossParentSize = isHorizontal ? parentSize.y : parentSize.x;

            var mainPos = cursor + childSize / 2f;
            var crossPos = CalculateCrossOffset(crossSize, crossParentSize, isHorizontal);

            positions.Add(isHorizontal
                ? new Vector2(mainPos, crossPos)
                : new Vector2(crossPos, -mainPos));

            cursor += childSize + spacing;
        }

        return positions;
    }

    private List<Vector2> CalculateGridPositions(List<RectTransform> children)
    {
        if (children.Count == 0) return new List<Vector2>();

        var positions = new List<Vector2>();
        var parentRect = transform as RectTransform;
        var parentSize = parentRect.rect.size;

        // グリッドのセルサイズを最初の子要素から決定
        var cellSize = children[0].sizeDelta;

        var cols = Mathf.Max(1, gridConstraintCount);
        var rows = Mathf.CeilToInt((float)children.Count / cols);

        var totalWidth = cols * cellSize.x + (cols - 1) * spacing;
        var totalHeight = rows * cellSize.y + (rows - 1) * spacing;

        var startOffset = CalculateGridStartOffset(totalWidth, totalHeight, parentSize);

        for (var i = 0; i < children.Count; i++)
        {
            var col = i % cols;
            var row = i / cols;

            var x = startOffset.x + col * (cellSize.x + spacing) + cellSize.x / 2f;
            var y = startOffset.y - row * (cellSize.y + spacing) - cellSize.y / 2f;

            positions.Add(new Vector2(x, y));
        }

        return positions;
    }

    private Vector2 CalculateStartOffset(float totalSize, Vector2 parentSize, bool isHorizontal)
    {
        var mainSize = isHorizontal ? parentSize.x : parentSize.y;

        // alignment列(水平)または行(垂直)方向のオフセット
        var mainAlignment = isHorizontal ? GetColumnAlignment() : GetRowAlignment();
        var mainOffset = mainAlignment switch
        {
            0 => -mainSize / 2f,
            1 => -totalSize / 2f,
            _ => mainSize / 2f - totalSize
        };

        return isHorizontal
            ? new Vector2(mainOffset, 0)
            : new Vector2(0, mainOffset);
    }

    private float CalculateCrossOffset(float childSize, float parentSize, bool isHorizontal)
    {
        var crossAlignment = isHorizontal ? GetRowAlignment() : GetColumnAlignment();
        return crossAlignment switch
        {
            0 => parentSize / 2f - childSize / 2f,
            1 => 0f,
            _ => -parentSize / 2f + childSize / 2f
        };
    }

    private Vector2 CalculateGridStartOffset(float totalWidth, float totalHeight, Vector2 parentSize)
    {
        var colAlign = GetColumnAlignment();
        var rowAlign = GetRowAlignment();

        var x = colAlign switch
        {
            0 => -parentSize.x / 2f,
            1 => -totalWidth / 2f,
            _ => parentSize.x / 2f - totalWidth
        };

        var y = rowAlign switch
        {
            0 => parentSize.y / 2f,
            1 => totalHeight / 2f,
            _ => -parentSize.y / 2f + totalHeight
        };

        return new Vector2(x, y);
    }

    // TextAnchorから列方向(水平)のアライメント取得 (0=Left, 1=Center, 2=Right)
    private int GetColumnAlignment()
    {
        return alignment switch
        {
            TextAnchor.UpperLeft or TextAnchor.MiddleLeft or TextAnchor.LowerLeft => 0,
            TextAnchor.UpperCenter or TextAnchor.MiddleCenter or TextAnchor.LowerCenter => 1,
            _ => 2
        };
    }

    // TextAnchorから行方向(垂直)のアライメント取得 (0=Upper, 1=Middle, 2=Lower)
    private int GetRowAlignment()
    {
        return alignment switch
        {
            TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight => 0,
            TextAnchor.MiddleLeft or TextAnchor.MiddleCenter or TextAnchor.MiddleRight => 1,
            _ => 2
        };
    }

    private static void ApplyPositions(List<RectTransform> children, List<Vector2> positions)
    {
        if (children.Count == 0) return;

        var parentRect = (RectTransform)children[0].parent;
        var parentSize = parentRect.rect.size;

        for (var i = 0; i < children.Count && i < positions.Count; i++)
        {
            var child = children[i];
            // アンカーが(0.5, 0.5)でない場合の座標系差分を補正
            var anchorCenter = (child.anchorMin + child.anchorMax) / 2f;
            var anchorCorrection = new Vector2(
                (0.5f - anchorCenter.x) * parentSize.x,
                (0.5f - anchorCenter.y) * parentSize.y
            );
            child.anchoredPosition = positions[i] + anchorCorrection;
        }
    }

    private void Awake()
    {
        if (layoutMode != LayoutMode.None) return;

        // Noneモード: 初期位置をキャッシュ
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i) as RectTransform;
            if (!child) continue;
            _cachedPositions[child] = child.anchoredPosition;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (layoutMode == LayoutMode.None) return;
        // エディタ時にレイアウトを即時プレビュー
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this) ApplyLayout();
        };
    }
#endif

    private void OnDestroy()
    {
        _animHandles.CancelAll();
    }
}
