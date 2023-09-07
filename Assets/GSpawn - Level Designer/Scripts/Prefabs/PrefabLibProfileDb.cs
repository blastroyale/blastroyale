#if UNITY_EDITOR
using System;
using UnityEngine;

namespace GSpawn
{
    public class PrefabLibProfileDb : ProfileDb<PrefabLibProfile>
    {
        private static PrefabLibProfileDb                       _instance;

        [NonSerialized]
        private PrefabLibProfileDbUI                            _ui;
        [NonSerialized]
        private PluginPrefabManagerUI                           _prefabManagerUI;
        [SerializeField]
        private PrefabFromSelectedObjectsCreationSettingsUI     _prefabCreationSettingsUI;
        [SerializeField]
        private ObjectGroupsFromPrefabLibsCreationSettingsUI    _objectGroupCreationSettingsUI;
        [NonSerialized]
        private ObjectGroupsFromPrefabLibsCreationSettings      _objectGroupCreationSettings;
        [NonSerialized]
        private PrefabFromSelectedObjectsCreationSettings       _prefabCreationSettings;

        public PrefabLibProfileDbUI                             ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<PrefabLibProfileDbUI>(PluginFolders.prefabLibProfiles);

                return _ui;
            }
        }
        public PluginPrefabManagerUI                            prefabManagerUI
        {
            get
            {
                if (_prefabManagerUI == null)
                    _prefabManagerUI = AssetDbEx.loadScriptableObject<PluginPrefabManagerUI>(PluginFolders.prefabLibProfiles);

                return _prefabManagerUI;
            }
        }
        public PrefabFromSelectedObjectsCreationSettingsUI      prefabCreationSettingsUI
        {
            get
            {
                if (_prefabCreationSettingsUI == null)
                    _prefabCreationSettingsUI = AssetDbEx.loadScriptableObject<PrefabFromSelectedObjectsCreationSettingsUI>(PluginFolders.prefabLibProfiles);

                return _prefabCreationSettingsUI;
            }
        }
        public ObjectGroupsFromPrefabLibsCreationSettingsUI     objectGroupCreationSettingsUI
        {
            get
            {
                if (_objectGroupCreationSettingsUI == null)
                    _objectGroupCreationSettingsUI = AssetDbEx.loadScriptableObject<ObjectGroupsFromPrefabLibsCreationSettingsUI>(PluginFolders.prefabLibProfiles);

                return _objectGroupCreationSettingsUI;
            }
        }
        public ObjectGroupsFromPrefabLibsCreationSettings       objectGroupCreationSettings
        {
            get
            {
                if (_objectGroupCreationSettings == null) _objectGroupCreationSettings = AssetDbEx.loadScriptableObject<ObjectGroupsFromPrefabLibsCreationSettings>(PluginFolders.prefabLibProfiles);
                return _objectGroupCreationSettings;
            }
        }
        public PrefabFromSelectedObjectsCreationSettings        prefabCreationSettings
        {
            get
            {
                if (_prefabCreationSettings == null) _prefabCreationSettings = AssetDbEx.loadScriptableObject<PrefabFromSelectedObjectsCreationSettings>(PluginFolders.prefabLibProfiles);
                return _prefabCreationSettings;
            }
        }
        public override string                                  folderPath                  { get { return PluginFolders.prefabLibProfiles; } }

        public static PrefabLibProfileDb                        instance
        {
            get     
            {
                if (_instance == null)
                {
                    _instance = AssetDbEx.loadScriptableObject<PrefabLibProfileDb>(PluginFolders.prefabLibProfiles);
                    _instance.deleteNullPrefabs();
                }
                    
                return _instance;
            }
        }
        public static bool                                      exists                      { get { return _instance != null; } }

        public void performPrefabAction(Action<PluginPrefab> prefabAction)
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).performPrefabAction(prefabAction);
        }

        public PluginPrefab getPrefab(GameObject prefabAsset)
        {
            PluginPrefab pluginPrefab = activeProfile.getPrefab(prefabAsset);
            if (pluginPrefab != null) return pluginPrefab;

            int profileCount = numProfiles;
            for (int profileIndex = 0; profileIndex < profileCount; ++profileIndex)
            {
                PrefabLibProfile libProfile = getProfile(profileIndex);
                if (libProfile == activeProfile) continue;

                pluginPrefab = getProfile(profileIndex).getPrefab(prefabAsset);
                if (pluginPrefab != null) return pluginPrefab;
            }

            return null;
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            int numLibProfiles = numProfiles;
            for (int i = 0; i < numLibProfiles; ++i)
                getProfile(i).onPrefabAssetWillBeDeleted(prefabAsset);

            if (_ui != null) _ui.onPrefabAssetWillBeDeleted(prefabAsset);
            if (_prefabManagerUI != null) _prefabManagerUI.onPrefabAssetWillBeDeleted(prefabAsset);
        }

        public void deleteNullPrefabs()
        {
            int numLibProfiles = numProfiles;
            for (int i = 0; i < numLibProfiles; ++i)
                getProfile(i).deleteNullPrefabs();
        }

        protected override void onEnabled()
        {
            deleteNullPrefabs();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_prefabManagerUI);
            ScriptableObjectEx.destroyImmediate(_ui);
            ScriptableObjectEx.destroyImmediate(_prefabCreationSettingsUI);
            ScriptableObjectEx.destroyImmediate(_objectGroupCreationSettingsUI);
        }
    }
}
#endif