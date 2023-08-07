#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class ObjectProjectionPivots
    {
        public static void calcHierarchyPivots(GameObject parent, OBB hierarchyWorldOBB, Plane projectionPlane, ObjectProjectionSettings projectionSettings, List<Vector3> pivots)
        {
            if (!parent.hierarchyHasMesh(false, false)) calcBoxPivots(hierarchyWorldOBB, projectionPlane, projectionSettings, pivots);
            else
            {
                calcHierarchyVertexPivots(parent, hierarchyWorldOBB, projectionPlane, projectionSettings, pivots);
                if (pivots.Count == 0) calcBoxPivots(hierarchyWorldOBB, projectionPlane, projectionSettings, pivots);
            }
        }

        public static void calcHierarchyUnitPivots(List<GameObject> parents, OBB unitWorldOBB, Plane projectionPlane, ObjectProjectionSettings projectionSettings, List<Vector3> pivots)
        {
            if (!GameObjectEx.hierarchiesHaveMeshes(parents, false, false)) calcBoxPivots(unitWorldOBB, projectionPlane, projectionSettings, pivots);
            else
            {
                calcHierarchiesVertexPivots(parents, unitWorldOBB, projectionPlane, projectionSettings, pivots);
                if (pivots.Count == 0) calcBoxPivots(unitWorldOBB, projectionPlane, projectionSettings, pivots);
            }
        }

        public static void calcHierarchyVertexPivots(GameObject parent, OBB hierarchyWorldOBB, Plane projectionPlane, ObjectProjectionSettings projectionSettings, List<Vector3> pivots)
        {
            Vector3 normal      = projectionPlane.normal;
            if (projectionSettings.halfSpace == ObjectProjectionHalfSpace.InFront) normal = -normal;

            Box3DFace boxFace   = Box3D.findMostAlignedFace(hierarchyWorldOBB.center, hierarchyWorldOBB.size, hierarchyWorldOBB.rotation, normal);
            ObjectVertexOverlap.overlapHierarchyWorldVerts(parent, hierarchyWorldOBB, boxFace, ObjectVertexOverlap.safeMinOverlapSize, pivots);
        }

        public static void calcHierarchiesVertexPivots(List<GameObject> parents, OBB unitWorldOBB, Plane projectionPlane, ObjectProjectionSettings projectionSettings, List<Vector3> pivots)
        {
            Vector3 normal      = projectionPlane.normal;
            if (projectionSettings.halfSpace == ObjectProjectionHalfSpace.InFront) normal = -normal;

            Box3DFace boxFace   = Box3D.findMostAlignedFace(unitWorldOBB.center, unitWorldOBB.size, unitWorldOBB.rotation, normal);
            ObjectVertexOverlap.overlapHierarchiesWorldVerts(parents, unitWorldOBB, boxFace, ObjectVertexOverlap.safeMinOverlapSize, pivots);
        }

        public static void calcBoxPivots(OBB box, Plane projectionPlane, ObjectProjectionSettings projectionSettings, List<Vector3> pivots)
        {
            Vector3 normal      = projectionPlane.normal;
            if (projectionSettings.halfSpace == ObjectProjectionHalfSpace.InFront) normal = -normal;
  
            Box3DFace boxFace   = Box3D.findMostAlignedFace(box.center, box.size, box.rotation, normal);
            Box3D.calcFaceCorners(box.center, box.size, box.rotation, boxFace, pivots);
        }

        public static Vector3 calcPivotProjectionOffset(Plane projectionPlane, List<Vector3> projectionPivots, ObjectProjectionSettings projectionSettings)
        {
            PlaneClassifyResult ptLocation = projectionPlane.classifyPoints(projectionPivots);
            if (ptLocation == PlaneClassifyResult.OnPlane) return Vector3.zero;

            int pivotIndex = -1;
            if (projectionSettings.halfSpace == ObjectProjectionHalfSpace.InFront)
            {
                if (projectionSettings.embedInSurface)
                {
                    if (ptLocation == PlaneClassifyResult.InFront || ptLocation == PlaneClassifyResult.Spanning) pivotIndex = projectionPlane.findIndexOfFurthestPointInFront(projectionPivots);
                    else if (ptLocation == PlaneClassifyResult.Behind) pivotIndex = projectionPlane.findIndexOfClosestPointBehind(projectionPivots);
                }
                else
                {
                    if (ptLocation == PlaneClassifyResult.InFront) pivotIndex = projectionPlane.findIndexOfClosestPointInFront(projectionPivots);
                    else if (ptLocation == PlaneClassifyResult.Behind || ptLocation == PlaneClassifyResult.Spanning) pivotIndex = projectionPlane.findIndexOfFurthestPointBehind(projectionPivots);
                }
            }
            else
            {
                if (projectionSettings.embedInSurface)
                {
                    if (ptLocation == PlaneClassifyResult.InFront) pivotIndex = projectionPlane.findIndexOfClosestPointInFront(projectionPivots);
                    else if (ptLocation == PlaneClassifyResult.Behind || ptLocation == PlaneClassifyResult.Spanning) pivotIndex = projectionPlane.findIndexOfFurthestPointBehind(projectionPivots);
                }
                else
                {
                    if (ptLocation == PlaneClassifyResult.InFront || ptLocation == PlaneClassifyResult.Spanning) pivotIndex = projectionPlane.findIndexOfFurthestPointInFront(projectionPivots);
                    else if (ptLocation == PlaneClassifyResult.Behind) pivotIndex = projectionPlane.findIndexOfClosestPointBehind(projectionPivots);
                }
            }

            if (pivotIndex >= 0)
            {
                Vector3 pivot           = projectionPivots[pivotIndex];
                Vector3 projectedPivot  = projectionPlane.projectPoint(pivot);
                return projectedPivot - pivot;
            }
            else return Vector3.zero;
        }
    }
}
#endif