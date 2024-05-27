#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectRayHit : RayHit
    {
        private GameObject  _hitObject;
        private MeshRayHit  _meshRayHit;

        public GameObject   hitObject     { get { return _hitObject; } }
        public MeshRayHit   meshRayHit    { get { return _meshRayHit; } }

        public static void sortByHitDistance(List<ObjectRayHit> hits)
        {
            hits.Sort(delegate(ObjectRayHit h0, ObjectRayHit h1)
            { return h0.hitEnter.CompareTo(h1.hitEnter); });
        }

        public ObjectRayHit(Ray hitRay, GameObject hitObject, Vector3 hitNormal, float hitEnter)
            : base(hitRay, hitNormal, hitEnter)
        {
            _hitObject = hitObject;
        }

        public ObjectRayHit(Ray hitRay, GameObject hitObject, MeshRayHit meshRayHit)
            : base(hitRay, meshRayHit.hitPoint, meshRayHit.hitNormal, meshRayHit.hitEnter)
        {
            _hitObject  = hitObject;
            _meshRayHit = meshRayHit;
        }
    }
}
#endif