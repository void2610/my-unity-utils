using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// TextMeshPro InputFieldのキャレット問題を修正するコンポーネント
    /// キャレットと選択ハイライトのRaycast Targetを無効化して、
    /// UI操作時の意図しないクリック問題を解決
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class TMPInputFieldCaretFixer : MonoBehaviour
    {
        [Header("修正設定")]
        [SerializeField] private bool fixOnAwake = true; // Awake時に自動修正
        [SerializeField] private bool fixOnSelect = true; // 選択時に修正
        [SerializeField] private float fixDelay = 0.01f; // 修正実行の遅延時間

        private TMP_InputField _inputField;

        private void Awake()
        {
            _inputField = GetComponent<TMP_InputField>();

            if (fixOnSelect)
            {
                // フォーカスを受け取ったタイミングで遅延実行
                _inputField.onSelect.AddListener(_ => Invoke(nameof(DisableCaretRaycast), fixDelay));
            }

            if (fixOnAwake)
            {
                // Awake時にも実行（既に要素が存在する場合用）
                Invoke(nameof(DisableCaretRaycast), fixDelay);
            }
        }

        private void Start()
        {
            // Start時にも一度実行（確実に修正するため）
            Invoke(nameof(DisableCaretRaycast), fixDelay);
        }

        /// <summary>
        /// キャレットと選択ハイライトのRaycast Targetを無効化
        /// </summary>
        private void DisableCaretRaycast()
        {
            if (_inputField == null || _inputField.textComponent == null) return;

            // 親オブジェクトの中から "Caret" や "Selection Highlight" を探す
            var parent = _inputField.textComponent.transform.parent;
            if (parent == null) return;

            // キャレットのRaycast Targetを無効化
            var caret = parent.Find("Caret");
            if (caret != null && caret.TryGetComponent(out Graphic caretGraphic))
            {
                caretGraphic.raycastTarget = false;
                Debug.Log($"[TMPInputFieldCaretFixer] キャレットのRaycast Targetを無効化: {gameObject.name}");
            }

            // 選択ハイライトのRaycast Targetを無効化
            var highlight = parent.Find("Selection Highlight");
            if (highlight != null && highlight.TryGetComponent(out Graphic highlightGraphic))
            {
                highlightGraphic.raycastTarget = false;
                Debug.Log($"[TMPInputFieldCaretFixer] 選択ハイライトのRaycast Targetを無効化: {gameObject.name}");
            }

            // 子オブジェクトも再帰的に検索（念のため）
            DisableCaretRaycastRecursive(parent);
        }

        /// <summary>
        /// 再帰的にキャレット関連要素を検索して修正
        /// </summary>
        private void DisableCaretRaycastRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                // キャレット関連の名前をチェック
                if (child.name.Contains("Caret") || child.name.Contains("Selection"))
                {
                    if (child.TryGetComponent(out Graphic graphic))
                    {
                        graphic.raycastTarget = false;
                    }
                }

                // 子オブジェクトも再帰的にチェック
                if (child.childCount > 0)
                {
                    DisableCaretRaycastRecursive(child);
                }
            }
        }

        /// <summary>
        /// 手動で修正を実行
        /// </summary>
        public void FixCaretRaycast()
        {
            DisableCaretRaycast();
        }

        /// <summary>
        /// InputFieldの参照を更新（動的に変更された場合用）
        /// </summary>
        public void RefreshInputField()
        {
            _inputField = GetComponent<TMP_InputField>();
            if (_inputField != null)
            {
                DisableCaretRaycast();
            }
        }

        /// <summary>
        /// 設定を変更
        /// </summary>
        public void UpdateSettings(bool fixOnAwakeNew, bool fixOnSelectNew, float fixDelayNew)
        {
            fixOnAwake = fixOnAwakeNew;
            fixOnSelect = fixOnSelectNew;
            fixDelay = fixDelayNew;
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタでテスト実行
        /// </summary>
        [ContextMenu("Fix Caret Raycast (Test)")]
        private void TestFixCaretRaycast()
        {
            if (Application.isPlaying)
            {
                DisableCaretRaycast();
            }
            else
            {
                Debug.Log("プレイモード中でのみ実行できます");
            }
        }
#endif
    }
}