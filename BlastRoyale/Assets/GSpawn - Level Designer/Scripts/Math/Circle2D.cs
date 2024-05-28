#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public struct Circle2D
    {
        public static bool containsPoint(Vector2 circleCenter, float circleRadius, Vector2 point)
        {
            return (point - circleCenter).magnitude <= circleRadius;
        }

        public static bool containsPoints(Vector2 circleCenter, float circleRadius, IEnumerable<Vector2> points)
        {
            foreach (var pt in points)
            {
                if ((pt - circleCenter).magnitude > circleRadius) 
                    return false;
            }

            return true;
        }

        public static bool intersectsRect(Vector2 circleCenter, float circleRadius, Rect rect)
        {
            float dSqr = 0.0f;
            if (rect.xMax < circleCenter.x) dSqr += Mathf.Pow(circleCenter.x - rect.xMax, 2.0f);
            else if (rect.xMin > circleCenter.x) dSqr += Mathf.Pow(circleCenter.x - rect.xMin, 2.0f);

            if (rect.yMax < circleCenter.y) dSqr += Mathf.Pow(circleCenter.y - rect.yMax, 2.0f);
            else if (rect.yMin > circleCenter.y) dSqr += Mathf.Pow(circleCenter.y - rect.yMin, 2.0f);

            return dSqr <= (circleRadius * circleRadius);
        }
    }
}
#endif