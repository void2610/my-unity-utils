using UnityEngine;
using UnityEngine.EventSystems;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// EventSystemで現在選択されているGameObjectを追跡するデバッグツール
    /// UIナビゲーションやフォーカス状態を視覚的に確認できる
    ///
    /// 使用例:
    /// - UIキーボード/コントローラーナビゲーションのデバッグ
    /// - フォーカスが正しく移動しているか確認
    /// - EventSystemの選択状態の追跡
    /// - UI入力の問題診断
    ///
    /// 使い方:
    /// 1. 任意のGameObjectにアタッチ
    /// 2. ゲームを実行
    /// 3. Inspectorの「Target Object」フィールドで現在選択中のUIを確認
    /// 4. キーボード/コントローラーでUIを操作すると、選択中の要素がリアルタイムで表示される
    ///
    /// ヒント:
    /// - 複数のUIシーンで使い回す場合はDontDestroyOnLoadを使用すると便利
    /// - EventSystemが存在しない場合は何も表示されません
    /// </summary>
    public class CurrentSelectedGameObjectChecker : MonoBehaviour
    {
        [SerializeField] private GameObject targetObject;

        private void Update()
        {
            targetObject = EventSystem.current.currentSelectedGameObject;
        }
    }
}
