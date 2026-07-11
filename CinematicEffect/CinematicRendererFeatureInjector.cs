using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// フルスクリーンシェーダー系エフェクトの <see cref="ScriptableRendererFeature"/> を、
/// URP レンダラ資産へ事前配置せずコードから実行時注入する。プロジェクト間の可搬性を保つため、
/// レンダラ資産 (Renderer2D.asset 等) を人手で編集しない (materal / overlay と同じ「自己調達」方針)。
/// </summary>
public static class CinematicRendererFeatureInjector
{
    private static readonly FieldInfo RendererDataListField =
        typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly HashSet<System.Type> Injected = new();

    /// <summary>指定フィーチャが現在の URP レンダラ群に無ければ生成して追加する (一度きり)。SetDirty でレンダラを再生成させて反映する。</summary>
    public static void EnsureFeature<T>() where T : ScriptableRendererFeature
    {
        if (Injected.Contains(typeof(T))) return;
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp) return;
        if (RendererDataListField?.GetValue(urp) is not ScriptableRendererData[] dataList) return;

        foreach (var data in dataList)
        {
            if (data == null) continue;
            if (HasFeature<T>(data)) continue;

            var feature = ScriptableObject.CreateInstance<T>();
            feature.name = typeof(T).Name;
            // 資産へ焼き込ませない (実行時のみの注入。エディタで誤って保存されても資産を汚さない)
            feature.hideFlags = HideFlags.HideAndDontSave;
            data.rendererFeatures.Add(feature);
            feature.Create();
            data.SetDirty(); // レンダラ再生成を促し、追加フィーチャを反映させる
        }

        Injected.Add(typeof(T));
    }

    private static bool HasFeature<T>(ScriptableRendererData data) where T : ScriptableRendererFeature
    {
        foreach (var f in data.rendererFeatures)
        {
            if (f is T) return true;
        }

        return false;
    }
}
