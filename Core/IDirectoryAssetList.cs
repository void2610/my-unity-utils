namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 同じフォルダのアセットを並べて持つ一覧の ScriptableObject。
    /// 同じフォルダのアセットが追加・削除・移動されると、エディタの DirectoryAssetListRegisterer が RegisterAssets を呼んで保存する
    /// </summary>
    public interface IDirectoryAssetList
    {
        /// <summary>
        /// 同じフォルダのアセットを一覧へ登録し直す (エディタ専用。RegisterAssetsInSameDirectory を使う想定)
        /// </summary>
        void RegisterAssets();
    }
}
