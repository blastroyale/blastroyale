#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class IntRangePrefabProfileDb : ProfileDb<IntRangePrefabProfile>
    {
        private static IntRangePrefabProfileDb  _instance;

        [NonSerialized]
        private IntRangePrefabProfileDbUI       _ui;
        [NonSerialized]
        private List<IntRangePrefab>            _irPrefabBuffer     = new List<IntRangePrefab>();

        public IntRangePrefabProfileDbUI        ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<IntRangePrefabProfileDbUI>(PluginFolders.intRangePrefabProfiles);

                return _ui;
            }
        }
        public override string                  folderPath          { get { return PluginFolders.intRangePrefabProfiles; } }

        public static IntRangePrefabProfileDb   instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDbEx.loadScriptableObject<IntRangePrefabProfileDb>(PluginFolders.intRangePrefabProfiles);
                    _instance.deleteNullPrefabs();
                }

                return _instance;
            }
        }
        public static bool                      exists              { get { return _instance != null; } }

        public void deletePrefabs(List<PluginPrefab> pluginPrefabs)
        {
            getPrefabs(pluginPrefabs, _irPrefabBuffer);
            deletePrefabs(_irPrefabBuffer);

            if (_ui != null) _ui.refresh();
        }

        public void getPrefabs(List<PluginPrefab> pluginPrefabs, List<IntRangePrefab> irPrefabs)
        {
            irPrefabs.Clear();
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).getPrefabs(pluginPrefabs, irPrefabs, true);
        }

        public void deletePrefabs(List<IntRangePrefab> irPrefabs)
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).deletePrefabs(irPrefabs);
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