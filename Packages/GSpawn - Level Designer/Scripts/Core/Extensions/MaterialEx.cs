#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;

namespace GSpawn
{
    public static class MaterialEx
    {
        public static void setZWriteEnabled(this Material material, bool enabled)
        {
            material.SetInt("_ZWrite", enabled ? 1 : 0);
        }

        public static void setZTestEnabled(this Material material, bool enabled)
        {
            material.SetInt("_ZTest", enabled ? (int)CompareFunction.LessEqual : (int)CompareFunction.Always);
        }

        public static void setZTestAlways(this Material material)
        {
            material.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        public static void setZTestLess(this Material material)
        {
            material.SetInt("_ZTest", (int)CompareFunction.Less);
        }

        public static void setCullModeBack(this Material material)
        {
            material.SetInt("_CullMode", (int)CullMode.Back);
        }

        public static void setCullModeFront(this Material material)
        {
            material.SetInt("_CullMode", (int)CullMode.Front);
        }

        public static void setCullModeOff(this Material material)
        {
            material.SetInt("_CullMode", (int)CullMode.Off);
        }
    }
}
#endif