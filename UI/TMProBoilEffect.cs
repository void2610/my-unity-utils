using TMPro;
using UnityEngine;

/// <summary>
/// TMProテキストに手書き風のボイルアニメーションを適用
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMProBoilEffect : MonoBehaviour
{
    [SerializeField] private float strength = 5f;
    [SerializeField] private float speed = 100f;
    [SerializeField] private float frequency = 100f;
    [SerializeField] private float frameRate = 2f;

    private TextMeshProUGUI _text;
    private TMP_MeshInfo[] _cachedMeshInfo;
    private bool _isInitialized;
    private bool _isUpdating;

    private void OnTextChanged(UnityEngine.Object obj)
    {
        if (obj == _text && !_isUpdating)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        _isUpdating = true;
        _text.ForceMeshUpdate();
        _cachedMeshInfo = _text.textInfo.CopyMeshInfoVertexData();
        _isInitialized = true;
        _isUpdating = false;
    }

    private void Update()
    {
        if (!_isInitialized || _text.textInfo.characterCount == 0) return;

        var textInfo = _text.textInfo;
        // コマ送りにするため時間をステップ状に量子化
        var time = Mathf.Floor(Time.time * frameRate) / frameRate * speed;

        for (var i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            var materialIndex = charInfo.materialReferenceIndex;
            var vertexIndex = charInfo.vertexIndex;
            var sourceVertices = _cachedMeshInfo[materialIndex].vertices;
            var destVertices = textInfo.meshInfo[materialIndex].vertices;

            // 各頂点を個別に変形（線自体が震えるボイル効果）
            for (var j = 0; j < 4; j++)
            {
                // 頂点ごとに異なる位相で揺らす
                var vertexPhase = time + i * frequency + j * 1.7f;
                var offset = new Vector3(
                    Mathf.Sin(vertexPhase) * strength,
                    Mathf.Cos(vertexPhase * 1.3f) * strength,
                    0
                );
                destVertices[vertexIndex + j] = sourceVertices[vertexIndex + j] + offset;
            }
        }

        // メッシュ更新
        for (var i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            _text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
    
    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }
}
