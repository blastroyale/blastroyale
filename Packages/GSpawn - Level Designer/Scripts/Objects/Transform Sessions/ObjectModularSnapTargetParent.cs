#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectModularSnapTargetParent
    {
        private GameObject  _gameObject;
        private Vector3     _surfaceAnchorDirection;
        private Transform   _transform;
        private Vector3     _localScaleSnapshot;

        public GameObject   gameObject              { get { return _gameObject; } set { _gameObject = value; _transform = _gameObject.transform; } }
        public Vector3      surfaceAnchorDirection  { get { return _surfaceAnchorDirection; } set { _surfaceAnchorDirection = value; } }
        public Transform    transform               { get { return _transform; } }
        public int          verticalStep            { get; set; }
        public int          originalVerticalStep    { get; set; }
        public Vector3      localScaleSnapshot      { get { return _localScaleSnapshot; } set { _localScaleSnapshot = value; } }

        public void updateVerticalStep(Plane basePlane, float verticalStepSize)
        {
            verticalStep = calcVerticalStep(transform.position, basePlane, verticalStepSize);
        }

        public static int calcVerticalStep(Vector3 position, Plane basePlane, float verticalStepSize)
        {
            Vector3 projectedPosition   = basePlane.projectPoint(position);
            Vector3 toPosition          = position - projectedPosition;
            float sign                  = Mathf.Sign(Vector3.Dot(toPosition, basePlane.normal));
            return (int)Mathf.Round(toPosition.magnitude / verticalStepSize) * (int)sign;
        }

        public static ObjectModularSnapTargetParent create(GameObject targetParent)
        {
            return new ObjectModularSnapTargetParent()
            { gameObject = targetParent };
        }

        public static void create(List<GameObject> parents, List<ObjectModularSnapTargetParent> modularSnapTargetParents)
        {
            modularSnapTargetParents.Clear();
            foreach (var parent in parents)
                modularSnapTargetParents.Add(create(parent));
        }

        public static void updateSurfaceAnchorDirections(List<ObjectModularSnapTargetParent> modularSnapTargetParents, Vector3 anchorPoint)
        {
            foreach (var parent in modularSnapTargetParents)
                parent.surfaceAnchorDirection = (parent.transform.position - anchorPoint);
        }

        public static void updateVerticalStep(List<ObjectModularSnapTargetParent> modularSnapTargetParents, Plane basePlane, float verticalStepSize)
        {
            foreach (var parent in modularSnapTargetParents)
                parent.updateVerticalStep(basePlane, verticalStepSize);
        }

        public static void calcOriginalVerticalStep(List<ObjectModularSnapTargetParent> modularSnapTargetParents, Plane basePlane, float verticalStepSize)
        {
            foreach (var parent in modularSnapTargetParents)
            {
                parent.updateVerticalStep(basePlane, verticalStepSize);
                parent.originalVerticalStep = parent.verticalStep;
            }
        }

        public static void resetVerticalStep(List<ObjectModularSnapTargetParent> modularSnapTargetParents)
        {
            foreach (var parent in modularSnapTargetParents)
                parent.verticalStep = 0;
        }

        public static void resetVerticalStepToOriginal(List<ObjectModularSnapTargetParent> modularSnapTargetParents)
        {
            foreach (var parent in modularSnapTargetParents)
                parent.verticalStep = parent.originalVerticalStep;
        }

        public static void storeLocalScaleSnapshots(List<ObjectModularSnapTargetParent> modularSnapTargetParents)
        {
            foreach (var parent in modularSnapTargetParents)
                parent.localScaleSnapshot = parent.transform.localScale;
        }
    }
}
#endif