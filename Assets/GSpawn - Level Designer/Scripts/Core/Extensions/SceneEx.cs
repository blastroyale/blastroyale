#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor;

namespace GSpawn
{
    public static class SceneEx
    {
        public static Scene getCurrent()
        {
            return SceneManager.GetActiveScene();
        }

        public static string getGUIDString(this Scene scene)
        {
            return AssetDatabase.GUIDFromAssetPath(scene.path).ToString();
        }

        public static string getActiveSceneGUIDString()
        {
            return AssetDatabase.GUIDFromAssetPath(getCurrent().path).ToString();
        }
    }
}
#endif
