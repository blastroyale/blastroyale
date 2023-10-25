#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public struct MeshRayHit
    {
        private float       _hitEnter;
        private Vector3     _hitPoint;
        private Vector3     _hitNormal;
        private int         _triangleIndex;

        public float        hitEnter        { get { return _hitEnter; } }
        public Vector3      hitPoint        { get { return _hitPoint; } }
        public Vector3      hitNormal       { get { return _hitNormal; } }
        public int          triangleIndex   { get { return _triangleIndex; } }

        public MeshRayHit(Ray ray, float hitEnter, int triangleIndex, Vector3 hitNormal)
        {
            _hitEnter       = hitEnter;
            _hitPoint       = ray.GetPoint(hitEnter);
            _triangleIndex  = triangleIndex;
            _hitNormal      = hitNormal;
        }
    }
}
#endif