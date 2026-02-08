using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// カーブの傾きで1文字ずつ再配置するアーチ状テキスト（アライメント対応）
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMProArchedText : MonoBehaviour
{
    private enum TextVAlignment
    {
        Base,
        Top,
        Bottom,
    }

    [Header("アライメント")]
    [SerializeField] private TextVAlignment vAlignment;
    [SerializeField] private HorizontalAlignmentOptions hAlignment;

    [Header("カーブ")]
    [SerializeField] private AnimationCurve vertexCurve = new(new Keyframe(0, 0), new Keyframe(0.5f, 0.25f), new Keyframe(1, 0f));

    private TextMeshProUGUI _textComponent;

    private bool TryGetTextComponent()
    {
        if (_textComponent == null)
            _textComponent = GetComponent<TextMeshProUGUI>();
        return _textComponent != null;
    }

    private void UpdateCurveMesh()
    {
        if (!TryGetTextComponent())
            return;

        _textComponent.ForceMeshUpdate();

        var textInfo = _textComponent.textInfo;
        var characterCount = textInfo.characterCount;

        if (characterCount == 0)
            return;

        var baseline = new Vector3();
        var prevAngleDirection = new Vector3();
        var prevOffsetToMidBaseline = new Vector3();
        Vector3[] vertices;
        var defaultBaseLine = 0f;
        var maxBaseLine = float.MinValue;
        var minBaseLine = float.MaxValue;
        var isFirst = true;

        for (var i = 0; i < characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            var vertexIndex = textInfo.characterInfo[i].vertexIndex;
            var materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            vertices = textInfo.meshInfo[materialIndex].vertices;

            // 文字の真ん中の点と基準ベースラインの高さを取得
            Vector3 offsetToMidBaseline = new Vector2(
                (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2,
                textInfo.characterInfo[i].baseLine);

            // 文字の中央下を原点に移動（回転と移動の準備）
            vertices[vertexIndex + 0] += -offsetToMidBaseline;
            vertices[vertexIndex + 1] += -offsetToMidBaseline;
            vertices[vertexIndex + 2] += -offsetToMidBaseline;
            vertices[vertexIndex + 3] += -offsetToMidBaseline;

            // 曲線の傾き(angle)の計算
            var x0 = (float)(i + 1) / (characterCount + 1);
            var x1 = x0 + 0.0001f;
            var y0 = vertexCurve.Evaluate(x0);
            var y1 = vertexCurve.Evaluate(x1);
            var horizontal = new Vector3(1, 0, 0);
            var tangent = new Vector3(0.0001f, y1 - y0);
            var angleDirection = tangent.normalized;
            var angle = Mathf.Acos(Vector3.Dot(horizontal, angleDirection)) * Mathf.Rad2Deg;
            var cross = Vector3.Cross(horizontal, tangent);
            angle = cross.z > 0 ? angle : 360 - angle;

            // angle回転させた頂点位置の計算
            var matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle));
            vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
            vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
            vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
            vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

            // 文字間の計算
            float characterSpace;
            if (isFirst)
            {
                baseline = offsetToMidBaseline;
                defaultBaseLine = baseline.y;
                characterSpace = 0;
                isFirst = false;
            }
            else
            {
                characterSpace = (offsetToMidBaseline - prevOffsetToMidBaseline).magnitude;
            }
            prevOffsetToMidBaseline = offsetToMidBaseline;

            // 文字位置を計算して移動
            baseline = baseline + (angleDirection + prevAngleDirection).normalized * characterSpace;
            vertices[vertexIndex + 0] += baseline;
            vertices[vertexIndex + 1] += baseline;
            vertices[vertexIndex + 2] += baseline;
            vertices[vertexIndex + 3] += baseline;

            prevAngleDirection = angleDirection;

            minBaseLine = Mathf.Min(minBaseLine, baseline.y);
            maxBaseLine = Mathf.Max(maxBaseLine, baseline.y);
        }

        // 揃えの計算
        if (hAlignment != HorizontalAlignmentOptions.Left || vAlignment != TextVAlignment.Base)
        {
            var hOffset = 0f;
            if (hAlignment == HorizontalAlignmentOptions.Center)
                hOffset = (prevOffsetToMidBaseline.x - baseline.x) * 0.5f;
            else if (hAlignment == HorizontalAlignmentOptions.Right)
                hOffset = prevOffsetToMidBaseline.x - baseline.x;

            var vOffset = 0f;
            if (vAlignment == TextVAlignment.Bottom)
                vOffset = defaultBaseLine - minBaseLine;
            else if (vAlignment == TextVAlignment.Top)
                vOffset = defaultBaseLine - maxBaseLine;

            var alignOffset = new Vector3(hOffset, vOffset, 0);

            for (var i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                var vertexIndex = textInfo.characterInfo[i].vertexIndex;
                var materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                vertices = textInfo.meshInfo[materialIndex].vertices;

                vertices[vertexIndex + 0] += alignOffset;
                vertices[vertexIndex + 1] += alignOffset;
                vertices[vertexIndex + 2] += alignOffset;
                vertices[vertexIndex + 3] += alignOffset;
            }
        }

        _textComponent.UpdateVertexData();
    }

    private void Awake()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// アーチ表示を強制的に再適用する
    /// </summary>
    public void ForceUpdate() => UpdateCurveMesh();

    private void OnEnable()
    {
        UpdateCurveMesh();
    }

    private void Update()
    {
        if (!_textComponent.havePropertiesChanged)
        {
            return;
        }

        UpdateCurveMesh();
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        UpdateCurveMesh();
    }

    private void OnDisable()
    {
        if (TryGetTextComponent())
            _textComponent.ForceMeshUpdate();
    }
#endif
}
