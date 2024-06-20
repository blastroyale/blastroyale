#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class ModularWallPrefabProfileDbWindow : PluginWindow
    {
        [SerializeField]
        private bool _wasSizeInitialized = false;

        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.modularWallSpawn, UIValues.mediumHeaderIconSize);
            ModularWallPrefabProfileDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            _header.title = "Modular Wall Prefabs";
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            ModularWallPrefabProfileDbUI.instance.onGUI();
        }

        protected override void onEnabled()
        {
            if (!_wasSizeInitialized)
            {
                setSize(new Vector3(500.0f, 300.0f));
                _wasSizeInitialized = true;
            }
        }
    }
}
#endif