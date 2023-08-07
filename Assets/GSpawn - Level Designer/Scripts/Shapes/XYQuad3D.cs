#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GSpawn
{
    public class XYQuad3D : Shape3D
    {
        [Flags]
        public enum WireEdgeFlags
        {
            None = 0,
            Top = 1,
            Right = 2,
            Bottom = 4,
            Left = 8,
            All = Top | Right | Bottom | Left
        }

        public class WireDrawSettings
        {
            private WireEdgeFlags   _wireEdgeFlags = WireEdgeFlags.All;
            public WireEdgeFlags    wireEdgeFlags { get { return _wireEdgeFlags; } set { _wireEdgeFlags = value; } }
        }

        private Vector3             _center             = modelCenter;
        private Vector2             _size               = Vector2.one;
        private Quaternion          _rotation           = Quaternion.identity;
        private WireDrawSettings    _wireDrawSettings   = new WireDrawSettings();
        private List<Vector3>       _corners            = new List<Vector3>(4);

        public Vector3              center              { get { return _center; } set { _center = value; } }
        public Vector2              size                { get { return _size; } set { _size = value.abs(); } }
        public float                width               { get { return _size.x; } set { _size.x = Mathf.Abs(value); } }
        public float                height              { get { return _size.y; } set { _size.y = Mathf.Abs(value); } }
        public Quaternion           rotation            { get { return _rotation; } set { _rotation = value; } }
        public Vector3              uAxis               { get { return _rotation * modelU; } }
        public Vector3              vAxis               { get { return _rotation * modelV; } }
        public Vector3              normal              { get { return _rotation * modelNormal; } }
        public Plane                plane               { get { return new Plane(normal, center); } }
        public WireDrawSettings     wireDrawSettings    { get { return _wireDrawSettings; } }

        public static Vector3       modelU              { get { return Vector3.right; } }
        public static Vector3       modelV              { get { return Vector3.up; } }
        public static Vector3       modelLook           { get { return Vector3.forward; } }
        public static Vector3       modelCenter         { get { return Vector3.zero; } }
        public static Vector3       modelNormal         { get { return modelLook; } }

        public void alignNormal(Vector3 axis)
        {
            rotation = QuaternionEx.create(normal, axis, uAxis) * _rotation;
        }

        public override void drawFilled()
        {
            Graphics.DrawMeshNow(MeshPool.instance.unitQuadXY, Matrix4x4.TRS(_center, _rotation, new Vector3(_size.x, _size.y, 1.0f)));
        }

        public override void drawWire()
        {
            if(_wireDrawSettings.wireEdgeFlags == WireEdgeFlags.All)
                Graphics.DrawMeshNow(MeshPool.instance.unitWireQuadXY, Matrix4x4.TRS(_center, _rotation, new Vector3(_size.x, _size.y, 1.0f)));
            else
            {
                Quad3D.calcCorners(_center, _size, _rotation, _corners);
                if ((_wireDrawSettings.wireEdgeFlags & WireEdgeFlags.Top) != 0)
                    GLEx.drawLine3D(_corners[(int)QuadCorner.TopLeft], _corners[(int)QuadCorner.TopRight]);
                if ((_wireDrawSettings.wireEdgeFlags & WireEdgeFlags.Right) != 0)
                    GLEx.drawLine3D(_corners[(int)QuadCorner.TopRight], _corners[(int)QuadCorner.BottomRight]);
                if ((_wireDrawSettings.wireEdgeFlags & WireEdgeFlags.Bottom) != 0)
                    GLEx.drawLine3D(_corners[(int)QuadCorner.BottomRight], _corners[(int)QuadCorner.BottomLeft]);
                if ((_wireDrawSettings.wireEdgeFlags & WireEdgeFlags.Left) != 0)
                    GLEx.drawLine3D(_corners[(int)QuadCorner.BottomLeft], _corners[(int)QuadCorner.TopLeft]);
            }
        }
    }
}
#endif