#if UNITY_EDITOR
namespace GSpawn
{
    public abstract class Prefs<T> : PluginSettings<T>
        where T : Prefs<T>
    {
        private static T    _instance;

        public static T     instance
        {
            get
            {
                if (_instance == null) _instance = AssetDbEx.loadScriptableObject<T>(PluginFolders.preferences);
                return _instance;
            }
        }
    }
}
#endif