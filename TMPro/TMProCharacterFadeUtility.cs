using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// TextMeshPro の文字フェード演出ユーティリティ。
/// </summary>
public static class TMProCharacterFadeUtility
{
    public readonly struct TextLineRange
    {
        public readonly int StartIndex;
        public readonly int EndIndex;

        public TextLineRange(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }

    public static void PrepareForCharacterFade(TextMeshProUGUI text)
    {
        SetTextAlpha(text, 1f);
        text.ForceMeshUpdate();

        var textInfo = text.textInfo;
        for (var i = 0; i < textInfo.characterCount; i++)
        {
            SetCharacterAlpha(textInfo, i, 0);
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    public static void BuildLineRanges(TextMeshProUGUI text, List<TextLineRange> ranges)
    {
        ranges.Clear();
        var textInfo = text.textInfo;

        for (var i = 0; i < textInfo.lineCount; i++)
        {
            var lineInfo = textInfo.lineInfo[i];
            ranges.Add(new TextLineRange(lineInfo.firstCharacterIndex, lineInfo.lastCharacterIndex));
        }
    }

    public static async UniTask FadeCharacterRangeAsync(
        TextMeshProUGUI text,
        TextLineRange range,
        float characterFadeDuration,
        CancellationToken ct)
    {
        for (var i = range.StartIndex; i <= range.EndIndex; i++)
        {
            await FadeCharacterAsync(text, i, characterFadeDuration, ct);
        }
    }

    public static async UniTask FadeAllCharactersAsync(
        TextMeshProUGUI text,
        float characterFadeDuration,
        CancellationToken ct)
    {
        var textInfo = text.textInfo;
        for (var i = 0; i < textInfo.characterCount; i++)
        {
            await FadeCharacterAsync(text, i, characterFadeDuration, ct);
        }
    }

    private static async UniTask FadeCharacterAsync(
        TextMeshProUGUI text,
        int characterIndex,
        float characterFadeDuration,
        CancellationToken ct)
    {
        var textInfo = text.textInfo;

        if (characterIndex < 0 || characterIndex >= textInfo.characterCount)
        {
            return;
        }

        if (!textInfo.characterInfo[characterIndex].isVisible)
        {
            return;
        }

        var safeDuration = Mathf.Max(0.01f, characterFadeDuration);
        var elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(elapsed / safeDuration) * byte.MaxValue);
            SetCharacterAlpha(textInfo, characterIndex, alpha);
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            await UniTask.Yield(cancellationToken: ct);
        }

        SetCharacterAlpha(textInfo, characterIndex, byte.MaxValue);
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private static void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        var color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private static void SetCharacterAlpha(TMP_TextInfo textInfo, int characterIndex, byte alpha)
    {
        var characterInfo = textInfo.characterInfo[characterIndex];
        if (!characterInfo.isVisible)
        {
            return;
        }

        var colors = textInfo.meshInfo[characterInfo.materialReferenceIndex].colors32;
        var vertexIndex = characterInfo.vertexIndex;

        for (var i = 0; i < 4; i++)
        {
            colors[vertexIndex + i].a = alpha;
        }
    }
}
