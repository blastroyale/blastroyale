#if UNITY_EDITOR

namespace GSpawn
{
    public class SegmentsObjectSpawnSettingsProfileDbUI : PluginUI
    {
        private ProfileSelectionUI<SegmentsObjectSpawnSettingsProfileDb, SegmentsObjectSpawnSettingsProfile> _profileSelectionUI;

        public static SegmentsObjectSpawnSettingsProfileDbUI instance { get { return SegmentsObjectSpawnSettingsProfileDb.instance.ui; } }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow     = 1.0f;
            _profileSelectionUI                 = new ProfileSelectionUI<SegmentsObjectSpawnSettingsProfileDb, SegmentsObjectSpawnSettingsProfile>();
            _profileSelectionUI.build(SegmentsObjectSpawnSettingsProfileDb.instance, "segments object spawn settings", contentContainer);
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
            SegmentsObjectSpawnSettingsProfileDb.instance.activeProfileChanged += onActiveProfileChanged;
        }

        protected override void onDisabled()
        {
            SegmentsObjectSpawnSettingsProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
        }

        private void onActiveProfileChanged(SegmentsObjectSpawnSettingsProfile newActiveProfile)
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
            SegmentsObjectSpawnSettingsProfileDb.instance.activeProfile.buildUI(contentContainer);
        }
    }
}
#endif