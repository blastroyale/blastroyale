#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class SphericalMeshUtil
    {
        public static float getSphereRadius(GameObject sphereObject)
        {
            return sphereObject.transform.lossyScale.getMaxAbsComp() * 0.5f;
        }

        public static Vector3 getSphereCenter(GameObject sphereObject)
        {
            return sphereObject.transform.position;
        }

        public static float sampleWorldHeightFromSphereSurface(GameObject sphereObject, PluginMesh sphericalMesh, Vector3 worldPt)
        {
            Vector3 rayOrigin   = getSphereCenter(sphereObject);
            Vector3 rayDir      = (worldPt - rayOrigin);
            float rayDirLength  = rayDir.magnitude;

            Ray ray                                 = new Ray(rayOrigin, rayDir.normalized);
            MeshRaycastConfig raycastConfig         = MeshRaycastConfig.defaultConfig;
            raycastConfig.canHitCameraCulledFaces   = true;

            MeshRayHit rayHit;
            if (sphericalMesh.raycastClosest(ray, sphereObject.transform, raycastConfig, out rayHit))
            {
                float sphereRadiusOnContact = (rayOrigin - rayHit.hitPoint).magnitude;
                return rayDirLength - sphereRadiusOnContact;
            }

            return 0.0f;
        }

        public static float sampleWorldHeightFromSphereSurface(GameObject sphereObject, Vector3 worldPt)
        {
            return (worldPt - getSphereCenter(sphereObject)).magnitude - getSphereRadius(sphereObject);
        }

        public static float getDistanceToPoint(GameObject sphereObject, PluginMesh sphericalMesh, Vector3 point)
        {
            return sampleWorldHeightFromSphereSurface(sphereObject, sphericalMesh, point);
        }

        public static float getDistanceToPoint(GameObject sphereObject, Vector3 point)
        {
            return sampleWorldHeightFromSphereSurface(sphereObject, point);
        }

        public static Vector3 projectPoint(GameObject sphereObject, PluginMesh sphericalMesh, Vector3 point)
        {
            Vector3 center = getSphereCenter(sphereObject);
            Vector3 normal = (point - center).normalized;

            float distToPt = getDistanceToPoint(sphereObject, sphericalMesh, point);
            return point - normal * distToPt;
        }

        public static Vector3 projectPoint(GameObject sphereObject, Vector3 point)
        {
            Vector3 center = getSphereCenter(sphereObject);
            Vector3 normal = (point - center).normalized;

            float distToPt = getDistanceToPoint(sphereObject, point);
            return point - normal * distToPt;
        }

        public static void projectPoints(GameObject sphereObject, PluginMesh sphericalMesh, List<Vector3> points)
        {
            int numPoints = points.Count;
            for (int i = 0; i < numPoints; ++i)
                points[i] = projectPoint(sphereObject, sphericalMesh, points[i]);
        }

        public static void projectPoints(GameObject sphereObject, List<Vector3> points)
        {
            int numPoints = points.Count;
            for (int i = 0; i < numPoints; ++i)
                points[i] = projectPoint(sphereObject, points[i]);
        }

        public static int findIndexOfClosestPointAbove(GameObject sphereObject, PluginMesh sphericalMesh, List<Vector3> points)
        {
            int closestPtIndex      = -1;
            float minDist           = float.MaxValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt          = points[ptIndex];
                float dist          = getDistanceToPoint(sphereObject, sphericalMesh, pt);
                if (dist > 0.0f && dist < minDist)
                {
                    closestPtIndex  = ptIndex;
                    minDist         = dist;
                }
            }

            return closestPtIndex;
        }

        public static int findIndexOfClosestPointAbove(GameObject sphereObject, List<Vector3> points)
        {
            int closestPtIndex      = -1;
            float minDist           = float.MaxValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt          = points[ptIndex];
                float dist          = getDistanceToPoint(sphereObject, pt);
                if (dist > 0.0f && dist < minDist)
                {
                    closestPtIndex  = ptIndex;
                    minDist         = dist;
                }
            }

            return closestPtIndex;
        }
    }
}
#endif
