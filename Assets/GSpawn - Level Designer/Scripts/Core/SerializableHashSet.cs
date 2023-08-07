#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public class IntHashSet         : SerializableHashSet<int> {}
    [Serializable]
    public class PluginGuidHashSet  : SerializableHashSet<PluginGuid> {}
    [Serializable]
    public class GameObjectHashSet  : SerializableHashSet<GameObject> {}

    // Note: Don't inherit from HashSet<T>. Doesn't serialize properly.
    [Serializable]
    public class SerializableHashSet<T> : ISerializationCallbackReceiver
    {
        private HashSet<T>  _hashSet            = new HashSet<T>();
        [SerializeField]
        private List<T>     _serializedList     = new List<T>();

        public HashSet<T>   hashSet             { get { return _hashSet; } }
        public int          Count               { get { return _hashSet.Count; } }

        public void OnBeforeSerialize()
        {
            _serializedList.Clear();

            foreach (T item in _hashSet)
                _serializedList.Add(item);
        }

        public void OnAfterDeserialize()
        {
            _hashSet.Clear();
            foreach (T item in _serializedList)
                _hashSet.Add(item);
        }

        public void Add(T item)
        {
            _hashSet.Add(item);
        }

        public bool Contains(T item)
        {
            return _hashSet.Contains(item);
        }

        public bool Remove(T item)
        {
            return _hashSet.Remove(item);
        }

        public void Clear()
        {
            _hashSet.Clear();
        }

        public int RemoveWhere(Predicate<T> match)
        {
            return _hashSet.RemoveWhere(match);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _hashSet.GetEnumerator();
        }
    }
}
#endif