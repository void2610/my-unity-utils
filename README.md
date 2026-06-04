# my-unity-utils

Unityプロジェクトで再利用可能なユーティリティスクリプト集

## 📦 概要

Unityゲーム開発で頻繁に使用するユーティリティスクリプトを34個収録しています。UI、アニメーション、オーディオ、デバッグツールなど、カテゴリ別に整理されています。

## 📂 スクリプト一覧

### UI/ (11ファイル)

- **ButtonSe.cs** - ボタンSE自動再生コンポーネント
- **ButtonSelectionGlow.cs** - ボタン選択/ホバー時のグロー効果
- **ButtonTween.cs** - ボタンアニメーション
- **CanvasGroupSwitcher.cs** - CanvasGroupの切り替え管理
- **FadeImageView.cs** - 画像フェード演出
- **MultiImageButton.cs** - 複数Graphic対応ボタン
- **MyButton.cs** - 拡張ボタンコンポーネント
- **SceneSwitchLeftButton.cs** - シーン切り替えボタン
- **TextAutoSizer.cs** - テキスト自動サイズ調整
- **TMPInputFieldCaretFixer.cs** - TextMeshPro InputFieldのキャレット修正
- **UILineRenderer.cs** - UI用ライン描画

### Animation/ (2ファイル)

- **FloatMove.cs** - オブジェクトの浮遊アニメーション
- **SpriteSheetAnimator.cs** - スプライトシートアニメーション再生

### Core/ (4ファイル)

- **ExtendedMethods.cs** - 拡張メソッド集（Transform, Image, Text等）
- **SerializableDictionary.cs** - Unity-serializable Dictionary実装
- **SingletonMonoBehaviour.cs** - スレッドセーフシングルトン
- **Utils.cs** - 汎用ユーティリティ関数

### Audio/ (2ファイル)

- **BgmManager.cs** - BGM再生管理（LitMotionフェード、ダッキング対応）
- **SeManager.cs** - SE再生管理（20チャンネル、重要度制御）

### Debug/ (3ファイル)

- **CurrentSelectedGameObjectChecker.cs** - UI選択状態デバッグツール
- **DebugLogDisplay.cs** - ゲーム画面上へのログ表示
- **GameViewCapture.cs** - ゲームビュースクリーンショット撮影

### System/ (12ファイル)

- **CameraAspectRatioHandler.cs** - カメラアスペクト比管理
- **CameraShake.cs** - カメラシェイク効果
- **CanvasAspectRatioFitter.cs** - Canvasアスペクト比調整
- **CreditService.cs** - クレジット表示サービス
- **DataPersistence.cs** - プラットフォーム非依存データ保存
- **InputActionExtensions.cs** - Input System + R3統合
- **IrisShot.cs** - アイリスショットトランジション
- **LicenseService.cs** - ライセンス管理サービス
- **RandomManager.cs** - シード付き乱数生成
- **RenderTextureAspectManager.cs** - RenderTextureアスペクト管理
- **TweetService.cs** - Twitter投稿サービス
- **VersionText.cs** - バージョン情報表示

## 🔧 使用方法

### Git Submoduleとして使用（推奨）

my-unity-templateと組み合わせて使用する場合：

```bash
# 自動セットアップ（my-unity-templateを使用）
# Unity Editor: Tools > Unity Template > Setup Utils Submodule
```

手動セットアップ：

```bash
# プロジェクトルートにSubmoduleを追加
git submodule add https://github.com/void2610/my-unity-utils.git my-unity-utils

# シンボリックリンクを作成
# Windows:
mklink /J Assets\Scripts\Utils ..\..\my-unity-utils

# macOS/Linux:
ln -s ../../my-unity-utils Assets/Scripts/Utils
```

### 直接コピー

```bash
# スクリプトをプロジェクトに直接コピー
cp -r my-unity-utils/* <YourUnityProject>/Assets/Scripts/Utils/
```

## 📚 依存関係

一部のスクリプトは以下のパッケージに依存しています：

- **Unity Input System** - InputActionExtensions.cs
- **TextMeshPro** - 各種TMPro関連スクリプト
- **R3** - ExtendedMethods.cs（条件付きコンパイル）
- **UniTask** - 各種async/await対応スクリプト
- **LitMotion** - BgmManager.cs, FloatMove.cs等
- **UIEffect** - IrisShot.cs
- **Addressables** - IrisShot.cs（条件付きコンパイル）

### Addressablesを使用する場合

IrisShot.csでAddressables機能を使用する場合は、以下の設定が必要です：

1. Addressablesパッケージをインストール
2. Scripting Define Symbolsに`ADDRESSABLES`を追加
   - Unity Editor > Project Settings > Player > Other Settings > Scripting Define Symbols
   - `ADDRESSABLES`を追加して適用

**注意:** `ADDRESSABLES`シンボルを定義しない場合、IrisShotはエラーメッセージを出力して動作しません。

## 🔄 更新方法

### Submoduleとして使用している場合

```bash
# 最新版を取得
cd my-unity-utils
git pull origin main
cd ..
git add my-unity-utils
git commit -m "Update my-unity-utils submodule"
```

### スクリプトを編集した場合

```bash
cd my-unity-utils
git add .
git commit -m "Update utility scripts"
git push
```

## 📄 ライセンス

MIT License

## 🔗 関連リポジトリ

- **[my-unity-template](https://github.com/void2610/my-unity-template)** - Unity開発環境自動セットアップツール
