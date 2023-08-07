#if UNITY_EDITOR
namespace GSpawn
{
    public class CurveObjectSpawnSettingsProfileDb : ProfileDb<CurveObjectSpawnSettingsProfile>
    {
        private static CurveObjectSpawnSettingsProfileDb    _instance;

        public override string                              folderPath { get { return PluginFolders.curveObjectSpawnSettingsProfiles; } }

        public static CurveObjectSpawnSettingsProfileDb     instance
        {
            get
            {
                if (_instance == null)
                    _instance = AssetDbEx.loadScriptableObject<CurveObjectSpawnSettingsProfileDb>(PluginFolders.curveObjectSpawnSettingsProfiles);

                return _instance;
            }
        }
        public static bool exists { get { return _instance != null; } }
    }
}
#endif