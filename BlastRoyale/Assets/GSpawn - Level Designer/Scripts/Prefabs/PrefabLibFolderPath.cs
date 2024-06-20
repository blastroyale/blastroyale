#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public class PrefabLibFolderPath
    {
        [SerializeField]
        private string      _path           = string.Empty;
        [SerializeField]
        private string      _reveresedPath  = string.Empty;

        public string       path            { get { return _path; } }
        public string       reversedPath    { get { return _reveresedPath; } }
        public bool         isValid         { get { return !string.IsNullOrEmpty(_path); } }

        public void set(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            _path           = folderPath;
            _reveresedPath  = string.Empty;
            var folders     = new List<string>(_path.Split(new char[] {'/', '\\' }));

            for (int folderIndex = folders.Count - 1; folderIndex >= 0; --folderIndex)
            {
                _reveresedPath += folders[folderIndex];
                if (folderIndex != 0) _reveresedPath += "/";
            }
        }
    }
}
#endif