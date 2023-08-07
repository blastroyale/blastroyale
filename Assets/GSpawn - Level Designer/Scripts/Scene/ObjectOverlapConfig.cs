#if UNITY_EDITOR
namespace GSpawn
{
    public enum ObjectOverlapPrefabMode
    {
        None = 0,
        PrefabInstanceRootIfPossible,
        OnlyPrefabInstanceRoot
    }

    public struct ObjectOverlapConfig
    {
        public bool                     requireFullOverlap;
        public ObjectOverlapPrefabMode  prefabMode;

        public static readonly ObjectOverlapConfig defaultConfig = new ObjectOverlapConfig()
        {
            requireFullOverlap      = false,
            prefabMode              = ObjectOverlapPrefabMode.None,
        };
    }
}
#endif