using System;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// ScriptableObject の List&lt;T&gt; フィールドに付けると、同じフォルダの T のアセットが追加・削除・移動されたときに、
    /// エディタの DirectoryAssetListRegisterer がアセット名順に登録し直して保存する
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SameDirectoryAssetsAttribute : Attribute
    {
    }
}
