#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectSurfaceSnapTargetParent
    {
        private GameObject                      _gameObject;
        private Vector3                         _surfaceAnchorDirection;
        private Transform                       _transform;
        private Vector3                         _localScaleSnapshot;
        private ObjectProjectionResult          _projectionResult               = ObjectProjectionResult.notProjectedResult;

        public GameObject                       gameObject                      { get { return _gameObject; } set { _gameObject = value; _transform = _gameObject.transform; } }
        public Transform                        transform                       { get { return _transform; } }
        public Vector3                          surfaceAnchorDirection          { get { return _surfaceAnchorDirection; } set { _surfaceAnchorDirection = value; } }
        public Vector3                          surfaceAnchorDirectionSnapshot  { get; set; }
        public ObjectSurfaceSnapTargetParent    unitAnchorObject                { get; set; }
        public Vector3                          unitAnchorDirection             { get; set; }
        public ObjectProjectionResult           projectionResult                { get { return _projectionResult; } set { _projectionResult = value; } }
        public Vector3                          localScaleSnapshot              { get { return _localScaleSnapshot; } set { _localScaleSnapshot = value; } }

        public static ObjectSurfaceSnapTargetParent create(GameObject targetParent)
        {
            return new ObjectSurfaceSnapTargetParent()
            { gameObject = targetParent };
        }

        public static void create(List<GameObject> parents, List<ObjectSurfaceSnapTargetParent> surfaceSnapTargetParents)
        {
            surfaceSnapTargetParents.Clear();
            foreach (var parent in parents)
                surfaceSnapTargetParents.Add(create(parent));
        }

        public static ObjectSurfaceSnapTargetParent establishAsUnit(List<ObjectSurfaceSnapTargetParent> surfaceSnapTargetParents)
        {
            var anchorObject                = surfaceSnapTargetParents[0];
            Vector3 anchorPosition          = anchorObject.transform.position;
            for (int index = 1; index < surfaceSnapTargetParents.Count; ++index)
            {
                var parent                  = surfaceSnapTargetParents[index];
                parent.unitAnchorObject     = anchorObject;
                parent.unitAnchorDirection  = parent.transform.position - anchorPosition;
            }

            return anchorObject;
        }

        public static void updateUnitAnchorDirections(List<ObjectSurfaceSnapTargetParent> surfaceSnapTargetParents)
        {
            var anchorObject                = surfaceSnapTargetParents[0];
            Vector3 anchorPosition = anchorObject.transform.position;
            for (int index = 1; index < surfaceSnapTargetParents.Count; ++index)
            {
                var parent                  = surfaceSnapTargetParents[index];
                parent.unitAnchorDirection  = parent.transform.position - anchorPosition;
            }
        }

        public static void updateSurfaceAnchorDirections(List<ObjectSurfaceSnapTargetParent> surfaceSnapTargetParents, Vector3 anchorPoint)
        {
            foreach (var parent in surfaceSnapTargetParents)
                parent.surfaceAnchorDirection = (parent.transform.position - anchorPoint);
        }

        public static void storeSurfaceAnchorDirectionSnapshots(List<ObjectSurfaceSnapTargetParent> surfaceSnapTargetParents)
        {
            foreach (var parent in surfaceSnapTargetParents)
                parent.surfaceAnchorDirectionSnapshot = parent.surfaceAnchorDirection;
        }

        public static void storeLocalScaleSnapshots(List<ObjectSurfaceSnapTargetParent> surfaceSnapTargetParents)
        {
            foreach (var parent in surfaceSnapTargetParents)
                parent.localScaleSnapshot = parent.transform.localScale;
        }
    }
}
#endif