#if UNITY_EDITOR
using System;

namespace GSpawn
{
    public class GridSettingsProfileDb : ProfileDb<GridSettingsProfile>
    {
        private static GridSettingsProfileDb    _instance;

        [NonSerialized]
        private GridSettingsProfileDbUI         _ui;

        public GridSettingsProfileDbUI          ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<GridSettingsProfileDbUI>(PluginFolders.gridProfiles);

                return _ui;
            }
        }

        public static GridSettingsProfileDb     instance
        {
            get
            {
                if (_instance == null) _instance = AssetDbEx.loadScriptableObject<GridSettingsProfileDb>(PluginFolders.gridProfiles);
                return _instance;
            }
        }
        public static bool                      exists      { get { return _instance != null; } }

        public override string                  folderPath  { get { return PluginFolders.gridProfiles; } }
    }
}
#endif