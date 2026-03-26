using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
#pragma warning disable IDE1006
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

        [FormerlySerializedAs("_serializedList")]
        [SerializeField]
        private List<Pair> serializedList = new List<Pair>();
#pragma warning restore IDE1006

        public IEnumerable<KeyValuePair<TKey, TValue>> EnumerateSerializedOrder()
        {
            foreach (var pair in serializedList)
            {
                if (pair == null)
                {
                    continue;
                }

                if (TryGetValue(pair.key, out var value))
                {
                    yield return new KeyValuePair<TKey, TValue>(pair.key, value);
                }
            }
        }

        /// <summary>
        /// Unityがオブジェクトをデシリアライズした後に呼ばれる
        /// シリアライズされたリストを辞書に変換する
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();

            foreach (var pair in serializedList)
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
        }
    }
}
