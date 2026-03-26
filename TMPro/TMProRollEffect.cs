using TMPro;
using UnityEngine;

/// <summary>
/// TextMeshPro の各文字を独立してコロコロ回転させる演出。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMProRollEffect : MonoBehaviour
{
    [SerializeField] private float rotationAngle = 20f;
    [SerializeField] private float updateInterval = 0.12f;
    [SerializeField] private float reselectionInterval = 1.2f;
    [SerializeField, Range(0f, 1f)] private float activeCharacterRate = 0.35f;
    [SerializeField, Min(1)] private int reselectionBatchSize = 1;
    [SerializeField] private float randomSeedOffset = 0.7f;
    [SerializeField] private bool useUnscaledTime = true;

    private TextMeshProUGUI _text;
    private TMP_MeshInfo[] _cachedMeshInfo;
    private bool[] _activeCharacters;
    private bool _isInitialized;
    private bool _isUpdatingMesh;
    private int _lastSelectionStepIndex = int.MinValue;

    /// <summary>
    /// 演出を即時再適用する。
    /// </summary>
    public void ForceUpdateEffect()
    {
        Initialize();
        ApplyEffect();
    }

    private void OnTextChanged(Object obj)
    {
        if (obj == _text && !_isUpdatingMesh)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (_text == null)
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        if (_text == null)
        {
            return;
        }

        _isUpdatingMesh = true;
        _text.ForceMeshUpdate();
        _cachedMeshInfo = _text.textInfo.CopyMeshInfoVertexData();
        _activeCharacters = null;
        _lastSelectionStepIndex = int.MinValue;
        _isInitialized = true;
        _isUpdatingMesh = false;
    }

    private void ApplyEffect()
    {
        if (_cachedMeshInfo == null || _cachedMeshInfo.Length == 0)
        {
            return;
        }

        _isUpdatingMesh = true;

        var textInfo = _text.textInfo;
        var timeSource = useUnscaledTime ? Time.unscaledTime : Time.time;
        var stepIndex = updateInterval > 0f
            ? Mathf.FloorToInt(timeSource / updateInterval)
            : 0;
        var selectionStepIndex = reselectionInterval > 0f
            ? Mathf.FloorToInt(timeSource / reselectionInterval)
            : 0;

        EnsureActiveCharacters(selectionStepIndex, textInfo.characterCount);

        for (var i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var sourceVertices = _cachedMeshInfo[i].vertices;
            var destinationVertices = textInfo.meshInfo[i].vertices;
            var vertexCount = Mathf.Min(sourceVertices.Length, destinationVertices.Length);
            System.Array.Copy(sourceVertices, destinationVertices, vertexCount);
        }

        for (var i = 0; i < textInfo.characterCount; i++)
        {
            var characterInfo = textInfo.characterInfo[i];
            if (!characterInfo.isVisible)
            {
                continue;
            }

            var materialIndex = characterInfo.materialReferenceIndex;
            var vertexIndex = characterInfo.vertexIndex;
            var vertices = textInfo.meshInfo[materialIndex].vertices;

            var pivot = (vertices[vertexIndex] + vertices[vertexIndex + 2]) * 0.5f;
            var angle = IsCharacterActive(i)
                ? GetRandomAngle(stepIndex, i)
                : 0f;
            var rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, angle));

            for (var j = 0; j < 4; j++)
            {
                var offset = vertices[vertexIndex + j] - pivot;
                vertices[vertexIndex + j] = rotationMatrix.MultiplyPoint3x4(offset) + pivot;
            }
        }

        for (var i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            _text.UpdateGeometry(meshInfo.mesh, i);
        }

        _isUpdatingMesh = false;
    }

    private void RestoreOriginalMesh()
    {
        if (_text == null || _cachedMeshInfo == null)
        {
            return;
        }

        var textInfo = _text.textInfo;
        for (var i = 0; i < textInfo.meshInfo.Length && i < _cachedMeshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            var sourceVertices = _cachedMeshInfo[i].vertices;
            var vertexCount = Mathf.Min(sourceVertices.Length, meshInfo.vertices.Length);
            System.Array.Copy(sourceVertices, meshInfo.vertices, vertexCount);
            meshInfo.mesh.vertices = meshInfo.vertices;
            _text.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private float GetRandomAngle(int stepIndex, int characterIndex)
    {
        var seed = (stepIndex + 1) * 73856093 ^ (characterIndex + 1) * 19349663;
        seed ^= Mathf.RoundToInt(randomSeedOffset * 1000f) * 83492791;
        var normalized = Mathf.Abs(seed % 10000) / 9999f;
        return (normalized * 2f - 1f) * rotationAngle;
    }

    private bool IsCharacterActive(int characterIndex)
    {
        return _activeCharacters != null
               && characterIndex >= 0
               && characterIndex < _activeCharacters.Length
               && _activeCharacters[characterIndex];
    }

    private void EnsureActiveCharacters(int selectionStepIndex, int characterCount)
    {
        if (characterCount <= 0)
        {
            _activeCharacters = null;
            _lastSelectionStepIndex = selectionStepIndex;
            return;
        }

        if (_activeCharacters == null || _activeCharacters.Length != characterCount)
        {
            _activeCharacters = new bool[characterCount];
            RebuildActiveCharacters(selectionStepIndex, characterCount);
            _lastSelectionStepIndex = selectionStepIndex;
            return;
        }

        if (_lastSelectionStepIndex == int.MinValue)
        {
            RebuildActiveCharacters(selectionStepIndex, characterCount);
            _lastSelectionStepIndex = selectionStepIndex;
            return;
        }

        if (selectionStepIndex <= _lastSelectionStepIndex)
        {
            return;
        }

        for (var step = _lastSelectionStepIndex + 1; step <= selectionStepIndex; step++)
        {
            ReselectActiveCharacters(step, characterCount);
        }

        _lastSelectionStepIndex = selectionStepIndex;
    }

    private void RebuildActiveCharacters(int selectionStepIndex, int characterCount)
    {
        System.Array.Clear(_activeCharacters, 0, _activeCharacters.Length);

        var activeCount = Mathf.Clamp(
            Mathf.RoundToInt(characterCount * activeCharacterRate),
            0,
            characterCount);

        for (var i = 0; i < activeCount; i++)
        {
            var characterIndex = GetNthInactiveCharacterIndex(selectionStepIndex, i, characterCount);
            if (characterIndex >= 0)
            {
                _activeCharacters[characterIndex] = true;
            }
        }
    }

    private void ReselectActiveCharacters(int selectionStepIndex, int characterCount)
    {
        if (activeCharacterRate <= 0f)
        {
            System.Array.Clear(_activeCharacters, 0, _activeCharacters.Length);
            return;
        }

        if (activeCharacterRate >= 1f)
        {
            for (var i = 0; i < _activeCharacters.Length; i++)
            {
                _activeCharacters[i] = true;
            }

            return;
        }

        var swapCount = Mathf.Min(reselectionBatchSize, characterCount);
        for (var i = 0; i < swapCount; i++)
        {
            var removeIndex = GetNthActiveCharacterIndex(selectionStepIndex, i, characterCount);
            var addIndex = GetNthInactiveCharacterIndex(selectionStepIndex, i, characterCount);

            if (removeIndex >= 0)
            {
                _activeCharacters[removeIndex] = false;
            }

            if (addIndex >= 0)
            {
                _activeCharacters[addIndex] = true;
            }
        }
    }

    private int GetNthActiveCharacterIndex(int selectionStepIndex, int ordinal, int characterCount)
    {
        var activeCount = CountActiveCharacters();
        if (activeCount == 0)
        {
            return -1;
        }

        var target = GetDeterministicIndex(selectionStepIndex, ordinal, activeCount, 1580030173);
        for (var i = 0; i < characterCount; i++)
        {
            if (!_activeCharacters[i])
            {
                continue;
            }

            if (target-- == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetNthInactiveCharacterIndex(int selectionStepIndex, int ordinal, int characterCount)
    {
        var inactiveCount = characterCount - CountActiveCharacters();
        if (inactiveCount == 0)
        {
            return -1;
        }

        var target = GetDeterministicIndex(selectionStepIndex, ordinal, inactiveCount, 122949829);
        for (var i = 0; i < characterCount; i++)
        {
            if (_activeCharacters[i])
            {
                continue;
            }

            if (target-- == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private int CountActiveCharacters()
    {
        if (_activeCharacters == null)
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i < _activeCharacters.Length; i++)
        {
            if (_activeCharacters[i])
            {
                count++;
            }
        }

        return count;
    }

    private int GetDeterministicIndex(int selectionStepIndex, int ordinal, int count, int multiplier)
    {
        if (count <= 0)
        {
            return -1;
        }

        var seed = (selectionStepIndex + 1) * multiplier ^ (ordinal + 1) * 19349663;
        seed ^= Mathf.RoundToInt(randomSeedOffset * 1000f) * 83492791;
        return Mathf.Abs(seed % count);
    }

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        Initialize();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        RestoreOriginalMesh();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        Initialize();
        ApplyEffect();
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        if (_text == null || !_isInitialized || _text.textInfo.characterCount == 0)
        {
            return;
        }

        if (_text.havePropertiesChanged)
        {
            Initialize();
        }

        ApplyEffect();
    }
}
