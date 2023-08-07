#if UNITY_EDITOR
using UnityEngine;
using System;

namespace GSpawn
{
    [Serializable]
    public struct PluginGuid : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string      _guidString;
        private Guid        _guid;

        public Guid         guid            { get { return _guid; } }

        public PluginGuid(Guid guid)
        {
            _guid = guid;
            _guidString = _guid.ToString();
        }

        public override int GetHashCode()
        {
            return guid.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PluginGuid)) return false;

            PluginGuid other = (PluginGuid)obj;
            return guid.Equals(other.guid);
        }

        public override string ToString()
        {
            return guid.ToString();
        }

        public static bool operator ==(PluginGuid guid0, PluginGuid guid1)
        {
            return guid0.guid == guid1.guid;
        }

        public static bool operator !=(PluginGuid guid0, PluginGuid guid1)
        {
            return guid0.guid != guid1.guid;
        }

        public void OnAfterDeserialize()
        {
            _guid = Guid.Parse(_guidString);
        }

        public void OnBeforeSerialize()
        {
            _guidString = guid.ToString();
        }

        private void generateGuid()
        {
            if (!string.IsNullOrEmpty(_guidString))
            {
                _guid = new Guid(_guidString);
            }
            else
            {
                _guid = Guid.NewGuid();
                _guidString = _guid.ToString();
            }
        }
    }
}
#endif