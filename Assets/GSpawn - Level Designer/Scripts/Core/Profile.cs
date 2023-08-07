#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public abstract class Profile : ScriptableObject
    {
        [SerializeField]
        private string  _profileName        = string.Empty;

        public string   profileName         { get { return _profileName; } set { if (!string.IsNullOrEmpty(value)) { _profileName = value; name = _profileName; } } }

        public override string ToString()
        {
            return _profileName;
        }
    }
}
#endif