using System;
using System.Collections.Generic;
using UnityEngine;

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Unityでシリアライズ可能な辞書実装
    /// Inspectorで辞書を編集可能にする
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : 
        Dictionary<TKey, TValue>, 
        ISerializationCallbackReceiver
    {
        [Serializable]
        public class Pair
        {
            public TKey key = default;
            public TValue value = default;

            public Pair(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }

        [SerializeField]
        private List<Pair> _serializedList = new List<Pair>();

        /// <summary>
        /// Unityがオブジェクトをデシリアライズした後に呼ばれる
        /// シリアライズされたリストを辞書に変換する
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();
            
            foreach (var pair in _serializedList)
            {
                if (pair.key != null && !ContainsKey(pair.key))
                {
                    Add(pair.key, pair.value);
                }
            }
        }

        /// <summary>
        /// Unityがオブジェクトをシリアライズする前に呼ばれる
        /// 辞書をシリアライズ可能なリストに変換する
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _serializedList.Clear();
            
            foreach (var kvp in this)
            {
                _serializedList.Add(new Pair(kvp.Key, kvp.Value));
            }
        }
    }
}