#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public struct Circle3D
    {
        private static List<Vector3> _vector3Buffer = new List<Vector3>();

        public static Vector3 calcRandomPoint(Vector3 circleCenter, float circleRadius, Vector3 circleU, Vector3 circleV)
        {
            float r         = circleRadius * Mathf.Sqrt(Random.Range(0.0f, 1.0f));
            float theta     = Random.Range(0.0f, 1.0f) * 2.0f * Mathf.PI;
            return circleCenter + circleU * r * Mathf.Cos(theta) + circleV * r * Mathf.Sin(theta);
        }

        public static void calcExtents(Vector3 circleCenter, float circleRadius, Vector3 circleU, Vector3 circleV, List<Vector3> extents)
        {
            extents.Clear();
            extents.Add(circleCenter - circleU * circleRadius);
            extents.Add(circleCenter + circleV * circleRadius);
            extents.Add(circleCenter + circleU * circleRadius);
            extents.Add(circleCenter - circleV * circleRadius);
        }

        public static bool containsPointAsInfiniteCylinder(Vector3 circleCenter, float circleRadius, Vector3 circleU, Vector3 circleV, Vector3 point)
        {
            Vector3 toPt    = point - circleCenter;
            float sqr0      = Vector3Ex.absDot(toPt, circleU);
            sqr0 *= sqr0;

            float sqr1 = Vector3Ex.absDot(toPt, circleV);
            sqr1 *= sqr1;

            return (sqr0 + sqr1) <= (circleRadius * circleRadius);
        }

        public static bool containsPointsAsInfiniteCylinder(Vector3 circleCenter, float circleRadius, Vector3 circleU, Vector3 circleV, IEnumerable<Vector3> points)
        {
            foreach (var pt in points)
            {
                Vector3 toPt = pt - circleCenter;
                float sqr0 = Vector3Ex.absDot(toPt, circleU);
                sqr0 *= sqr0;

                float sqr1 = Vector3Ex.absDot(toPt, circleV);
                sqr1 *= sqr1;

                if ((sqr0 + sqr1) > (circleRadius * circleRadius)) return false;
            }

            return true;
        }

        public static bool intersectsOBBAsInfiniteCylinder(Vector3 circleCenter, float circleRadius, Vector3 circleU, Vector3 circleV, OBB obb)
        {
            calcExtents(circleCenter, circleRadius, circleU, circleV, _vector3Buffer);
            if (obb.containsPoint(_vector3Buffer[0])) return true;
            if (obb.containsPoint(_vector3Buffer[1])) return true;
            if (obb.containsPoint(_vector3Buffer[2])) return true;
            if (obb.containsPoint(_vector3Buffer[3])) return true;

            Box3DFace[] boxFaces = Box3D.facesArray;
            foreach(var boxFace in boxFaces)
            {
                Box3D.calcFaceCorners(obb.center, obb.size, obb.rotation, boxFace, _vector3Buffer);
                for (int ptIndex = 0; ptIndex < 4; ++ptIndex)
                {
                    if (intersectsSegment(circleCenter, circleRadius, circleU, circleV, _vector3Buffer[ptIndex], _vector3Buffer[(ptIndex + 1) % 4])) return true;
                }
            }

            return false;
        }

        public static bool intersectsSegment(Vector3 circleCenter, float circleRadius, Vector3 circleU, Vector3 circleV, Vector3 p0, Vector3 p1)
        {
            Vector3 segDir  = p1 - p0;
            float segLength = segDir.magnitude;
            segDir.Normalize();

            Vector3 toCenter    = p0 - circleCenter;
            float dot           = Vector3.Dot(toCenter, segDir);

            if (dot < 0.0f) return containsPointAsInfiniteCylinder(circleCenter, circleRadius, circleU, circleV, p0);
            else if (dot > segLength) return containsPointAsInfiniteCylinder(circleCenter, circleRadius, circleU, circleV, p1);
            else return containsPointAsInfiniteCylinder(circleCenter, circleRadius, circleU, circleV, p0 + segDir * dot);
        }
    }
}
#endif