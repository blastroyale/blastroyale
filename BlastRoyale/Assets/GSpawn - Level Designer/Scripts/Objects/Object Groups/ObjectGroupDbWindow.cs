#if UNITY_EDITOR
namespace GSpawn
{
    public class ObjectGroupDbWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.objectGroup);
            ObjectGroupDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            bool plural             = ObjectGroupDb.instance.numObjectGroups != 1;
            _header.title           = "Object Groups: " + ObjectGroupDb.instance.numObjectGroups + (plural ? " groups" : " group");
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            ObjectGroupDbUI.instance.onGUI();
        }
    }
}
#endif