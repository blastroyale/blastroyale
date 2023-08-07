#if UNITY_EDITOR
namespace GSpawn
{
    public class CurvePrefabProfileDbWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.curveSpawn);
            CurvePrefabProfileDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            bool plural             = CurvePrefabProfileDb.instance.activeProfile.numPrefabs != 1;
            _header.title           = CurvePrefabProfileDb.instance.activeProfile.numPrefabs + (plural ? " prefabs" : " prefab");
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            CurvePrefabProfileDbUI.instance.onGUI();
        }
    }
}
#endif