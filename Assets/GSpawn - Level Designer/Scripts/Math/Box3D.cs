#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public enum Box3DCorner
    {
        FrontTopLeft = 0,
        FrontTopRight,
        FrontBottomRight,
        FrontBottomLeft,
        BackTopLeft,
        BackTopRight,
        BackBottomRight,
        BackBottomLeft
    }

    public enum Box3DFace
    {
        Front = 0,
        Back,
        Left,
        Right,
        Bottom,
        Top
    }

    public enum Box3DFaceAreaType
    {
        Invalid = 0,
        Quad,
        Line
    }

    public struct Box3DFaceAreaDesc
    {
        public Box3DFaceAreaType    areaType;
        public float                area;

        public Box3DFaceAreaDesc(Box3DFaceAreaType areaType, float area)
        {
            this.areaType = areaType;
            this.area = area;
        }

        public static Box3DFaceAreaDesc getInvalid()
        {
            return new Box3DFaceAreaDesc(Box3DFaceAreaType.Invalid, 0.0f);
        }
    }

    public struct Box3DFaceDesc
    {
        public Box3DFace    face;
        public Plane        plane;
        public Vector3      center;
        public float        width;
        public float        height;
        public Vector3      right;
        public Vector3      look;
    }

    public static class Box3D
    {
        private static List<Vector3>    _vector3Buffer  = new List<Vector3>();
        private static Box3DFace[]      _faces          = new Box3DFace[6];

        static Box3D()
        {
            _faces[(int)Box3DFace.Front] = (Box3DFace.Front);
            _faces[(int)Box3DFace.Back] = (Box3DFace.Back);
            _faces[(int)Box3DFace.Left] = (Box3DFace.Left);
            _faces[(int)Box3DFace.Right] = (Box3DFace.Right);
            _faces[(int)Box3DFace.Bottom] = (Box3DFace.Bottom);
            _faces[(int)Box3DFace.Top] = (Box3DFace.Top);
        }

        public static Box3DFace[] facesArrayCopy    { get { return _faces.Clone() as Box3DFace[]; } }
        public static Box3DFace[] facesArray        { get { return _faces; } }

        public static int setBoxFaceBit(int faceBits, Box3DFace boxFace)
        {
            return faceBits | (1 << (int)boxFace);
        }

        public static bool checkBoxFaceBit(int faceBits, Box3DFace boxFace)
        {
            return (faceBits & (1 << (int)boxFace)) != 0;
        }

        public static Box3DFace getNextFace(Box3DFace boxFace)
        {
            int newFace = ((int)boxFace + 1) % 6;
            return (Box3DFace)newFace;
        }

        public static Box3DFace getPreviousFace(Box3DFace boxFace)
        {
            int newFace = ((int)boxFace - 1);
            if (newFace < 0) newFace = 5;
            return (Box3DFace)newFace;
        }

        public static PlaneClassifyResult classifyAgainstPlane(List<Vector3> boxCorners, Plane plane)
        {
            int numInFront = 0;
            int numBehind = 0;

            const float onPlaneEps = 1e-6f;
            for (int ptIndex = 0; ptIndex < boxCorners.Count; ++ptIndex)
            {
                float dist = plane.GetDistanceToPoint(boxCorners[ptIndex]);
                if (dist < -onPlaneEps) ++numBehind;
                else if (dist > onPlaneEps) ++numInFront;
            }

            if (numInFront != 0 && numBehind != 0) return PlaneClassifyResult.Spanning;
            if (numInFront != 0 && numBehind == 0) return PlaneClassifyResult.InFront;
            if (numBehind != 0 && numInFront == 0) return PlaneClassifyResult.Behind;
            return PlaneClassifyResult.OnPlane;
        }

        public static PlaneClassifyResult classifyAgainstPlane(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Plane plane)
        {            
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;

            const float onPlaneEps = 1e-6f;
            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = plane.GetDistanceToPoint(_vector3Buffer[ptIndex]);
                if (dist < -onPlaneEps) ++numBehind;
                else if (dist > onPlaneEps) ++numInFront;
            }

            if (numInFront != 0 && numBehind != 0) return PlaneClassifyResult.Spanning;
            if (numInFront != 0 && numBehind == 0) return PlaneClassifyResult.InFront;
            if (numBehind != 0 && numInFront == 0) return PlaneClassifyResult.Behind;
            return PlaneClassifyResult.OnPlane;
        }

        public static PlaneClassifyResult classifyAgainstPlane(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Plane plane, float onPlaneEps)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;
