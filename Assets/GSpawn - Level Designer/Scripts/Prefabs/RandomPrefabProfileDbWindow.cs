#if UNITY_EDITOR
namespace GSpawn
{
    public class RandomPrefabProfileDbWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.prefab, UIValues.smallHeaderIconSize);
            RandomPrefabProfileDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            bool plural             = RandomPrefabProfileDb.instance.activeProfile.numPrefabs != 1;
            _header.title           = RandomPrefabProfileDb.instance.activeProfile.numPrefabs + (plural ? " prefabs" : " prefab");
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            RandomPrefabProfileDbUI.instance.onGUI();
        }
    }
}
#endif