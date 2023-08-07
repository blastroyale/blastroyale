#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectOnTerrainMeshProjector : ObjectProjector
    {
        private MeshRaycastConfig _meshRaycastConfig = new MeshRaycastConfig()
        { canHitCameraCulledFaces = true, flipNegativeScaleTriangles = true };

        public GameObject terrainMeshObject { get; set; }

        public override bool projectHierarchies(List<GameObject> parents, ObjectProjectionSettings settings, List<ObjectProjectionResult> results)
        {
            if (results != null) results.Clear();
            if (settings.projectAsUnit)
            {
                PluginMesh terrainMesh = PluginMeshDb.instance.getPluginMesh(terrainMeshObject.getMesh());
                if (terrainMesh == null) return false;

                OBB terrainOBB = ObjectBounds.calcMeshWorldOBB(terrainMeshObject);
                if (!terrainOBB.isValid) return false;

                OBB unitWorldOBB = ObjectBounds.calcHierarchiesWorldOBB(parents, _projectableBoundsQConfig);
                if (!unitWorldOBB.isValid) return false;

                Vector3 terrainUp           = calcTerrainUp();
                Plane terrainTopPlane       = new Plane(terrainUp, terrainOBB.center + terrainUp * terrainOBB.extents.y);
                Plane terrainBottomPlane    = new Plane(-terrainUp, terrainOBB.center - terrainUp * terrainOBB.extents.y);

                Ray ray                     = new Ray();
                ray.origin                  = terrainTopPlane.projectPoint(unitWorldOBB.center) + terrainUp * 0.1f;
                ray.direction               = -terrainUp;
        
                MeshRayHit meshRayHit;
                if (!terrainMesh.raycastClosest(ray, terrainMeshObject.transform, _meshRaycastConfig, out meshRayHit)) return false;

                if (settings.alignAxis) unitWorldOBB = alignHierarchyUnit(parents, unitWorldOBB, meshRayHit.hitNormal, settings);

                Vector3 projectionPlaneNormal   = settings.alignAxis ? meshRayHit.hitNormal : calcTerrainUp();
                Plane projectionPlane           = new Plane(projectionPlaneNormal, meshRayHit.hitPoint);
                ObjectProjectionPivots.calcHierarchyUnitPivots(parents, unitWorldOBB, projectionPlane, settings, _projectionPivotBuffer);
                if (_projectionPivotBuffer.Count == 0) return false;

                if (settings.embedInSurface)
                {
                    // Note: Offset pivots far above (or below) the terrain in order to simplify raycasting in 'calcEmbedOffset'.
                    Sphere groupSphere      = new Sphere(unitWorldOBB);
                    Vector3 projectedCenter = settings.halfSpace == ObjectProjectionHalfSpace.InFront ? terrainTopPlane.projectPoint(groupSphere.center) : terrainBottomPlane.projectPoint(groupSphere.center);
                    Vector3 moveOffset      = (settings.halfSpace == ObjectProjectionHalfSpace.InFront ? terrainUp : -terrainUp) * ((projectedCenter - groupSphere.center).magnitude + groupSphere.radius);
                    Vector3Ex.offsetPoints(_projectionPivotBuffer, moveOffset);
                    foreach (var parent in parents)
                        parent.transform.position += moveOffset;

                    Vector3 projectionOffset = calcEmbedOffset(_projectionPivotBuffer, terrainMesh, settings);
                    if (projectionOffset.sqrMagnitude > 1e-5f)
                    {
                        projectionOffset += calcOffsetVector(projectionPlane.normal, settings);
                        foreach (var parent in parents)
                            parent.transform.position += projectionOffset;
                    }
                    else
                    {
                        // Note: If the projection offset is 0, we need to restore the objects' positions.
                        //       Otherwise they will float in the air.
                        foreach (var parent in parents)
                            parent.transform.position -= moveOffset;
                    }
                }
                else
                {
                    Vector3 projectionOffset = ObjectProjectionPivots.calcPivotProjectionOffset(projectionPlane, _projectionPivotBuffer, settings);
                    projectionOffset += calcOffsetVector(projectionPlane.normal, settings);
                    foreach (var parent in parents)
                        parent.transform.position += projectionOffset;
                }

                if (results != null)
                {
                    var result                  = new ObjectProjectionResult();
                    result.wasProjected         = true;
                    result.projectionPlane      = projectionPlane;
                    result.projectedPosition    = result.projectionPlane.projectPoint(unitWorldOBB.center);
                    result.terrainNormal        = meshRayHit.hitNormal;
                    results.Add(result);
                }

                return true;
            }
            else return projectHierarchiesIndividually(parents, settings, results);
        }

        public override ObjectProjectionResult projectHierarchy(GameObject parent, ObjectProjectionSettings settings)
        {
            PluginMesh terrainMesh = PluginMeshDb.instance.getPluginMesh(terrainMeshObject.getMesh());
            if (terrainMesh == null) return ObjectProjectionResult.notProjectedResult;

            OBB terrainOBB = ObjectBounds.calcMeshWorldOBB(terrainMeshObject);
            if (!terrainOBB.isValid) return ObjectProjectionResult.notProjectedResult;

            OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(parent, _projectableBoundsQConfig);
            if (!hierarchyWorldOBB.isValid) return ObjectProjectionResult.notProjectedResult;
   
            Vector3 terrainUp           = calcTerrainUp();
            Plane terrainTopPlane       = new Plane(terrainUp, terrainOBB.center + terrainUp * terrainOBB.extents.y);
            Plane terrainBottomPlane    = new Plane(-terrainUp, terrainOBB.center - terrainUp * terrainOBB.extents.y);

            Ray ray                     = new Ray();
            ray.origin                  = terrainTopPlane.projectPoint(parent.transform.position) + terrainUp * 0.1f;
            ray.direction               = -terrainUp;

            MeshRayHit meshRayHit;
            if (!terrainMesh.raycastClosest(ray, terrainMeshObject.transform, _meshRaycastConfig, out meshRayHit))
                return ObjectProjectionResult.notProjectedResult;
         
            if (settings.alignAxis) hierarchyWorldOBB = alignHierarchy(parent, hierarchyWorldOBB, meshRayHit.hitNormal, settings);

            Vector3 projectionPlaneNormal   = settings.alignAxis ? meshRayHit.hitNormal : calcTerrainUp();
            Plane projectionPlane           = new Plane(projectionPlaneNormal, meshRayHit.hitPoint);
            ObjectProjectionPivots.calcHierarchyPivots(parent, hierarchyWorldOBB, projectionPlane, settings, _projectionPivotBuffer);
            if (_projectionPivotBuffer.Count == 0) return ObjectProjectionResult.notProjectedResult;

            if (settings.embedInSurface)
            {
                // Note: Offset pivots far above (or below) the terrain in order to simplify raycasting in 'calcEmbedOffset'.
                Sphere hierarchySphere      = new Sphere(hierarchyWorldOBB);
                Vector3 projectedCenter     = settings.halfSpace == ObjectProjectionHalfSpace.InFront ? terrainTopPlane.projectPoint(hierarchySphere.center) : terrainBottomPlane.projectPoint(hierarchySphere.center);
                Vector3 moveOffset = (settings.halfSpace == ObjectProjectionHalfSpace.InFront ? terrainUp : -terrainUp) * ((projectedCenter - hierarchySphere.center).magnitude + hierarchySphere.radius);
                Vector3Ex.offsetPoints(_projectionPivotBuffer, moveOffset);
                parent.transform.position   += moveOffset;

                Vector3 projectionOffset = calcEmbedOffset(_projectionPivotBuffer, terrainMesh, settings);
                if (projectionOffset.sqrMagnitude > 1e-5f)
                {
                    projectionOffset += calcOffsetVector(projectionPlane.normal, settings);
                    parent.transform.position += projectionOffset;
                }
                else
                {
                    // Note: If the projection offset is 0, we need to restore the object position.
                    //       Otherwise it will float in the air.
                    parent.transform.position -= moveOffset;
                }
            }
            else
            {
                Vector3 projectionOffset    = ObjectProjectionPivots.calcPivotProjectionOffset(projectionPlane, _projectionPivotBuffer, settings);
                projectionOffset            += calcOffsetVector(projectionPlane.normal, settings);
                parent.transform.position   += projectionOffset;
            }

            var result                      = new ObjectProjectionResult();
            result.wasProjected             = true;
            result.projectionPlane          = projectionPlane;
            result.projectedPosition        = result.projectionPlane.projectPoint(hierarchyWorldOBB.center);
            result.terrainNormal            = meshRayHit.hitNormal;

            return result;
        }

        private Vector3 calcTerrainUp()
        {
            return ObjectPrefs.instance.getTerrainMeshUp(terrainMeshObject);
        }

        private Vector3 calcEmbedOffset(List<Vector3> projectionPivots, PluginMesh terrainMesh, ObjectProjectionSettings settings)
        {
            Ray ray = new Ray();
            ray.direction           = settings.halfSpace == ObjectProjectionHalfSpace.InFront ? -calcTerrainUp() : calcTerrainUp();
      
            MeshRayHit meshRayHit;
            int bestPtIndex         = -1;
            Vector3 bestHitPoint    = Vector3.zero;
            float bestDist          = float.MinValue;

            int numPoints = _projectionPivotBuffer.Count;
            for (int ptIndex = 0; ptIndex < numPoints; ++ptIndex)
            {
                ray.origin = _projectionPivotBuffer[ptIndex];
                if (!terrainMesh.raycastClosest(ray, terrainMeshObject.transform, _meshRaycastConfig, out meshRayHit))
                    continue;

                float d = (_projectionPivotBuffer[ptIndex] - meshRayHit.hitPoint).magnitude;
                if (d > bestDist)
                {
                    bestPtIndex     = ptIndex;
                    bestHitPoint    = meshRayHit.hitPoint;
                    bestDist        = d;
                }
            }

            return bestPtIndex >= 0 ? (bestHitPoint - projectionPivots[bestPtIndex]) : Vector3.zero;
        }
    }
}
#endif