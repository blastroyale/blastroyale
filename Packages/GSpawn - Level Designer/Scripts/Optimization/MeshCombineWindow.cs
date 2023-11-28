#if UNITY_EDITOR

namespace GSpawn
{
    public class MeshCombineWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            _header         = new UIHeader(rootVisualElement, TexturePool.instance.chemistry, UIValues.mediumHeaderIconSize);
            _header.title   = "Mesh Combine";
            MeshCombineUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            MeshCombineUI.instance.onGUI();
        }
    }
}
#endif