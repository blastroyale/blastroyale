#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey>                  _serializedKeys     = new List<TKey>();
        [SerializeField]
        private List<TValue>                _serializedValues   = new List<TValue>();

        public void OnBeforeSerialize()
        {
            _serializedKeys.Clear();
            _serializedValues.Clear();

            foreach(var pair in this)
            {
                _serializedKeys.Add(pair.Key);
                _serializedValues.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();

            for (int pairIndex = 0; pairIndex < _serializedKeys.Count; ++pairIndex)
                Add(_serializedKeys[pairIndex], _serializedValues[pairIndex]);
        }
    }
}
#endif