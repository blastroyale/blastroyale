#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public struct ObjectProjectionResult
    {
        public bool                                     wasProjected;
        public Plane                                    projectionPlane;
        public Vector3                                  projectedPosition;
        public Vector3                                  terrainNormal;

        public static readonly ObjectProjectionResult   notProjectedResult              = new ObjectProjectionResult() { wasProjected = false };
    }

    public abstract class ObjectProjector
    {
        protected List<Vector3>                             _projectionPivotBuffer      = new List<Vector3>();
        protected static readonly ObjectBounds.QueryConfig  _projectableBoundsQConfig   = new ObjectBounds.QueryConfig()
        {
            volumelessSize      = Vector3.zero,
            objectTypes         = GameObjectType.All & (~GameObjectType.Terrain),
            includeInactive     = false,
            includeInvisible    = false
        };

        public abstract bool projectHierarchies(List<GameObject> parents, ObjectProjectionSettings settings, List<ObjectProjectionResult> results);
        public abstract ObjectProjectionResult projectHierarchy(GameObject parent, ObjectProjectionSettings settings);

        public static ObjectBounds.QueryConfig projectableBoundsQConfig { get { return _projectableBoundsQConfig; } }

        protected Vector3 calcOffsetVector(Vector3 surfaceNormal, ObjectProjectionSettings settings)
        {
            return surfaceNormal * (settings.halfSpace == ObjectProjectionHalfSpace.InFront ? settings.inFrontOffset : -settings.behindOffset);
        }

        protected OBB alignHierarchy(GameObject parent, OBB hierarchyWorldOBB, Vector3 normal, ObjectProjectionSettings settings)
        {
            Transform objectTransform   = parent.transform;
            Vector3 alignmentAxis       = objectTransform.flexiToLocalAxis(hierarchyWorldOBB, settings.alignmentAxis, settings.invertAlignmentAxis);
            objectTransform.alignAxis(alignmentAxis, normal, hierarchyWorldOBB.center);
            hierarchyWorldOBB.rotation  = objectTransform.rotation;

            return hierarchyWorldOBB;
        }

        protected OBB alignHierarchyUnit(List<GameObject> parents, OBB unitWorldOBB, Vector3 normal, ObjectProjectionSettings settings)
        {
            foreach (var parent in parents)
            {
                OBB hierarchyWorldOBB       = ObjectBounds.calcHierarchyWorldOBB(parent, _projectableBoundsQConfig);
                if (!hierarchyWorldOBB.isValid) continue;

                Transform objectTransform   = parent.transform;
                Vector3 alignmentAxis       = objectTransform.flexiToLocalAxis(hierarchyWorldOBB, settings.alignmentAxis, settings.invertAlignmentAxis);
                objectTransform.alignAxis(alignmentAxis, normal, unitWorldOBB.center);
            }

            return ObjectBounds.calcHierarchiesWorldOBB(parents, _projectableBoundsQConfig);
        }

        protected bool projectHierarchiesIndividually(List<GameObject> parents, ObjectProjectionSettings settings, List<ObjectProjectionResult> results)
        {
            bool anythingProjected = false;
            if (results != null)
            {
                foreach (var parent in parents)
                {
                    var result = projectHierarchy(parent, settings);
                    results.Add(result);
                    anythingProjected |= result.wasProjected;
                }
            }
            else
            {
                foreach (var parent in parents)
                {
                    anythingProjected |= projectHierarchy(parent, settings).wasProjected;
                }
            }

            return anythingProjected;
        }
    }
}
#endif