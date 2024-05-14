using UnityEditor;
using UnityEngine;

namespace PlayFab.PfEditor
{
    public static class PlayFabHelp
    {
        [MenuItem("Tools/PlayFab/GettingStarted")]
        private static void GettingStarted()
        {
            Application.OpenURL("https://docs.microsoft.com/en-us/gaming/playfab/index#pivot=documentation&panel=quickstarts");
        }

        [MenuItem("Tools/PlayFab/Docs")]
        private static void Documentation()
        {
            Application.OpenURL("https://docs.microsoft.com/en-us/gaming/playfab/api-references/");
        }

        [MenuItem("Tools/PlayFab/Dashboard")]
        private static void Dashboard()
        {
            Application.OpenURL("https://developer.playfab.com/");
        }
    }
}
