#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class RayHit
    {
        private Vector3     _hitPoint;
        private float       _hitEnter;
        private Vector3     _hitNormal;
        private Plane       _hitPlane;

        public Vector3      hitPoint    { get { return _hitPoint; } }
        public float        hitEnter    { get { return _hitEnter; } }
        public Vector3      hitNormal   { get { return _hitNormal; } }
        public Plane        hitPlane    { get { return _hitPlane; } }

        public RayHit(Ray hitRay, Vector3 hitNormal, float hitEnter)
        {
            _hitPoint   = hitRay.GetPoint(hitEnter);
            _hitEnter   = hitEnter;
            _hitNormal  = hitNormal;
            _hitPlane   = new Plane(_hitNormal, _hitPoint);
        }

        public RayHit(Ray hitRay, Vector3 hitPoint, Vector3 hitNormal, float hitEnter)
        {
            _hitPoint   = hitPoint;
            _hitEnter   = hitEnter;
            _hitNormal  = hitNormal;
            _hitPlane   = new Plane(_hitNormal, _hitPoint);
        }

        public RayHit(Ray hitRay, Vector3 hitPoint, Plane hitPlane, float hitEnter)
        {
            _hitPoint   = hitPoint;
            _hitEnter   = hitEnter;
            _hitNormal  = hitPlane.normal;
            _hitPlane   = hitPlane;
        }
    }
}
#endif