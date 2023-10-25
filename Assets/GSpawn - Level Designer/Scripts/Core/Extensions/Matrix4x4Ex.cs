#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class Matrix4x4Ex
    {
        public static Matrix4x4 createTranslation(Vector3 translation)
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetColumn(3, Vector4Ex.create(translation, 1.0f));

            return matrix;
        }

        public static Matrix4x4 createRotationFromRightUp(Vector3 right, Vector3 up)
        {
            right.Normalize();
            up.Normalize();
            Vector3 look = Vector3.Cross(up, right).normalized;

            Matrix4x4 matrix = Matrix4x4.identity;

            matrix[0, 0] = right.x;
            matrix[1, 0] = right.y;
            matrix[2, 0] = right.z;

            matrix[0, 1] = up.x;
            matrix[1, 1] = up.y;
            matrix[2, 1] = up.z;

            matrix[0, 2] = look.x;
            matrix[1, 2] = look.y;
            matrix[2, 2] = look.z;

            return matrix;
        }

        public static Ray transformRay(this Matrix4x4 mtx, Ray ray)
        {
            Vector3 origin = mtx.MultiplyPoint(ray.origin);
            Vector3 direction = mtx.MultiplyVector(ray.direction).normalized;

            return new Ray(origin, direction);
        }

        public static Matrix4x4 calcRelativeTransform(this Matrix4x4 matrix, Matrix4x4 relativeTo)
        {
            return relativeTo.inverse * matrix;
        }

        public static Vector3 getRight(this Matrix4x4 matrix)
        {
            return matrix.GetColumn(0).normalized;
        }

        public static Vector3 getUp(this Matrix4x4 matrix)
        {
            return matrix.GetColumn(1).normalized;
        }

        public static Vector3 getForward(this Matrix4x4 matrix)
        {
            return matrix.GetColumn(2).normalized;
        }

        public static Vector3 getTranslation(this Matrix4x4 matrix)
        {
            return matrix.GetColumn(3);
        }

        public static Vector3 getPositiveScale(this Matrix4x4 matrix)
        {
            return new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
        }

        public static Vector3 getNormalizedAxis(this Matrix4x4 matrix, int axisIndex)
        {
            return matrix.GetColumn(axisIndex).normalized;
        }

        public static void getNormalizedAxes(this Matrix4x4 matrix, Vector3[] normAxes)
        {
            normAxes[0] = matrix.GetColumn(0).normalized;
            normAxes[1] = matrix.GetColumn(1).normalized;
            normAxes[2] = matrix.GetColumn(2).normalized;
        }

        public static void transformPoints(this Matrix4x4 matrix, List<Vector3> points, List<Vector3> transformedPts)
        {
            transformedPts.Clear();
            foreach (var pt in points)
                transformedPts.Add(matrix.MultiplyPoint(pt));
        }

        public static void transformPoints(this Matrix4x4 matrix, List<Vector3> points)
        {
            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
                points[ptIndex] = matrix.MultiplyPoint(points[ptIndex]);
        }
    }
}
#endif