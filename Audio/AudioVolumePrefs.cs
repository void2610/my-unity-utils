namespace Void2610.UnityTemplate
{
    /// <summary>
    /// BGM / SE の音量を保存する PlayerPrefs キーの置き場
    /// PlayerPrefs は保存先を差し替えられないので、テストが本番の音量を書き換えないよう接頭辞で別キーへ逃がせるようにする
    /// </summary>
    public static class AudioVolumePrefs
    {
        /// <summary>音量キーの接頭辞。null なら本番のキーをそのまま使う</summary>
        public static string KeyPrefix { get; set; }

        public static string BgmVolumeKey => KeyPrefix + "BgmVolume";
        public static string SeVolumeKey => KeyPrefix + "SeVolume";
    }
}
