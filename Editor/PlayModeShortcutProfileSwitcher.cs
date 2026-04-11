using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace GardenGnome.Editor.Utils
{
    /// <summary>
    /// プレイモード中だけ一時ショートカットプロファイルへ切り替え、
    /// 修飾なしのFキーを使うEditorショートカットを無効化する。
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeShortcutProfileSwitcher
    {
        private const string TempProfilePrefix = "GardenGnome.PlayModeNoPlainF";
        private const string OriginalProfileKey = "GardenGnome.PlayModeShortcutProfileSwitcher.OriginalProfile";
        private const string TempProfileKey = "GardenGnome.PlayModeShortcutProfileSwitcher.TempProfile";
        private const string IsActiveKey = "GardenGnome.PlayModeShortcutProfileSwitcher.IsActive";

        static PlayModeShortcutProfileSwitcher()
        {
            CleanupStaleProfile();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    ActivatePlayModeProfile();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    RestoreOriginalProfile();
                    break;
            }
        }

        private static void ActivatePlayModeProfile()
        {
            if (SessionState.GetBool(IsActiveKey, false))
                return;

            var shortcutManager = ShortcutManager.instance;
            var originalProfileId = shortcutManager.activeProfileId;
            var tempProfileId = CreateTempProfileId();
            var bindings = CaptureBindings(shortcutManager);

            DeleteProfileIfExists(shortcutManager, tempProfileId);
            shortcutManager.CreateProfile(tempProfileId);
            shortcutManager.activeProfileId = tempProfileId;

            foreach (var pair in bindings)
            {
                shortcutManager.RebindShortcut(pair.Key, pair.Value);

                if (UsesPlainF(pair.Value))
                    shortcutManager.RebindShortcut(pair.Key, ShortcutBinding.empty);
            }

            SessionState.SetString(OriginalProfileKey, originalProfileId);
            SessionState.SetString(TempProfileKey, tempProfileId);
            SessionState.SetBool(IsActiveKey, true);
        }

        private static void RestoreOriginalProfile()
        {
            if (!SessionState.GetBool(IsActiveKey, false))
                return;

            var shortcutManager = ShortcutManager.instance;
            var originalProfileId = SessionState.GetString(OriginalProfileKey, string.Empty);
            var tempProfileId = SessionState.GetString(TempProfileKey, string.Empty);

            if (!string.IsNullOrEmpty(originalProfileId))
                shortcutManager.activeProfileId = originalProfileId;

            DeleteProfileIfExists(shortcutManager, tempProfileId);
            ClearSessionState();
        }

        private static void CleanupStaleProfile()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!SessionState.GetBool(IsActiveKey, false))
                return;

            RestoreOriginalProfile();
        }

        private static Dictionary<string, ShortcutBinding> CaptureBindings(IShortcutManager shortcutManager)
        {
            var bindings = new Dictionary<string, ShortcutBinding>();

            foreach (var shortcutId in shortcutManager.GetAvailableShortcutIds())
                bindings[shortcutId] = shortcutManager.GetShortcutBinding(shortcutId);

            return bindings;
        }

        private static bool UsesPlainF(ShortcutBinding binding)
        {
            foreach (var keyCombination in binding.keyCombinationSequence)
            {
                if (keyCombination.keyCode != KeyCode.F)
                    continue;

                if (HasAnyModifier(keyCombination))
                    continue;

                return true;
            }

            return false;
        }

        private static bool HasAnyModifier(KeyCombination keyCombination)
        {
            return keyCombination.alt
                   || keyCombination.action
                   || keyCombination.shift;
        }

        private static void DeleteProfileIfExists(IShortcutManager shortcutManager, string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
                return;

            foreach (var existingProfileId in shortcutManager.GetAvailableProfileIds())
            {
                if (!string.Equals(existingProfileId, profileId, StringComparison.Ordinal))
                    continue;

                shortcutManager.DeleteProfile(profileId);
                return;
            }
        }

        private static string CreateTempProfileId()
        {
            return $"{TempProfilePrefix}.{Application.productName}";
        }

        private static void ClearSessionState()
        {
            SessionState.EraseString(OriginalProfileKey);
            SessionState.EraseString(TempProfileKey);
            SessionState.EraseBool(IsActiveKey);
        }
    }
}
