#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class TileRuleProfileDb : ProfileDb<TileRuleProfile>
    {
        private static TileRuleProfileDb    _instance;

        [NonSerialized]
        private TileRuleProfileDbUI         _ui;
        [NonSerialized]
        private List<TileRulePrefab>        _tileRulePrefabBuffer = new List<TileRulePrefab>();

        public TileRuleProfileDbUI          ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<TileRuleProfileDbUI>(PluginFolders.tileRuleProfiles);

                return _ui;
            }
        }
        public override string              folderPath  { get { return PluginFolders.tileRuleProfiles; } }

        public static TileRuleProfileDb instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDbEx.loadScriptableObject<TileRuleProfileDb>(PluginFolders.tileRuleProfiles);
                    _instance.deleteNullPrefabs();
                }

                return _instance;
            }
        }
        public static bool                  exists      { get { return _instance != null; } }

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

        public void deleteNullPrefabs()
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).deleteNullPrefabs();

            if (_ui != null) _ui.refresh();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_ui);
        }
    }
}
#endif