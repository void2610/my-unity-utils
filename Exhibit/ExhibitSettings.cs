using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// 展示モードの設定を管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "ExhibitSettings", menuName = "Settings/ExhibitSettings")]
    public class ExhibitSettings : ScriptableObject
    {
        [Header("マスタースイッチ")]
        [SerializeField] private bool enableExhibitMode;

        [Header("シーン設定")]
        [SerializeField] private string idleReturnSceneName = "TitleScene";
        [SerializeField] private string sessionLimitSceneName = "ThanksScene";
        [SerializeField] private string[] idleSkipSceneNames = { "TitleScene", "ThanksScene" };

        [Header("アイドル時ポーズ＆タイトル戻り")]
        [SerializeField] private bool enableIdlePauseAndReturn = true;
        [SerializeField] private float idleToPauseSeconds = 60f;
        [SerializeField] private float pauseToTitleSeconds = 30f;

        [Header("セッション時間制限")]
        [SerializeField] private bool enableSessionTimeLimit = true;
        [SerializeField] private float sessionTimeLimitSeconds = 600f;

        [Header("タイトル画面PV再生")]
        [SerializeField] private bool enableTitleIdlePV = true;
        [SerializeField] private float titleIdleToPVSeconds = 30f;

        /// <summary>展示モード有効か</summary>
        public bool EnableExhibitMode => enableExhibitMode;

        /// <summary>アイドル時の戻り先シーン名</summary>
        public string IdleReturnSceneName => idleReturnSceneName;
        /// <summary>セッション制限時の遷移先シーン名</summary>
        public string SessionLimitSceneName => sessionLimitSceneName;
        /// <summary>アイドル検知をスキップするシーン名一覧</summary>
        public string[] IdleSkipSceneNames => idleSkipSceneNames;

        /// <summary>アイドル時ポーズ＆タイトル戻りが有効か</summary>
        public bool EnableIdlePauseAndReturn => enableExhibitMode && enableIdlePauseAndReturn;
        /// <summary>アイドル→ポーズまでの秒数</summary>
        public float IdleToPauseSeconds => idleToPauseSeconds;
        /// <summary>ポーズ→タイトル戻りまでの秒数</summary>
        public float PauseToTitleSeconds => pauseToTitleSeconds;

        /// <summary>セッション時間制限が有効か</summary>
        public bool EnableSessionTimeLimit => enableExhibitMode && enableSessionTimeLimit;
        /// <summary>セッション制限秒数</summary>
        public float SessionTimeLimitSeconds => sessionTimeLimitSeconds;

        /// <summary>タイトルPV再生が有効か</summary>
        public bool EnableTitleIdlePV => enableExhibitMode && enableTitleIdlePV;
        /// <summary>タイトルでアイドル→PV再生までの秒数</summary>
        public float TitleIdleToPVSeconds => titleIdleToPVSeconds;
    }
}
