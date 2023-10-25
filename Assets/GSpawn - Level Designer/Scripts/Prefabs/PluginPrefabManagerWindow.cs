#if UNITY_EDITOR
namespace GSpawn
{
    public class PluginPrefabManagerWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.prefab, UIValues.smallHeaderIconSize);
            PluginPrefabManagerUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            bool plural             = PluginPrefabManagerUI.instance.numPrefabs != 1;
            _header.title           = "Prefab Manager: " + PluginPrefabManagerUI.instance.numPrefabs + (plural ? " prefabs" : " prefab");
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            PluginPrefabManagerUI.instance.onGUI();
        }
    }
}
#endif