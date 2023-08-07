#if UNITY_EDITOR
using System;
using UnityEngine;

namespace GSpawn
{
    public static class EnumEx
    {
        public static Enum findValue(int valueIndex, Type enumType)
        {
            if (!enumType.IsEnum) throw new UnityException("EnumEx.findValue: 'enumType' must be an Enum type.");

            var enumValues  = Enum.GetValues(enumType);
            int valIndex    = 0;
            foreach (var enumVal in enumValues)
            {
                if (valIndex == valueIndex) return (Enum)enumVal;
                ++valIndex;
            }

            return default;
        }

        public static int findValueIndex(Enum value, Type enumType)
        {
            if (!enumType.IsEnum) throw new UnityException("EnumEx.findValueIndex 'enumType' must be an Enum type.");

            var enumValues  = Enum.GetValues(enumType);
            int valIndex    = 0;
            foreach (var enumVal in enumValues)
            {
                if (value.Equals(enumVal)) return valIndex;
                ++valIndex;
            }

            return -1;
        }
    }
}
#endif