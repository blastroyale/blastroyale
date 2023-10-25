#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class Probability
    {
        public static bool evalChance(float chance)
        {
            return Random.Range(0.0f, 1.0f) < chance;
        }
    }
}
#endif