;
            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = plane.GetDistanceToPoint(_vector3Buffer[ptIndex]);
                if (dist < -onPlaneEps) ++numBehind;
                else if (dist > onPlaneEps) ++numInFront;
            }

            if (numInFront != 0 && numBehind != 0) return PlaneClassifyResult.Spanning;
            if (numInFront != 0 && numBehind == 0) return PlaneClassifyResult.InFront;
            if (numBehind != 0 && numInFront == 0) return PlaneClassifyResult.Behind;
            return PlaneClassifyResult.OnPlane;
        }

        public static bool isSpanningOrSittingOnPlane(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Plane plane, float onPlaneEps)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;
 
            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = plane.GetDistanceToPoint(_vector3Buffer[ptIndex]);
                if (dist < -onPlaneEps) ++numBehind;
                else if (dist > onPlaneEps) ++numInFront;
            }

            PlaneClassifyResult classifyResult;
            if (numInFront != 0 && numBehind != 0) classifyResult = PlaneClassifyResult.Spanning;
            else if (numInFront != 0 && numBehind == 0) classifyResult = PlaneClassifyResult.InFront;
            else if (numBehind != 0 && numInFront == 0) classifyResult = PlaneClassifyResult.Behind;
            else classifyResult = PlaneClassifyResult.OnPlane;

            if (classifyResult == PlaneClassifyResult.Behind || classifyResult == PlaneClassifyResult.OnPlane) return false;
            if (classifyResult == PlaneClassifyResult.Spanning) return true;

            int closestInFront = plane.findIndexOfClosestPointInFront(_vector3Buffer);
            float d = plane.GetDistanceToPoint(_vector3Buffer[closestInFront]);
            return d <= onPlaneEps;
        }

        public static bool isSpanningOrOnOrInFrontOfPlane(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Plane plane, float onPlaneEps, float allowedInFrontAmount)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;

            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = plane.GetDistanceToPoint(_vector3Buffer[ptIndex]);
                if (dist < -onPlaneEps) ++numBehind;
                else if (dist > onPlaneEps) ++numInFront;
            }

            PlaneClassifyResult classifyResult;
            if (numInFront != 0 && numBehind != 0) classifyResult = PlaneClassifyResult.Spanning;
            else if (numInFront != 0 && numBehind == 0) classifyResult = PlaneClassifyResult.InFront;
            else if (numBehind != 0 && numInFront == 0) classifyResult = PlaneClassifyResult.Behind;
            else classifyResult = PlaneClassifyResult.OnPlane;

            if (classifyResult == PlaneClassifyResult.Behind || classifyResult == PlaneClassifyResult.OnPlane) return false;
            if (classifyResult == PlaneClassifyResult.Spanning) return true;

            int closestInFront = plane.findIndexOfClosestPointInFront(_vector3Buffer);
            float d = plane.GetDistanceToPoint(_vector3Buffer[closestInFront]);
            return d <= allowedInFrontAmount;
        }

        public static bool isSpanningOrSittingOnUnityTerrain(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Terrain terrain, float onTerrainEps)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;

            float terrainYPos = terrain.transform.position.y;
            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = terrain.getDistanceToPoint(terrainYPos, _vector3Buffer[ptIndex]);
                if (dist < -onTerrainEps) ++numBehind;
                else if (dist > onTerrainEps) ++numInFront;
            }

            PlaneClassifyResult classifyResult;
            if (numInFront != 0 && numBehind != 0) classifyResult = PlaneClassifyResult.Spanning;
            else if (numInFront != 0 && numBehind == 0) classifyResult = PlaneClassifyResult.InFront;
            else if (numBehind != 0 && numInFront == 0) classifyResult = PlaneClassifyResult.Behind;
            else classifyResult = PlaneClassifyResult.OnPlane;

            if (classifyResult == PlaneClassifyResult.Behind || classifyResult == PlaneClassifyResult.OnPlane) return false;
            if (classifyResult == PlaneClassifyResult.Spanning) return true;

            int closestAbove = terrain.findIndexOfClosestPointAbove(_vector3Buffer);
            float d = terrain.getDistanceToPoint(terrainYPos, _vector3Buffer[closestAbove]);
            return d <= onTerrainEps;
        }

        public static bool isSpanningOrOnOrInFrontOfUnityTerrain(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Terrain terrain, float onTerrainEps, float allowedInFrontAmount)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;

            float terrainYPos = terrain.transform.position.y;
            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = terrain.getDistanceToPoint(terrainYPos, _vector3Buffer[ptIndex]);
                if (dist < -onTerrainEps) ++numBehind;
                else if (dist > onTerrainEps) ++numInFront;
                else
                {
                    // When on the terrain, treat as being both in front and behind
                    ++numBehind;
                    ++numInFront;
                }
            }

            PlaneClassifyResult classifyResult;
            if (numInFront != 0 && numBehind != 0) classifyResult = PlaneClassifyResult.Spanning;
            else if (numInFront != 0 && numBehind == 0) classifyResult = PlaneClassifyResult.InFront;
            else if (numBehind != 0 && numInFront == 0) classifyResult = PlaneClassifyResult.Behind;
            else classifyResult = PlaneClassifyResult.OnPlane;
           
            if (classifyResult == PlaneClassifyResult.Behind || classifyResult == PlaneClassifyResult.OnPlane) return false;
            if (classifyResult == PlaneClassifyResult.Spanning) return true;

            int closestAbove = terrain.findIndexOfClosestPointAbove(_vector3Buffer);
            float d = terrain.getDistanceToPoint(terrainYPos, _vector3Buffer[closestAbove]);        
            return d <= allowedInFrontAmount;
        }

        public static bool isSpanningOrSittingOnTerrainMesh(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, GameObject gameObject, PluginMesh terrainMesh, float onTerrainEps)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;

            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = TerrainMeshUtil.getDistanceToPoint(gameObject, terrainMesh, _vector3Buffer[ptIndex]);
                if (dist < -onTerrainEps) ++numBehind;
                else if (dist > onTerrainEps) ++numInFront;
            }

            PlaneClassifyResult classifyResult;
            if (numInFront != 0 && numBehind != 0) classifyResult = PlaneClassifyResult.Spanning;
            else if (numInFront != 0 && numBehind == 0) classifyResult = PlaneClassifyResult.InFront;
            else if (numBehind != 0 && numInFront == 0) classifyResult = PlaneClassifyResult.Behind;
            else classifyResult = PlaneClassifyResult.OnPlane;

            if (classifyResult == PlaneClassifyResult.Behind || classifyResult == PlaneClassifyResult.OnPlane) return false;
            if (classifyResult == PlaneClassifyResult.Spanning) return true;

            int closestAbove = TerrainMeshUtil.findIndexOfClosestPointAbove(gameObject, terrainMesh, _vector3Buffer);
            float d = TerrainMeshUtil.getDistanceToPoint(gameObject, terrainMesh, _vector3Buffer[closestAbove]);
            return d <= onTerrainEps;
        }

        public static bool isSpanningOrOnOrInFrontOfTerrainMesh(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, GameObject gameObject, PluginMesh terrainMesh, float onTerrainEps, float allowedInFrontAmount)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;

            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = TerrainMeshUtil.getDistanceToPoint(gameObject, terrainMesh, _vector3Buffer[ptIndex]);
                if (dist < -onTerrainEps) ++numBehind;
                else if (dist > onTerrainEps) ++numInFront;
            }

            PlaneClassifyResult classifyResult;
            if (numInFront != 0 && numBehind != 0) classifyResult = PlaneClassifyResult.Spanning;
            else if (numInFront != 0 && numBehind == 0) classifyResult = PlaneClassifyResult.InFront;
            else if (numBehind != 0 && numInFront == 0) classifyResult = PlaneClassifyResult.Behind;
            else classifyResult = PlaneClassifyResult.OnPlane;

            if (classifyResult == PlaneClassifyResult.Behind || classifyResult == PlaneClassifyResult.OnPlane) return false;
            if (classifyResult == PlaneClassifyResult.Spanning) return true;

            int closestAbove = TerrainMeshUtil.findIndexOfClosestPointAbove(gameObject, terrainMesh, _vector3Buffer);
            float d = TerrainMeshUtil.getDistanceToPoint(gameObject, terrainMesh, _vector3Buffer[closestAbove]);
            return d <= allowedInFrontAmount;
        }

        public static bool isSpanningOrSittingOnSphericalMesh(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, GameObject gameObject, PluginMesh sphericalMesh, float onSphereEps)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;

            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = SphericalMeshUtil.getDistanceToPoint(gameObject, sphericalMesh, _vector3Buffer[ptIndex]);
                if (dist < -onSphereEps) ++numBehind;
                else if (dist > onSphereEps) ++numInFront;
            }

            PlaneClassifyResult classifyResult;
            if (numInFront != 0 && numBehind != 0) classifyResult = PlaneClassifyResult.Spanning;
            else if (numInFront != 0 && numBehind == 0) classifyResult = PlaneClassifyResult.InFront;
            else if (numBehind != 0 && numInFront == 0) classifyResult = PlaneClassifyResult.Behind;
            else classifyResult = PlaneClassifyResult.OnPlane;

            if (classifyResult == PlaneClassifyResult.Behind || classifyResult == PlaneClassifyResult.OnPlane) return false;
            if (classifyResult == PlaneClassifyResult.Spanning) return true;

            int closestAbove = SphericalMeshUtil.findIndexOfClosestPointAbove(gameObject, sphericalMesh, _vector3Buffer);
            float d = SphericalMeshUtil.getDistanceToPoint(gameObject, sphericalMesh, _vector3Buffer[closestAbove]);
            return d <= onSphereEps;
        }

        public static bool isSpanningOrSittingOnSphericalMesh(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, GameObject gameObject, float onSphereEps)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;

            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = SphericalMeshUtil.getDistanceToPoint(gameObject, _vector3Buffer[ptIndex]);
                if (dist < -onSphereEps) ++numBehind;
                else if (dist > onSphereEps) ++numInFront;
            }

            PlaneClassifyResult classifyResult;
            if (numInFront != 0 && numBehind != 0) classifyResult = PlaneClassifyResult.Spanning;
            else if (numInFront != 0 && numBehind == 0) classifyResult = PlaneClassifyResult.InFront;
            else if (numBehind != 0 && numInFront == 0) classifyResult = PlaneClassifyResult.Behind;
            else classifyResult = PlaneClassifyResult.OnPlane;

            if (classifyResult == PlaneClassifyResult.Behind || classifyResult == PlaneClassifyResult.OnPlane) return false;
            if (classifyResult == PlaneClassifyResult.Spanning) return true;

            int closestAbove = SphericalMeshUtil.findIndexOfClosestPointAbove(gameObject, _vector3Buffer);
            float d = SphericalMeshUtil.getDistanceToPoint(gameObject, _vector3Buffer[closestAbove]);
            return d <= onSphereEps;
        }

        public static bool isSpanningOrOnOrInFrontOfSphericalMesh(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, GameObject gameObject, float onSphereEps, float allowedInFrontAmount)
        {
            calcCorners(boxCenter, boxSize, boxRotation, _vector3Buffer, false);

            int numInFront  = 0;
            int numBehind   = 0;

            for (int ptIndex = 0; ptIndex < _vector3Buffer.Count; ++ptIndex)
            {
                float dist = SphericalMeshUtil.getDistanceToPoint(gameObject, _vector3Buffer[ptIndex]);
                if (dist < -onSphereEps) ++numBehind;
                else if (dist > onSphereEps) ++numInFront;
            }

            PlaneClassifyResult classifyResult;
            if (numInFront != 0 && numBehind != 0) classifyResult = PlaneClassifyResult.Spanning;
            else if (numInFront != 0 && numBehind == 0) classifyResult = PlaneClassifyResult.InFront;
            else if (numBehind != 0 && numInFront == 0) classifyResult = PlaneClassifyResult.Behind;
            else classifyResult = PlaneClassifyResult.OnPlane;

            if (classifyResult == PlaneClassifyResult.Behind || classifyResult == PlaneClassifyResult.OnPlane) return false;
            if (classifyResult == PlaneClassifyResult.Spanning) return true;

            int closestAbove = SphericalMeshUtil.findIndexOfClosestPointAbove(gameObject, _vector3Buffer);
            float d = SphericalMeshUtil.getDistanceToPoint(gameObject, _vector3Buffer[closestAbove]);
            return d <= allowedInFrontAmount;
        }

        public static int getFaceAxisIndex(Box3DFace face)
        {
            if (face == Box3DFace.Top || face == Box3DFace.Bottom) return 1;
            if (face == Box3DFace.Left || face == Box3DFace.Right) return 0;
            if (face == Box3DFace.Back || face == Box3DFace.Front) return 2;

            return -1;
        }

        public static Box3DFace findFaceClosestToPoint(Vector3 point, Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation)
        {
            float minDist       = float.MaxValue;
            Box3DFace bestFace  = Box3DFace.Front;

            foreach (var face in _faces)
            {
                Plane facePlane = Box3D.calcFacePlane(boxCenter, boxSize, boxRotation, face);
                float dist      = facePlane.absDistanceToPoint(point);
                if (dist < minDist)
                {
                    bestFace    = face;
                    minDist     = dist;
                }
            }

            return bestFace;
        }

        public static bool firstNonZeroAreaFace(Vector3 boxSize, out Box3DFace boxFace)
        {
            boxFace = Box3DFace.Front;
            foreach (var face in _faces)
            {
                Box3DFaceAreaDesc areaDesc = getFaceAreaDesc(boxSize, face);
                if (areaDesc.area != 0.0f)
                {
                    boxFace = face;
                    return true;
                }
            }

            return false;
        }

        public static Box3DFaceDesc getFaceDesc(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Box3DFace boxFace)
        {
            Box3DFaceDesc desc  = new Box3DFaceDesc();
            desc.face           = boxFace;

            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = boxRotation * Vector3.right;
            Vector3 upAxis      = boxRotation * Vector3.up;
            Vector3 lookAxis    = boxRotation * Vector3.forward;

            switch (boxFace)
            {
                case Box3DFace.Front:

                    desc.plane  = new Plane(-lookAxis, boxCenter - lookAxis * extents.z);
                    desc.center = boxCenter - lookAxis * extents.z;
                    desc.width  = boxSize.x;
                    desc.height = boxSize.y;
                    desc.look   = upAxis;
                    desc.right  = Vector3.Cross(desc.plane.normal, desc.look).normalized;
                    break;

                case Box3DFace.Back:

                    desc.plane  = new Plane(lookAxis, boxCenter + lookAxis * extents.z);
                    desc.center = boxCenter + lookAxis * extents.z;
                    desc.width  = boxSize.x;
                    desc.height = boxSize.y;
                    desc.look   = upAxis;
                    desc.right  = Vector3.Cross(desc.plane.normal, desc.look).normalized;
                    break;

                case Box3DFace.Left:

                    desc.plane  = new Plane(-rightAxis, boxCenter - rightAxis * extents.x);
                    desc.center = boxCenter - rightAxis * extents.x;
                    desc.width  = boxSize.z;
                    desc.height = boxSize.y;
                    desc.look   = upAxis;
                    desc.right  = Vector3.Cross(desc.plane.normal, desc.look).normalized;
                    break;

                case Box3DFace.Right:

                    desc.plane  = new Plane(rightAxis, boxCenter + rightAxis * extents.x);
                    desc.center = boxCenter + rightAxis * extents.x;
                    desc.width  = boxSize.z;
                    desc.height = boxSize.y;
                    desc.look   = upAxis;
                    desc.right  = Vector3.Cross(desc.plane.normal, desc.look).normalized;
                    break;

                case Box3DFace.Bottom:

                    desc.plane  = new Plane(-upAxis, boxCenter - upAxis * extents.y);
                    desc.center = boxCenter - upAxis * extents.y;
                    desc.width  = boxSize.x;
                    desc.height = boxSize.z;
                    desc.look   = -lookAxis;
                    desc.right  = Vector3.Cross(desc.plane.normal, desc.look).normalized;
                    break;

                default:

                    desc.plane  = new Plane(upAxis, boxCenter + upAxis * extents.y);
                    desc.center = boxCenter + upAxis * extents.y;
                    desc.width  = boxSize.x;
                    desc.height = boxSize.z;
                    desc.look   = lookAxis;
                    desc.right  = Vector3.Cross(desc.plane.normal, desc.look).normalized;
                    break;
            }

            return desc;
        }

        public static Box3DFace findMostAlignedFace(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Vector3 direction)
        {
            int bestFaceIndex   = 0;
            float bestScore     = Vector3.Dot(direction, calcFaceNormal(boxCenter, boxSize, boxRotation, _faces[0]));

            for (int faceIndex = 1; faceIndex < _faces.Length; ++faceIndex)
            {
                float score = Vector3.Dot(direction, calcFaceNormal(boxCenter, boxSize, boxRotation, _faces[faceIndex]));
                if (score > bestScore)
                {
                    bestScore       = score;
                    bestFaceIndex   = faceIndex;
                }
            }

            return _faces[bestFaceIndex];
        }

        public static Vector2 getFaceSize(Vector3 boxSize, Box3DFace boxFace)
        {
            int faceAxisIndex = getFaceAxisIndex(boxFace);
            return new Vector2(boxSize[(faceAxisIndex + 1) % 3], boxSize[(faceAxisIndex + 2) % 3]);
        }

        public static Box3DFaceAreaDesc getFaceAreaDesc(Vector3 boxSize, Box3DFace boxFace)
        {
            if (boxFace == Box3DFace.Front || boxFace == Box3DFace.Back)
            {
                float area = boxSize.x * boxSize.y;
                if (area < 1e-6f) return new Box3DFaceAreaDesc(Box3DFaceAreaType.Line, area);
                return new Box3DFaceAreaDesc(Box3DFaceAreaType.Quad, area);
            }
            else if (boxFace == Box3DFace.Left || boxFace == Box3DFace.Right)
            {
                float area = boxSize.y * boxSize.z;
                if (area < 1e-6f) return new Box3DFaceAreaDesc(Box3DFaceAreaType.Line, area);
                return new Box3DFaceAreaDesc(Box3DFaceAreaType.Quad, area);
            }
            else
            {
                float area = boxSize.x * boxSize.z;
                if (area < 1e-6f) return new Box3DFaceAreaDesc(Box3DFaceAreaType.Line, area);
                return new Box3DFaceAreaDesc(Box3DFaceAreaType.Quad, area);
            }
        }

        public static Plane calcFacePlane(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Box3DFace boxFace)
        {
            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = boxRotation * Vector3.right;
            Vector3 upAxis      = boxRotation * Vector3.up;
            Vector3 lookAxis    = boxRotation * Vector3.forward;

            switch (boxFace)
            {
                case Box3DFace.Front:

                    return new Plane(-lookAxis, boxCenter - lookAxis * extents.z);

                case Box3DFace.Back:

                    return new Plane(lookAxis, boxCenter + lookAxis * extents.z);

                case Box3DFace.Left:

                    return new Plane(-rightAxis, boxCenter - rightAxis * extents.x);

                case Box3DFace.Right:

                    return new Plane(rightAxis, boxCenter + rightAxis * extents.x);

                case Box3DFace.Bottom:

                    return new Plane(-upAxis, boxCenter - upAxis * extents.y);

                default:

                    return new Plane(upAxis, boxCenter + upAxis * extents.y);
            }
        }

        public static Plane calcFacePlane(Vector3 boxCenter, Vector3 boxSize, Box3DFace boxFace)
        {
            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = Vector3.right;
            Vector3 upAxis      = Vector3.up;
            Vector3 lookAxis    = Vector3.forward;

            switch (boxFace)
            {
                case Box3DFace.Front:

                    return new Plane(-lookAxis, boxCenter - lookAxis * extents.z);

                case Box3DFace.Back:

                    return new Plane(lookAxis, boxCenter + lookAxis * extents.z);

                case Box3DFace.Left:

                    return new Plane(-rightAxis, boxCenter - rightAxis * extents.x);

                case Box3DFace.Right:

                    return new Plane(rightAxis, boxCenter + rightAxis * extents.x);

                case Box3DFace.Bottom:

                    return new Plane(-upAxis, boxCenter - upAxis * extents.y);

                default:

                    return new Plane(upAxis, boxCenter + upAxis * extents.y);
            }
        }

        public static Vector3 calcFaceNormal(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Box3DFace boxFace)
        {
            Vector3 rightAxis   = boxRotation * Vector3.right;
            Vector3 upAxis      = boxRotation * Vector3.up;
            Vector3 lookAxis    = boxRotation * Vector3.forward;

            switch (boxFace)
            {
                case Box3DFace.Front:

                    return -lookAxis;

                case Box3DFace.Back:

                    return lookAxis;

                case Box3DFace.Left:

                    return -rightAxis;

                case Box3DFace.Right:

                    return rightAxis;

                case Box3DFace.Bottom:

                    return -upAxis;

                default:

                    return upAxis;
            }
        }

        public static Vector3 calcFaceCenter(Vector3 boxCenter, Vector3 boxSize, Box3DFace boxFace)
        {
            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = Vector3.right;
            Vector3 upAxis      = Vector3.up;
            Vector3 lookAxis    = Vector3.forward;

            switch (boxFace)
            {
                case Box3DFace.Front:

                    return boxCenter - lookAxis * extents.z;

                case Box3DFace.Back:

                    return boxCenter + lookAxis * extents.z;

                case Box3DFace.Left:

                    return boxCenter - rightAxis * extents.x;

                case Box3DFace.Right:

                    return boxCenter + rightAxis * extents.x;

                case Box3DFace.Bottom:

                    return boxCenter - upAxis * extents.y;

                default:

                    return boxCenter + upAxis * extents.y;
            }
        }

        public static Vector3 calcFaceCenter(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Box3DFace boxFace)
        {
            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = boxRotation * Vector3.right;
            Vector3 upAxis      = boxRotation * Vector3.up;
            Vector3 lookAxis    = boxRotation * Vector3.forward;

            switch(boxFace)
            {
                case Box3DFace.Front:

                    return boxCenter - lookAxis * extents.z;

                case Box3DFace.Back:

                    return boxCenter + lookAxis * extents.z;

                case Box3DFace.Left:

                    return boxCenter - rightAxis * extents.x;

                case Box3DFace.Right:

                    return boxCenter + rightAxis * extents.x;

                case Box3DFace.Bottom:

                    return boxCenter - upAxis * extents.y;

                default:

                    return boxCenter + upAxis * extents.y;
            }
        }

        public static Vector3 getFaceAxisMask(Box3DFace boxFace)
        {
            if (boxFace == Box3DFace.Left || boxFace == Box3DFace.Right) return new Vector3(0.0f, 1.0f, 1.0f);
            if (boxFace == Box3DFace.Bottom || boxFace == Box3DFace.Top) return new Vector3(1.0f, 0.0f, 1.0f);
            return new Vector3(1.0f, 1.0f, 0.0f);
        }

        public static void calcFaceCorners(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Box3DFace boxFace, Vector3[] faceCorners)
        {
            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = boxRotation * Vector3.right;
            Vector3 upAxis      = boxRotation * Vector3.up;
            Vector3 lookAxis    = boxRotation * Vector3.forward;
            Vector3 faceCenter  = Vector3.zero;

            switch (boxFace)
            {
                case Box3DFace.Left:

                    faceCenter      = boxCenter - rightAxis * extents.x;
                    faceCorners[0]  = (faceCenter + upAxis * extents.y + lookAxis * extents.z);
                    faceCorners[1]  = (faceCenter - upAxis * extents.y + lookAxis * extents.z);
                    faceCorners[2]  = (faceCenter - upAxis * extents.y - lookAxis * extents.z);
                    faceCorners[3]  = (faceCenter + upAxis * extents.y - lookAxis * extents.z);
                    return;

                case Box3DFace.Right:

                    faceCenter      = boxCenter + rightAxis * extents.x;
                    faceCorners[0]  = (faceCenter + upAxis * extents.y + lookAxis * extents.z);
                    faceCorners[1]  = (faceCenter - upAxis * extents.y + lookAxis * extents.z);
                    faceCorners[2]  = (faceCenter - upAxis * extents.y - lookAxis * extents.z);
                    faceCorners[3]  = (faceCenter + upAxis * extents.y - lookAxis * extents.z);
                    return;

                case Box3DFace.Bottom:

                    faceCenter      = boxCenter - upAxis * extents.y;
                    faceCorners[0]  = (faceCenter + rightAxis * extents.x + lookAxis * extents.z);
                    faceCorners[1]  = (faceCenter + rightAxis * extents.x - lookAxis * extents.z);
                    faceCorners[2]  = (faceCenter - rightAxis * extents.x - lookAxis * extents.z);
                    faceCorners[3]  = (faceCenter - rightAxis * extents.x + lookAxis * extents.z);
                    return;

                case Box3DFace.Top:

                    faceCenter      = boxCenter + upAxis * extents.y;
                    faceCorners[0]  = (faceCenter + rightAxis * extents.x + lookAxis * extents.z);
                    faceCorners[1]  = (faceCenter + rightAxis * extents.x - lookAxis * extents.z);
                    faceCorners[2]  = (faceCenter - rightAxis * extents.x - lookAxis * extents.z);
                    faceCorners[3]  = (faceCenter - rightAxis * extents.x + lookAxis * extents.z);
                    return;

                case Box3DFace.Front:

                    faceCenter      = boxCenter - lookAxis * extents.z;
                    faceCorners[0]  = (faceCenter + rightAxis * extents.x + upAxis * extents.y);
                    faceCorners[1]  = (faceCenter + rightAxis * extents.x - upAxis * extents.y);
                    faceCorners[2]  = (faceCenter - rightAxis * extents.x - upAxis * extents.y);
                    faceCorners[3]  = (faceCenter - rightAxis * extents.x + upAxis * extents.y);
                    return;

                default:

                    faceCenter      = boxCenter + lookAxis * extents.z;
                    faceCorners[0]  = (faceCenter + rightAxis * extents.x + upAxis * extents.y);
                    faceCorners[1]  = (faceCenter + rightAxis * extents.x - upAxis * extents.y);
                    faceCorners[2]  = (faceCenter - rightAxis * extents.x - upAxis * extents.y);
                    faceCorners[3]  = (faceCenter - rightAxis * extents.x + upAxis * extents.y);
                    return;
            }
        }

        public static void calcFaceCorners(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Box3DFace boxFace, List<Vector3> faceCorners)
        {
            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = boxRotation * Vector3.right;
            Vector3 upAxis      = boxRotation * Vector3.up;
            Vector3 lookAxis    = boxRotation * Vector3.forward;

            faceCorners.Clear();
            Vector3 faceCenter  = Vector3.zero;

            switch(boxFace)
            {
                case Box3DFace.Left:

                    faceCenter = boxCenter - rightAxis * extents.x;
                    faceCorners.Add(faceCenter + upAxis * extents.y + lookAxis * extents.z);
                    faceCorners.Add(faceCenter - upAxis * extents.y + lookAxis * extents.z);
                    faceCorners.Add(faceCenter - upAxis * extents.y - lookAxis * extents.z);
                    faceCorners.Add(faceCenter + upAxis * extents.y - lookAxis * extents.z);
                    return;

                case Box3DFace.Right:

                    faceCenter = boxCenter + rightAxis * extents.x;
                    faceCorners.Add(faceCenter + upAxis * extents.y + lookAxis * extents.z);
                    faceCorners.Add(faceCenter - upAxis * extents.y + lookAxis * extents.z);
                    faceCorners.Add(faceCenter - upAxis * extents.y - lookAxis * extents.z);
                    faceCorners.Add(faceCenter + upAxis * extents.y - lookAxis * extents.z);
                    return;

                case Box3DFace.Bottom:

                    faceCenter = boxCenter - upAxis * extents.y;
                    faceCorners.Add(faceCenter + rightAxis * extents.x + lookAxis * extents.z);
                    faceCorners.Add(faceCenter + rightAxis * extents.x - lookAxis * extents.z);
                    faceCorners.Add(faceCenter - rightAxis * extents.x - lookAxis * extents.z);
                    faceCorners.Add(faceCenter - rightAxis * extents.x + lookAxis * extents.z);
                    return;

                case Box3DFace.Top:

                    faceCenter = boxCenter + upAxis * extents.y;
                    faceCorners.Add(faceCenter + rightAxis * extents.x + lookAxis * extents.z);
                    faceCorners.Add(faceCenter + rightAxis * extents.x - lookAxis * extents.z);
                    faceCorners.Add(faceCenter - rightAxis * extents.x - lookAxis * extents.z);
                    faceCorners.Add(faceCenter - rightAxis * extents.x + lookAxis * extents.z);
                    return;

                case Box3DFace.Front:

                    faceCenter = boxCenter - lookAxis * extents.z;
                    faceCorners.Add(faceCenter + rightAxis * extents.x + upAxis * extents.y);
                    faceCorners.Add(faceCenter + rightAxis * extents.x - upAxis * extents.y);
                    faceCorners.Add(faceCenter - rightAxis * extents.x - upAxis * extents.y);
                    faceCorners.Add(faceCenter - rightAxis * extents.x + upAxis * extents.y);
                    return;

                default:

                    faceCenter = boxCenter + lookAxis * extents.z;
                    faceCorners.Add(faceCenter + rightAxis * extents.x + upAxis * extents.y);
                    faceCorners.Add(faceCenter + rightAxis * extents.x - upAxis * extents.y);
                    faceCorners.Add(faceCenter - rightAxis * extents.x - upAxis * extents.y);
                    faceCorners.Add(faceCenter - rightAxis * extents.x + upAxis * extents.y);
                    return;
            }
        }

        public static void calcFaceCenterAndCorners(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Box3DFace boxFace, List<Vector3> centerAndCorners)
        {
            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = boxRotation * Vector3.right;
            Vector3 upAxis      = boxRotation * Vector3.up;
            Vector3 lookAxis    = boxRotation * Vector3.forward;

            centerAndCorners.Clear();
            Vector3 faceCenter  = Vector3.zero;

            switch (boxFace)
            {
                case Box3DFace.Left:

                    faceCenter = boxCenter - rightAxis * extents.x;
                    centerAndCorners.Add(faceCenter);
                    centerAndCorners.Add(faceCenter + upAxis * extents.y + lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter - upAxis * extents.y + lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter - upAxis * extents.y - lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter + upAxis * extents.y - lookAxis * extents.z);
                    return;

                case Box3DFace.Right:

                    faceCenter = boxCenter + rightAxis * extents.x;
                    centerAndCorners.Add(faceCenter);
                    centerAndCorners.Add(faceCenter + upAxis * extents.y + lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter - upAxis * extents.y + lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter - upAxis * extents.y - lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter + upAxis * extents.y - lookAxis * extents.z);
                    return;

                case Box3DFace.Bottom:

                    faceCenter = boxCenter - upAxis * extents.y;
                    centerAndCorners.Add(faceCenter);
                    centerAndCorners.Add(faceCenter + rightAxis * extents.x + lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter + rightAxis * extents.x - lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter - rightAxis * extents.x - lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter - rightAxis * extents.x + lookAxis * extents.z);
                    return;

                case Box3DFace.Top:

                    faceCenter = boxCenter + upAxis * extents.y;
                    centerAndCorners.Add(faceCenter);
                    centerAndCorners.Add(faceCenter + rightAxis * extents.x + lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter + rightAxis * extents.x - lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter - rightAxis * extents.x - lookAxis * extents.z);
                    centerAndCorners.Add(faceCenter - rightAxis * extents.x + lookAxis * extents.z);
                    return;

                case Box3DFace.Front:

                    faceCenter = boxCenter - lookAxis * extents.z;
                    centerAndCorners.Add(faceCenter);
                    centerAndCorners.Add(faceCenter + rightAxis * extents.x + upAxis * extents.y);
                    centerAndCorners.Add(faceCenter + rightAxis * extents.x - upAxis * extents.y);
                    centerAndCorners.Add(faceCenter - rightAxis * extents.x - upAxis * extents.y);
                    centerAndCorners.Add(faceCenter - rightAxis * extents.x + upAxis * extents.y);
                    return;

                default:

                    faceCenter = boxCenter + lookAxis * extents.z;
                    centerAndCorners.Add(faceCenter);
                    centerAndCorners.Add(faceCenter + rightAxis * extents.x + upAxis * extents.y);
                    centerAndCorners.Add(faceCenter + rightAxis * extents.x - upAxis * extents.y);
                    centerAndCorners.Add(faceCenter - rightAxis * extents.x - upAxis * extents.y);
                    centerAndCorners.Add(faceCenter - rightAxis * extents.x + upAxis * extents.y);
                    return;
            }
        }

        public static void calcCorners(Vector3 boxCenter, Vector3 boxSize, List<Vector3> corners, bool append)
        {
            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = Vector3.right;
            Vector3 upAxis      = Vector3.up;
            Vector3 lookAxis    = Vector3.forward;

            if (!append) corners.Clear();

            Vector3 rightOffset     = rightAxis * extents.x;
            Vector3 upOffset        = upAxis * extents.y;

            Vector3 faceCenter      = boxCenter - lookAxis * extents.z;
            Vector3 leftOfCenter    = faceCenter - rightOffset;
            Vector3 rightOfCenter   = faceCenter + rightOffset;

            corners.Add(leftOfCenter + upOffset);           // FrontTopLeft
            corners.Add(rightOfCenter + upOffset);          // FrontTopRight
            corners.Add(rightOfCenter - upOffset);          // FrontBottomRight
            corners.Add(leftOfCenter - upOffset);           // FrontBottomLeft

            faceCenter = boxCenter + lookAxis * extents.z;
            leftOfCenter = faceCenter - rightOffset;
            rightOfCenter = faceCenter + rightOffset;

            corners.Add(rightOfCenter + upOffset);          // BackTopLeft
            corners.Add(leftOfCenter + upOffset);           // BackTopRight
            corners.Add(leftOfCenter - upOffset);           // BackBottomRight
            corners.Add(rightOfCenter - upOffset);          // BackBottomLeft
        }

        public static void calcCorners(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, List<Vector3> corners, bool append)
        {
            Vector3 extents         = boxSize * 0.5f;
            Vector3 rightAxis       = boxRotation * Vector3.right;
            Vector3 upAxis          = boxRotation * Vector3.up;
            Vector3 lookAxis        = boxRotation * Vector3.forward;

            if (!append) corners.Clear();

            Vector3 rightOffset     = rightAxis * extents.x;
            Vector3 upOffset        = upAxis * extents.y;

            Vector3 faceCenter      = boxCenter - lookAxis * extents.z;
            Vector3 leftOfCenter    = faceCenter - rightOffset;
            Vector3 rightOfCenter   = faceCenter + rightOffset;

            corners.Add(leftOfCenter + upOffset);           // FrontTopLeft
            corners.Add(rightOfCenter + upOffset);          // FrontTopRight
            corners.Add(rightOfCenter - upOffset);          // FrontBottomRight
            corners.Add(leftOfCenter - upOffset);           // FrontBottomLeft

            faceCenter              = boxCenter + lookAxis * extents.z;
            leftOfCenter            = faceCenter - rightOffset;
            rightOfCenter           = faceCenter + rightOffset;

            corners.Add(rightOfCenter + upOffset);          // BackTopLeft
            corners.Add(leftOfCenter + upOffset);           // BackTopRight
            corners.Add(leftOfCenter - upOffset);           // BackBottomRight
            corners.Add(rightOfCenter - upOffset);          // BackBottomLeft
        }

        public static Vector3 calcCorner(Vector3 boxCenter, Vector3 boxSize, Quaternion boxRotation, Box3DCorner boxCorner)
        {
            Vector3 extents     = boxSize * 0.5f;
            Vector3 rightAxis   = boxRotation * Vector3.right;
            Vector3 upAxis      = boxRotation * Vector3.up;
            Vector3 lookAxis    = boxRotation * Vector3.forward;

            if ((int)boxCorner <= (int)Box3DCorner.FrontBottomLeft)
            {
                Vector3 faceCenter = boxCenter - lookAxis * extents.z;
                if (boxCorner == Box3DCorner.FrontTopLeft) return faceCenter - rightAxis * extents.x + upAxis * extents.y;
                else if (boxCorner == Box3DCorner.FrontTopRight) return faceCenter + rightAxis * extents.x + upAxis * extents.y;
                else if (boxCorner == Box3DCorner.FrontBottomRight) return faceCenter + rightAxis * extents.x - upAxis * extents.y;
                else return faceCenter - rightAxis * extents.x - upAxis * extents.y;
            }
            else
            {
                Vector3 faceCenter = boxCenter - lookAxis * extents.z;
                if (boxCorner == Box3DCorner.BackTopLeft) return faceCenter + rightAxis * extents.x + upAxis * extents.y;
                else if (boxCorner == Box3DCorner.BackTopRight) return faceCenter - rightAxis * extents.x + upAxis * extents.y;
                else if (boxCorner == Box3DCorner.BackBottomRight) return faceCenter - rightAxis * extents.x - upAxis * extents.y;
                else return faceCenter + rightAxis * extents.x - upAxis * extents.y;
            }
        }

        public static void transform(Vector3 boxCenter, Vector3 boxSize, Matrix4x4 transformMatrix, out Vector3 newBoxCenter, out Vector3 newBoxSize)
        {
            Vector3 rightAxis       = transformMatrix.GetColumn(0);
            Vector3 upAxis          = transformMatrix.GetColumn(1);
            Vector3 lookAxis        = transformMatrix.GetColumn(2);

            Vector3 extents         = boxSize * 0.5f;
            Vector3 newExtentsRight = rightAxis * extents.x;
            Vector3 newExtentsUp    = upAxis * extents.y;
            Vector3 newExtentsLook  = lookAxis * extents.z;

            float newExtentX        = Mathf.Abs(newExtentsRight.x) + Mathf.Abs(newExtentsUp.x) + Mathf.Abs(newExtentsLook.x);
            float newExtentY        = Mathf.Abs(newExtentsRight.y) + Mathf.Abs(newExtentsUp.y) + Mathf.Abs(newExtentsLook.y);
            float newExtentZ        = Mathf.Abs(newExtentsRight.z) + Mathf.Abs(newExtentsUp.z) + Mathf.Abs(newExtentsLook.z);

            newBoxCenter            = transformMatrix.MultiplyPoint(boxCenter);
            newBoxSize              = new Vector3(newExtentX, newExtentY, newExtentZ) * 2.0f;
        }
    }
}
#endif