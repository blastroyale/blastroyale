#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class ObjectPositionCalculator
    {
        private static ObjectBounds.QueryConfig     _boundsQConfig      = new ObjectBounds.QueryConfig();

        static ObjectPositionCalculator()
        {
            _boundsQConfig.volumelessSize       = Vector3.zero;
            _boundsQConfig.objectTypes          = GameObjectType.All;
            _boundsQConfig.includeInactive      = false;
            _boundsQConfig.includeInvisible     = false;
        }

        public static Vector3 calcRootPosition(GameObject root, Vector3 desiredOOBBCenter, Vector3 desiredWorldScale, Quaternion desiredWorldRotation)
        {
            OBB obb                             = ObjectBounds.calcHierarchyWorldOBB(root, _boundsQConfig);
            Transform rootTransform             = root.transform;
            Matrix4x4 rootTransformMatrix       = Matrix4x4.TRS(Vector3.zero, rootTransform.rotation, rootTransform.lossyScale);
            Matrix4x4 desiredTransformMatrix    = Matrix4x4.TRS(Vector3.zero, desiredWorldRotation, desiredWorldScale);
            Matrix4x4 inverseTransformMatrix    = desiredTransformMatrix * rootTransformMatrix.inverse;

            Vector3 relationshipVector          = rootTransform.position - obb.center;
            relationshipVector                  = inverseTransformMatrix.MultiplyVector(relationshipVector);

            return desiredOOBBCenter + relationshipVector;
        }

        public static Vector3 calcRootPosition(GameObject root, OBB hierarchyOBB, Vector3 desiredOOBBCenter, Vector3 desiredWorldScale, Quaternion desiredWorldRotation)
        {
            Transform rootTransform             = root.transform;

            Matrix4x4 rootTransformMatrix       = Matrix4x4.TRS(Vector3.zero, rootTransform.rotation, rootTransform.lossyScale);
            Matrix4x4 desiredTransformMatrix    = Matrix4x4.TRS(Vector3.zero, desiredWorldRotation, desiredWorldScale);
            Matrix4x4 inverseTransformMatrix    = desiredTransformMatrix * rootTransformMatrix.inverse;

            Vector3 relationshipVector          = rootTransform.position - hierarchyOBB.center;
            relationshipVector                  = inverseTransformMatrix.MultiplyVector(relationshipVector);

            return desiredOOBBCenter + relationshipVector;
        }
    }
}
#endif