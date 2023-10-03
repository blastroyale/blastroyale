#if UNITY_EDITOR
namespace GSpawn
{
    public class PluginObjectLayerDbWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header         = new UIHeader(rootVisualElement, TexturePool.instance.layers);
            _header.title   = "Object Layers";

            PluginObjectLayerDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            PluginObjectLayerDbUI.instance.onGUI();
        }
    }
}
#endif