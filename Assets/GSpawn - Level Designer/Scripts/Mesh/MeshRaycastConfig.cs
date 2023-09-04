#if UNITY_EDITOR
namespace GSpawn
{
    public struct MeshRaycastConfig
    {
        public bool canHitCameraCulledFaces;
        public bool flipNegativeScaleTriangles;

        public static readonly MeshRaycastConfig defaultConfig = new MeshRaycastConfig()
        {
            canHitCameraCulledFaces     = false,
            flipNegativeScaleTriangles  = true
        };
    }
}
#endif