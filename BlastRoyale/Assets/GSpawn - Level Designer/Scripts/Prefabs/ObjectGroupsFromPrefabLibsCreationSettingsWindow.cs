#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class ObjectGroupsFromPrefabLibsCreationSettingsWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            ObjectGroupsFromPrefabLibsCreationSettingsUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            ObjectGroupsFromPrefabLibsCreationSettingsUI.instance.onGUI();
        }

        protected override void onEnabled()
        {
            setMinMaxSize(new Vector2(300.0f, 105.0f));
        }
    }
}
#endif