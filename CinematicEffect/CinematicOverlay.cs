using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// CinematicEffect の <see cref="ScreenFadeEffect"/> / <see cref="ImageFlashEffect"/> 等が利用する
/// 全画面オーバーレイ Image を、 シーン上に事前配置することなくコードから宣言的に自動生成する Singleton。
///
/// Effect が初めて呼ばれた瞬間に Canvas (ScreenSpaceOverlay / sortingOrder=short.MaxValue) と
/// 全画面ストレッチの透明 Image が一括構築される。 各シーン専用に生きるため、 シーン遷移ごとに作り直される。
/// </summary>
public sealed class CinematicOverlay : SingletonMonoBehaviour<CinematicOverlay>
{
    private Image _image;

    /// <summary>フェード / フラッシュの塗りつぶし対象となる全画面 Image。</summary>
    public Image Image
    {
        get
        {
            if (_image == null) BuildOverlay();
            return _image;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        // シーンごとに使う前提 (DontDestroyOnLoad しない)。 ルート扱いされて DDOL が付くケースをここで打ち消す
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        BuildOverlay();
    }

    private void BuildOverlay()
    {
        if (_image != null) return;

        var canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // 通常 UI より常に手前
        if (gameObject.GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();

        var imgGo = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imgGo.transform.SetParent(transform, false);
        var rt = (RectTransform)imgGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        _image = imgGo.GetComponent<Image>();
        _image.color = new Color(0f, 0f, 0f, 0f);
        _image.raycastTarget = false;
    }
}
