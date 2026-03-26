using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class TMProWideShadow : MonoBehaviour
{
    private enum UpdateMode
    {
        Auto = 0,
        Manual = 1,
    }

    [SerializeField] private Color color = new(0f, 0f, 0f, 0.7f);
    [SerializeField] private Vector2 offset = new(12f, -12f);
    [SerializeField][Min(0f)] private float blurRadius = 24f;
    [SerializeField][Range(-1f, 1f)] private float spread = 0.08f;
    [SerializeField][Min(0f)] private float intensity = 1f;
    [SerializeField][Min(0f)] private float padding = 8f;
    [SerializeField][Range(1, 4)] private int downsample = 1;
    [SerializeField] private UpdateMode updateMode = UpdateMode.Auto;

    public Color ShadowColor
    {
        get => color;
        set
        {
            color = value;
            SetDirty();
        }
    }

    public Vector2 Offset
    {
        get => offset;
        set
        {
            offset = value;
            SetDirty();
        }
    }

    public float BlurRadius
    {
        get => blurRadius;
        set
        {
            blurRadius = Mathf.Max(0f, value);
            SetDirty();
        }
    }

    public float Spread
    {
        get => spread;
        set
        {
            spread = Mathf.Clamp(value, -1f, 1f);
            SetDirty();
        }
    }

    public float Intensity
    {
        get => intensity;
        set
        {
            intensity = Mathf.Max(0f, value);
            SetDirty();
        }
    }

    private static readonly int _faceColorId = Shader.PropertyToID("_FaceColor");
    private static readonly int _underlayColorId = Shader.PropertyToID("_UnderlayColor");
    private static readonly int _underlayOffsetXId = Shader.PropertyToID("_UnderlayOffsetX");
    private static readonly int _underlayOffsetYId = Shader.PropertyToID("_UnderlayOffsetY");
    private static readonly int _underlayDilateId = Shader.PropertyToID("_UnderlayDilate");
    private static readonly int _underlaySoftnessId = Shader.PropertyToID("_UnderlaySoftness");
    private static readonly int _glowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int _glowOffsetId = Shader.PropertyToID("_GlowOffset");
    private static readonly int _glowInnerId = Shader.PropertyToID("_GlowInner");
    private static readonly int _glowOuterId = Shader.PropertyToID("_GlowOuter");
    private static readonly int _blurOffsetId = Shader.PropertyToID("_BlurOffset");
    private static readonly int _shadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int _shadowStrengthId = Shader.PropertyToID("_ShadowStrength");

    private readonly List<Material> _drawMaterials = new();
    private readonly List<Mesh> _drawMeshes = new();
    private TextMeshProUGUI _text;
    private RectTransform _textRect;
    private Canvas _canvas;
    private TMProWideShadowReplica _replica;
    private Material _blurMaterial;
    private Material _compositeMaterial;
    private RenderTexture _outputTexture;
    private int _lastStateHash;
    private bool _isDirty = true;

    public void RefreshShadow()
    {
        SetDirty();
        RenderShadowIfNeeded(force: true);
    }

    private void OnRectTransformDimensionsChange()
    {
        SetDirty();
    }

    private void OnDidApplyAnimationProperties()
    {
        SetDirty();
    }

    private void OnCanvasHierarchyChanged()
    {
        _canvas = null;
        SetDirty();
    }

    private void OnTransformParentChanged()
    {
        SetDirty();
        if (_replica)
        {
            DestroyReplica();
            EnsureReplica();
        }
    }

    private void CacheComponents()
    {
        if (!_text)
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        if (!_textRect)
        {
            _textRect = _text ? _text.rectTransform : transform as RectTransform;
        }

        _canvas = null;
    }

    private void RenderShadowIfNeeded(bool force)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        CacheComponents();
        EnsureBlurMaterial();
        EnsureCompositeMaterial();
        EnsureReplica();

        if (!_text || !_replica || !_blurMaterial || !_compositeMaterial)
        {
            return;
        }

        var stateHash = ComputeStateHash();
        if (!force && !_isDirty && stateHash == _lastStateHash)
        {
            return;
        }

        RenderShadow();
        _isDirty = false;
        _lastStateHash = ComputeStateHash();
    }

    private void RenderShadow()
    {
        RefreshTextMeshIfNeeded();
        if (!HasRenderableText())
        {
            SetReplicaEnabled(false);
            ReleaseOutputTexture();
            return;
        }

        var rect = _textRect.rect;
        var totalPadding = CalculatePadding();
        var targetSize = CalculateTextureSize(rect, totalPadding);
        if (targetSize.x <= 1 || targetSize.y <= 1)
        {
            SetReplicaEnabled(false);
            ReleaseOutputTexture();
            return;
        }

        EnsureOutputTexture(targetSize);

        var descriptor = _outputTexture.descriptor;
        var source = RenderTexture.GetTemporary(descriptor);
        var ping = RenderTexture.GetTemporary(descriptor);
        var pong = RenderTexture.GetTemporary(descriptor);

        try
        {
            RenderSilhouette(source, rect, totalPadding);
            ApplyBlur(source, ping, pong, _outputTexture);
            SyncReplica(rect, totalPadding);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(source);
            RenderTexture.ReleaseTemporary(ping);
            RenderTexture.ReleaseTemporary(pong);
        }
    }

    private void RefreshTextMeshIfNeeded()
    {
        if (!_text)
        {
            return;
        }

        if (_text.havePropertiesChanged || _text.textInfo.characterCount == 0)
        {
            _text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        }
    }

    private bool HasRenderableText()
    {
        if (!_text || string.IsNullOrEmpty(_text.text))
        {
            return false;
        }

        var meshInfos = _text.textInfo.meshInfo;
        for (var i = 0; i < meshInfos.Length; i++)
        {
            if (meshInfos[i].vertexCount > 0 && meshInfos[i].mesh != null)
            {
                return true;
            }
        }

        return false;
    }

    private float CalculatePadding()
    {
        var offsetExtent = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
        var spreadPadding = Mathf.Abs(spread) * 32f;
        return padding + spreadPadding + blurRadius * 2f + offsetExtent;
    }

    private Vector2Int CalculateTextureSize(Rect rect, float totalPadding)
    {
        var scaleFactor = Mathf.Max(1f, GetCanvasScaleFactor());
        var renderScale = scaleFactor / Mathf.Max(1, downsample);
        var width = Mathf.CeilToInt((rect.width + totalPadding * 2f) * renderScale);
        var height = Mathf.CeilToInt((rect.height + totalPadding * 2f) * renderScale);
        return new Vector2Int(Mathf.Max(2, width), Mathf.Max(2, height));
    }

    private float GetCanvasScaleFactor()
    {
        if (!_canvas)
        {
            _canvas = _text.canvas ?? GetComponentInParent<Canvas>();
        }

        return _canvas ? Mathf.Max(0.01f, _canvas.scaleFactor) : 1f;
    }

    private void RenderSilhouette(RenderTexture destination, Rect rect, float totalPadding)
    {
        var commandBuffer = new CommandBuffer { name = "TmpWideShadow.RenderSilhouette" };
        try
        {
            commandBuffer.SetRenderTarget(destination);
            commandBuffer.ClearRenderTarget(clearDepth: true, clearColor: true, Color.clear);

            var projection = Matrix4x4.Ortho(
                rect.xMin - totalPadding,
                rect.xMax + totalPadding,
                rect.yMin - totalPadding,
                rect.yMax + totalPadding,
                -1f,
                1f);
            commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, projection);

            var meshInfos = _text.textInfo.meshInfo;
            EnsureDrawMaterialCapacity(meshInfos);

            for (var i = 0; i < meshInfos.Length; i++)
            {
                var meshInfo = meshInfos[i];
                if (meshInfo.vertexCount <= 0 || !meshInfo.mesh || !meshInfo.material)
                {
                    continue;
                }

                var drawMaterial = _drawMaterials[i];
                drawMaterial.CopyPropertiesFromMaterial(meshInfo.material);
                PrepareDrawMaterial(drawMaterial);
                var drawMesh = GetPreparedMesh(i, meshInfo.mesh, meshInfo.vertexCount);
                commandBuffer.DrawMesh(drawMesh, Matrix4x4.identity, drawMaterial, 0, 0);
            }

            Graphics.ExecuteCommandBuffer(commandBuffer);
        }
        finally
        {
            commandBuffer.Release();
        }
    }

    private void PrepareDrawMaterial(Material material)
    {
        if (material.HasProperty(_faceColorId))
        {
            material.SetColor(_faceColorId, Color.white);
        }

        if (material.HasProperty(_underlayColorId))
        {
            material.SetColor(_underlayColorId, Color.clear);
        }

        if (material.HasProperty(_underlayOffsetXId))
        {
            material.SetFloat(_underlayOffsetXId, 0f);
        }

        if (material.HasProperty(_underlayOffsetYId))
        {
            material.SetFloat(_underlayOffsetYId, 0f);
        }

        if (material.HasProperty(_underlayDilateId))
        {
            material.SetFloat(_underlayDilateId, 0f);
        }

        if (material.HasProperty(_underlaySoftnessId))
        {
            material.SetFloat(_underlaySoftnessId, 0f);
        }

        if (material.HasProperty(_glowColorId))
        {
            material.SetColor(_glowColorId, Color.clear);
        }

        if (material.HasProperty(_glowOffsetId))
        {
            material.SetFloat(_glowOffsetId, 0f);
        }

        if (material.HasProperty(_glowInnerId))
        {
            material.SetFloat(_glowInnerId, 0f);
        }

        if (material.HasProperty(_glowOuterId))
        {
            material.SetFloat(_glowOuterId, 0f);
        }
    }

    private void ApplyBlur(RenderTexture source, RenderTexture ping, RenderTexture pong, RenderTexture destination)
    {
        var scaleFactor = Mathf.Max(1f, GetCanvasScaleFactor());
        var radiusInPixels = blurRadius * scaleFactor / Mathf.Max(1, downsample);
        if (radiusInPixels <= 0.01f)
        {
            Graphics.Blit(source, destination);
            return;
        }

        var passCount = Mathf.Clamp(Mathf.CeilToInt(radiusInPixels / 2f), 1, 24);
        Graphics.Blit(source, ping);

        var current = ping;
        var next = pong;
        for (var passIndex = 0; passIndex < passCount; passIndex++)
        {
            // Keep each sample radius small and accumulate blur over many passes.
            // Large per-pass offsets create visible ghost copies instead of a smooth penumbra.
            _blurMaterial.SetVector(_blurOffsetId, new Vector4(1f, 0f, 0f, 0f));
            Graphics.Blit(current, next, _blurMaterial, 0);
            Swap(ref current, ref next);

            _blurMaterial.SetVector(_blurOffsetId, new Vector4(0f, 1f, 0f, 0f));
            Graphics.Blit(current, next, _blurMaterial, 0);
            Swap(ref current, ref next);
        }

        Graphics.Blit(current, destination);
    }

    private void SyncReplica(Rect rect, float totalPadding)
    {
        var replicaRect = _replica.rectTransform;
        replicaRect.SetParent(_textRect.parent, worldPositionStays: false);
        replicaRect.anchorMin = _textRect.anchorMin;
        replicaRect.anchorMax = _textRect.anchorMax;
        replicaRect.pivot = _textRect.pivot;
        replicaRect.anchoredPosition3D = _textRect.anchoredPosition3D + (Vector3)offset;
        replicaRect.sizeDelta = _textRect.sizeDelta + new Vector2(totalPadding * 2f, totalPadding * 2f);
        replicaRect.localRotation = _textRect.localRotation;
        replicaRect.localScale = _textRect.localScale;

        _replica.raycastTarget = false;
        _replica.maskable = false;
        _replica.texture = _outputTexture;
        _replica.material = _compositeMaterial;
        var shadowColor = color;
        shadowColor.a *= _text.color.a;
        _compositeMaterial.SetColor(_shadowColorId, shadowColor);
        _compositeMaterial.SetFloat(_shadowStrengthId, Mathf.Max(0f, intensity));
        _replica.color = Color.white;
        _replica.enabled = true;
        _replica.gameObject.SetActive(true);
        MoveReplicaBehindText();
    }

    private void EnsureOutputTexture(Vector2Int size)
    {
        if (_outputTexture && (_outputTexture.width != size.x || _outputTexture.height != size.y))
        {
            ReleaseOutputTexture();
        }

        if (_outputTexture)
        {
            return;
        }

        _outputTexture = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
        {
            name = $"{nameof(TMProWideShadow)}_{GetInstanceID()}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
        _outputTexture.Create();
    }

    private void ReleaseOutputTexture()
    {
        if (!_outputTexture)
        {
            return;
        }

        _outputTexture.Release();
        Destroy(_outputTexture);
        _outputTexture = null;
    }

    private void EnsureReplica()
    {
        CacheComponents();
        if (!_textRect)
        {
            return;
        }

        if (_replica)
        {
            return;
        }

        var parent = _textRect.parent;
        if (!parent)
        {
            return;
        }

        var replicaObject = new GameObject($"{gameObject.name} Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMProWideShadowReplica));
        replicaObject.hideFlags = HideFlags.HideInHierarchy;
        replicaObject.layer = gameObject.layer;
        _replica = replicaObject.GetComponent<TMProWideShadowReplica>();
        _replica.enabled = false;
        _replica.gameObject.SetActive(false);
        _replica.rectTransform.SetParent(parent, worldPositionStays: false);
    }

    private void DestroyReplica()
    {
        if (!_replica)
        {
            return;
        }

        Destroy(_replica.gameObject);
        _replica = null;
    }

    private void SetReplicaEnabled(bool enabled)
    {
        if (!_replica)
        {
            return;
        }

        _replica.enabled = enabled;
        _replica.gameObject.SetActive(enabled);
    }

    private void MoveReplicaBehindText()
    {
        if (!_replica || !_textRect || _replica.transform.parent != _textRect.parent)
        {
            return;
        }

        var textIndex = _textRect.GetSiblingIndex();
        var replicaIndex = _replica.transform.GetSiblingIndex();
        var targetIndex = replicaIndex < textIndex ? textIndex - 1 : textIndex;
        _replica.transform.SetSiblingIndex(Mathf.Max(0, targetIndex));
    }

    private void EnsureBlurMaterial()
    {
        if (_blurMaterial)
        {
            return;
        }

        var shader = Shader.Find("Hidden/TmpWideShadow/Blur");
        if (!shader)
        {
            return;
        }

        _blurMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
    }

    private void EnsureCompositeMaterial()
    {
        if (_compositeMaterial)
        {
            return;
        }

        var shader = Shader.Find("Hidden/TmpWideShadow/Composite");
        if (!shader)
        {
            return;
        }

        _compositeMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
    }

    private void ReleaseBlurMaterial()
    {
        if (!_blurMaterial)
        {
            return;
        }

        Destroy(_blurMaterial);
        _blurMaterial = null;
    }

    private void ReleaseCompositeMaterial()
    {
        if (!_compositeMaterial)
        {
            return;
        }

        Destroy(_compositeMaterial);
        _compositeMaterial = null;
    }

    private void EnsureDrawMaterialCapacity(TMP_MeshInfo[] meshInfos)
    {
        while (_drawMaterials.Count < meshInfos.Length)
        {
            _drawMaterials.Add(null);
        }

        for (var i = 0; i < meshInfos.Length; i++)
        {
            var sourceMaterial = meshInfos[i].material;
            if (!sourceMaterial)
            {
                continue;
            }

            var current = _drawMaterials[i];
            if (current && current.shader == sourceMaterial.shader)
            {
                continue;
            }

            if (current)
            {
                Destroy(current);
            }

            _drawMaterials[i] = new Material(sourceMaterial)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
    }

    private void ReleaseDrawMaterials()
    {
        for (var i = 0; i < _drawMaterials.Count; i++)
        {
            if (_drawMaterials[i])
            {
                Destroy(_drawMaterials[i]);
            }
        }

        _drawMaterials.Clear();

        for (var i = 0; i < _drawMeshes.Count; i++)
        {
            if (_drawMeshes[i])
            {
                Destroy(_drawMeshes[i]);
            }
        }

        _drawMeshes.Clear();
    }

    private Mesh GetPreparedMesh(int index, Mesh sourceMesh, int vertexCount)
    {
        while (_drawMeshes.Count <= index)
        {
            _drawMeshes.Add(null);
        }

        var mesh = _drawMeshes[index];
        if (!mesh)
        {
            mesh = new Mesh
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = $"{nameof(TMProWideShadow)}Mesh_{index}",
            };
            mesh.MarkDynamic();
            _drawMeshes[index] = mesh;
        }

        CopyAndScaleMesh(sourceMesh, mesh, vertexCount);
        return mesh;
    }

    private void CopyAndScaleMesh(Mesh sourceMesh, Mesh destinationMesh, int vertexCount)
    {
        var vertices = sourceMesh.vertices;
        var scale = Mathf.Max(0.05f, 1f + spread);
        if (!Mathf.Approximately(scale, 1f))
        {
            for (var vertexIndex = 0; vertexIndex + 3 < vertexCount; vertexIndex += 4)
            {
                var center = (vertices[vertexIndex] + vertices[vertexIndex + 1] + vertices[vertexIndex + 2] + vertices[vertexIndex + 3]) * 0.25f;
                vertices[vertexIndex] = center + (vertices[vertexIndex] - center) * scale;
                vertices[vertexIndex + 1] = center + (vertices[vertexIndex + 1] - center) * scale;
                vertices[vertexIndex + 2] = center + (vertices[vertexIndex + 2] - center) * scale;
                vertices[vertexIndex + 3] = center + (vertices[vertexIndex + 3] - center) * scale;
            }
        }

        destinationMesh.Clear(false);
        destinationMesh.vertices = vertices;
        destinationMesh.uv = sourceMesh.uv;
        destinationMesh.uv2 = sourceMesh.uv2;
        destinationMesh.colors32 = sourceMesh.colors32;
        destinationMesh.normals = sourceMesh.normals;
        destinationMesh.tangents = sourceMesh.tangents;
        destinationMesh.triangles = sourceMesh.triangles;
        destinationMesh.bounds = sourceMesh.bounds;
    }

    private int ComputeStateHash()
    {
        CacheComponents();
        var rect = _textRect.rect;
        var canvasScale = GetCanvasScaleFactor();

        var hash = new HashCode();
        hash.Add(_text.text);
        hash.Add(_text.fontSize);
        hash.Add(_text.color);
        hash.Add(_text.fontSharedMaterial ? _text.fontSharedMaterial.GetInstanceID() : 0);
        hash.Add(rect.size);
        hash.Add(_textRect.anchoredPosition);
        hash.Add(_textRect.lossyScale);
        hash.Add(canvasScale);
        hash.Add(color);
        hash.Add(offset);
        hash.Add(blurRadius);
        hash.Add(spread);
        hash.Add(intensity);
        hash.Add(padding);
        hash.Add(downsample);
        hash.Add((int)updateMode);
        return hash.ToHashCode();
    }

    private void SetDirty()
    {
        _isDirty = true;
    }

    private void OnTextChanged(UnityEngine.Object changedObject)
    {
        if (changedObject == _text)
        {
            SetDirty();
        }
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
        EnsureBlurMaterial();
        EnsureCompositeMaterial();
        EnsureReplica();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        SetDirty();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        ReleaseOutputTexture();
        SetReplicaEnabled(false);
    }

    private void LateUpdate()
    {
        if (updateMode != UpdateMode.Auto)
        {
            return;
        }

        RenderShadowIfNeeded(force: false);
    }

    private void OnDestroy()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        ReleaseOutputTexture();
        ReleaseDrawMaterials();
        ReleaseBlurMaterial();
        ReleaseCompositeMaterial();
        DestroyReplica();
    }
}
