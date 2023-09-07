#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ScatterBrushPrefabProfileDb : ProfileDb<ScatterBrushPrefabProfile>
    {
        private static ScatterBrushPrefabProfileDb      _instance;

        [NonSerialized]
        private ScatterBrushPrefabProfileDbUI           _ui;
        [NonSerialized]
        private List<ScatterBrushPrefab>                _scatterBrushPrefabBuffer     = new List<ScatterBrushPrefab>();

        public ScatterBrushPrefabProfileDbUI            ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<ScatterBrushPrefabProfileDbUI>(PluginFolders.scatterBrushPrefabProfiles);

                return _ui;
            }
        }
        public override string                          folderPath                      { get { return PluginFolders.scatterBrushPrefabProfiles; } }

        public static ScatterBrushPrefabProfileDb       instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDbEx.loadScriptableObject<ScatterBrushPrefabProfileDb>(PluginFolders.scatterBrushPrefabProfiles);
                    _instance.deleteNullPrefabs();
                }

                return _instance;
            }
        }
        public static bool                          exists                      { get { return _instance != null; } }

        public void deletePrefabs(List<PluginPrefab> pluginPrefabs)
        {
            getPrefabs(pluginPrefabs, _scatterBrushPrefabBuffer);
            deletePrefabs(_scatterBrushPrefabBuffer);

            if (_ui != null) _ui.refresh();
        }

        public void getPrefabs(List<PluginPrefab> pluginPrefabs, List<ScatterBrushPrefab> brushPrefabs)
        {
            brushPrefabs.Clear();
            int numProfiles = base.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                getProfile(i).getPrefabs(pluginPrefabs, brushPrefabs, true);
        }

        public void deletePrefabs(List<ScatterBrushPrefab> brushPrefabs)
        {
            int numProfiles = base.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                getProfile(i).deletePrefabs(brushPrefabs);
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