#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public static class TransformDiffCheck
    {
        [Flags]
        public enum Axes
        {
            None = 0,
            X = 1,
            Y = 2,
            Z = 4,
            All = X | Y | Z
        }

        public struct DiffInfo
        {
            public Axes     position;
            public Axes     rotation;
            public Axes     scale;

            public bool     positionDiffers     { get { return position != Axes.None; } }
            public bool     xPositionDiffers    { get { return (position & Axes.X) != 0; } }
            public bool     yPositionDiffers    { get { return (position & Axes.Y) != 0; } }
            public bool     zPositionDiffers    { get { return (position & Axes.Z) != 0; } }
            public bool     rotationDiffers     { get { return rotation != Axes.None; } }
            public bool     xRotationDiffers    { get { return (rotation & Axes.X) != 0; } }
            public bool     yRotationDiffers    { get { return (rotation & Axes.Y) != 0; } }
            public bool     zRotationDiffers    { get { return (rotation & Axes.Z) != 0; } }
            public bool     scaleDiffers        { get { return scale != Axes.None; } }
            public bool     xScaleDiffers       { get { return (scale & Axes.X) != 0; } }
            public bool     yScaleDiffers       { get { return (scale & Axes.Y) != 0; } }
            public bool     zScaleDiffers       { get { return (scale & Axes.Z) != 0; } }
            public bool     anythingDiffers     { get { return positionDiffers | rotationDiffers | scaleDiffers; } }

            public bool positionAxisDiffers(int axisIndex)
            {
                if (axisIndex == 0) return xPositionDiffers;
                if (axisIndex == 1) return yPositionDiffers;
                return zPositionDiffers;
            }

            public void getPositionDiff(bool[] diff)
            {
                diff[0] = xPositionDiffers;
                diff[1] = yPositionDiffers;
                diff[2] = zPositionDiffers;
            }

            public bool rotationAxisDiffers(int axisIndex)
            {
                if (axisIndex == 0) return xRotationDiffers;
                if (axisIndex == 1) return yRotationDiffers;
                return zRotationDiffers;
            }

            public void getRotationDiff(bool[] diff)
            {
                diff[0] = xRotationDiffers;
                diff[1] = yRotationDiffers;
                diff[2] = zRotationDiffers;
            }

            public bool scaleAxisDiffers(int axisIndex)
            {
                if (axisIndex == 0) return xScaleDiffers;
                if (axisIndex == 1) return yScaleDiffers;
                return zScaleDiffers;
            }

            public void getScaleDiff(bool[] diff)
            {
                diff[0] = xScaleDiffers;
                diff[1] = yScaleDiffers;
                diff[2] = zScaleDiffers;
            }

            public static DiffInfo getDefaultDiffInfo()
            {
                return new DiffInfo()
                {
                    position = Axes.None,
                    rotation = Axes.None,
                    scale = Axes.None
                };
            }
        }

        public static DiffInfo checkLocalDiff(List<Transform> transforms)
        {
            DiffInfo diffInfo = DiffInfo.getDefaultDiffInfo();
            bool fullPosDiff = false;

            int numTransforms = transforms.Count;
            for (int i = 0; i < numTransforms; ++i)
            {
                Transform transform = transforms[i];
                Vector3 localEuler = transform.localRotation.eulerAngles;

                bool exit = false;
                for (int j = i + 1; j < numTransforms; ++j)
                {
                    Transform otherTransform = transforms[j];
         
                    int diffCount = 0;
                    if (!fullPosDiff)
                    {
                        // Note: Using Vector3Ex.checkDiff seems to be slow. Also, not caching 
                        //       the vector values in local variables seems to speed things up a bit.
                        if (otherTransform.localPosition.x != transform.localPosition.x) { diffInfo.position |= Axes.X; ++diffCount; }
                        if (otherTransform.localPosition.y != transform.localPosition.y) { diffInfo.position |= Axes.Y; ++diffCount; }
                        if (otherTransform.localPosition.z != transform.localPosition.z) { diffInfo.position |= Axes.Z; ++diffCount; }
                        if (diffCount == 3) fullPosDiff = true;
                    }
                    else diffCount = 3;

                    Vector3 otherLocalEuler = otherTransform.localRotation.eulerAngles;
                    if (otherLocalEuler.x != localEuler.x) { diffInfo.rotation |= Axes.X; ++diffCount; }
                    if (otherLocalEuler.y != localEuler.y) { diffInfo.rotation |= Axes.Y; ++diffCount; }
                    if (otherLocalEuler.z != localEuler.z) { diffInfo.rotation |= Axes.Z; ++diffCount; }

                    if (otherTransform.localScale.x != transform.localScale.x) { diffInfo.scale |= Axes.X; ++diffCount; }
                    if (otherTransform.localScale.y != transform.localScale.y) { diffInfo.scale |= Axes.Y; ++diffCount; }
                    if (otherTransform.localScale.z != transform.localScale.z) { diffInfo.scale |= Axes.Z; ++diffCount; }

                    if (diffCount == 9)
                    {
                        exit = true;
                        break;
                    }
                }

                if (exit) break;
            }

            return diffInfo;
        }
    }
}
#endif