#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectOnSphereProjector : ObjectProjector
    {
        private MeshRaycastConfig _meshRaycastConfig = new MeshRaycastConfig()
        { canHitCameraCulledFaces = true, flipNegativeScaleTriangles = true };

        public Sphere       sphere          { get; set; }
        public GameObject   sphereObject    { get; set; }

        public override bool projectHierarchies(List<GameObject> parents, ObjectProjectionSettings settings, List<ObjectProjectionResult> results)
        {
            if (results != null) results.Clear();
            if (settings.projectAsUnit)
            {
                OBB unitWorldOBB = ObjectBounds.calcHierarchiesWorldOBB(parents, _projectableBoundsQConfig);
                if (!unitWorldOBB.isValid) return false;

                Vector3 prjPlaneNormal  = (unitWorldOBB.center - sphere.center).normalized;
                Vector3 ptOnProjectionPlane = sphere.center + prjPlaneNormal * sphere.radius;
                Plane projectionPlane   = new Plane(prjPlaneNormal, ptOnProjectionPlane);
                if (settings.alignAxis) unitWorldOBB = alignHierarchyUnit(parents, unitWorldOBB, projectionPlane.normal, settings);

                ObjectProjectionPivots.calcHierarchyUnitPivots(parents, unitWorldOBB, projectionPlane, settings, _projectionPivotBuffer);
                if (_projectionPivotBuffer.Count == 0) return false;

                Vector3 projectionOffset = ObjectProjectionPivots.calcPivotProjectionOffset(projectionPlane, _projectionPivotBuffer, settings);
                if (settings.embedInSurface)
                {
                    Vector3Ex.offsetPoints(_projectionPivotBuffer, projectionOffset);
                    projectionOffset += calcEmbedOffset(_projectionPivotBuffer, projectionPlane, settings);
                }

                projectionOffset += calcOffsetVector(projectionPlane.normal, settings);
                foreach (var parent in parents)
                    parent.transform.position += projectionOffset;

                if (results != null)
                {
                    var result                  = new ObjectProjectionResult();
                    result.wasProjected         = true;
                    result.projectionPlane      = new Plane(prjPlaneNormal, ptOnProjectionPlane + projectionOffset);
                    result.projectedPosition    = result.projectionPlane.projectPoint(unitWorldOBB.center);
                    results.Add(result);
                }

                return true;
            }
            else return projectHierarchiesIndividually(parents, settings, results);
        }

        public override ObjectProjectionResult projectHierarchy(GameObject parent, ObjectProjectionSettings settings)
        {
            OBB hierarchyWorldOBB   = ObjectBounds.calcHierarchyWorldOBB(parent, _projectableBoundsQConfig);
            if (!hierarchyWorldOBB.isValid) return ObjectProjectionResult.notProjectedResult;

            Vector3 prjPlaneNormal  = (hierarchyWorldOBB.center - sphere.center).normalized;
            Vector3 ptOnProjectionPlane = sphere.center + prjPlaneNormal * sphere.radius;
            Plane projectionPlane   = new Plane(prjPlaneNormal, ptOnProjectionPlane);
            if (settings.alignAxis) hierarchyWorldOBB = alignHierarchy(parent, hierarchyWorldOBB, projectionPlane.normal, settings);

            ObjectProjectionPivots.calcHierarchyPivots(parent, hierarchyWorldOBB, projectionPlane, settings, _projectionPivotBuffer);
            if (_projectionPivotBuffer.Count == 0) return ObjectProjectionResult.notProjectedResult;

            Vector3 projectionOffset = ObjectProjectionPivots.calcPivotProjectionOffset(projectionPlane, _projectionPivotBuffer, settings);
            if (settings.embedInSurface)
            {
                Vector3Ex.offsetPoints(_projectionPivotBuffer, projectionOffset);
                projectionOffset += calcEmbedOffset(_projectionPivotBuffer, projectionPlane, settings);
            }

            projectionOffset            += calcOffsetVector(projectionPlane.normal, settings);
            parent.transform.position   += projectionOffset;

            var result                  = new ObjectProjectionResult();
            result.wasProjected         = true;
            result.projectionPlane      = new Plane(prjPlaneNormal, ptOnProjectionPlane + projectionOffset);
            result.projectedPosition    = result.projectionPlane.projectPoint(hierarchyWorldOBB.center);

            return result;
        }

        private Vector3 calcEmbedOffset(List<Vector3> projectionPivots, Plane initialProjectionPlane, ObjectProjectionSettings settings)
        {
            PluginMesh sphereMesh = PluginMeshDb.instance.getPluginMesh(sphereObject.getMesh());
            if (sphereMesh == null) return Vector3.zero;

            MeshRayHit meshRayHit;
            Ray ray                 = new Ray();
            ray.origin              = sphere.center;
            int bestPtIndex         = -1;
            Vector3 bestHitPoint    = Vector3.zero;

            int numPoints = projectionPivots.Count;
            if (settings.halfSpace == ObjectProjectionHalfSpace.InFront)
            {
                float bestDist      = float.MinValue;
                ray.direction       = -initialProjectionPlane.normal;
                for (int ptIndex = 0; ptIndex < numPoints; ++ptIndex)
                {
                    ray.origin      = projectionPivots[ptIndex];
                    if ((ray.origin - sphere.center).magnitude <= sphere.radius) continue;

                    if (!sphereMesh.raycastClosest(ray, sphereObject.transform, _meshRaycastConfig, out meshRayHit))
                        continue;

                    float d = (ray.origin - meshRayHit.hitPoint).magnitude;
                    if (d > bestDist)
                    {
                        bestDist        = d;
                        bestPtIndex     = ptIndex;
                        bestHitPoint    = meshRayHit.hitPoint;
                    }
                }
            }
            else
            {
                float bestDist  = float.MaxValue;
                ray.direction   = initialProjectionPlane.normal;
                for (int ptIndex = 0; ptIndex < numPoints; ++ptIndex)
                {
                    ray.origin  = projectionPivots[ptIndex];
                    if ((ray.origin - sphere.center).magnitude >= sphere.radius) continue;

                    if (!sphereMesh.raycastClosest(ray, sphereObject.transform, _meshRaycastConfig, out meshRayHit))
                        continue;

                    float d = (ray.origin - meshRayHit.hitPoint).magnitude;
                    if (d < bestDist)
                    {
                        bestDist        = d;
                        bestPtIndex     = ptIndex;
                        bestHitPoint    = meshRayHit.hitPoint;
                    }
                }
            }

            return bestPtIndex >= 0 ? (bestHitPoint - projectionPivots[bestPtIndex]) : Vector3.zero;
        }
    }
}
#endif