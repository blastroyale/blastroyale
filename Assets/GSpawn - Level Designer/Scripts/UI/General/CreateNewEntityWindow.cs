#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class CreateNewEntityWindow : PluginWindow
    {
        protected override void onBuildUI()
        {
            CreateNewEntityUI.instance.build(rootVisualElement, this);
        }

        protected override void onEnabled()
        {
            setMinMaxSize(new Vector2(300.0f, 130.0f));
        }
    }
}
#endif