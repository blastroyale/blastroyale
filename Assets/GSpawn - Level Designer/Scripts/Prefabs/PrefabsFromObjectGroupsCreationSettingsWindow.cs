#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class PrefabsFromObjectGroupsCreationSettingsWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            PrefabsFromObjectGroupsCreationSettingsUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            PrefabsFromObjectGroupsCreationSettingsUI.instance.onGUI();
        }

        protected override void onEnabled()
        {
            setMinMaxSize(new Vector2(300.0f, 85.0f));
        }
    }
}
#endif