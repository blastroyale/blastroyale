#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public enum TerrainFlattenMode
    {
        Lowest = 0,
        Average,
        Highest
    }

    public struct TerrainFlattenConfig
    {
        public int                  terrainQuadRadius;
        public TerrainFlattenMode   mode;
        public bool                 applyFalloff;

        public static readonly 
            TerrainFlattenConfig    defaultConfig = new TerrainFlattenConfig()
        {
            terrainQuadRadius       = 1,
            mode                    = TerrainFlattenMode.Average,
            applyFalloff            = true
        };
    }

    public struct TerrainPatch
    {
        public int minX;
        public int maxX;
        public int minZ;
        public int maxZ;

        public int getWidth()
        {
            return maxX - minX + 1;
        }

        public int getDepth()
        {
            return maxZ - minZ + 1;
        }
    }

    public static class TerrainEx
    {
        private static List<Vector3> _vector3Buffer = new List<Vector3>();

        public static void flattenAroundOBB(this Terrain terrain, OBB worldOBB, TerrainFlattenConfig flattenConfig)
        {
            worldOBB.calcCorners(_vector3Buffer, false);
            AABB worldAABB = new AABB(_vector3Buffer);
            TerrainPatch terrainPatch = terrain.calcTerrainPatch(worldAABB, flattenConfig.terrainQuadRadius);

            int patchWidth      = terrainPatch.getWidth();
            int patchDepth      = terrainPatch.getDepth();
            float[,] heights    = terrain.terrainData.GetHeights(terrainPatch.minX, terrainPatch.minZ, patchWidth, patchDepth);
            float maxPatchSize  = Mathf.Max(patchWidth, patchDepth);
            float invHalfExt    = 1.0f / maxPatchSize;
            Vector2 midVertex   = new Vector2((maxPatchSize - 1) * 0.5f, (maxPatchSize - 1) * 0.5f);

            if (flattenConfig.mode == TerrainFlattenMode.Lowest)
            {
                float minHeight = float.MaxValue;
                for (int z = 0; z < patchDepth; ++z)
                {
                    for (int x = 0; x < patchWidth; ++x)
                    {
                        float h = heights[z, x];
                        if (h < minHeight) minHeight = h;
                    }
                }
               
                if (flattenConfig.applyFalloff)
                {
                    for (int z = 0; z < patchDepth; ++z)
                    {
                        for (int x = 0; x < patchWidth; ++x)
                        {
                            float t = (midVertex - new Vector2(x, z)).magnitude * invHalfExt;
                            heights[z, x] = minHeight + (heights[z, x] - minHeight) * t;
                        }
                    }
                }
                else
                {
                    for (int z = 0; z < patchDepth; ++z)
                    {
                        for (int x = 0; x < patchWidth; ++x)
                            heights[z, x] = minHeight;
                    }
                }
            }
            else if (flattenConfig.mode == TerrainFlattenMode.Average)
            {
                float avgHeight = 0.0f;
                for (int z = 0; z < patchDepth; ++z)
                {
                    for (int x = 0; x < patchWidth; ++x)
                        avgHeight += heights[z, x];
                }
                avgHeight /= (float)(patchWidth * patchDepth);

                if (flattenConfig.applyFalloff)
                {
                    for (int z = 0; z < patchDepth; ++z)
                    {
                        for (int x = 0; x < patchWidth; ++x)
                        {
                            float t = (midVertex - new Vector2(x, z)).magnitude * invHalfExt;
                            heights[z, x] = avgHeight + (heights[z, x] - avgHeight) * t;
                        }
                    }
                }
                else
                {
                    for (int z = 0; z < patchDepth; ++z)
                    {
                        for (int x = 0; x < patchWidth; ++x)
                            heights[z, x] = avgHeight;
                    }
                }
            }
            else if (flattenConfig.mode == TerrainFlattenMode.Highest)
            {
                float maxHeight = float.MinValue;
                for (int z = 0; z < patchDepth; ++z)
                {
                    for (int x = 0; x < patchWidth; ++x)
                    {
                        float h = heights[z, x];
                        if (h > maxHeight) maxHeight = h;
                    }
                }

                if (flattenConfig.applyFalloff)
                {
                    for (int z = 0; z < patchDepth; ++z)
                    {
                        for (int x = 0; x < patchWidth; ++x)
                        {
                            float t = (midVertex - new Vector2(x, z)).magnitude * invHalfExt;
                            heights[z, x] = maxHeight + (heights[z, x] - maxHeight) * t;
                        }
                    }
                }
                else
                {
                    for (int z = 0; z < patchDepth; ++z)
                    {
                        for (int x = 0; x < patchWidth; ++x)
                            heights[z, x] = maxHeight;
                    }
                }
            }

            UndoEx.record(terrain.terrainData);
            terrain.terrainData.SetHeightsDelayLOD(terrainPatch.minX, terrainPatch.minZ, heights);
            terrain.terrainData.SyncHeightmap();
        }

        public static Vector2 calcQuadSize(this Terrain terrain)
        {
            float invHeightMapRes = 1.0f / terrain.terrainData.heightmapResolution;
            return new Vector2(terrain.terrainData.size.x * invHeightMapRes, terrain.terrainData.size.z * invHeightMapRes);
        }

        public static void calcTerrainPatchCorners(this Terrain terrain, int quadRadius, AABB worldAABB, Vector3[] corners)
        {
            var patch           = terrain.calcTerrainPatch(worldAABB, quadRadius);
            Vector2 quadSize    = terrain.calcQuadSize();

            float minX = terrain.transform.position.x + quadSize.x * patch.minX;
            float maxX = terrain.transform.position.x + quadSize.x * patch.maxX;
            float minZ = terrain.transform.position.z + quadSize.y * patch.minZ;
            float maxZ = terrain.transform.position.z + quadSize.y * patch.maxZ;

            corners[0] = new Vector3(minX, 0.0f, minZ);
            corners[1] = new Vector3(minX, 0.0f, maxZ);
            corners[2] = new Vector3(maxX, 0.0f, maxZ);
            corners[3] = new Vector3(maxX, 0.0f, minZ);
        }

        public static TerrainPatch calcTerrainPatch(this Terrain terrain, AABB worldAABB, int patchExtentOffset = 0)
        {
            if (patchExtentOffset < 0) patchExtentOffset = 0;

            Vector3 toMin       = worldAABB.min - terrain.transform.position;
            Vector3 toMax       = worldAABB.max - terrain.transform.position;

            TerrainData terrainData = terrain.terrainData;
            float uMin          = toMin.x / terrainData.size.x;
            float uMax          = toMax.x / terrainData.size.x;
            float vMin          = toMin.z / terrainData.size.z;
            float vMax          = toMax.z / terrainData.size.z;

            TerrainPatch terrainPatch = new TerrainPatch();
            terrainPatch.minX   = Mathf.Clamp(Mathf.FloorToInt(uMin * terrainData.heightmapResolution) - patchExtentOffset, 0, terrainData.heightmapResolution - 1);
            terrainPatch.maxX   = Mathf.Clamp(Mathf.CeilToInt(uMax * terrainData.heightmapResolution) + patchExtentOffset, 0, terrainData.heightmapResolution - 1);
            terrainPatch.minZ   = Mathf.Clamp(Mathf.FloorToInt(vMin * terrainData.heightmapResolution) - patchExtentOffset, 0, terrainData.heightmapResolution - 1);
            terrainPatch.maxZ   = Mathf.Clamp(Mathf.CeilToInt(vMax * terrainData.heightmapResolution) + patchExtentOffset, 0, terrainData.heightmapResolution - 1);

            return terrainPatch;
        }

        public static AABB calcModelAABB(this Terrain terrain)
        {
            TerrainData terrainData = terrain.terrainData;
            if (terrainData == null) return AABB.getInvalid();
            Vector3 terrainSize = terrainData.bounds.size;

            return new AABB(terrainData.bounds.center, terrainSize);
        }

        public static OBB calcWorldOBB(this Terrain terrain)
        {
            var modelABB = terrain.calcModelAABB();
            return new OBB(modelABB.center + terrain.transform.position, modelABB.size);
        }

        public static bool isWorldOBBCompletelyInsideTerrainArea(this Terrain terrain, OBB worldOBB)
        {
            worldOBB.calcCorners(_vector3Buffer, false);

            Vector2 normCoords      = new Vector2();
            Vector3 invTerrainSize  = terrain.terrainData.size.getInverse();
            Vector3 relativePos     = new Vector3();
            Vector3 terrainPos      = terrain.transform.position;

            foreach (var pt in _vector3Buffer)
            {
                relativePos.x       = pt.x - terrainPos.x;
                relativePos.y       = pt.y - terrainPos.y;
                relativePos.z       = pt.z - terrainPos.z;

                normCoords.x        = relativePos.x * invTerrainSize.x;
                normCoords.y        = relativePos.z * invTerrainSize.z;

                if (normCoords.x < 0.0f || normCoords.x > 1.0f) return false;
                if (normCoords.y < 0.0f || normCoords.y > 1.0f) return false;
            }

            return true;
        }

        public static bool isWorldPointInsideTerrainArea(this Terrain terrain, Vector3 worldPt)
        {
            Vector2 normCoords  = terrain.worldToNormalizedCoords(worldPt);
            if (normCoords.x < 0.0f || normCoords.x > 1.0f) return false;
            if (normCoords.y < 0.0f || normCoords.y > 1.0f) return false;

            return true;
        }

        public static void calcQuadCorners(this Terrain terrain, Vector3 worldPos, Vector3[] quadCorners)
        {
            quadCorners[0]          = Vector3.zero;
            quadCorners[1]          = Vector3.zero;
            quadCorners[2]          = Vector3.zero;
            quadCorners[3]          = Vector3.zero;

            Vector3 terrainPos      = terrain.transform.position;
            Vector3 relativePos     = worldPos - terrainPos;
            TerrainData terrainData = terrain.terrainData;

            int numQuadsX           = terrainData.heightmapResolution;
            int numQuadsZ           = terrainData.heightmapResolution;
            float quadWidth         = terrainData.size.x / (float)numQuadsX;
            float quadDepth         = terrainData.size.z / (float)numQuadsZ;

            int quadX               = Mathf.FloorToInt(relativePos.x / quadWidth);
            int quadZ               = Mathf.FloorToInt(relativePos.z / quadDepth);

            if (quadX < 0 || quadX >= numQuadsX) return;
            if (quadZ < 0 || quadZ >= numQuadsZ) return;

            quadCorners[0]          = terrainPos + terrain.transform.right * quadX * quadWidth + terrain.transform.forward * quadZ * quadDepth;
            quadCorners[1]          = quadCorners[0] + terrain.transform.forward * quadDepth;
            quadCorners[2]          = quadCorners[1] + terrain.transform.right * quadWidth;
            quadCorners[3]          = quadCorners[0] + terrain.transform.right * quadWidth;

            quadCorners[0].y        = terrain.sampleWorldHeight(terrainPos.y, quadCorners[0]);
            quadCorners[1].y        = terrain.sampleWorldHeight(terrainPos.y, quadCorners[1]);
            quadCorners[2].y        = terrain.sampleWorldHeight(terrainPos.y, quadCorners[2]);
            quadCorners[3].y        = terrain.sampleWorldHeight(terrainPos.y, quadCorners[3]);
        }

        public static Vector2 worldToNormalizedCoords(this Terrain terrain, Vector3 worldPos)
        {
            Vector3 relativePos = worldPos - terrain.transform.position;
            Vector3 coords      = Vector3.Scale(relativePos, new Vector3(1.0f / terrain.terrainData.size.x, 1.0f, 1.0f / terrain.terrainData.size.z));
            return new Vector2(coords.x, coords.z);
        }

        public static Vector3 getInterpolatedNormal(this Terrain terrain, Vector3 worldPos)
        {
            Vector2 normCoords = terrain.worldToNormalizedCoords(worldPos);
            return terrain.terrainData.GetInterpolatedNormal(normCoords.x, normCoords.y);
        }

        public static Vector3 getNormal(this Terrain terrain, Vector3 worldPos)
        {
            TerrainCollider collider = terrain.gameObject.getTerrainCollider();
            if (collider == null) return getInterpolatedNormal(terrain, worldPos);

            worldPos.y = terrain.transform.position.y;
            worldPos.y += terrain.terrainData.bounds.size.y + 0.01f;

            RaycastHit rayHit;
            Ray ray = new Ray(worldPos, Vector3.down);
            if (collider.Raycast(ray, out rayHit, float.MaxValue)) return rayHit.normal;
            else return getInterpolatedNormal(terrain, worldPos);
        }

        public static float sampleWorldHeight(this Terrain terrain, float terrainYPos, Vector3 point)
        {
            float terrainHeight = terrain.SampleHeight(point);
            return terrainHeight + terrainYPos;
        }

        public static float getDistanceToPoint(this Terrain terrain, float terrainYPos, Vector3 point)
        {
            float terrainHeight = terrain.sampleWorldHeight(terrainYPos, point);
            return (point.y - terrainHeight);
        }

        public static Vector3 projectPoint(this Terrain terrain, float terrainYPos, Vector3 point)
        {
            // Note: Causes issues when the scene uses a grid of terrains and some objects reside 
            //       on the boundary between 2 terrains. Seems that it's not needed anyway.
            //if (!terrain.isWorldPointInsideTerrainArea(point)) return point;

            float distToPt = terrain.getDistanceToPoint(terrainYPos, point);
            return point - Vector3.up * distToPt;
        }

        public static void projectPoints(this Terrain terrain, float terrainYPos, List<Vector3> points)
        {
            int numPoints = points.Count;
            for (int i = 0; i < numPoints; ++i)
                points[i] = terrain.projectPoint(terrainYPos, points[i]);
        }

        public static int findIndexOfFurthestPointAbove(this Terrain terrain, List<Vector3> points)
        {
            int furthestPtIndex = -1;
            float maxDist       = float.MinValue;
            float terrainYPos   = terrain.gameObject.transform.position.y;

            for(int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float dist = terrain.getDistanceToPoint(terrainYPos, pt);
                if (dist > 0.0f && dist > maxDist) 
                {
                    furthestPtIndex = ptIndex;
                    maxDist = dist;
                }
            }

            return furthestPtIndex;
        }

        public static int findIndexOfFurthestPointBelow(this Terrain terrain, List<Vector3> points)
        {
            int furthestPtIndex = -1;
            float minDist       = float.MaxValue;
            float terrainYPos   = terrain.gameObject.transform.position.y;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float dist = terrain.getDistanceToPoint(terrainYPos, pt);
                if (dist < 0.0f && dist < minDist)
                {
                    furthestPtIndex = ptIndex;
                    minDist = dist;
                }
            }

            return furthestPtIndex;
        }

        public static int findIndexOfClosestPointAbove(this Terrain terrain, List<Vector3> points)
        {
            int closestPtIndex  = -1;
            float minDist       = float.MaxValue;
            float terrainYPos   = terrain.gameObject.transform.position.y;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float dist = terrain.getDistanceToPoint(terrainYPos, pt);
                if (dist > 0.0f && dist < minDist)
                {
                    closestPtIndex = ptIndex;
                    minDist = dist;
                }
            }

            return closestPtIndex;
        }

        public static int findIndexOfClosestPointBelow(this Terrain terrain, List<Vector3> points)
        {
            int closestPtIndex  = -1;
            float minDist       = float.MinValue;
            float terrainYPos   = terrain.gameObject.transform.position.y;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float dist = terrain.getDistanceToPoint(terrainYPos, pt);
                if (dist < 0.0f && dist > minDist)
                {
                    closestPtIndex = ptIndex;
                    minDist = dist;
                }
            }

            return closestPtIndex;
        }
    }
}
#endif