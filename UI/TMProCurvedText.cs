using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AnimationCurveとcurveScaleでシンプルにカーブさせるテキスト
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMProCurvedText : MonoBehaviour
{
    [Header("カーブ")]
    [SerializeField] private AnimationCurve _vertexCurve = new(new Keyframe(0, 0), new Keyframe(0.5f, 0.25f), new Keyframe(1, 0f));
    [SerializeField] private float _curveScale = 100.0f;

    private TextMeshProUGUI _textComponent;

    private void UpdateCurveMesh()
    {
        _textComponent.ForceMeshUpdate();

        var textInfo = _textComponent.textInfo;
        var characterCount = textInfo.characterCount;

        if (characterCount == 0)
            return;

        var boundsMinX = _textComponent.bounds.min.x;
        var boundsMaxX = _textComponent.bounds.max.x;

        Vector3[] vertices;
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

            // カーブの傾きに沿って文字サイズをかけて傾き量を計算
            var x0 = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX);
            var x1 = x0 + 0.0001f;
            var y0 = _vertexCurve.Evaluate(x0) * _curveScale;
            var y1 = _vertexCurve.Evaluate(x1) * _curveScale;
            var charSize = boundsMaxX - boundsMinX;
            var horizontal = new Vector3(1, 0, 0);
            var tangent = new Vector3(charSize * 0.0001f, y1 - y0);
            var angle = Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * Mathf.Rad2Deg;
            var cross = Vector3.Cross(horizontal, tangent);
            angle = cross.z > 0 ? angle : 360 - angle;

            // angle回転させてbaseをy0だけ上げた頂点位置の計算
            var matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);
            vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
            vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
            vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
            vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

            // 文字位置を戻す
            vertices[vertexIndex + 0] += offsetToMidBaseline;
            vertices[vertexIndex + 1] += offsetToMidBaseline;
            vertices[vertexIndex + 2] += offsetToMidBaseline;
            vertices[vertexIndex + 3] += offsetToMidBaseline;
        }

        _textComponent.UpdateVertexData();
    }

    private void Awake()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
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

    private void OnEnable()
    {
        UpdateCurveMesh();
    }

    private void OnDisable()
    {
        _textComponent.ForceMeshUpdate();
    }
#endif
}
