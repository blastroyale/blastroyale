#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class RandomPrefabProfileDb : ProfileDb<RandomPrefabProfile>
    {
        private static RandomPrefabProfileDb    _instance;

        [NonSerialized]
        private RandomPrefabProfileDbUI         _ui;
        [NonSerialized]
        private List<RandomPrefab>              _randomPrefabBuffer     = new List<RandomPrefab>();

        public RandomPrefabProfileDbUI          ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<RandomPrefabProfileDbUI>(PluginFolders.randomPrefabProfiles);

                return _ui;
            }
        }
        public override string                  folderPath              { get { return PluginFolders.randomPrefabProfiles; } }

        public static RandomPrefabProfileDb     instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDbEx.loadScriptableObject<RandomPrefabProfileDb>(PluginFolders.randomPrefabProfiles);
                    _instance.deleteNullPrefabs();
                }

                return _instance;
            }
        }
        public static bool                      exists                  { get { return _instance != null; } }

        public void deletePrefabs(List<PluginPrefab> pluginPrefabs)
        {
            getPrefabs(pluginPrefabs, _randomPrefabBuffer);
            deletePrefabs(_randomPrefabBuffer);

            if (_ui != null) _ui.refresh();
        }

        public void getPrefabs(List<PluginPrefab> pluginPrefabs, List<RandomPrefab> randomPrefabs)
        {
            randomPrefabs.Clear();
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).getPrefabs(pluginPrefabs, randomPrefabs, true);
        }

        public void deletePrefabs(List<RandomPrefab> randomPrefabs)
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).deletePrefabs(randomPrefabs);
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).onPrefabAssetWillBeDeleted(prefabAsset);

            if (_ui != null) _ui.onPrefabAssetWillBeDeleted(prefabAsset);
        }

        public void deleteNullPrefabs()
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).deleteNullPrefabs();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_ui);
        }
    }
}
#endif