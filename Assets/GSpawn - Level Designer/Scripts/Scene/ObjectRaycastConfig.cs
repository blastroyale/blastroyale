#if UNITY_EDITOR
namespace GSpawn
{
    public enum ObjectRaycastPrecision
    {
        BestFit = 1,
        Box
    }

    public struct ObjectRaycastConfig
    {
        public MeshRaycastConfig        meshConfig;
        public TerrainRaycastConfig     terrainConfig;
        public ObjectRaycastPrecision   raycastPrecision;

        public static readonly ObjectRaycastConfig defaultConfig = new ObjectRaycastConfig()
        {
            meshConfig          = MeshRaycastConfig.defaultConfig,
            terrainConfig       = TerrainRaycastConfig.defaultConfig,
            raycastPrecision    = ObjectRaycastPrecision.BestFit
        };
    }
}
#endif