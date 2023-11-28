#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class DeleteEntityWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            DeleteEntityUI.instance.build(rootVisualElement, this);
        }

        protected override void onEnabled()
        {
            setMinMaxSize(new Vector2(500.0f, 120.0f));
        }
    }
}
#endif