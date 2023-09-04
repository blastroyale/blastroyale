#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class Vector4Ex
    {
        public static Vector4 create(Vector3 vec, float w)
        {
            return new Vector4(vec.x, vec.y, vec.z, w);
        }

        public static Vector4 create(Color color)
        {
            return new Vector4(color.r, color.g, color.b, color.a);
        }
    }
}
#endif