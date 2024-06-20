#if UNITY_EDITOR
using System;
using UnityEngine;

namespace GSpawn
{
    [Serializable]
    public class PrefabToPrefabData : SerializableDictionary<GameObject, PrefabData> { }

    public class PrefabDataDb : ScriptableObject
    {
        private static PrefabDataDb     _instance;

        [SerializeField]
        private PrefabToPrefabData      _prefabDataMap  = new PrefabToPrefabData();

        public static PrefabDataDb      instance
        {
            get
            {
                if (_instance == null) _instance = AssetDbEx.loadScriptableObject<PrefabDataDb>(PluginFolders.pluginInternal);
                return _instance;
            }
        }
        public static bool              exists          { get { return _instance != null; } }

        public void refresh()
        {
            _prefabDataMap.Clear();
        }

        public PrefabData getData(GameObject prefabAsset)
        {
            if (!_prefabDataMap.ContainsKey(prefabAsset))
                _prefabDataMap.Add(prefabAsset, new PrefabData(prefabAsset));

            return _prefabDataMap[prefabAsset];
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            if (_prefabDataMap.ContainsKey(prefabAsset))
                _prefabDataMap.Remove(prefabAsset);
        }

        public void deleteNullPrefabs()
        {
            var newMap = new PrefabToPrefabData();
            foreach (var pair in _prefabDataMap)
                if (pair.Key != null) newMap.Add(pair.Key, pair.Value);

            _prefabDataMap.Clear();
            _prefabDataMap = newMap;
        }
    }
}
#endif