#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public static class Symmetry
    {
        public struct MirroredRotation
        {
            public Quaternion   rotation;
            public Vector3      axesScaleSign;

            public MirroredRotation(Quaternion rotation, Vector3 axesScaleSign)
            {
                this.rotation = rotation;
                this.axesScaleSign = axesScaleSign;
            }

            public static MirroredRotation identity { get { return new MirroredRotation() { rotation = Quaternion.identity, axesScaleSign = Vector3.one }; } }
        }

        public struct MirroredOBB
        {
            public OBB              obb;
            public MirroredRotation mirroredRotation;

            public MirroredOBB(OBB obb, MirroredRotation mirroredRotation)
            {
                this.obb = obb;
                this.mirroredRotation = mirroredRotation;
            }
        }

        public static Vector3Int mirror3DGridCell(Vector3Int cell, Vector3Int mirrorCell, PlaneId mirrorPlaneId)
        {
            Vector3Int toMirrorPos = mirrorCell - cell;
            switch (mirrorPlaneId)
            {
                case PlaneId.XY:

                    return new Vector3Int(cell.x, cell.y, cell.z + toMirrorPos.z * 2);

                case PlaneId.YZ:

                    return new Vector3Int(cell.x + toMirrorPos.x * 2, cell.y, cell.z);

                case PlaneId.ZX:

                    return new Vector3Int(cell.x, cell.y + toMirrorPos.y * 2, cell.z);
            }

            return cell;
        }

        public static Vector3 mirrorPosition(Vector3 position, Plane mirrorPlane)
        {
            float distanceToPt = mirrorPlane.GetDistanceToPoint(position);
            return position - mirrorPlane.normal * distanceToPt * 2.0f;
        }

        public static Vector3 mirrorNormal(Vector3 normal, Plane mirrorPlane)
        {
            Plane mirrorPlaneAtOrigin   = new Plane(mirrorPlane.normal, 0.0f);
            Vector3 mirroredNormal      = mirrorPosition(normal, mirrorPlaneAtOrigin).normalized;

            return mirroredNormal;
        }

        public static OBB mirrorOBB(OBB obb, bool mirrorRotation, Plane mirrorPlane)
        {
            OBB mirroredOBB     = new OBB(obb);
            mirroredOBB.center  = mirrorPosition(mirroredOBB.center, mirrorPlane);

            if (mirrorRotation)
            {
                MirroredRotation mirroredRotation = Symmetry.mirrorRotation(mirroredOBB.rotationMatrix, Vector3.one, mirrorPlane);
                mirroredOBB.rotation = mirroredRotation.rotation;
            }

            return mirroredOBB;
        }

        public static MirroredOBB mirrorOBB(MirroredOBB obb, bool mirrorRotation, Plane mirrorPlane)
        {
            MirroredOBB mirroredOBB     = obb;
            mirroredOBB.obb.center      = mirrorPosition(obb.obb.center, mirrorPlane);

            if (mirrorRotation)
            {
                mirroredOBB.mirroredRotation = Symmetry.mirrorRotation(mirroredOBB.obb.rotationMatrix, obb.mirroredRotation.axesScaleSign, mirrorPlane);
                mirroredOBB.obb.rotation = mirroredOBB.mirroredRotation.rotation;
            }

            return mirroredOBB;
        }

        public static MirroredRotation mirrorRotation(Matrix4x4 rotationMtx, Vector3 axesSigns, Plane mirrorPlane)
        {
            Vector3 right   = rotationMtx.getNormalizedAxis(0) * axesSigns.x;
            Vector3 up      = rotationMtx.getNormalizedAxis(1) * axesSigns.y;
            Vector3 look    = rotationMtx.getNormalizedAxis(2) * axesSigns.z;

            right           = mirrorNormal(right, mirrorPlane);
            up              = mirrorNormal(up, mirrorPlane);
            look            = mirrorNormal(look, mirrorPlane);

            Quaternion newRotation  = Quaternion.LookRotation(look, up);
            Vector3 resultRightAxis = Vector3.Cross(up, look);

            Vector3 axesScaleSign   = Vector3.one;
            float dot               = Vector3.Dot(resultRightAxis, right);
            if (dot <= 0.0f) axesScaleSign[0] = -1.0f;

            return new MirroredRotation(newRotation, axesScaleSign);
        }
    }
}
#endif