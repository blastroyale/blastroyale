#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class ObjectProjection
    {
        private static ObjectOnPlaneProjector           _planeProjector         = new ObjectOnPlaneProjector();
        private static ObjectOnSphereProjector          _sphereProjector        = new ObjectOnSphereProjector();
        private static ObjectOnTerrainMeshProjector     _terrainMeshProjector   = new ObjectOnTerrainMeshProjector();
        private static ObjectOnUnityTerrainProjector    _unityTerrainProjector  = new ObjectOnUnityTerrainProjector();

        public static void projectHierarchiesOnPlane(List<GameObject> parents, Plane plane, ObjectProjectionSettings projectionSettings, List<ObjectProjectionResult> projectionResults)
        {
            _planeProjector.projectionPlane = plane;
            _planeProjector.projectHierarchies(parents, projectionSettings, projectionResults);
        }

        public static ObjectProjectionResult projectHierarchyOnPlane(GameObject parent, Plane plane, ObjectProjectionSettings projectionSettings)
        {
            _planeProjector.projectionPlane = plane;
            return _planeProjector.projectHierarchy(parent, projectionSettings);
        }

        public static void projectHierarchiesOnObject(List<GameObject> parents, GameObject surfaceObject, GameObjectType surfaceObjectType, Plane projectionObjectPlane, ObjectProjectionSettings projectionSettings, List<ObjectProjectionResult> projectionResults)
        {
            if (surfaceObjectType == GameObjectType.Terrain) projectHierarchiesOnUnityTerrain(parents, surfaceObject.getTerrain(), projectionSettings, projectionResults);
            else if (surfaceObjectType == GameObjectType.Mesh)
            {
                PluginObjectLayer pluginLayer = PluginObjectLayerDb.instance.getLayer(surfaceObject.layer);
                if (pluginLayer.isTerrainMesh) projectHierarchiesOnTerrainMesh(parents, surfaceObject, projectionSettings, projectionResults);
                else if (pluginLayer.isSphericalMesh) projectHierarchiesOnSphereMesh(parents, surfaceObject, projectionSettings, projectionResults);
                else projectHierarchiesOnPlane(parents, projectionObjectPlane, projectionSettings, projectionResults);
            }
            else if (surfaceObjectType == GameObjectType.Sprite) projectHierarchiesOnPlane(parents, projectionObjectPlane, projectionSettings, projectionResults);
            else projectHierarchiesOnPlane(parents, projectionObjectPlane, projectionSettings, projectionResults);
        }

        public static ObjectProjectionResult projectHierarchyOnObject(GameObject parent, GameObject surfaceObject, GameObjectType surfaceObjectType, Plane projectionObjectPlane, ObjectProjectionSettings projectionSettings)
        {
            if (surfaceObjectType == GameObjectType.Terrain) return projectHierarchyOnUnityTerrain(parent, surfaceObject.getTerrain(), projectionSettings);
            else if (surfaceObjectType == GameObjectType.Mesh)
            {
                PluginObjectLayer pluginLayer = PluginObjectLayerDb.instance.getLayer(surfaceObject.layer);
                if (pluginLayer.isTerrainMesh) return projectHierarchyOnTerrainMesh(parent, surfaceObject, projectionSettings);
                else if (pluginLayer.isSphericalMesh) return projectHierarchyOnSphereMesh(parent, surfaceObject, projectionSettings);
                else return projectHierarchyOnPlane(parent, projectionObjectPlane, projectionSettings);
            }
            else if (surfaceObjectType == GameObjectType.Sprite) return projectHierarchyOnPlane(parent, projectionObjectPlane, projectionSettings);
            else return projectHierarchyOnPlane(parent, projectionObjectPlane, projectionSettings);
        }

        public static void projectHierarchiesOnSphereMesh(List<GameObject> parents, GameObject sphereMeshObject, ObjectProjectionSettings projectionSettings, List<ObjectProjectionResult> projectionResults)
        {
            OBB worldOBB = ObjectBounds.calcMeshWorldOBB(sphereMeshObject);
            if (!worldOBB.isValid) return;

            _sphereProjector.sphere             = new Sphere(worldOBB.center, sphereMeshObject.getMesh().calcInscribedWorldSphereRadius(sphereMeshObject.transform));
            _sphereProjector.sphereObject       = sphereMeshObject;
            _sphereProjector.projectHierarchies(parents, projectionSettings, projectionResults);
        }

        public static ObjectProjectionResult projectHierarchyOnSphereMesh(GameObject parent, GameObject sphereMeshObject, ObjectProjectionSettings projectionSettings)
        {
            OBB worldOBB = ObjectBounds.calcMeshWorldOBB(sphereMeshObject);
            if (!worldOBB.isValid) return ObjectProjectionResult.notProjectedResult;

            _sphereProjector.sphere             = new Sphere(worldOBB.center, sphereMeshObject.getMesh().calcInscribedWorldSphereRadius(sphereMeshObject.transform));
            _sphereProjector.sphereObject       = sphereMeshObject;
            return _sphereProjector.projectHierarchy(parent, projectionSettings);
        }

        public static bool projectHierarchiesOnTerrainMesh(List<GameObject> parents, GameObject terrainMeshObject, ObjectProjectionSettings projectionSettings, List<ObjectProjectionResult> projectionResults)
        {
            _terrainMeshProjector.terrainMeshObject = terrainMeshObject;
            return _terrainMeshProjector.projectHierarchies(parents, projectionSettings, projectionResults);
        }

        public static ObjectProjectionResult projectHierarchyOnTerrainMesh(GameObject parent, GameObject terrainMeshObject, ObjectProjectionSettings projectionSettings)
        {
            _terrainMeshProjector.terrainMeshObject = terrainMeshObject;
            return _terrainMeshProjector.projectHierarchy(parent, projectionSettings);
        }

        public static bool projectHierarchiesOnUnityTerrain(List<GameObject> parents, Terrain terrain, ObjectProjectionSettings projectionSettings, List<ObjectProjectionResult> projectionResults)
        {
            _unityTerrainProjector.unityTerrain = terrain;
            return _unityTerrainProjector.projectHierarchies(parents, projectionSettings, projectionResults);
        }

        public static ObjectProjectionResult projectHierarchyOnUnityTerrain(GameObject parent, Terrain terrain, ObjectProjectionSettings projectionSettings)
        {
            _unityTerrainProjector.unityTerrain = terrain;
            return _unityTerrainProjector.projectHierarchy(parent, projectionSettings);
        }

        public static bool projectHierarchyOnTerrains(GameObject parent, OBB enclosingOBB, TerrainCollection terrains, ObjectProjectionSettings projectionSettings)
        {
            if (projectHierarchyOnUnityTerrains(parent, enclosingOBB, terrains.unityTerrains, projectionSettings)) return true;
            return projectHierarchyOnTerrainMeshes(parent, enclosingOBB, terrains.terrainMeshes, projectionSettings);
        }

        public static bool projectHierarchyOnUnityTerrains(GameObject parent, OBB enclosingOBB, List<Terrain> unityTerrains, ObjectProjectionSettings projectionSettings)
        {
            if (!enclosingOBB.isValid) enclosingOBB = ObjectBounds.calcHierarchyWorldOBB(parent, ObjectProjector.projectableBoundsQConfig);
            if (!enclosingOBB.isValid) return false;

            Terrain bestTerrain = findUnityTerrainToProjectOnto(enclosingOBB, unityTerrains);
            if (bestTerrain != null) return projectHierarchyOnUnityTerrain(parent, bestTerrain, projectionSettings).wasProjected;
            return false;
        }

        public static bool projectHierarchyOnTerrainMeshes(GameObject parent, OBB enclosingOBB, List<GameObject> terrainMeshes, ObjectProjectionSettings projectionSettings)
        {
            if (!enclosingOBB.isValid) enclosingOBB = ObjectBounds.calcHierarchyWorldOBB(parent, ObjectProjector.projectableBoundsQConfig);
            if (!enclosingOBB.isValid) return false;
            projectionSettings.projectAsUnit = true;

            GameObject bestTerrain = findTerrainMeshToProjectOnto(enclosingOBB, terrainMeshes);
            if (bestTerrain != null) return projectHierarchyOnTerrainMesh(parent, bestTerrain, projectionSettings).wasProjected;
            return false;
        }

        public static bool projectHierarchiesOnTerrainsAsUnit(List<GameObject> parents, TerrainCollection terrains, ObjectProjectionSettings projectionSettings)
        {
            OBB enclosingOBB = ObjectBounds.calcHierarchiesWorldOBB(parents, ObjectProjector.projectableBoundsQConfig);
  
            if (projectHierarchiesOnUnityTerrainsAsUnit(parents, enclosingOBB, terrains.unityTerrains, projectionSettings)) return true;
            return projectHierarchiesOnTerrainMeshesAsUnit(parents, enclosingOBB, terrains.terrainMeshes, projectionSettings);
        }

        public static bool projectHierarchiesOnTerrainsAsUnit(List<GameObject> parents, OBB enclosingOBB, TerrainCollection terrains, ObjectProjectionSettings projectionSettings)
        {
            if (!enclosingOBB.isValid) enclosingOBB = ObjectBounds.calcHierarchiesWorldOBB(parents, ObjectProjector.projectableBoundsQConfig);

            if (projectHierarchiesOnUnityTerrainsAsUnit(parents, enclosingOBB, terrains.unityTerrains, projectionSettings)) return true;
            return projectHierarchiesOnTerrainMeshesAsUnit(parents, enclosingOBB, terrains.terrainMeshes, projectionSettings);
        }

        public static bool projectHierarchiesOnUnityTerrainsAsUnit(List<GameObject> parents, OBB enclosingOBB, List<Terrain> unityTerrains, ObjectProjectionSettings projectionSettings)
        {
            if (!enclosingOBB.isValid) enclosingOBB = ObjectBounds.calcHierarchiesWorldOBB(parents, ObjectProjector.projectableBoundsQConfig);
            if (!enclosingOBB.isValid) return false;
            projectionSettings.projectAsUnit = true;

            Terrain bestTerrain = findUnityTerrainToProjectOnto(enclosingOBB, unityTerrains);
            if (bestTerrain != null) return projectHierarchiesOnUnityTerrain(parents, bestTerrain, projectionSettings, null);
            return false;
        }

        public static bool projectHierarchiesOnTerrainMeshesAsUnit(List<GameObject> parents, OBB enclosingOBB, List<GameObject> terrainMeshes, ObjectProjectionSettings projectionSettings)
        {
            if (!enclosingOBB.isValid) enclosingOBB = ObjectBounds.calcHierarchiesWorldOBB(parents, ObjectProjector.projectableBoundsQConfig);
            if (!enclosingOBB.isValid) return false;
            projectionSettings.projectAsUnit = true;

            GameObject bestTerrain = findTerrainMeshToProjectOnto(enclosingOBB, terrainMeshes);
            if (bestTerrain != null) return projectHierarchiesOnTerrainMesh(parents, bestTerrain, projectionSettings, null);
            return false;
        }

        private static Terrain findUnityTerrainToProjectOnto(OBB enclosingOBB, List<Terrain> unityTerrains)
        {
            // Attempt to find the terrain whose area completely contains the OBB
            Terrain bestTerrain = null;
            foreach (var terrain in unityTerrains)
            {
                if (terrain.isWorldOBBCompletelyInsideTerrainArea(enclosingOBB))
                {
                    bestTerrain = terrain;
                    break;
                }
            }

            if (bestTerrain == null)
            {
                // If no terrain has been found, attempt to find the first the terrain whose center point
                // is closest to the OBB center.
                float minDistance = float.MaxValue;
                foreach (var terrain in unityTerrains)
                {
                    OBB terrainOBB = terrain.calcWorldOBB();
                    //if (terrainOBB.intersectsOBB(enclosingOBB))
                    {
                        float d = (terrainOBB.center - enclosingOBB.center).magnitude;
                        if (d < minDistance)
                        {
                            d = minDistance;
                            bestTerrain = terrain;
                        }
                    }
                }
            }

            return bestTerrain;
        }

        private static GameObject findTerrainMeshToProjectOnto(OBB enclosingOBB, List<GameObject> terrainMeshes)
        {
            // Attempt to find the terrain whose area completely contains the OBB
            GameObject bestTerrain = null;
            foreach (var terrain in terrainMeshes)
            {
                if (TerrainMeshUtil.isWorldOBBCompletelyInsideTerrainArea(terrain, enclosingOBB))
                {
                    bestTerrain = terrain;
                    break;
                }
            }

            if (bestTerrain == null)
            {
                ObjectBounds.QueryConfig boundsQConfig = ObjectBounds.QueryConfig.defaultConfig;
                boundsQConfig.objectTypes = GameObjectType.Mesh;

                // If no terrain has been found, attempt to find the first the terrain whose center point
                // is closest to the OBB center.
                float minDistance = float.MaxValue;
                foreach (var terrain in terrainMeshes)
                {
                    var terrainOBB = ObjectBounds.calcWorldOBB(terrain, boundsQConfig);
                    //if (terrainOBB.intersectsOBB(enclosingOBB))
                    {
                        float d = (terrainOBB.center - enclosingOBB.center).magnitude;
                        if (d < minDistance)
                        {
                            d           = minDistance;
                            bestTerrain = terrain;
                        }
                    }
                }
            }

            return bestTerrain;
        }
    }
}
#endif