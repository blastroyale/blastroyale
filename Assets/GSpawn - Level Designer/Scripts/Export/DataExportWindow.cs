#if UNITY_EDITOR

namespace GSpawn
{
    public class DataExportWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.settings);
            _header.setIconSize(UIValues.smallHeaderIconSize);
            _header.title = "Data Export";
            DataExportUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            DataExportUI.instance.onGUI();
        }
    }
}
#endif