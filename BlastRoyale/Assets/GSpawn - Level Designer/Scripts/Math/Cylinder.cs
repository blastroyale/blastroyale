#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public struct Cylinder
    {
        private Vector3     _basePoint;
        private Vector3     _heightAxis;
        private float       _height;
        private float       _radius;

        public Vector3      basePoint   { get { return _basePoint; } set { _basePoint = value; } }
        public Vector3      heightAxis  { get { return _heightAxis; } set { _heightAxis = value.normalized; } }
        public float        height      { get { return _height; } set { _height = Mathf.Max(0.0f, value); } }
        public float        radius      { get { return _radius; } set { _radius = Mathf.Max(0.0f, value); } }
    }
}
#endif