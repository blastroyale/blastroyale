#if UNITY_EDITOR
namespace GSpawn
{
    public static class UIRefresh
    {
        public static void refreshShortcutToolTips()
        {
            PluginInspectorUI.instance.refresh();
        }
    }
}
#endif