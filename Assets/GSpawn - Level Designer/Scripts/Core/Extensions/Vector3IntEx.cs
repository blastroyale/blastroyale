#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class Vector3IntEx
    {
        public static Vector3Int abs(this Vector3Int vec)
        {
            return new Vector3Int(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
        }

        public static bool matchCoords(this Vector3Int vec, int x, int y, int z)
        {
            return vec.x == x && vec.y == y && vec.z == z;
        }

        public static bool inRange(this Vector3Int vec, Vector3Int min, Vector3Int max)
        {
            return  vec.x >= min.x && vec.x <= max.x && 
                    vec.y >= min.y && vec.y <= max.y && 
                    vec.z >= min.z && vec.z <= max.z;
        }
    }
}
#endif