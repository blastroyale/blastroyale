#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class RenameEntityWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            RenameEntityUI.instance.build(rootVisualElement, this);
        }

        protected override void onEnabled()
        {
            setMinMaxSize(new Vector2(400.0f, 130.0f));
        }
    }
}
#endif