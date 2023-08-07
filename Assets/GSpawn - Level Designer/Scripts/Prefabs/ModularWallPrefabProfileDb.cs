#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ModularWallPrefabProfileDb : ProfileDb<ModularWallPrefabProfile>
    {
        private static ModularWallPrefabProfileDb   _instance;

        private ModularWallPrefabProfileDbUI        _ui;

        public ModularWallPrefabProfileDbUI         ui
        {
            get
            {
                if (_ui == null) _ui = AssetDbEx.loadScriptableObject<ModularWallPrefabProfileDbUI>(PluginFolders.modularWallPrefabProfiles);
                return _ui;
            }
        }
        public override string                      folderPath          { get { return PluginFolders.modularWallPrefabProfiles; } }

        public static ModularWallPrefabProfileDb    instance
        {
            get
            {
                if (_instance == null)
                    _instance = AssetDbEx.loadScriptableObject<ModularWallPrefabProfileDb>(PluginFolders.modularWallPrefabProfiles);

                return _instance;
            }
        }
        public static bool exists { get { return _instance != null; } }

        public void deletePrefabs(List<PluginPrefab> pluginPrefabs)
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).deletePrefabs(pluginPrefabs);

            if (_ui != null) _ui.refresh();
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).onPrefabAssetWillBeDeleted(prefabAsset);

            if (_ui != null) _ui.refresh();
        }

        public bool containsWallPiecePrefabAsset(GameObject prefabAsset)
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
            {
                if (getProfile(i).containsWallPiecePrefabAsset(prefabAsset)) return true;
            }

            return false;
        }

        public bool containsPillarPrefabAsset(GameObject prefabAsset)
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
            {
                if (getProfile(i).containsPillarPrefabAsset(prefabAsset)) return true;
            }

            return false;
        }
    }
}
#endif