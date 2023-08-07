#if UNITY_EDITOR
namespace GSpawn
{
    public class ShortcutsWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header         = new UIHeader(rootVisualElement, TexturePool.instance.hotkeys);
            _header.title   = "Shortcut Management";

            ShortcutProfileDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            ShortcutProfileDbUI.instance.onGUI();
        }
    }
}
#endif