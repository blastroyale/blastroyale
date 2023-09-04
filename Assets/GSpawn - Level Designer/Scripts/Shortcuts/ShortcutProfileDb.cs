#if UNITY_EDITOR
using System;
using UnityEngine;

namespace GSpawn
{
    public class ShortcutProfileDb : ProfileDb<ShortcutProfile>
    {
        private static ShortcutProfileDb    _instance;

        // Note: As with all UI objects, make them NonSerialized so that they can be imported 
        //       into another project.
        [NonSerialized]
        private ShortcutProfileDbUI         _ui;

        public ShortcutProfileDbUI          ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<ShortcutProfileDbUI>(PluginFolders.shortcutProfiles);

                return _ui;
            }
        }
        public override string              folderPath      { get { return PluginFolders.shortcutProfiles; } }

        public static ShortcutProfileDb     instance
        {
            get
            {
                if (_instance == null)
                    _instance = AssetDbEx.loadScriptableObject<ShortcutProfileDb>(PluginFolders.shortcutProfiles);

                return _instance;
            }
        }
        public static bool                  exists          { get { return _instance != null; } }

        public bool processEvent(Event e)
        {
            return activeProfile.processEvent(e);
        }

        protected override void onActiveProfileChanged()
        {
            UIRefresh.refreshShortcutToolTips();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_ui);
        }
    }
}
#endif