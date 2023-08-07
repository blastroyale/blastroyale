#if UNITY_EDITOR
namespace GSpawn
{
    public class GridSettingsProfileDbWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header                     = new UIHeader(rootVisualElement, TexturePool.instance.grid);
            _header.title               = "Grid Settings";
            GridSettingsProfileDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            _header.backgroundColor     = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            GridSettingsProfileDbUI.instance.onGUI();
        }
    }
}
#endif