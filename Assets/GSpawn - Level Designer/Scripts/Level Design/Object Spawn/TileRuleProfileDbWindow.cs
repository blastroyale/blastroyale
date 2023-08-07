#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class TileRuleProfileDbWindow : PluginWindow
    {
        [SerializeField]
        private bool _wasSizeInitialized = false;

        protected override void onBuildUI()
        {
            _header = new UIHeader(rootVisualElement, TexturePool.instance.tileRuleBrushSpawn, UIValues.mediumHeaderIconSize);
            TileRuleProfileDbUI.instance.build(rootVisualElement, this);
        }

        protected override void onGUI()
        {
            _header.title = "Tile Rules";
            _header.backgroundColor = focusedWindow == this ? UIValues.focusedHeaderColor : UIValues.unfocusedHeaderColor;
            TileRuleProfileDbUI.instance.onGUI();
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