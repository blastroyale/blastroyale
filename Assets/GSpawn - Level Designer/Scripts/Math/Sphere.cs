#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public struct Sphere
    {
        public enum RightUpExtents
        {
            Left = 0,
            Up,
            Right,
            Down
        }

        public Vector3      center;
        private float       _radius;

        public float        radius  { get { return _radius; } set { _radius = Mathf.Max(0.0f, value); } }

        public Sphere(Vector3 center, float radius)
        {
            this.center     = center;
            _radius         = Mathf.Max(0.0f, radius);
        }

        public Sphere(AABB aabb)
        {
            center  = aabb.center;
            _radius = aabb.extents.magnitude;
        }

        public Sphere(OBB obb)
        {
            center  = obb.center;
            _radius = obb.extents.magnitude;
        }

        public Sphere(IEnumerable<Vector3> points)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (Vector3 pt in points)
            {
                min = Vector3.Min(pt, min);
                max = Vector3.Max(pt, max);
            }

            center  = (min + max) * 0.5f;
            _radius = (max - min).magnitude * 0.5f;
        }

        public bool containsPoint(Vector3 point)
        {
            Vector3 toCenter;
            toCenter.x = center.x - point.x;
            toCenter.y = center.y - point.y;
            toCenter.z = center.z - point.z;

            return (toCenter.x * toCenter.x + toCenter.y * toCenter.y + toCenter.z * toCenter.z) <= _radius * _radius;
        }

        public bool raycast(Ray ray)
        {
            Vector3 sphereCenterToRayOrigin = ray.origin - center;
            float a = Vector3.SqrMagnitude(ray.direction);
            float b = 2.0f * Vector3.Dot(ray.direction, sphereCenterToRayOrigin);
            float c = Vector3.SqrMagnitude(sphereCenterToRayOrigin) - _radius * _radius;

            float t1, t2;
            if (MathEx.solveQuadratic(a, b, c, out t1, out t2))
            {
                if (t1 < 0.0f && t2 < 0.0f) return false;

                if (t1 < 0.0f)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                return true;
            }

            return false;
        }

        public bool raycast(Ray ray, out float t)
        {
            t = 0.0f;

            Vector3 sphereCenterToRayOrigin = ray.origin - center;
            float a = Vector3.SqrMagnitude(ray.direction);
            float b = 2.0f * Vector3.Dot(ray.direction, sphereCenterToRayOrigin);
            float c = Vector3.SqrMagnitude(sphereCenterToRayOrigin) - _radius * _radius;

            float t1, t2;
            if (MathEx.solveQuadratic(a, b, c, out t1, out t2))
            {
                if (t1 < 0.0f && t2 < 0.0f) return false;

                if (t1 < 0.0f)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }
                t = t1;

                return true;
            }

            return false;
        }

        public void getRightUpExtents(Vector3 right, Vector3 up, List<Vector3> extents)
        {
            extents.Clear();
            extents.Add(center - right * _radius);
            extents.Add(center + up * _radius);
            extents.Add(center + right * _radius);
            extents.Add(center - up * _radius);
        }

        public static bool containsPoint(Vector3 sphereCenter, float sphereRadius, Vector3 point)
        {
            float toCenterX = sphereCenter.x - point.x;
            float toCenterY = sphereCenter.y - point.y;
            float toCenterZ = sphereCenter.z - point.z;
            return (toCenterX * toCenterX + toCenterY * toCenterY + toCenterZ * toCenterZ) <= sphereRadius * sphereRadius;
        }

        static public bool raycast(Vector3 sphereCenter, float sphereRadius, Ray ray, out float t)
        {
            t = 0.0f;

            Vector3 sphereCenterToRayOrigin = ray.origin - sphereCenter;
            float a = Vector3.SqrMagnitude(ray.direction);
            float b = 2.0f * Vector3.Dot(ray.direction, sphereCenterToRayOrigin);
            float c = Vector3.SqrMagnitude(sphereCenterToRayOrigin) - sphereRadius * sphereRadius;

            float t1, t2;
            if (MathEx.solveQuadratic(a, b, c, out t1, out t2))
            {
                if (t1 < 0.0f && t2 < 0.0f) return false;

                if (t1 < 0.0f)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }
                t = t1;

                return true;
            }

            return false;
        }
    }
}
#endif