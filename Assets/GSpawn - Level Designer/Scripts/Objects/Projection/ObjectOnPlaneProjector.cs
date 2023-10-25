#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectOnPlaneProjector : ObjectProjector
    {
        public Plane projectionPlane { get; set; }

        public override bool projectHierarchies(List<GameObject> parents, ObjectProjectionSettings settings, List<ObjectProjectionResult> results)
        {
            if (results != null) results.Clear();
            if (settings.projectAsUnit)
            {
                OBB unitWorldOBB = ObjectBounds.calcHierarchiesWorldOBB(parents, _projectableBoundsQConfig);
                if (!unitWorldOBB.isValid) return false;
                if (settings.alignAxis) unitWorldOBB = alignHierarchyUnit(parents, unitWorldOBB, projectionPlane.normal, settings);

                ObjectProjectionPivots.calcHierarchyUnitPivots(parents, unitWorldOBB, projectionPlane, settings, _projectionPivotBuffer);
                if (_projectionPivotBuffer.Count == 0) return false;

                Vector3 projectionOffset = ObjectProjectionPivots.calcPivotProjectionOffset(projectionPlane, _projectionPivotBuffer, settings);
                projectionOffset += calcOffsetVector(projectionPlane.normal, settings);

                foreach (var parent in parents)
                    parent.transform.position += projectionOffset;

                if (results != null)
                {
                    var result                  = new ObjectProjectionResult();
                    result.wasProjected         = true;
                    result.projectionPlane      = projectionPlane;
                    result.projectedPosition    = result.projectionPlane.projectPoint(unitWorldOBB.center);
                    results.Add(result);
                }

                return true;
            }
            else return projectHierarchiesIndividually(parents, settings, results);
        }

        public override ObjectProjectionResult projectHierarchy(GameObject parent, ObjectProjectionSettings settings)
        {
            OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(parent, _projectableBoundsQConfig);
            if (!hierarchyWorldOBB.isValid) return ObjectProjectionResult.notProjectedResult;
            if (settings.alignAxis) hierarchyWorldOBB = alignHierarchy(parent, hierarchyWorldOBB, projectionPlane.normal, settings);

            ObjectProjectionPivots.calcHierarchyPivots(parent, hierarchyWorldOBB, projectionPlane, settings, _projectionPivotBuffer);
            if (_projectionPivotBuffer.Count == 0) return ObjectProjectionResult.notProjectedResult;

            Vector3 projectionOffset    = ObjectProjectionPivots.calcPivotProjectionOffset(projectionPlane, _projectionPivotBuffer, settings);
            projectionOffset            += calcOffsetVector(projectionPlane.normal, settings);
            parent.transform.position   += projectionOffset;

            var result                  = new ObjectProjectionResult();
            result.wasProjected         = true;
            result.projectionPlane      = projectionPlane;
            result.projectedPosition    = result.projectionPlane.projectPoint(parent.transform.position);

            return result;
        }
    }
}
#endif
