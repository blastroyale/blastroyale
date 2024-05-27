#if UNITY_EDITOR
using System;
using System.Reflection;

namespace GSpawn
{
    public static class ReflectionEx
    {
        public static FieldInfo[] getPrivateInstanceFields(Type classType)
        {
            return classType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
#endif