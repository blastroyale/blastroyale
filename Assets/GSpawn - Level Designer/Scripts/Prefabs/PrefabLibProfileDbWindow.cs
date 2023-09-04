#if UNITY_EDITOR
namespace GSpawn
{
    public class PrefabLibProfileDbWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.libraryDb);
            PrefabLibProfileDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            bool plural             = PrefabLibProfileDb.instance.activeProfile.numLibs != 1;
            _header.title           = "Prefab Library Manager: " + PrefabLibProfileDb.instance.activeProfile.numLibs + (plural ? " libs" : " lib");
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            PrefabLibProfileDbUI.instance.onGUI();
        }
    }
}
#endif