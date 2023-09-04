#if UNITY_EDITOR
namespace GSpawn
{
    public class IntPatternDbWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.intPattern);
            IntPatternDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            bool plural             = IntPatternDb.instance.numPatterns != 1;
            _header.title           = "Integer Patterns: " + IntPatternDb.instance.numPatterns + (plural ? " patterns" : " pattern");
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            IntPatternDbUI.instance.onGUI();
        }
    }
}
#endif