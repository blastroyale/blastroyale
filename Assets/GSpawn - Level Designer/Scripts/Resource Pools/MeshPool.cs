#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class MeshPool : Singleton<MeshPool>
    {
        private Mesh _unitCircleXY;
        private Mesh _unitWireCircleXY;
        private Mesh _unitCoordSystem;
        private Mesh _unitBox;
        private Mesh _unitWireBox;
        private Mesh _unitQuadXY;
        private Mesh _unitWireQuadXY;
        private Mesh _unitQuadXZ;
        private Mesh _unitXAxis;

        public Mesh unitCircleXY
        {
            get
            {
                if (_unitCircleXY == null) _unitCircleXY = CircleMesh.createXY(1.0f, 200, Color.white);
                return _unitCircleXY;
            }
        }
        public Mesh unitWireCircleXY
        {
            get
            {
                if (_unitWireCircleXY == null) _unitWireCircleXY = CircleMesh.createWireXY(1.0f, 200, Color.white);
                return _unitWireCircleXY;
            }
        }
        public Mesh unitCoordSystem
        {
            get
            {
                if (_unitCoordSystem == null) _unitCoordSystem = LineMesh.createCoordSystemAxes(1.0f, Color.white);
                return _unitCoordSystem;
            }
        }
        public Mesh unitBox
        {
            get
            {
                if (_unitBox == null) _unitBox = BoxMesh.create(1.0f, 1.0f, 1.0f, Color.white);
                return _unitBox;
            }
        }
        public Mesh unitWireBox
        {
            get
            {
                if (_unitWireBox == null) _unitWireBox = BoxMesh.createWire(1.0f, 1.0f, 1.0f, Color.white);
                return _unitWireBox;
            }
        }
        public Mesh unitQuadXY
        {
            get
            {
                if (_unitQuadXY == null) _unitQuadXY = QuadMesh.createXY(1.0f, 1.0f, Color.white);
                return _unitQuadXY;
            }
        }
        public Mesh unitWireQuadXY
        {
            get
            {
                if (_unitWireQuadXY == null) _unitWireQuadXY = QuadMesh.createWireXY(1.0f, 1.0f, Color.white);
                return _unitWireQuadXY;
            }
        }
        public Mesh unitQuadXZ
        {
            get
            {
                if (_unitQuadXZ == null) _unitQuadXZ = QuadMesh.createXZ(1.0f, 1.0f, Color.white);
                return _unitQuadXZ;
            }
        }
        public Mesh unitXAxis
        {
            get
            {
                if (_unitXAxis == null) _unitXAxis = LineMesh.createXAxis(Vector3.zero, 1.0f, Color.white);
                return _unitXAxis;
            }
        }
    }
}
#endif