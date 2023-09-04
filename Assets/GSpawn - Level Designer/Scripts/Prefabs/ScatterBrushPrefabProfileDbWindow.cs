#if UNITY_EDITOR
namespace GSpawn
{
    public class ScatterBrushPrefabProfileDbWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.scatterBrushSpawn);
            ScatterBrushPrefabProfileDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            bool plural             = ScatterBrushPrefabProfileDb.instance.activeProfile.numPrefabs != 1;
            _header.title           = ScatterBrushPrefabProfileDb.instance.activeProfile.numPrefabs + (plural ? " prefabs" : " prefab");
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            ScatterBrushPrefabProfileDbUI.instance.onGUI();
        }
    }
}
#endif