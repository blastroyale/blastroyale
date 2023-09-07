#if UNITY_EDITOR
namespace GSpawn
{
    public class IntRangePrefabProfileDbWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.prefab, UIValues.smallHeaderIconSize);
            IntRangePrefabProfileDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            bool plural             = IntRangePrefabProfileDb.instance.activeProfile.numPrefabs != 1;
            _header.title           = IntRangePrefabProfileDb.instance.activeProfile.numPrefabs + (plural ? " prefabs" : " prefab");
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            IntRangePrefabProfileDbUI.instance.onGUI();
        }
    }
}
#endif