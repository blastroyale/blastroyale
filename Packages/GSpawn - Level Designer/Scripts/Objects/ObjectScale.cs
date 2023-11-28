#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class ObjectScale
    {
        public class ScaleConfig
        {
            public Vector3              pivot                       = Vector3.zero;
            public Vector3              scaleFactor                 = Vector3.one;
            public Quaternion           scaleAxesRotation           = Quaternion.identity;
            public ObjectScaleStartData scaleStartData              = new ObjectScaleStartData();
            public bool                 alignScaleFactorToScaleAxes = false;
        }

        public static void scale(GameObject gameObject, ScaleConfig scaleConfig)
        {
            Transform transform     = gameObject.transform;
            Vector3 scaleRight      = scaleConfig.scaleAxesRotation * Vector3.right;
            Vector3 scaleUp         = scaleConfig.scaleAxesRotation * Vector3.up;
            Vector3 scaleLook       = scaleConfig.scaleAxesRotation * Vector3.forward;

            if (scaleConfig.alignScaleFactorToScaleAxes)
            {
                Vector3 remappedScaleFactor = scaleConfig.scaleFactor;

                int axis = transform.findIndexOfMostAlignedAxis(scaleRight);
                remappedScaleFactor[axis] = scaleConfig.scaleFactor.x;

                axis = transform.findIndexOfMostAlignedAxis(scaleUp);
                remappedScaleFactor[axis] = scaleConfig.scaleFactor.y;

                axis = transform.findIndexOfMostAlignedAxis(scaleLook);
                remappedScaleFactor[axis] = scaleConfig.scaleFactor.z;

                Vector3 finalLocalScale = Vector3.Scale(transform.localScale, remappedScaleFactor);
                transform.localScale = finalLocalScale.replaceZero(Vector3.Scale(scaleConfig.scaleStartData.localScale, scaleConfig.scaleFactor));
            }
            else
            {
                Vector3 finalLocalScale = Vector3.Scale(transform.localScale, scaleConfig.scaleFactor);
                transform.localScale = finalLocalScale.replaceZero(Vector3.Scale(scaleConfig.scaleStartData.localScale, scaleConfig.scaleFactor));
            }

            Vector3 fromPivotToPos  = transform.position - scaleConfig.pivot;
            fromPivotToPos          = fromPivotToPos.replaceZero(scaleConfig.scaleStartData.pivotToPosition);

            float rightOffset       = Vector3.Dot(fromPivotToPos, scaleRight) * scaleConfig.scaleFactor.x;
            float upOffset          = Vector3.Dot(fromPivotToPos, scaleUp) * scaleConfig.scaleFactor.y;
            float lookOffset        = Vector3.Dot(fromPivotToPos, scaleLook) * scaleConfig.scaleFactor.z;

            transform.position      = scaleConfig.pivot + scaleRight * rightOffset + scaleUp * upOffset + scaleLook * lookOffset;
        }
    }
}
#endif