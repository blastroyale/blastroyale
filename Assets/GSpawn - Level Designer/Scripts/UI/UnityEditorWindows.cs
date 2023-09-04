#if UNITY_EDITOR
using UnityEditor;

namespace GSpawn
{
    public static class UnityEditorWindows
    {
        public static void showProjectBrowser()
        {
            System.Type wndType = System.Reflection.Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.ProjectBrowser");
            EditorWindow.GetWindow(wndType).Show();
        }

        public static void showLightingWindow()
        {
            System.Type wndType = System.Reflection.Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.LightingWindow");
            EditorWindow.GetWindow(wndType).Show();
        }
    }
}
#endif