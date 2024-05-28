#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class PrefabFromSelectedObjectsCreationSettingsWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            PrefabFromSelectedObjectsCreationSettingsUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            PrefabFromSelectedObjectsCreationSettingsUI.instance.onGUI();
        }

        protected override void onEnabled()
        {
            setMinMaxSize(new Vector2(400.0f, 120.0f));
        }
    }
}
#endif