#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class Vector3Ex
    {
        /// <summary>
        /// Finds a point on the segment p0-p1 such that the distance between this point
        /// and 'from' is equal to 'length'.
        /// </summary>
        public static bool calcPointOnSegment(Vector3 from, Vector3 p0, Vector3 p1, float length, out Vector3 pt)
        {
            pt              = Vector3.zero;
            Vector3 p0ToP1  = p1 - p0;
            Vector3 p0ToO   = from - p0;

            // Coefficients for the quadratic equation:
            // |O - (P0 + (P1 - P0) * t)| = length
            // a = (P1 - P0)^2
            // b = -2 * (O - P0)(P1 - P0)
            // c = (O - P0)^2 - length^2
            float a = Vector3.Dot(p0ToP1, p0ToP1);
            float b = -2.0f * Vector3.Dot((from - p0), p0ToP1);
            float c = Vector3.Dot(p0ToO, p0ToO) - length * length;
            float t, t1, t2;
            if (!MathEx.solveQuadratic(a, b, c, out t1, out t2)) return false;

            // Pick the t that lies between 0 and 1
            t = t1;
            if (t < 0.0f || t > 1.0f) t = t2;
            if (t < 0.0f || t > 1.0f) return false;

            // Calculate the point and return success
            pt = p0 + (p1 - p0) * t;
            return true;
        }

        public static Vector3 roundCorrectError(this Vector3 vec, float eps)
        {
            return new Vector3(MathEx.roundCorrectError(vec.x, eps), MathEx.roundCorrectError(vec.y, eps), MathEx.roundCorrectError(vec.z, eps));
        }

        public static Vector3 add(Vector3 v0, Vector3 v1, Vector3 weight)
        {
            return new Vector3(v0.x + weight.x * v1.x, v0.y + weight.y * v1.y, v0.z + weight.z * v1.z);
        }

        public static Vector3 add(Vector3 v0, float val, Vector3 weight)
        {
            return new Vector3(v0.x + weight.x * val, v0.y + weight.y * val, v0.z + weight.z * val);
        }

        public static Vector3 create(float value)
        {
            return new Vector3(value, value, value);
        }

        public static Vector3 createAxis(int axisIndex, Quaternion rotation)
        {
            if (axisIndex == 0)         return rotation * Vector3.right;
            else if (axisIndex == 1)    return rotation * Vector3.up;
            return rotation * Vector3.forward;
        }

        public static Vector3 replaceZero(this Vector3 vec, float val)
        {
            Vector3 v = vec;
            if (v.x == 0.0f) v.x = val;
            if (v.y == 0.0f) v.y = val;
            if (v.z == 0.0f) v.z = val;

            return v;
        }

        public static Vector3 replaceZero(this Vector3 vec, float val, float zeroEps)
        {
            Vector3 v = vec;
            if (Mathf.Abs(v.x) < zeroEps) v.x = val;
            if (Mathf.Abs(v.y) < zeroEps) v.y = val;
            if (Mathf.Abs(v.z) < zeroEps) v.z = val;

            return v;
        }

        public static Vector3 replaceZero(this Vector3 vec, Vector3 replacement)
        {
            Vector3 v = vec;
            if (v.x == 0.0f) v.x = replacement.x;
            if (v.y == 0.0f) v.y = replacement.y;
            if (v.z == 0.0f) v.z = replacement.z;

            return v;
        }

        public static Vector3 replace(this Vector3 vec, int compIndex, float newVal)
        {
            Vector3 newVec      = vec;
            newVec[compIndex]   = newVal;
            return newVec;
        }

        public static Vector3 replaceOther(this Vector3 vec, int maskedCompIndex, float newVal)
        {
            Vector3 newVec = vec;
            newVec[(maskedCompIndex + 1) % 3] = newVal;
            newVec[(maskedCompIndex + 2) % 3] = newVal;
            return newVec;
        }

        public static bool anyZero(this Vector3 vec, float eps)
        {
            if (Mathf.Abs(vec.x) <= eps) return true;
            if (Mathf.Abs(vec.y) <= eps) return true;
            if (Mathf.Abs(vec.z) <= eps) return true;

            return false;
        }

        public static bool anyLessEqual(this Vector3 vec, float val)
        {
            return vec.x <= val || vec.y <= val || vec.z <= val;
        }

        public static int countNegative(this Vector3 vec)
        {
            int counter = 0;
            if (vec.x < 0.0f) ++counter;
            if (vec.y < 0.0f) ++counter;
            if (vec.z < 0.0f) ++counter;

            return counter;
        }

        public static float getSizeAlongAxis(Vector3 size, Quaternion sizeRotation, Vector3 axis)
        {
            size = sizeRotation * size;
            return absDot(size, axis);
        }

        public static void checkDiff(this Vector3 vec, Vector3 other, bool[] diff)
        {
            diff[0] = vec.x != other.x;
            diff[1] = vec.y != other.y;
            diff[2] = vec.z != other.z;
        }

        public static Vector3 calcCenter(IEnumerable<Vector3> points)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (Vector3 pt in points)
            {
                min = Vector3.Min(pt, min);
                max = Vector3.Max(pt, max);
            }

            return (min + max) * 0.5f;
        }

        public static Vector3 getFirstUnalignedAxisVec(this Vector3 vec)
        {
            vec.Normalize();

            Vector3 worldAxis = Vector3.right;
            float absDot = Mathf.Abs(Vector3.Dot(vec, worldAxis));
            if ((1.0f - absDot) > 1e-4f) return worldAxis;

            worldAxis = Vector3.up;
            absDot = Mathf.Abs(Vector3.Dot(vec, worldAxis));
            if ((1.0f - absDot) > 1e-4f) return worldAxis;

            return Vector3.forward;
        }

        public static void offsetPoints(List<Vector3> points, Vector3 offset)
        {
            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
                points[ptIndex] += offset;
        }

        public static Vector3 abs(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static Vector3 getSignVector(this Vector3 v)
        {
            return new Vector3(Mathf.Sign(v.x), Mathf.Sign(v.y), Mathf.Sign(v.z));
        }

        public static float getMaxAbsComp(this Vector3 v)
        {
            float maxAbsComp = Mathf.Abs(v.x);

            float absY = Mathf.Abs(v.y);
            if (absY > maxAbsComp) maxAbsComp = absY;
            float absZ = Mathf.Abs(v.z);
            if (absZ > maxAbsComp) maxAbsComp = absZ;

            return maxAbsComp;
        }

        public static float getMinAbsComp(this Vector3 v)
        {
            float minAbsComp = Mathf.Abs(v.x);

            float absY = Mathf.Abs(v.y);
            if (absY < minAbsComp) minAbsComp = absY;
            float absZ = Mathf.Abs(v.z);
            if (absZ < minAbsComp) minAbsComp = absZ;

            return minAbsComp;
        }

        public static int getMaxAbsCompIndex(this Vector3 v)
        {
            float maxAbsComp    = Mathf.Abs(v.x);
            int compIndex       = 0;

            float absY = Mathf.Abs(v.y);
            if (absY > maxAbsComp)
            {
                maxAbsComp = absY;
                compIndex = 1;
            }
            float absZ = Mathf.Abs(v.z);
            if (absZ > maxAbsComp) compIndex = 2;

            return compIndex;
        }

        public static int getMinAbsCompIndex(this Vector3 v)
        {
            float minAbsComp    = Mathf.Abs(v.x);
            int compIndex       = 0;

            float absY = Mathf.Abs(v.y);
            if (absY < minAbsComp)
            {
                minAbsComp = absY;
                compIndex = 1;
            }
            float absZ = Mathf.Abs(v.z);
            if (absZ < minAbsComp) compIndex = 2;

            return compIndex;
        }

        public static float absDot(this Vector3 v1, Vector3 v2)
        {
            return Mathf.Abs(Vector3.Dot(v1, v2));
        }

        public static float getDistanceToSegment(this Vector3 point, Vector3 point0, Vector3 point1)
        {
            Vector3 segmentDir  = (point1 - point0);
            float segmentLength = segmentDir.magnitude;
            segmentDir.Normalize();

            Vector3 fromStartToPt = (point - point0);
            float projection    = Vector3.Dot(segmentDir, fromStartToPt);

            if (projection >= 0.0f && projection <= segmentLength)
                return ((point0 + segmentDir * projection) - point).magnitude;

            if (projection < 0.0f) return fromStartToPt.magnitude;
            return (point1 - point).magnitude;
        }

        public static Vector3 projectOnSegment(this Vector3 point, Vector3 point0, Vector3 point1)
        {
            Vector3 segmentDir = (point1 - point0).normalized;
            return point0 + segmentDir * Vector3.Dot((point - point0), segmentDir);
        }

        public static int findIndexOfPointFurthestFromPoint(List<Vector3> points, Vector3 pt)
        {
            float minDistSq     = float.MinValue;
            int closestPtIndex  = -1;

            for (int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 point = points[ptIndex];

                float distSq = (point - pt).sqrMagnitude;
                if (distSq > minDistSq)
                {
                    minDistSq = distSq;
                    closestPtIndex = ptIndex;
                }
            }

            return closestPtIndex;
        }

        public static int findIndexOfPointClosestToPoint(List<Vector3> points, Vector3 pt)
        {
            float minDistSq     = float.MaxValue;
            int closestPtIndex  = -1;

            for(int ptIndex = 0; ptIndex < points.Count; ++ptIndex)
            {
                Vector3 point = points[ptIndex];

                float distSq = (point - pt).sqrMagnitude;
                if(distSq < minDistSq)
                {
                    minDistSq = distSq;
                    closestPtIndex = ptIndex;
                }
            }

            return closestPtIndex;
        }

        public static int findIndexOfPointClosestToPoint(Vector3[] points, Vector3 pt)
        {
            float minDistSq     = float.MaxValue;
            int closestPtIndex  = -1;

            for (int ptIndex = 0; ptIndex < points.Length; ++ptIndex)
            {
                Vector3 point = points[ptIndex];

                float distSq = (point - pt).sqrMagnitude;
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    closestPtIndex = ptIndex;
                }
            }

            return closestPtIndex;
        }

        public static Vector3 getInverse(this Vector3 vector)
        {
            return new Vector3(1.0f / vector.x, 1.0f / vector.y, 1.0f / vector.z);
        }

        public static bool isAligned(this Vector3 vector, Vector3 other, bool checkSameDirection)
        {
            if (!checkSameDirection)
            {
                float absDot = vector.absDot(other);
                return Mathf.Abs(absDot - 1.0f) < 1e-5f;
            }
            else
            {
                float dot = Vector3.Dot(vector, other);
                return dot > 0.0f && Mathf.Abs(dot - 1.0f) < 1e-5f;
            }
        }

        public static void findIndicesOfLowestAndHighestPointsAlongAxis(Vector3 axis, Vector3 origin, List<Vector3> points, out int lowestIndex, out int highestIndex)
        {
            float maxDist       = float.MinValue;
            float minDist       = float.MaxValue;
            lowestIndex         = -1;
            highestIndex        = -1;

            int numPoints       = points.Count;
            for (int ptIndex = 0; ptIndex < numPoints; ++ptIndex)
            {
                float d = Vector3.Dot(points[ptIndex] - origin, axis);
                if (d < 0.0f)
                {
                    if (d < minDist)
                    {
                        minDist = d;
                        lowestIndex = ptIndex;
                    }
                }
                else
                {
                    if (d > maxDist)
                    {
                        maxDist = d;
                        highestIndex = ptIndex;
                    }
                }
            }

            if (lowestIndex < 0 && highestIndex >= 0) lowestIndex = highestIndex;
            else if (highestIndex < 0 && lowestIndex >= 0) highestIndex = lowestIndex;
        }
    }
}
#endif