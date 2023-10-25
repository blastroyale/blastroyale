#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public struct AABB
    {
        private Vector3     _center;
        private Vector3     _size;
        private bool        _isValid;

        public Vector3      center      { get { return _center; } set { _center = value; } }
        public Vector3      size        { get { return _size; } set { _size = value.abs(); } }
        public Vector3      extents     { get { return new Vector3(_size.x * 0.5f, _size.y * 0.5f, _size.z * 0.5f); } }
        public Vector3      min 
        { 
            get { return new Vector3(_center.x - _size.x * 0.5f, _center.y - _size.y * 0.5f, _center.z - _size.z * 0.5f); } 
            set { refreshCenterAndSize(Vector3.Min(value, max), max); } 
        }
        public Vector3      max
        {
            get { return new Vector3(_center.x + _size.x * 0.5f, _center.y + _size.y * 0.5f, _center.z + _size.z * 0.5f); }
            set { refreshCenterAndSize(min, Vector3.Max(value, min)); }
        }
        public float        volume      { get { return _size.x * _size.y * _size.z; } }
        public bool         isValid     { get { return _isValid; } }

        public static AABB getInvalid()
        {
            return new AABB();
        }

        public AABB(Vector3 center, Vector3 size)
        {
            _center     = center;
            _size       = size;
            _isValid    = true;
        }

        public AABB(Bounds bounds)
        {
            _center     = bounds.center;
            _size       = bounds.size;
            _isValid    = true;
        }

        public AABB(IEnumerable<Vector3> points)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (Vector3 pt in points)
            {
                min = Vector3.Min(pt, min);
                max = Vector3.Max(pt, max);
            }

            _center     = (min + max) * 0.5f;
            _size       = max - min;
            _isValid    = true;
        }

        public AABB(IEnumerable<Vector2> points)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector2 pt in points)
            {
                min = Vector2.Min(pt, min);
                max = Vector2.Max(pt, max);
            }

            _center     = (min + max) * 0.5f;
            _size       = max - min;
            _isValid    = true;
        }

        public AABB(OBB obb)
        {
            _center     = Vector3.zero;
            _size       = Vector3.one;
            _isValid    = true;
            transform(obb.transformMatrix);
        }

        public static AABB createFromMinMax(Vector3 min, Vector3 max)
        {
            return new AABB()
            {
                _center     = (min + max) * 0.5f,
                _size       = (max - min),
                _isValid    = true
            };
        }

        public OBB toOBB()
        {
            return new OBB(_center, _size, Quaternion.identity);
        }

        public Bounds toBounds()
        {
            return new Bounds(center, size);
        }

        public bool intersects(AABB aabb)
        {
            Vector3 min0 = min;
            Vector3 max0 = max;
            Vector3 min1 = aabb.min;
            Vector3 max1 = aabb.max;

            return (min0.x <= max1.x) && (min0.y <= max1.y) && (min0.z <= max1.z) 
                && (max0.x >= min1.x) && (max0.y >= min1.y) && (max0.z >= min1.z);
        }

        public void enclosePoint(Vector3 point)
        {
            Vector3 thisMin = min;
            Vector3 thisMax = max;

            if (point.x < thisMin.x) thisMin.x = point.x;
            if (point.x > thisMax.x) thisMax.x = point.x;
            if (point.y < thisMin.y) thisMin.y = point.y;
            if (point.y > thisMax.y) thisMax.y = point.y;
            if (point.z < thisMin.z) thisMin.z = point.z;
            if (point.z > thisMax.z) thisMax.z = point.z;

            refreshCenterAndSize(thisMin, thisMax);
        }

        public void enclosePoints(IEnumerable<Vector3> points)
        {
            foreach (var pt in points)
                enclosePoint(pt);
        }

        public void encloseAABB(AABB aabb)
        {
            Vector3 thisMin = min;
            Vector3 thisMax = max;

            Vector3 otherMin = aabb.min;
            Vector3 otherMax = aabb.max;

            if (otherMin.x < thisMin.x) thisMin.x = otherMin.x;
            if (otherMin.y < thisMin.y) thisMin.y = otherMin.y;
            if (otherMin.z < thisMin.z) thisMin.z = otherMin.z;

            if (otherMax.x > thisMax.x) thisMax.x = otherMax.x;
            if (otherMax.y > thisMax.y) thisMax.y = otherMax.y;
            if (otherMax.z > thisMax.z) thisMax.z = otherMax.z;

            refreshCenterAndSize(thisMin, thisMax);
        }

        public Sphere getEnclosingSphere()
        {
            return new Sphere(_center, extents.magnitude);
        }

        public void inflate(float amount)
        {
            size += Vector3Ex.create(amount);
        }

        public void inflate(Vector3 amount)
        {
            size += amount;
        }

        public void transform(Matrix4x4 transformMatrix)
        {
            Box3D.transform(_center, _size, transformMatrix, out _center, out _size);
        }

        public void calcCenterAndCorners(List<Vector3> centerAndCorners)
        {
            calcCorners(centerAndCorners);
            centerAndCorners.Add(center);
        }

        public void calcCorners(List<Vector3> cornerPoints)
        {
            Box3D.calcCorners(_center, _size, Quaternion.identity, cornerPoints, false);
        }

        public AABB calcInwardFaceExtrusion(Box3DFace face, float extrudeAmount)
        {
            Box3DFaceDesc faceDesc = Box3D.getFaceDesc(_center, _size, Quaternion.identity, face);

            Vector3 size = _size;
            if (face == Box3DFace.Left || face == Box3DFace.Right) size.x = extrudeAmount;
            else if (face == Box3DFace.Bottom || face == Box3DFace.Top) size.y = extrudeAmount;
            else size.z = extrudeAmount;

            return new AABB(faceDesc.center - faceDesc.plane.normal * extrudeAmount * 0.5f, size);
        }

        public bool containsPoint(Vector3 pt)
        {
            Vector3 boxMin = min;
            Vector3 boxMax = max;

            return pt.x >= boxMin.x && pt.x <= boxMax.x &&
                   pt.y >= boxMin.y && pt.y <= boxMax.y &&
                   pt.z >= boxMin.z && pt.z <= boxMax.z;
        }

        public bool containsAnyPoint(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Vector3 boxMin = min;
            Vector3 boxMax = max;

            return (p0.x >= boxMin.x && p0.x <= boxMax.x &&
                    p0.y >= boxMin.y && p0.y <= boxMax.y &&
                    p0.z >= boxMin.z && p0.z <= boxMax.z) ||
                   (p1.x >= boxMin.x && p1.x <= boxMax.x &&
                    p1.y >= boxMin.y && p1.y <= boxMax.y &&
                    p1.z >= boxMin.z && p1.z <= boxMax.z) ||
                   (p2.x >= boxMin.x && p2.x <= boxMax.x &&
                    p2.y >= boxMin.y && p2.y <= boxMax.y &&
                    p2.z >= boxMin.z && p2.z <= boxMax.z);
        }

        public bool containsPoints(IEnumerable<Vector3> points)
        {
            foreach (var pt in points)
                if (!containsPoint(pt)) return false;

            return true;
        }

        public Vector3 calcClosestPoint(Vector3 point)
        {
            Vector3 fromCenterToPt  = point - _center;
            Vector3 extents         = _size * 0.5f;
            Vector3 closestPt       = _center;

            float projection        = Vector3.Dot(Vector3.right, fromCenterToPt);
            if (projection > extents[0]) projection = extents[0];
            else if (projection < -extents[0]) projection = -extents[0];
            closestPt += Vector3.right * projection;

            projection              = Vector3.Dot(Vector3.up, fromCenterToPt);
            if (projection > extents[1]) projection = extents[1];
            else if (projection < -extents[1]) projection = -extents[1];
            closestPt += Vector3.up * projection;

            projection              = Vector3.Dot(Vector3.forward, fromCenterToPt);
            if (projection > extents[2]) projection = extents[2];
            else if (projection < -extents[2]) projection = -extents[2];
            closestPt += Vector3.forward * projection;

            return closestPt;
        }

        public bool raycast(Ray ray, out float t)
        {
            t = 0.0f;

            Bounds bounds = new Bounds(_center, _size);
            return bounds.IntersectRay(ray, out t);
        }

        public bool raycast(Ray ray)
        {
            float t;
            return raycast(ray, out t);
        }

        private void refreshCenterAndSize(Vector3 newMin, Vector3 newMax)
        {
            _center.x   = (newMin.x + newMax.x) * 0.5f;
            _center.y   = (newMin.y + newMax.y) * 0.5f;
            _center.z   = (newMin.z + newMax.z) * 0.5f;

            _size.x     = newMax.x - newMin.x;
            _size.y     = newMax.y - newMin.y;
            _size.z     = newMax.z - newMin.z;
        }
    }
}
#endif