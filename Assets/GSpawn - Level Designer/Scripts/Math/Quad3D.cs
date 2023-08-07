#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public enum QuadCorner
    {
        TopLeft = 0,
        TopRight,
        BottomRight,
        BottomLeft
    }

    public static class Quad3D
    {       
        public static void calcCorners(Vector3 quadCenter, Vector2 quadSize, Quaternion quadRotation, List<Vector3> corners)
        {
            corners.Clear();
            Vector3 u = quadRotation * Vector3.right;
            Vector3 v = quadRotation * Vector3.up;

            Vector3 extents = quadSize * 0.5f;
            corners.Add(quadCenter - u * extents.x + v * extents.y);
            corners.Add(quadCenter + u * extents.x + v * extents.y);
            corners.Add(quadCenter + u * extents.x - v * extents.y);
            corners.Add(quadCenter - u * extents.x - v * extents.y);
        }

        public static Vector3 calcCorner(Vector3 quadCenter, Vector2 quadSize, Quaternion quadRotation, QuadCorner quadCorner)
        {
            Vector3 u       = quadRotation * Vector3.right;
            Vector3 v       = quadRotation * Vector3.up;
            Vector3 extents = quadSize * 0.5f;

            if (quadCorner == QuadCorner.TopLeft) return quadCenter - u * extents.x + v * extents.y;
            else if (quadCorner == QuadCorner.TopRight) return quadCenter + u * extents.x + v * extents.y;
            else if (quadCorner == QuadCorner.BottomRight) return quadCenter + u * extents.x - v * extents.y;
            return quadCenter - u * extents.x - v * extents.y;
        }

        public static OBB calcOBB(Vector3 quadCenter, Vector2 quadSize, Quaternion quadRotation)
        {
            Vector3 size = quadSize;
            return new OBB(quadCenter, size, quadRotation);
        }

        public static bool raycast(Ray ray, out float t, Vector3 quadCenter, float quadWidth, float quadHeight, Vector3 quadU, Vector3 quadV)
        {
            t = 0.0f;
            Vector3 quadNormal  = Vector3.Normalize(Vector3.Cross(quadU, quadV));
            Plane quadPlane     = new Plane(quadNormal, quadCenter);

            float rayEnter;
            if (quadPlane.Raycast(ray, out rayEnter) &&
                containsPoint(ray.GetPoint(rayEnter), false, quadCenter, quadWidth, quadHeight, quadU, quadV))
            {
                t = rayEnter;
                return true;
            }

            return false;
        }

        public static bool containsPoint(Vector3 point, bool checkOnPlane, Vector3 quadCenter, float quadWidth, float quadHeight, Vector3 quadU, Vector3 quadV)
        {
            Plane quadPlane = new Plane(Vector3.Cross(quadU, quadV).normalized, quadCenter);
            if (checkOnPlane && quadPlane.absDistanceToPoint(point) > 1e-5f) return false;

            Vector3 toPoint = point - quadCenter;
            float dotRight  = toPoint.absDot(quadU);
            float dotUp     = toPoint.absDot(quadV);

            if (dotRight > quadWidth * 0.5f) return false;
            if (dotUp > quadHeight * 0.5f) return false;

            return true;
        }
    }
}
#endif