#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class Vector2Ex
    {
        public static Vector2 create(float val)
        {
            return new Vector2(val, val);
        }

        public static float absDot(this Vector2 v1, Vector2 v2)
        {
            return Mathf.Abs(Vector2.Dot(v1, v2));
        }

        public static Vector2 abs(this Vector2 v)
        {
            return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        }

        public static int findIndexOfPointClosestToPoint(List<Vector2> points, Vector2 pt)
        {
            float minDistSq         = float.MaxValue;
            int closestPtIndex      = -1;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector2 point = points[ptIndex];

                float distSq = (point - pt).sqrMagnitude;
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    closestPtIndex = ptIndex;
                }
            }

            return closestPtIndex;
        }
    }
}
#endif