#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class TerrainMeshUtil
    {
        private static List<Vector3> _vector3Buffer = new List<Vector3>();

        public static OBB calcWorldOBB(GameObject terrainObject)
        {
            ObjectBounds.QueryConfig boundsQConfig = ObjectBounds.QueryConfig.defaultConfig;
            boundsQConfig.objectTypes = GameObjectType.Mesh;

            return ObjectBounds.calcWorldOBB(terrainObject, boundsQConfig);
        }

        public static bool isWorldPointInsideTerrainArea(GameObject terrainObject, Vector3 worldPt)
        {
            var terrainOBB = calcWorldOBB(terrainObject);
            if (!terrainOBB.isValid) return false;

            Vector3Int axisMask;
            if (ObjectPrefs.instance.terrainMeshUpAxis == Axis.Y)       axisMask = new Vector3Int(1, 0, 1);
            else if (ObjectPrefs.instance.terrainMeshUpAxis == Axis.X)  axisMask = new Vector3Int(0, 1, 1);
            else axisMask = new Vector3Int(1, 1, 0);

            return terrainOBB.containsPoint(worldPt, axisMask);
        }

        public static bool isWorldPointInsideTerrainArea(GameObject terrainObject, OBB terrainWorldOBB, Vector3 worldPt)
        {
            Vector3Int axisMask;
            if (ObjectPrefs.instance.terrainMeshUpAxis == Axis.Y)       axisMask = new Vector3Int(1, 0, 1);
            else if (ObjectPrefs.instance.terrainMeshUpAxis == Axis.X)  axisMask = new Vector3Int(0, 1, 1);
            else axisMask = new Vector3Int(1, 1, 0);

            return terrainWorldOBB.containsPoint(worldPt, axisMask);
        }

        public static bool isWorldOBBCompletelyInsideTerrainArea(GameObject terrainObject, OBB worldOBB)
        {
            var terrainOBB = calcWorldOBB(terrainObject);
            if (!terrainOBB.isValid) return false;

            worldOBB.calcCorners(_vector3Buffer, false);

            Vector3Int axisMask;
            if (ObjectPrefs.instance.terrainMeshUpAxis == Axis.Y)       axisMask = new Vector3Int(1, 0, 1);
            else if (ObjectPrefs.instance.terrainMeshUpAxis == Axis.X)  axisMask = new Vector3Int(0, 1, 1);
            else axisMask = new Vector3Int(1, 1, 0);

            return terrainOBB.containsPoints(_vector3Buffer, axisMask);
        }

        public static float sampleWorldHeightAlongUpAxis(GameObject terrainObject, PluginMesh terrainMesh, Vector3 worldPt)
        {
            Vector3 terrainUp       = ObjectPrefs.instance.getTerrainMeshUp(terrainObject);
            Plane terrainHrzPlane   = ObjectPrefs.instance.getTerrainMeshHorizontalPlane(terrainObject);
            Vector3 rayOrigin       = terrainHrzPlane.projectPoint(worldPt);

            AABB meshAABB           = terrainMesh.aabb;
            rayOrigin               += terrainUp * (meshAABB.size[(int)ObjectPrefs.instance.terrainMeshUpAxis] + 1e-2f);
            Ray ray                 = new Ray(rayOrigin, -terrainUp);

            MeshRaycastConfig raycastConfig         = MeshRaycastConfig.defaultConfig;
            raycastConfig.canHitCameraCulledFaces   = true;

            MeshRayHit rayHit;
            if (terrainMesh.raycastClosest(ray, terrainObject.transform, raycastConfig, out rayHit))
                return Vector3.Dot(ray.GetPoint(rayHit.hitEnter), terrainUp);

            return 0.0f;
        }

        public static float getDistanceToPoint(GameObject terrainObject, PluginMesh terrainMesh, Vector3 worldPt)
        {
            float height        = sampleWorldHeightAlongUpAxis(terrainObject, terrainMesh, worldPt);
            Vector3 terrainUp   = ObjectPrefs.instance.getTerrainMeshUp(terrainObject);
            return (Vector3.Dot(worldPt, terrainUp) - height);
        }

        public static Vector3 projectPoint(GameObject terrainObject, PluginMesh terrainMesh, Vector3 point)
        {
            if (!isWorldPointInsideTerrainArea(terrainObject, point)) return point;

            Vector3 terrainUp   = ObjectPrefs.instance.getTerrainMeshUp(terrainObject);
            float distToPt      = getDistanceToPoint(terrainObject, terrainMesh, point);
            return point - terrainUp * distToPt;
        }

        public static void projectPoints(GameObject terrainObject, PluginMesh terrainMesh, List<Vector3> points)
        {
            OBB terrainOBB = calcWorldOBB(terrainObject);
            if (!terrainOBB.isValid) return;

            int numPoints = points.Count;
            for (int i = 0; i < numPoints; ++i)
            {
                if (!isWorldPointInsideTerrainArea(terrainObject, terrainOBB, points[i])) continue;

                Vector3 terrainUp   = ObjectPrefs.instance.getTerrainMeshUp(terrainObject);
                float distToPt      = getDistanceToPoint(terrainObject, terrainMesh, points[i]);
                points[i]           = points[i] - terrainUp * distToPt;
            }
        }

        public static int findIndexOfClosestPointAbove(GameObject terrainObject, PluginMesh terrainMesh, List<Vector3> points)
        {
            int closestPtIndex      = -1;
            float minDist           = float.MaxValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt          = points[ptIndex];
                float dist          = getDistanceToPoint(terrainObject, terrainMesh, pt);
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