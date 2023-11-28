#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectOnUnityTerrainProjector : ObjectProjector
    {
        public Terrain unityTerrain { get; set; }

        public override bool projectHierarchies(List<GameObject> parents, ObjectProjectionSettings settings, List<ObjectProjectionResult> results)
        {
            if (results != null) results.Clear();
            if (settings.projectAsUnit)
            {
                OBB terrainOBB = unityTerrain.calcWorldOBB();
                if (!terrainOBB.isValid) return false;

                OBB unitWorldOBB = ObjectBounds.calcHierarchiesWorldOBB(parents, _projectableBoundsQConfig);
                if (!unitWorldOBB.isValid) return false;

                Vector3 terrainUp               = calcTerrainUp();
                Plane terrainTopPlane           = new Plane(terrainUp, terrainOBB.center + terrainUp * terrainOBB.extents.y);
                Plane terrainBottomPlane        = new Plane(-terrainUp, terrainOBB.center - terrainUp * terrainOBB.extents.y);

                Vector3 projectedCenter         = unityTerrain.projectPoint(unityTerrain.transform.position.y, unitWorldOBB.center);
                Vector3 terrainNormal           = unityTerrain.getInterpolatedNormal(projectedCenter);
                Vector3 projectedCenterNormal   = settings.alignAxis ? terrainNormal : calcTerrainUp();

                if (settings.alignAxis) unitWorldOBB = alignHierarchyUnit(parents, unitWorldOBB, projectedCenterNormal, settings);

                Plane projectionPlane           = new Plane(projectedCenterNormal, projectedCenter);
                ObjectProjectionPivots.calcHierarchyUnitPivots(parents, unitWorldOBB, projectionPlane, settings, _projectionPivotBuffer);
                if (_projectionPivotBuffer.Count == 0) return false;
            
                if (settings.embedInSurface)
                {
                    // Note: We need to offset completely above or below the terrain (depending on half space).
                    //       Otherwise, the results will not be correct when the objects are spanning the terrain
                    //       surface.
                    Sphere groupSphere      = new Sphere(unitWorldOBB);
                    projectedCenter         = settings.halfSpace == ObjectProjectionHalfSpace.InFront ? terrainTopPlane.projectPoint(groupSphere.center) : terrainBottomPlane.projectPoint(groupSphere.center);
                    Vector3 moveOffset      = (settings.halfSpace == ObjectProjectionHalfSpace.InFront ? terrainUp : -terrainUp) * ((projectedCenter - groupSphere.center).magnitude + groupSphere.radius);
                    Vector3Ex.offsetPoints(_projectionPivotBuffer, moveOffset);
                    foreach (var parent in parents)
                        parent.transform.position += moveOffset;

                    Vector3 projectionOffset = calcEmbedOffset(_projectionPivotBuffer);  
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
                    Vector3 projectionOffset    = ObjectProjectionPivots.calcPivotProjectionOffset(projectionPlane, _projectionPivotBuffer, settings);
                    projectionOffset            += calcOffsetVector(projectionPlane.normal, settings);
                    foreach (var parent in parents)
                        parent.transform.position += projectionOffset;
                }

                if (results != null)
                {
                    var result                  = new ObjectProjectionResult();
                    result.wasProjected         = true;
                    result.projectionPlane      = projectionPlane;
                    result.projectedPosition    = result.projectionPlane.projectPoint(unitWorldOBB.center);
                    result.terrainNormal        = terrainNormal;
                    results.Add(result);
                }

                return true;
            }
            else return projectHierarchiesIndividually(parents, settings, results);
        }

        public override ObjectProjectionResult projectHierarchy(GameObject parent, ObjectProjectionSettings settings)
        {
            OBB terrainOBB = unityTerrain.calcWorldOBB();
            if (!terrainOBB.isValid) return ObjectProjectionResult.notProjectedResult;

            OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(parent, _projectableBoundsQConfig);
            if (!hierarchyWorldOBB.isValid) return ObjectProjectionResult.notProjectedResult;

            Vector3 terrainUp               = calcTerrainUp();
            Plane terrainTopPlane           = new Plane(terrainUp, terrainOBB.center + terrainUp * terrainOBB.extents.y);
            Plane terrainBottomPlane        = new Plane(-terrainUp, terrainOBB.center - terrainUp * terrainOBB.extents.y);

            // Note: It is important to project parent.transform.position and not hierarchyWorldOBB.center.
            //       It produces much better results.
            Vector3 projectedCenter         = unityTerrain.projectPoint(unityTerrain.transform.position.y, parent.transform.position);
            Vector3 terrainNormal           = unityTerrain.getInterpolatedNormal(projectedCenter);
            Vector3 projectedCenterNormal   = settings.alignAxis ? terrainNormal : calcTerrainUp();

            if (settings.alignAxis) hierarchyWorldOBB = alignHierarchy(parent, hierarchyWorldOBB, projectedCenterNormal, settings);

            Plane projectionPlane = new Plane(projectedCenterNormal, projectedCenter);
            ObjectProjectionPivots.calcHierarchyPivots(parent, hierarchyWorldOBB, projectionPlane, settings, _projectionPivotBuffer);
            if (_projectionPivotBuffer.Count == 0) return ObjectProjectionResult.notProjectedResult;

            if (settings.embedInSurface)
            {
                Sphere hierarchySphere      = new Sphere(hierarchyWorldOBB);
                projectedCenter             = settings.halfSpace == ObjectProjectionHalfSpace.InFront ? terrainTopPlane.projectPoint(hierarchySphere.center) : terrainBottomPlane.projectPoint(hierarchySphere.center);
                Vector3 moveOffset          = (settings.halfSpace == ObjectProjectionHalfSpace.InFront ? terrainUp : -terrainUp) * ((projectedCenter - hierarchySphere.center).magnitude + hierarchySphere.radius);
                Vector3Ex.offsetPoints(_projectionPivotBuffer, moveOffset);
                parent.transform.position   += moveOffset;

                Vector3 projectionOffset = calcEmbedOffset(_projectionPivotBuffer);
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
            result.terrainNormal            = terrainNormal;

            return result;
        }

        private Vector3 calcTerrainUp()
        {
            return unityTerrain.transform.up;
        }

        private Vector3 calcEmbedOffset(List<Vector3> projectionPivots)
        {
            int bestPtIndex             = -1;
            Vector3 bestHitPoint        = Vector3.zero;
            float bestDist              = float.MinValue;
            float terrainYPos           = unityTerrain.transform.position.y;

            int numPoints = _projectionPivotBuffer.Count;
            for (int ptIndex = 0; ptIndex < numPoints; ++ptIndex)
            {
                Vector3 projectedPt     = unityTerrain.projectPoint(terrainYPos, _projectionPivotBuffer[ptIndex]);
                float d                 = (_projectionPivotBuffer[ptIndex] - projectedPt).magnitude;
                if (d > bestDist)
                {
                    bestPtIndex = ptIndex;
                    bestHitPoint = projectedPt;
                    bestDist = d;
                }
            }

            return bestPtIndex >= 0 ? (bestHitPoint - projectionPivots[bestPtIndex]) : Vector3.zero;
        }
    }
}
#endif