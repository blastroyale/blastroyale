#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class GridSettingsProfileDbUI : PluginUI
    {
        private VisualElement                                                   _settingsContainer;
        private ProfileSelectionUI<GridSettingsProfileDb, GridSettingsProfile>  _profileSelectionUI;

        public static GridSettingsProfileDbUI instance { get { return GridSettingsProfileDb.instance.ui; } }

        protected override void onBuild()
        {
            _profileSelectionUI = new ProfileSelectionUI<GridSettingsProfileDb, GridSettingsProfile>();
            _profileSelectionUI.build(GridSettingsProfileDb.instance, "grid settings", contentContainer);

            _settingsContainer  = new VisualElement();
            contentContainer.Add(_settingsContainer);
            GridSettingsProfileDb.instance.activeProfile.buildUI(_settingsContainer);
        }

        protected override void onRefresh()
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            _settingsContainer.Clear();
            GridSettingsProfileDb.instance.activeProfile.buildUI(_settingsContainer);
            SceneView.RepaintAll();
        }

        protected override void onEnabled()
        {
            GridSettingsProfileDb.instance.activeProfileChanged += onActiveProfileChanged;
        }

        protected override void onDisabled()
        {
            GridSettingsProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
        }

        protected override void onUndoRedo()
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            SceneView.RepaintAll();
        }

        private void onActiveProfileChanged(GridSettingsProfile newActiveProfile)
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            _settingsContainer.Clear();
            newActiveProfile.buildUI(_settingsContainer);
            SceneView.RepaintAll();
        }
    }
}
#endif