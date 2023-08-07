#if UNITY_EDITOR

namespace GSpawn
{
    public class BoxObjectSpawnSettingsProfileDbUI : PluginUI
    {
        private ProfileSelectionUI<BoxObjectSpawnSettingsProfileDb, BoxObjectSpawnSettingsProfile> _profileSelectionUI;

        public static BoxObjectSpawnSettingsProfileDbUI instance { get { return BoxObjectSpawnSettingsProfileDb.instance.ui; } }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow = 1.0f;
            _profileSelectionUI = new ProfileSelectionUI<BoxObjectSpawnSettingsProfileDb, BoxObjectSpawnSettingsProfile>();
            _profileSelectionUI.build(BoxObjectSpawnSettingsProfileDb.instance, "box object spawn settings", contentContainer);
            build();
        }

        protected override void onRefresh()
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            build();
        }

        protected override void onEnabled()
        {
            BoxObjectSpawnSettingsProfileDb.instance.activeProfileChanged += onActiveProfileChanged;
        }

        protected override void onDisabled()
        {
            BoxObjectSpawnSettingsProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
        }

        private void onActiveProfileChanged(BoxObjectSpawnSettingsProfile newActiveProfile)
        {
            onRefresh();
        }

        protected override void onUndoRedo()
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();
        }

        private void build()
        {
            contentContainer.removeAllChildrenExcept(_profileSelectionUI);
            BoxObjectSpawnSettingsProfileDb.instance.activeProfile.buildUI(contentContainer);
        }
    }
}
#endif