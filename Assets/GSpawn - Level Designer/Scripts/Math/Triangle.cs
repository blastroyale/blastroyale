#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class Triangle3D
    {
        public static bool raycast(Ray ray, out float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            t = 0.0f;

            float rayEnter;
            Plane trianglePlane = new Plane(p0, p1, p2);
            if (trianglePlane.Raycast(ray, out rayEnter) && 
                containsPoint(ray.GetPoint(rayEnter), false, p0, p1, p2))
            {
                t = rayEnter;
                return true;
            }
       
            return false;
        }

        public static bool raycast(Ray ray, out float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 normal)
        {
            t = 0.0f;

            float rayEnter;
            Plane trianglePlane = new Plane(normal, p0);

            if (trianglePlane.Raycast(ray, out rayEnter) &&
                containsPoint(ray.GetPoint(rayEnter), false, p0, p1, p2, normal))
            {
                t = rayEnter;
                return true;
            }

            return false;
        }

        public static bool raycast(Ray ray, out float t, Vector3[] pts, Vector3 normal)
        {
            t = 0.0f;

            float rayEnter;
            Plane trianglePlane = new Plane(normal, pts[0]);

            if (trianglePlane.Raycast(ray, out rayEnter))
            {
                Vector3 point = ray.GetPoint(rayEnter);
                Vector3 edge0 = pts[1] - pts[0];
                Vector3 edge1 = pts[2] - pts[1];
                Vector3 edge2 = pts[0] - pts[2];

                Vector3 edgeNormal = Vector3.Cross(edge0, normal).normalized;
                if (Vector3.Dot(point - pts[0], edgeNormal) > 0.0f) return false;

                edgeNormal = Vector3.Cross(edge1, normal).normalized;
                if (Vector3.Dot(point - pts[1], edgeNormal) > 0.0f) return false;

                edgeNormal = Vector3.Cross(edge2, normal).normalized;
                if (Vector3.Dot(point - pts[2], edgeNormal) > 0.0f) return false;
    
                t = rayEnter;
                return true;
            }

            return false;
        }

        public static bool raycastWire(Ray ray, out float t, Vector3 p0, Vector3 p1, Vector3 p2, float wireEps)
        {
            t = 0.0f;

            float rayEnter;
            Plane trianglePlane = new Plane(p0, p1, p2);
            if (trianglePlane.Raycast(ray, out rayEnter))
            {
                Vector3 intersectPt = ray.GetPoint(rayEnter);
                float distToSegment = intersectPt.getDistanceToSegment(p0, p1);
                if (distToSegment <= wireEps)
                {
                    t = rayEnter;
                    return true;
                }

                distToSegment = intersectPt.getDistanceToSegment(p1, p2);
                if (distToSegment <= wireEps)
                {
                    t = rayEnter;
                    return true;
                }

                distToSegment = intersectPt.getDistanceToSegment(p2, p0);
                if (distToSegment <= wireEps)
                {
                    t = rayEnter;
                    return true;
                }
            }

            return false;
        }

        public static bool containsPoint(Vector3 point, bool checkOnPlane, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Vector3 edge0   = p1 - p0;
            Vector3 edge1   = p2 - p1;
            Vector3 edge2   = p0 - p2;
            Vector3 normal  = Vector3.Cross(edge0, -edge2).normalized;
    
            if (checkOnPlane)
            {
                float distanceToPt = Vector3.Dot(point - p0, normal);
                if (Mathf.Abs(distanceToPt) > 1e-5f) return false;
            }

            Vector3 edgeNormal = Vector3.Cross(edge0, normal).normalized;
            if (Vector3.Dot(point - p0, edgeNormal) > 0.0f) return false;
            
            edgeNormal = Vector3.Cross(edge1, normal).normalized;
            if (Vector3.Dot(point - p1, edgeNormal) > 0.0f) return false;
          
            edgeNormal = Vector3.Cross(edge2, normal).normalized;
            if (Vector3.Dot(point - p2, edgeNormal) > 0.0f) return false;

            return true;
        }

        public static bool containsPoint(Vector3 point, bool checkOnPlane, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 normal)
        {
            Vector3 edge0 = p1 - p0;
            Vector3 edge1 = p2 - p1;
            Vector3 edge2 = p0 - p2;

            if (checkOnPlane)
            {
                float distanceToPt = Vector3.Dot(point - p0, normal);
                if (Mathf.Abs(distanceToPt) > 1e-5f) return false;
            }

            Vector3 edgeNormal = Vector3.Cross(edge0, normal).normalized;
            if (Vector3.Dot(point - p0, edgeNormal) > 0.0f) return false;

            edgeNormal = Vector3.Cross(edge1, normal).normalized;
            if (Vector3.Dot(point - p1, edgeNormal) > 0.0f) return false;

            edgeNormal = Vector3.Cross(edge2, normal).normalized;
            if (Vector3.Dot(point - p2, edgeNormal) > 0.0f) return false;

            return true;
        }
    }
}
#endif