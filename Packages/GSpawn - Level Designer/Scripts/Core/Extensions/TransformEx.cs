#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class TransformEx
    {
        // Useful for holding positive and negative axes of a coordinate system.
        private static Vector3[] _localAxesBuffer0 = new Vector3[6];
        private static Vector3[] _localAxesBuffer1 = new Vector3[6];

        public static Vector3 calcAveragePosition(List<GameObject> gameObjects)
        {
            if (gameObjects.Count == 0) return Vector3.zero;

            Vector3 avgPos = Vector3.zero;
            foreach (var go in gameObjects)
                avgPos += go.transform.position;

            avgPos /= (float)gameObjects.Count;
            return avgPos;
        }

        public static TransformTRS createTransformTRS(this Transform transform)
        {
            TransformTRS trs = new TransformTRS();
            trs.extract(transform);
            return trs;
        }

        public static void getTransforms(IEnumerable<GameObject> gameObjects, List<Transform> transforms)
        {
            transforms.Clear();
            foreach (var gameObject in gameObjects)
                if (gameObject != null) transforms.Add(gameObject.transform);
        }

        public static void getLocalPositions(IEnumerable<Transform> transforms, List<Vector3> localPositions)
        {
            localPositions.Clear();
            foreach (var transform in transforms)
                localPositions.Add(transform.localPosition);
        }

        public static void getLocalEulerAngles(IEnumerable<Transform> transforms, List<Vector3> localEulerAngles)
        {
            localEulerAngles.Clear();
            foreach (var transform in transforms)
                localEulerAngles.Add(transform.localRotation.eulerAngles);
        }

        public static void getLocalScales(IEnumerable<Transform> transforms, List<Vector3> localScales)
        {
            localScales.Clear();
            foreach (var transform in transforms)
                localScales.Add(transform.localScale);
        }

        public static int findIndexOfMostAlignedAxis(Vector3[] axes, Vector3 axis)
        {
            int bestAxis = 0;
            float bestDot = Mathf.Abs(Vector3.Dot(axes[0], axis));

            float dot = Mathf.Abs(Vector3.Dot(axes[1], axis));
            if (dot > bestDot)
            {
                bestDot = dot;
                bestAxis = 1;
            }

            dot = Mathf.Abs(Vector3.Dot(axes[2], axis));
            if (dot > bestDot) bestAxis = 2;

            return bestAxis;
        }

        public static int findIndexOfMostAlignedAxis(this Transform transform, Vector3 axis)
        {
            int bestAxis    = 0;
            float bestDot   = Mathf.Abs(Vector3.Dot(transform.right, axis));

            float dot = Mathf.Abs(Vector3.Dot(transform.up, axis));
            if (dot > bestDot)
            {
                bestDot = dot;
                bestAxis = 1;
            }

            dot = Mathf.Abs(Vector3.Dot(transform.forward, axis));
            if (dot > bestDot) bestAxis = 2;

            return bestAxis;
        }

        public static Vector3 calcFirstUnalignedAxisVec(this Transform transform, Vector3 axis)
        {
            Vector3 localAxis   = transform.right;
            float absDot        = Mathf.Abs(Vector3.Dot(axis, localAxis));
            if ((1.0f - absDot) > 1e-4f) return localAxis;

            localAxis           = transform.up;
            absDot              = Mathf.Abs(Vector3.Dot(axis, localAxis));
            if ((1.0f - absDot) > 1e-4f) return localAxis;

            return transform.forward;
        }

        public static void transformPoints(this Transform transform, List<Vector3> points)
        {
            for(int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
                points[ptIndex] = transform.TransformPoint(points[ptIndex]);
        }

        public static void getParents(IEnumerable<Transform> transforms, List<Transform> parents)
        {
            if (transforms == null) return;

            parents.Clear();
            foreach (var transform in transforms)
            {
                bool foundParent = false;
                foreach (var possibleParent in transforms)
                {
                    if (possibleParent != transform)
                    {
                        if (transform.IsChildOf(possibleParent.transform))
                        {
                            foundParent = true;
                            break;
                        }
                    }
                }

                if (!foundParent) parents.Add(transform);
            }
        }

        public static void setLocalScaleFromPivot(this Transform transform, Vector3 localScale, Vector3 pivot)
        {
            Vector3 scaleFactor     = Vector3.Scale(localScale, transform.localScale.getInverse());
            transform.localScale    = localScale;

            Vector3 right           = transform.right;
            Vector3 up              = transform.up;
            Vector3 look            = transform.forward;
            Vector3 fromPivotToPos  = transform.position - pivot;

            float rightOffset       = Vector3.Dot(fromPivotToPos, right) * scaleFactor.x;
            float upOffset          = Vector3.Dot(fromPivotToPos, up) * scaleFactor.y;
            float lookOffset        = Vector3.Dot(fromPivotToPos, look) * scaleFactor.z;

            transform.position      = pivot + right * rightOffset + up * upOffset + look * lookOffset;
        }

        public static Plane getLocalPlane(this Transform transform, PlaneDescriptor planeDesc)
        {
            Vector3 firstAxis       = transform.getLocalAxis(planeDesc.firstAxisDescriptor);
            Vector3 secondAxis      = transform.getLocalAxis(planeDesc.secondAxisDescriptor);

            return new Plane(Vector3.Normalize(Vector3.Cross(firstAxis, secondAxis)), transform.position);
        }

        public static void rotateAround(this Transform transform, Quaternion rotation, Vector3 pivot)
        {
            Vector3 fromPivotToPos  = transform.position - pivot;
            transform.rotation      = rotation * transform.rotation;
            fromPivotToPos          = rotation * fromPivotToPos;
            transform.position      = pivot + fromPivotToPos;
        }

        public static Quaternion alignAxis(this Transform transform, int alignmentAxisIndex, Vector3 destAxis)
        {
            Vector3 alignmentAxis                           = transform.right;
            if (alignmentAxisIndex == 1) alignmentAxis      = transform.up;
            else if (alignmentAxisIndex == 2) alignmentAxis = transform.forward;

            float dot = Vector3.Dot(alignmentAxis, destAxis);
            if (1.0f - dot < 1e-6f) return Quaternion.identity;

            if (dot + 1.0f < 1e-6f)
            {
                Vector3 rotationAxis                            = transform.forward;
                if (alignmentAxisIndex == 0) rotationAxis       = transform.up;
                else if (alignmentAxisIndex == 2) rotationAxis  = transform.right;

                transform.Rotate(rotationAxis, 180.0f, Space.World);
                return Quaternion.AngleAxis(180.0f, rotationAxis);
            }
            else
            {
                Vector3 rotationAxis    = Vector3.Cross(alignmentAxis, destAxis).normalized;
                float rotationAngle     = Vector3.SignedAngle(alignmentAxis, destAxis, rotationAxis);
                transform.Rotate(rotationAxis, rotationAngle, Space.World);
                return Quaternion.AngleAxis(rotationAngle, rotationAxis);
            }
        }

        public static Quaternion alignAxis(this Transform transform, Vector3 alignmentAxis, Vector3 destAxis, Vector3 rotationPivot)
        {
            float dot = Vector3.Dot(alignmentAxis, destAxis);
            if (1.0f - dot < 1e-6f) return Quaternion.identity;
            if (dot + 1.0f < 1e-6f)
            {
                Vector3 rotationAxis    = transform.calcFirstUnalignedAxisVec(alignmentAxis);
                Quaternion rotation     = Quaternion.AngleAxis(180.0f, rotationAxis);
                transform.RotateAround(rotationPivot, rotationAxis, 180.0f);
                return rotation;
            }
            else
            {
                Vector3 rotationAxis    = Vector3.Cross(alignmentAxis, destAxis).normalized;
                float rotationAngle     = Vector3.SignedAngle(alignmentAxis, destAxis, rotationAxis);
                Quaternion rotation     = Quaternion.AngleAxis(rotationAngle, rotationAxis);
                transform.RotateAround(rotationPivot, rotationAxis, rotationAngle);
                return rotation;
            }
        }

        public static Quaternion calcAlignmentRotation(this Transform transform, Vector3 alignmentAxis, Vector3 destAxis)
        {
            float dot = Vector3.Dot(alignmentAxis, destAxis);
            if (1.0f - dot < 1e-6f) return Quaternion.identity;
            if (dot + 1.0f < 1e-6f)
            {
                Vector3 rotationAxis    = transform.calcFirstUnalignedAxisVec(alignmentAxis);
                Quaternion rotation     = Quaternion.AngleAxis(180.0f, rotationAxis);
                return rotation;
            }
            else
            {
                Vector3 rotationAxis    = Vector3.Cross(alignmentAxis, destAxis).normalized;
                float rotationAngle     = Vector3.SignedAngle(alignmentAxis, destAxis, rotationAxis);
                Quaternion rotation     = Quaternion.AngleAxis(rotationAngle, rotationAxis);
                return rotation;
            }
        }

        public static void setWorldScale(this Transform transform, Vector3 worldScale)
        {
            transform.localScale = Vector3.one;
            transform.localScale = new Vector3(worldScale.x / transform.lossyScale.x, worldScale.y / transform.lossyScale.y, worldScale.z / transform.lossyScale.z);
        }

        public static Vector3 flexiToLocalAxis(this Transform transform, OBB worldOBB, FlexiAxis flexiAxis, bool invertAxis)
        {
            if (!worldOBB.isValid) return Vector3.zero;

            int axisIndex;
            if (flexiAxis == FlexiAxis.Longest) axisIndex       = worldOBB.size.getMaxAbsCompIndex();
            else if (flexiAxis == FlexiAxis.Shortest) axisIndex = worldOBB.size.getMinAbsCompIndex();
            else if (flexiAxis == FlexiAxis.X) axisIndex        = 0;
            else if (flexiAxis == FlexiAxis.Y) axisIndex        = 1;
            else axisIndex = 2;

            if (axisIndex == 0) return invertAxis ? -transform.right : transform.right;
            if (axisIndex == 1) return invertAxis ? -transform.up : transform.up;
            return invertAxis ? -transform.forward : transform.forward;
        }

        public static Vector3 flexiToLocalAxis(this Transform transform, Vector3 objectSize, FlexiAxis flexiAxis, bool invertAxis)
        {
            int axisIndex;
            if (flexiAxis == FlexiAxis.Longest)         axisIndex = objectSize.getMaxAbsCompIndex();
            else if (flexiAxis == FlexiAxis.Shortest)   axisIndex = objectSize.getMinAbsCompIndex();
            else if (flexiAxis == FlexiAxis.X)          axisIndex = 0;
            else if (flexiAxis == FlexiAxis.Y)          axisIndex = 1;
            else axisIndex = 2;
         
            if (axisIndex == 0) return invertAxis ? -transform.right : transform.right;
            if (axisIndex == 1) return invertAxis ? -transform.up : transform.up;
            return invertAxis ? -transform.forward : transform.forward;
        }

        public static AxisDescriptor flexiToLocalAxisDesc(this Transform transform, OBB worldOBB, FlexiAxis flexiAxis, bool invertAxis)
        {
            if (!worldOBB.isValid) return new AxisDescriptor(0, AxisSign.Positive);

            int axisIndex;
            if (flexiAxis == FlexiAxis.Longest) axisIndex       = worldOBB.size.getMaxAbsCompIndex();
            else if (flexiAxis == FlexiAxis.Shortest) axisIndex = worldOBB.size.getMinAbsCompIndex();
            else if (flexiAxis == FlexiAxis.X) axisIndex        = 0;
            else if (flexiAxis == FlexiAxis.Y) axisIndex        = 1;
            else axisIndex = 2;

            return new AxisDescriptor(axisIndex, invertAxis ? AxisSign.Negative : AxisSign.Positive);
        }

        public static AxisDescriptor flexiToLocalAxisDesc(this Transform transform, Vector3 objectSize, FlexiAxis flexiAxis, bool invertAxis)
        {
            int axisIndex;
            if (flexiAxis == FlexiAxis.Longest) axisIndex       = objectSize.getMaxAbsCompIndex();
            else if (flexiAxis == FlexiAxis.Shortest) axisIndex = objectSize.getMinAbsCompIndex();
            else if (flexiAxis == FlexiAxis.X) axisIndex        = 0;
            else if (flexiAxis == FlexiAxis.Y) axisIndex        = 1;
            else axisIndex = 2;

            return new AxisDescriptor(axisIndex, invertAxis ? AxisSign.Negative : AxisSign.Positive);
        }

        public static Vector3 modularWallToLocalAxis(this Transform transform, ModularWallAxis wallAxis, bool invertAxis)
        {
            int axisIndex;
            if (wallAxis == ModularWallAxis.X)      axisIndex = 0;
            else if (wallAxis == ModularWallAxis.Y) axisIndex = 1;
            else axisIndex = 2;

            if (axisIndex == 0) return invertAxis ? -transform.right : transform.right;
            if (axisIndex == 1) return invertAxis ? -transform.up : transform.up;
            return invertAxis ? -transform.forward : transform.forward;
        }

        public static AxisDescriptor modularWallToLocalAxisDesc(this Transform transform, ModularWallAxis wallAxis, bool invertAxis)
        {
            int axisIndex;
            if (wallAxis == ModularWallAxis.X)      axisIndex = 0;
            else if (wallAxis == ModularWallAxis.Y) axisIndex = 1;
            else axisIndex = 2;

            return new AxisDescriptor(axisIndex, invertAxis ? AxisSign.Negative : AxisSign.Positive);
        }

        public static Vector3 getLocalAxis(this Transform transform, int axisIndex)
        {
            Vector3 axis = transform.right;
            if (axisIndex == 1) axis = transform.up;
            else if (axisIndex == 2) axis = transform.forward;

            return axis;
        }

        public static Vector3 getLocalAxis(this Transform transform, AxisDescriptor axisDesc)
        {
            Vector3 axis                        = transform.right;
            if (axisDesc.index == 1) axis       = transform.up;
            else if (axisDesc.index == 2) axis  = transform.forward;

            return axisDesc.sign == AxisSign.Positive ? axis : -axis;
        }

        public static bool checkCoordSystemAxesAlignment(this Transform transform, Quaternion coordSystemRotation, float angleEps)
        {
            if (angleEps < 0.0f) angleEps = 0.0f;
            else if (angleEps > 90.0f) angleEps = 90.0f;

            // Calculate the local axes of the other coordinate system
            Vector3 right       = coordSystemRotation * Vector3.right;
            Vector3 up          = coordSystemRotation * Vector3.up;
            Vector3 look        = coordSystemRotation * Vector3.forward;

            // Keep track of the number of aligned axes
            int numAlignedAxes = 0;

            // Store coordinate system axes inside arrays to be able to use a for loop
            _localAxesBuffer0[0] = right;
            _localAxesBuffer0[1] = up;
            _localAxesBuffer0[2] = look;
            _localAxesBuffer0[3] = -right;
            _localAxesBuffer0[4] = -up;
            _localAxesBuffer0[5] = -look;

            _localAxesBuffer1[0] = transform.right;
            _localAxesBuffer1[1] = transform.up;
            _localAxesBuffer1[2] = transform.forward;

            for (int transformAxisIndex = 0; transformAxisIndex < 3; ++transformAxisIndex)
            {
                Vector3 axis = _localAxesBuffer1[transformAxisIndex];
                foreach (var otherAxis in _localAxesBuffer0)
                {
                    float angle = Vector3.Angle(axis, otherAxis);
                    if (angle < angleEps)
                    {
                        ++numAlignedAxes;
                        break;
                    }
                }

                if (numAlignedAxes >= 2) return true;
            }

            return false;
        }
    }
}
#endif