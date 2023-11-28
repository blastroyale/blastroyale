#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEngine;

namespace GSpawn
{
    public class CurvePrefabProfileDb : ProfileDb<CurvePrefabProfile>
    {
        private static CurvePrefabProfileDb     _instance;

        [NonSerialized]
        private CurvePrefabProfileDbUI          _ui;
        [NonSerialized]
        private List<CurvePrefab>               _curvePrefabBuffer  = new List<CurvePrefab>();

        public CurvePrefabProfileDbUI           ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<CurvePrefabProfileDbUI>(PluginFolders.curvePrefabProfiles);

                return _ui;
            }
        }
        public override string                  folderPath          { get { return PluginFolders.curvePrefabProfiles; } }

        public static CurvePrefabProfileDb      instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDbEx.loadScriptableObject<CurvePrefabProfileDb>(PluginFolders.curvePrefabProfiles);
                    _instance.deleteNullPrefabs();
                }
                return _instance;
            }
        }
        public static bool                      exists              { get { return _instance != null; } }

        public void deletePrefabs(List<PluginPrefab> pluginPrefabs)
        {
            getPrefabs(pluginPrefabs, _curvePrefabBuffer);
            deletePrefabs(_curvePrefabBuffer);

            if (_ui != null) _ui.refresh();
        }

        public void getPrefabs(List<PluginPrefab> pluginPrefabs, List<CurvePrefab> curvePrefabs)
        {
            curvePrefabs.Clear();
            int numProfiles = base.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                getProfile(i).getPrefabs(pluginPrefabs, curvePrefabs, true);
        }

        public void deletePrefabs(List<CurvePrefab> curvePrefabs)
        {
            int numProfiles = base.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                getProfile(i).deletePrefabs(curvePrefabs);
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            int numProfiles = base.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                getProfile(i).onPrefabAssetWillBeDeleted(prefabAsset);

            if (_ui != null) _ui.onPrefabAssetWillBeDeleted(prefabAsset);
        }

        public void deleteNullPrefabs()
        {
            int numProfiles = base.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                getProfile(i).deleteNullPrefabs();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_ui);
        }
    }
}
#endif