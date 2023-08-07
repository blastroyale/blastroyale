#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class PlaneEx
    {
        public static Plane createPlaneWithMostAlignedNormal(int axisIndex, Quaternion frameRotation, Vector3 refVector, Vector3 ptOnPlane)
        {
            if (axisIndex == 0)
            {
                Vector3 bestNormal = frameRotation * Vector3.forward;
                Vector3 up = frameRotation * Vector3.up;
                if (bestNormal.absDot(refVector) < up.absDot(refVector)) bestNormal = up;

                return new Plane(bestNormal, ptOnPlane);
            }
            else
            if (axisIndex == 1)
            {
                Vector3 bestNormal = frameRotation * Vector3.forward;
                Vector3 right = frameRotation * Vector3.right;
                if (bestNormal.absDot(refVector) < right.absDot(refVector)) bestNormal = right;

                return new Plane(bestNormal, ptOnPlane);
            }
            else
            {
                Vector3 bestNormal = frameRotation * Vector3.right;
                Vector3 up = frameRotation * Vector3.up;
                if (bestNormal.absDot(refVector) < up.absDot(refVector)) bestNormal = up;

                return new Plane(bestNormal, ptOnPlane);
            }
        }

        public static PlaneClassifyResult classifyPoint(this Plane plane, Vector3 point, float onPlaneEps = 1e-5f)
        {
            float d = plane.GetDistanceToPoint(point);
            if (d < onPlaneEps) return PlaneClassifyResult.Behind;
            if (d > onPlaneEps) return PlaneClassifyResult.InFront;
            return PlaneClassifyResult.OnPlane;
        }

        public static PlaneClassifyResult classifyPoints(this Plane plane, IEnumerable<Vector3> points, float onPlaneEps = 1e-5f)
        {
            int numInFront  = 0;
            int numBehind   = 0;

            foreach(var pt in points)
            {
                float d = plane.GetDistanceToPoint(pt);
                if (d < onPlaneEps) ++numBehind;
                else if (d > onPlaneEps) ++numInFront;
            }

            if (numInFront != 0 && numBehind == 0) return PlaneClassifyResult.InFront;
            else if (numInFront == 0 && numBehind != 0) return PlaneClassifyResult.Behind;
            else if (numInFront == 0 && numBehind == 0) return PlaneClassifyResult.OnPlane;
            return PlaneClassifyResult.Spanning;
        }

        public static Vector3 projectPoint(this Plane plane, Vector3 point)
        {
            float distToPt = plane.GetDistanceToPoint(point);
            return point - plane.normal * distToPt;
        }

        public static Plane invertNormal(this Plane plane)
        {
            return new Plane(-plane.normal, -plane.distance);
        }

        public static float absDistanceToPoint(this Plane plane, Vector3 point)
        {
            return Mathf.Abs(plane.GetDistanceToPoint(point));
        }

        public static void projectPoints(this Plane plane, List<Vector3> points, List<Vector3> projectedPoints)
        {
            projectedPoints.Clear();

            foreach (var pt in points)
                projectedPoints.Add(plane.projectPoint(pt));
        }

        public static void projectPoints(this Plane plane, List<Vector3> points)
        {
            int numPoints = points.Count;
            for (int ptIndex = 0; ptIndex < numPoints; ++ptIndex)
                points[ptIndex] = plane.projectPoint(points[ptIndex]);
        }

        public static int findIndexOfClosestPoint(this Plane plane, List<Vector3> points)
        {
            int closestPtIndex = -1;
            float minDist = float.MaxValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                float absDist = Mathf.Abs(plane.GetDistanceToPoint(points[ptIndex]));
                if (absDist < minDist)
                {
                    minDist = absDist;
                    closestPtIndex = ptIndex;
                }
            }

            return closestPtIndex;
        }

        public static int findIndexOfFurthestPointInFront(this Plane plane, List<Vector3> points)
        {
            int furthestPtIndex = -1;
            float maxDist       = float.MinValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float distance = plane.GetDistanceToPoint(pt);

                if (distance > 0.0f && distance > maxDist)
                {
                    maxDist = distance;
                    furthestPtIndex = ptIndex;
                }
            }

            return furthestPtIndex;
        }

        public static int findIndexOfClosestPointInFront(this Plane plane, List<Vector3> points)
        {
            int closestPtIndex  = -1;
            float minDist       = float.MaxValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float distance = plane.GetDistanceToPoint(pt);

                if (distance > 0.0f && distance < minDist)
                {
                    minDist = distance;
                    closestPtIndex = ptIndex;
                }
            }

            return closestPtIndex;
        }

        public static int findIndexOfClosestPointBehind(this Plane plane, List<Vector3> points)
        {
            int closestPtIndex  = -1;
            float minDist       = float.MinValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float distance = plane.GetDistanceToPoint(pt);

                if (distance < 0.0f && distance > minDist)
                {
                    minDist = distance;
                    closestPtIndex = ptIndex;
                }
            }

            return closestPtIndex;
        }

        public static int findIndexOfClosestPointInFrontOrOnPlane(this Plane plane, List<Vector3> points)
        {
            int closestPtIndex  = -1;
            float minDist       = float.MaxValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float distance = plane.GetDistanceToPoint(pt);

                if ((distance >= 0.0f && distance < minDist) || Mathf.Abs(distance) < 1e-4f)
                {
                    minDist = distance;
                    closestPtIndex = ptIndex;
                }
            }

            return closestPtIndex;
        }

        public static int findIndexOfClosestPointBehindOrOnPlane(this Plane plane, List<Vector3> points)
        {
            int closestPtIndex  = -1;
            float minDist       = float.MinValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float distance = plane.GetDistanceToPoint(pt);

                if ((distance <= 0.0f && distance > minDist) || Mathf.Abs(distance) < 1e-4f)
                {
                    minDist = distance;
                    closestPtIndex = ptIndex;
                }
            }

            return closestPtIndex;
        }

        public static int findIndexOfFurthestPointBehind(this Plane plane, List<Vector3> points)
        {
            int furthestPtIndex = -1;
            float minDist       = float.MaxValue;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 pt = points[ptIndex];
                float distance = plane.GetDistanceToPoint(pt);

                if (distance < 0.0f && distance < minDist)
                {
                    minDist = distance;
                    furthestPtIndex = ptIndex;
                }
            }

            return furthestPtIndex;
        }
    }
}
#endif