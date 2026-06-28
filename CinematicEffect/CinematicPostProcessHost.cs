using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Void2610.UnityTemplate;

/// <summary>
/// CinematicEffect の PostProcess 系演出 (Vignette / ChromaticAberration / LensDistortion / ...) が
/// 利用する URP <see cref="UnityEngine.Rendering.Volume"/> を載せる単一の GameObject を、
/// シーン上の事前配置不要でコードから宣言的に自動生成する Singleton。
///
/// 各 PostProcessVolumeEffectBase が初回再生時にこの Singleton の gameObject に Volume コンポーネントを
/// AddComponent する。 さらに <see cref="EnsureCameraPostProcessingEnabled"/> で Camera.main の URP
/// <c>renderPostProcessing</c> フラグを ON にする (URP カメラ設定で OFF だと Volume がいくら効いていても
/// 画面に反映されないため)。
/// </summary>
public sealed class CinematicPostProcessHost : SingletonMonoBehaviour<CinematicPostProcessHost>
{
    /// <summary>
    /// Camera.main の URP <c>renderPostProcessing</c> を ON にする。 PostProcessVolumeEffectBase が
    /// 初回再生時に呼ぶ (Awake では Camera.main が未準備なケースがあるため、 都度確実に有効化する)。
    /// </summary>
    public static void EnsureCameraPostProcessingEnabled()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var data = cam.GetUniversalAdditionalCameraData();
        if (data != null) data.renderPostProcessing = true;
    }

    protected override void Awake()
    {
        base.Awake();
        // シーンごとに使う前提 (DontDestroyOnLoad しない)。 ルート扱いされて DDOL が付くケースをここで打ち消す
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        EnsureCameraPostProcessingEnabled();
    }
}
