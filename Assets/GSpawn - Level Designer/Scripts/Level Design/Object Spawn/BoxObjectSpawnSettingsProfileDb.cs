#if UNITY_EDITOR
using System;

namespace GSpawn
{
    public class BoxObjectSpawnSettingsProfileDb : ProfileDb<BoxObjectSpawnSettingsProfile>
    {
        private static BoxObjectSpawnSettingsProfileDb _instance;

        // Note: As with all UI objects, make them NonSerialized so that they can be imported 
        //       into another project.
        [NonSerialized]
        private BoxObjectSpawnSettingsProfileDbUI       _ui;

        public BoxObjectSpawnSettingsProfileDbUI        ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<BoxObjectSpawnSettingsProfileDbUI>(PluginFolders.boxObjectSpawnSettingsProfiles);

                return _ui;
            }
        }
        public override string                          folderPath  { get { return PluginFolders.boxObjectSpawnSettingsProfiles; } }

        public static BoxObjectSpawnSettingsProfileDb   instance
        {
            get
            {
                if (_instance == null)
                    _instance = AssetDbEx.loadScriptableObject<BoxObjectSpawnSettingsProfileDb>(PluginFolders.boxObjectSpawnSettingsProfiles);

                return _instance;
            }
        }
        public static bool                              exists      { get { return _instance != null; } }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_ui);
        }
    }
}
#endif