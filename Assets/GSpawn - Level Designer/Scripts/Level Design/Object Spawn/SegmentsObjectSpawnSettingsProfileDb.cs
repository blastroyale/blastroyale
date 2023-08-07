#if UNITY_EDITOR
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class SegmentsObjectSpawnSettingsProfileDb : ProfileDb<SegmentsObjectSpawnSettingsProfile>
    {
        private static SegmentsObjectSpawnSettingsProfileDb _instance;

        [NonSerialized]
        private SegmentsObjectSpawnSettingsProfileDbUI      _ui;

        public SegmentsObjectSpawnSettingsProfileDbUI       ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<SegmentsObjectSpawnSettingsProfileDbUI>(PluginFolders.segmentsObjectSpawnSettingsProfiles);

                return _ui;
            }
        }
        public override string                              folderPath  { get { return PluginFolders.segmentsObjectSpawnSettingsProfiles; } }

        public static SegmentsObjectSpawnSettingsProfileDb  instance
        {
            get
            {
                if (_instance == null)
                    _instance = AssetDbEx.loadScriptableObject<SegmentsObjectSpawnSettingsProfileDb>(PluginFolders.segmentsObjectSpawnSettingsProfiles);

                return _instance;
            }
        }
        public static bool                                  exists      { get { return _instance != null; } }

        public void onIntPatternsWillBeDeleted(List<IntPattern> patterns)
        {
            int profileCount = numProfiles;
            for (int i = 0; i < profileCount; ++i)
                getProfile(i).onIntPatternsWillBeDeleted(patterns);
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_ui);
        }
    }
}
#endif