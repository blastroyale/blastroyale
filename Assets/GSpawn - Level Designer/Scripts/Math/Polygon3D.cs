#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class Polygon3DCullResult
    {
        public int          numVerts;
        public Vector3[]    verts;
    }

    public class Polygon3DSplitResult
    {
        public PlaneClassifyResult  classifyResult;
        public int                  numFrontVerts;
        public Vector3[]            frontVerts;
        public int                  numBackVerts;
        public Vector3[]            backVerts;
    }

    public static class Polygon3D
    {
        private static List<Vector3>        _cullPolyPoints     = new List<Vector3>(3);
        private static Polygon3DSplitResult _cullSplitResult    = new Polygon3DSplitResult();
        private static float[]              _splitD             = new float[3];

        // Note: Returns false if the entire polygon has been culled. Otherwise, it returns true.
        public static bool cullCW(Vector3[] cwPolyPoints, int numPolyPoints, AABB aabb, int aabbFaceMask, Polygon3DCullResult result, float onPlaneEps = 1e-5f)
        {
            if (aabbFaceMask == 0) return true;

            _cullPolyPoints.Clear();
            for (int i = 0; i < numPolyPoints; ++i)
                _cullPolyPoints.Add(cwPolyPoints[i]);

            var boxFaces = Box3D.facesArray;
            foreach (var boxFace in boxFaces)
            {
                // Note: Skip if this face is ignored.
                if (!Box3D.checkBoxFaceBit(aabbFaceMask, boxFace)) continue;

                // Create a plane for this face which acts as the split plane
                Plane splitPlane = Box3D.calcFacePlane(aabb.center, aabb.size, boxFace);

                // Split the polygon
                splitCW(_cullPolyPoints, numPolyPoints, splitPlane, _cullSplitResult, onPlaneEps);
        
                // If the polygon is completely in front of the face, it means
                // it is completely outside the AABB so we can cull it completely.
                if (_cullSplitResult.classifyResult == PlaneClassifyResult.InFront)
                {
                    result.numVerts = 0;
                    return false;
                }

                // We are interested in maintaining the points which lie behind the AABB face
                if (_cullSplitResult.numBackVerts != 0)
                {
                    // Copy back-side verts into the polygon point buffer
                    _cullPolyPoints.Clear();
                    numPolyPoints = _cullSplitResult.numBackVerts;
                    for (int i = 0; i < numPolyPoints; ++i)
                        _cullPolyPoints.Add(_cullSplitResult.backVerts[i]);
                }
            }

            result.numVerts = numPolyPoints;
            if (numPolyPoints == 0) return false;

            if (result.verts == null || result.verts.Length < numPolyPoints)
                result.verts = new Vector3[numPolyPoints];

            _cullPolyPoints.CopyTo(result.verts, 0);

            return true;
        }

        public static void splitCW(Vector3[] cwPolyPoints, int numPolyPoints, Plane splitPlane, Polygon3DSplitResult result, float onPlaneEps = 1e-5f)
        {
            result.numFrontVerts    = 0;
            result.numBackVerts     = 0;

            if (_splitD.Length < numPolyPoints) _splitD = new float[numPolyPoints];

            if (result.frontVerts == null || result.frontVerts.Length < numPolyPoints + 1)
                result.frontVerts = new Vector3[numPolyPoints + 1];

            if (result.backVerts == null || result.backVerts.Length < numPolyPoints + 1)
                result.backVerts = new Vector3[numPolyPoints + 1];

            for (int i = 0; i < numPolyPoints; ++i)
                _splitD[i] = splitPlane.GetDistanceToPoint(cwPolyPoints[i]);

            int numPointsOnPlane = 0;
            for (int i = 0; i < numPolyPoints; ++i)
            {
                int nextPtIndex = (i + 1) % numPolyPoints;

                float d0 = _splitD[i];
                float d1 = _splitD[nextPtIndex];

                if (d0 > onPlaneEps)
                {
                    result.frontVerts[result.numFrontVerts++] = cwPolyPoints[i];
                    if (d1 < -onPlaneEps)
                    {
                        Vector3 segment = cwPolyPoints[nextPtIndex] - cwPolyPoints[i];
                        float t = d0 / Vector3.Dot(segment, -splitPlane.normal);
                        result.frontVerts[result.numFrontVerts++] = cwPolyPoints[i] + segment * t;
                        result.backVerts[result.numBackVerts++] = result.frontVerts[result.numFrontVerts - 1];
                    }
                }
                else
                if (d0 < -onPlaneEps)
                {
                    result.backVerts[result.numBackVerts++] = cwPolyPoints[i];
                    if (d1 > onPlaneEps)
                    {
                        Vector3 segment = cwPolyPoints[nextPtIndex] - cwPolyPoints[i];
                        float t = -d0 / Vector3.Dot(segment, splitPlane.normal);
                        result.backVerts[result.numBackVerts++] = cwPolyPoints[i] + segment * t;
                        result.frontVerts[result.numFrontVerts++] = result.backVerts[result.numBackVerts - 1];
                    }
                }
                else
                {
                    // Note: We need this separate counter to handle special cases. For example, a triangle
                    //       that has one point behind, one on plane and one in front would be treated as
                    //       being on plane due to the way we perform the final checks at the end of the function.
                    ++numPointsOnPlane;
                    result.frontVerts[result.numFrontVerts++] = cwPolyPoints[i];
                    result.backVerts[result.numBackVerts++] = cwPolyPoints[i];
                }
            }

            // Exactly on plane?
            if (numPointsOnPlane == numPolyPoints) result.classifyResult = PlaneClassifyResult.OnPlane;
            // In front?
            else if (result.numFrontVerts == numPolyPoints && result.numBackVerts == numPointsOnPlane) result.classifyResult = PlaneClassifyResult.InFront;
            // Behind?
            else if (result.numBackVerts == numPolyPoints && result.numFrontVerts == numPointsOnPlane) result.classifyResult = PlaneClassifyResult.Behind;
            // Spanning
            else result.classifyResult = PlaneClassifyResult.Spanning;
        }

        public static void splitCW(List<Vector3> cwPolyPoints, int numPolyPoints, Plane splitPlane, Polygon3DSplitResult result, float onPlaneEps = 1e-5f)
        {
            result.numFrontVerts    = 0;
            result.numBackVerts     = 0;

            if (_splitD.Length < numPolyPoints) _splitD = new float[numPolyPoints];

            if (result.frontVerts == null || result.frontVerts.Length < numPolyPoints + 1)
                result.frontVerts = new Vector3[numPolyPoints + 1];

            if (result.backVerts == null || result.backVerts.Length < numPolyPoints + 1)
                result.backVerts = new Vector3[numPolyPoints + 1];

            for (int i = 0; i < numPolyPoints; ++i)
                _splitD[i] = splitPlane.GetDistanceToPoint(cwPolyPoints[i]);

            int numPointsOnPlane = 0;
            for (int i = 0; i < numPolyPoints; ++i)
            {
                int nextPtIndex = (i + 1) % numPolyPoints;

                float d0 = _splitD[i];
                float d1 = _splitD[nextPtIndex];

                if (d0 > onPlaneEps)
                {
                    result.frontVerts[result.numFrontVerts++] = cwPolyPoints[i];
                    if (d1 < -onPlaneEps)
                    {
                        Vector3 segment = cwPolyPoints[nextPtIndex] - cwPolyPoints[i];
                        float t = d0 / Vector3.Dot(segment, -splitPlane.normal);
                        result.frontVerts[result.numFrontVerts++] = cwPolyPoints[i] + segment * t;
                        result.backVerts[result.numBackVerts++] = result.frontVerts[result.numFrontVerts - 1];
                    }
                }
                else
                if (d0 < -onPlaneEps)
                {
                    result.backVerts[result.numBackVerts++] = cwPolyPoints[i];
                    if (d1 > onPlaneEps)
                    {
                        Vector3 segment = cwPolyPoints[nextPtIndex] - cwPolyPoints[i];
                        float t = -d0 / Vector3.Dot(segment, splitPlane.normal);
                        result.backVerts[result.numBackVerts++] = cwPolyPoints[i] + segment * t;
                        result.frontVerts[result.numFrontVerts++] = result.backVerts[result.numBackVerts - 1];
                    }
                }
                else
                {
                    // Note: We need this separate counter to handle special cases. For example, a triangle
                    //       that has one point behind, one on plane and one in front would be treated as
                    //       being on plane due to the way we perform the final checks at the end of the function.
                    ++numPointsOnPlane;
                    result.frontVerts[result.numFrontVerts++] = cwPolyPoints[i];
                    result.backVerts[result.numBackVerts++] = cwPolyPoints[i];
                }
            }

            // Exactly on plane?
            if (numPointsOnPlane == numPolyPoints) result.classifyResult = PlaneClassifyResult.OnPlane;
            // In front?
            else if (result.numFrontVerts == numPolyPoints && result.numBackVerts == numPointsOnPlane) result.classifyResult = PlaneClassifyResult.InFront;
            // Behind?
            else if (result.numBackVerts == numPolyPoints && result.numFrontVerts == numPointsOnPlane) result.classifyResult = PlaneClassifyResult.Behind;
            // Spanning
            else result.classifyResult = PlaneClassifyResult.Spanning;
        }
    }
}
#endif