using System.Collections.Generic;
using UnityEngine;

namespace Void2610.UnityTemplate.Tests
{
    /// <summary>
    /// DirectoryAssetListRegistererTest で登録し直される一覧
    /// </summary>
    public class DirectoryAssetListTestList : ScriptableObject, IDirectoryAssetList
    {
        [SerializeField] private List<DirectoryAssetListTestEntry> entries = new();

        public IReadOnlyList<DirectoryAssetListTestEntry> Entries => entries;

        public void RegisterAssets() => this.RegisterAssetsInSameDirectory(entries, x => x.name);
    }
}
