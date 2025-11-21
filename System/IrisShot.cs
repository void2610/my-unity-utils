using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// アイリスショット（円形ワイプ）トランジションエフェクト
    /// 画面を円形に開閉するシーン遷移演出を提供
    ///
    /// 使用例:
    /// - シーン遷移時の演出
    /// - ドラマチックな場面転換
    /// - フォーカス効果
    /// - レトロゲーム風の画面切り替え
    ///
    /// 必要な依存関係:
    /// - UIEffect (Coffee.UIExtensions.Unmask)
    /// - UniTask (Cysharp.Threading.Tasks)
    /// - LitMotion
    /// - Addressables
    ///
    /// セットアップ手順:
    /// 1. UIEffectパッケージをインストール
    /// 2. IrisShotプレハブを作成:
    ///    - Canvas > Image (黒い全画面画像)
    ///    - その子に小さな白い円形Image + Unmaskコンポーネント
    /// 3. プレハブをAddressablesに登録（アドレス: "IrisShot"）
    ///
    /// 使い方:
    /// // シーンアウト（画面を閉じる）
    /// await IrisShot.StartIrisOut();
    /// // シーン切り替え処理
    /// SceneManager.LoadScene("NextScene");
    ///
    /// // シーンイン（画面を開く）
    /// await IrisShot.StartIrisIn();
    ///
    /// // 特定のCanvasを指定
    /// await IrisShot.StartIrisOut(myCanvas);
    /// </summary>
    public class IrisShot
    {
        private static GameObject _irisShotObj;

        /// <summary>
        /// シーン遷移時にクリアする
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _irisShotObj = null;
        }

        /// <summary>
        /// アイリスアウト（画面を閉じる）
        /// 大きな円から小さな円へとスケールダウンし、最終的に画面全体が黒くなる
        /// </summary>
        /// <param name="canvas">エフェクトを配置するCanvas（nullの場合は自動検索）</param>
        /// <returns>完了を待機可能なUniTask</returns>
        public static async UniTask StartIrisOut(Canvas canvas = null)
        {
            var irisShotObj = await LoadIrisShotObj(canvas);
            var unMask = irisShotObj.GetComponentInChildren<Unmask>();

            unMask.transform.localScale = Vector3.one * 25f;

            await LMotion.Create(Vector3.one * 25f, Vector3.one * 0.5f, 0.5f)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(scale => unMask.transform.localScale = scale)
                .ToUniTask();

            await LMotion.Create(Vector3.one * 0.5f, Vector3.one * 1.5f, 0.3f)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(scale => unMask.transform.localScale = scale)
                .ToUniTask();

            await LMotion.Create(Vector3.one * 1.5f, Vector3.zero, 0.5f)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(scale => unMask.transform.localScale = scale)
                .ToUniTask();

            await UniTask.Delay(500, ignoreTimeScale: true);
        }

        /// <summary>
        /// アイリスイン（画面を開く）
        /// 小さな円から大きな円へとスケールアップし、最終的に画面全体が見えるようになる
        /// </summary>
        /// <param name="canvas">エフェクトを配置するCanvas（nullの場合は自動検索）</param>
        /// <returns>完了を待機可能なUniTask</returns>
        public static async UniTask StartIrisIn(Canvas canvas = null)
        {
            var irisShotObj = await LoadIrisShotObj(canvas);
            var unMask = irisShotObj.GetComponentInChildren<Unmask>();

            unMask.transform.localScale = Vector3.zero;

            await LMotion.Create(Vector3.zero, Vector3.one * 0.5f, 0.3f)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(scale => unMask.transform.localScale = scale)
                .ToUniTask();

            await LMotion.Create(Vector3.one * 0.5f, Vector3.one * 1.5f, 0.5f)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(scale => unMask.transform.localScale = scale)
                .ToUniTask();

            await LMotion.Create(Vector3.one * 1.5f, Vector3.one * 25f, 0.3f)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(scale => unMask.transform.localScale = scale)
                .ToUniTask();

            await UniTask.Delay(500, ignoreTimeScale: true);
        }

        /// <summary>
        /// IrisShotプレハブをロードしてインスタンス化
        /// シングルトンパターンで既存のインスタンスがあればそれを返す
        /// </summary>
        /// <param name="canvas">エフェクトを配置するCanvas（nullの場合は自動検索）</param>
        /// <returns>IrisShotのGameObject</returns>
        public static async UniTask<GameObject> LoadIrisShotObj(Canvas canvas = null)
        {
            if (_irisShotObj) return _irisShotObj;

            // キャンバスを探す
            if (!canvas)
            {
                canvas = Object.FindFirstObjectByType<Canvas>();
                if (!canvas)
                {
                    Debug.LogError("Canvas not found in the scene.");
                    return null;
                }
            }

            var prefab = await Addressables.LoadAssetAsync<GameObject>("IrisShot").ToUniTask();
            var instance = Object.Instantiate(prefab, canvas.transform);
            _irisShotObj = instance;
            return _irisShotObj;
        }
    }
}
