#if UNITY_EDITOR
using UnityEditor;

namespace GSpawn
{
    public static class PluginProgressDialog
    {
        static string _title = string.Empty;

        public static void begin(string title)
        {
            if (title != null) _title = title;
            else _title = string.Empty;
        }

        public static void updateItemProgress(string itemName, float progress)
        {
            EditorUtility.DisplayProgressBar(_title, "Please wait... (" + itemName + ")", progress);
        }

        public static void updateProgress(string message, float progress)
        {
            EditorUtility.DisplayProgressBar(_title, message, progress);
        }

        public static void end()
        {
            _title = string.Empty;
            EditorUtility.ClearProgressBar();
        }
    }
}
#endif