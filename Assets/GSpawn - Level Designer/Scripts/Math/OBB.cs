#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public struct OBB
    {
        [SerializeField]
        private Vector3     _size;
        [SerializeField]
        private Vector3     _center;
        [SerializeField]
        private Quaternion  _rotation;
        [SerializeField]
        private bool        _isValid;

        public bool         isValid         { get { return _isValid; } }
        public float        volume          { get { return _size.x * _size.y * _size.z; } }
        public Vector3      center          { get { return _center; } set { _center = value; } }
        public Vector3      size            { get { return _size; } set { _size = value.abs(); } }
        public Vector3      extents         { get { return new Vector3(_size.x * 0.5f, _size.y * 0.5f, _size.z * 0.5f); } }
        public Quaternion   rotation        { get { return _rotation; } set { _rotation = value; } }
        public Matrix4x4    rotationMatrix  { get { return Matrix4x4.TRS(Vector3.zero, _rotation, Vector3.one); } }
        public Matrix4x4    transformMatrix { get { return Matrix4x4.TRS(center, _rotation, size); } }
        public Vector3      right           { get { return _rotation * Vector3.right; } }
        public Vector3      up              { get { return _rotation * Vector3.up; } }
        public Vector3      look            { get { return _rotation * Vector3.forward; } }
        public Vector3      min 
        { 
            get 
            { 
                Vector3 halfSize = extents;
                return center - right * halfSize.x - up * halfSize.y - look * halfSize.z;
            } 
        }
        public Vector3 max
        {
            get
            {
                Vector3 halfSize = extents;
                return center + right * halfSize.x + up * halfSize.y + look * halfSize.z;
            }
        }

        public OBB(bool valid)
        {
            _center     = Vector3.zero;
            _size       = Vector3.zero;
            _rotation   = Quaternion.identity;
            _isValid    = valid;
        }

        public OBB(Bounds bounds)
        {
            _center     = bounds.center;
            _size       = bounds.size;
            _rotation   = Quaternion.identity;
            _isValid    = true;
        }

        public OBB(AABB aabb)
        {
            _center     = aabb.center;
            _size       = aabb.size;
            _rotation   = Quaternion.identity;
            _isValid    = true;
        }

        public OBB(Vector3 center)
        {
            _center     = center;
            _size       = Vector3.zero;
            _rotation   = Quaternion.identity;
            _isValid    = true;
        }

        public OBB(Vector3 center, Vector3 size)
        {
            _center     = center;
            _size       = size;
            _rotation   = Quaternion.identity;
            _isValid    = true;
        }

        public OBB(Vector3 center, Vector3 size, Quaternion rotation)
        {
            _center     = center;
            _size       = size;
            _rotation   = rotation;
            _isValid    = true;
        }

        public OBB(Vector3 center, Quaternion rotation)
        {
            _center     = center;
            _size       = Vector3.zero;
            _rotation   = rotation;
            _isValid    = true;
        }

        public OBB(Quaternion rotation)
        {
            _center     = Vector3.zero;
            _size       = Vector3.zero;
            _rotation   = rotation;
            _isValid    = true;
        }

        public OBB(Bounds bounds, Quaternion rotation)
        {
            _center     = bounds.center;
            _size       = bounds.size;
            _rotation   = rotation;
            _isValid    = true;
        }

        public OBB(AABB aabb, Quaternion rotation)
        {
            _center     = aabb.center;
            _size       = aabb.size;
            _rotation   = rotation;
            _isValid    = true;
        }

        public OBB(AABB modelSpaceAABB, Transform transform)
        {
            _size       = Vector3.Scale(modelSpaceAABB.size.abs(), transform.lossyScale.abs());
            _center     = transform.TransformPoint(modelSpaceAABB.center);
            _rotation   = transform.rotation;
            _isValid    = true;
        }

        public OBB(OBB copy)
        {
            _size       = copy._size;
            _center     = copy._center;
            _rotation   = copy._rotation;
            _isValid    = copy._isValid;
        }

        public static OBB getInvalid()
        {
            return new OBB();
        }

        public static OBB createFromSegment(Vector3 pt0, Vector3 pt1, float segmentThickness)
        {
            Vector3 segmentDir  = pt1 - pt0;
            float segmentLength = segmentDir.magnitude;
            if (segmentLength < 1e-5f) return getInvalid();

            Vector3 size        = new Vector3(segmentThickness, segmentThickness, segmentLength);
            return new OBB(pt0 + segmentDir * 0.5f, size, Quaternion.LookRotation(segmentDir.normalized, segmentDir.getFirstUnalignedAxisVec()));
        }

        public void clipToAlignedOBB(OBB clipOBB)
        {
            Vector3 clipRight           = clipOBB.right;
            Vector3 clipUp              = clipOBB.up;
            Vector3 clipLook            = clipOBB.look;
            Vector3 clipExtents         = clipOBB.extents;
            Vector3 clipCenter          = clipOBB.center;

            Vector3 dir                 = min - clipCenter;
            float dmin_x                = Vector3.Dot(dir, clipRight);
            float dmin_y                = Vector3.Dot(dir, clipUp);
            float dmin_z                = Vector3.Dot(dir, clipLook);

            dir                         = max - clipCenter;
            float dmax_x                = Vector3.Dot(dir, clipRight);
            float dmax_y                = Vector3.Dot(dir, clipUp);
            float dmax_z                = Vector3.Dot(dir, clipLook);

            bool outX                   = false;
            bool outY                   = false;
            bool outZ                   = false;

            if (dmin_x > clipExtents.x && dmax_x > clipExtents.x)
            {
                Plane plane = new Plane(clipRight, clipCenter + clipRight * clipExtents.x);
                _center     = plane.projectPoint(_center);
                _size.x     = 0.0f;
                outX        = true;
            }
            else if (dmin_x < -clipExtents.x && dmax_x < -clipExtents.x)
            {
                Plane plane = new Plane(-clipRight, clipCenter - clipRight * clipExtents.x);
                _center     = plane.projectPoint(_center);
                _size.x     = 0.0f;
                outX        = true;
            }

            if (dmin_y > clipExtents.y && dmax_y > clipExtents.y)
            {
                Plane plane = new Plane(clipUp, clipCenter + clipUp * clipExtents.y);
                _center     = plane.projectPoint(_center);
                _size.y     = 0.0f;
                outY        = true;
            }
            else if (dmin_y < -clipExtents.y && dmax_y < -clipExtents.y)
            {
                Plane plane = new Plane(-clipUp, clipCenter - clipUp * clipExtents.y);
                _center     = plane.projectPoint(_center);
                _size.y     = 0.0f;
                outY        = true;
            }

            if (dmin_z > clipExtents.z && dmax_z > clipExtents.z)
            {
                Plane plane = new Plane(clipLook, clipCenter + clipLook * clipExtents.z);
                _center     = plane.projectPoint(_center);
                _size.z     = 0.0f;
                outZ        = true;
            }
            else if (dmin_z < -clipExtents.z && dmax_z < -clipExtents.z)
            {
                Plane plane = new Plane(-clipLook, clipCenter - clipLook * clipExtents.z);
                _center     = plane.projectPoint(_center);
                _size.z     = 0.0f;
                outZ        = true;
            }

            if (!outX)
            {
                if (dmin_x < -clipExtents.x)
                {
                    float clipAmount    = -(dmin_x + clipExtents.x);
                    _center             += clipRight * clipAmount * 0.5f;
                    _size.x             -= clipAmount;
                }
                if (dmax_x > clipExtents.x)
                {
                    float clipAmount    = dmax_x - clipExtents.x;
                    _center             -= clipRight * clipAmount * 0.5f;
                    _size.x             -= clipAmount;
                }
            }

            if (!outY)
            {
                if (dmin_y < -clipExtents.y)
                {
                    float clipAmount    = -(dmin_y + clipExtents.y);
                    _center             += clipUp * clipAmount * 0.5f;
                    _size.y             -= clipAmount;
                }
                if (dmax_y > clipExtents.y)
                {
                    float clipAmount    = dmax_y - clipExtents.y;
                    _center             -= clipUp * clipAmount * 0.5f;
                    _size.y             -= clipAmount;
                }
            }

            if (!outZ)
            {
                if (dmin_z < -clipExtents.z)
                {
                    float clipAmount    = -(dmin_z + clipExtents.z);
                    _center             += clipLook * clipAmount * 0.5f;
                    _size.z             -= clipAmount;
                }
                if (dmax_z > clipExtents.z)
                {
                    float clipAmount    = dmax_z - clipExtents.z;
                    _center             -= clipLook * clipAmount * 0.5f;
                    _size.z             -= clipAmount;
                }
            }
        }

        public void inflate(float amount)
        {
            size += Vector3Ex.create(amount);
        }

        public void inflateClampZero(float amount)
        {
            size += Vector3Ex.create(amount);
            size = Vector3.Max(size, Vector3.zero);
        }

        public void inflate(float amount, int ignoreAxis)
        {
            Vector3 inflate = Vector3Ex.create(amount);
            inflate[ignoreAxis] = 0.0f;
            size += inflate;
        }

        public void transform(Matrix4x4 transformMtx)
        {
            center      = transformMtx.MultiplyPoint(_center);
            rotation    = QuaternionEx.create(transformMtx) * _rotation;
            size        = Vector3.Scale(transformMtx.getPositiveScale(), _size);
        }

        public void rotateAroundPivot(Quaternion rotation, Vector3 pivot)
        {
            _rotation           = rotation * _rotation;

            Vector3 toCenter    = _center - pivot;
            toCenter            = rotation * toCenter;
            center              = pivot + toCenter;
        }

        public void calcCenterAndCorners(List<Vector3> centerAndCorners)
        {
            calcCorners(centerAndCorners, false);
            centerAndCorners.Add(center);
        }

        public void calcCorners(List<Vector3> cornerPoints, bool append)
        {
            Box3D.calcCorners(_center, _size, _rotation, cornerPoints, append);
        }

        public void enclosePoint(Vector3 point)
        {
            // X axis
            float dot = Vector3.Dot((point - _center), right);
            if (dot > extents.x)
            {
                float delta = dot - extents.x;
                _size.x += delta;
                _center += right * delta * 0.5f;
            }
            else
            if (dot < -extents.x)
            {
                float delta = dot + extents.x;
                _size.x += Mathf.Abs(delta);
                _center += right * delta * 0.5f;
            }

            // Y axis
            dot = Vector3.Dot((point - _center), up);
            if (dot > extents.y)
            {
                float delta = dot - extents.y;
                _size.y += delta;
                _center += up * delta * 0.5f;
            }
            else
            if (dot < -extents.y)
            {
                float delta = dot + extents.y;
                _size.y += Mathf.Abs(delta);
                _center += up * delta * 0.5f;
            }

            // Z axis
            dot = Vector3.Dot((point - _center), look);
            if (dot > extents.z)
            {
                float delta = dot - extents.z;
                _size.z += delta;
                _center += look * delta * 0.5f;
            }
            else
            if (dot < -extents.z)
            {
                float delta = dot + extents.z;
                _size.z += Mathf.Abs(delta);
                _center += look * delta * 0.5f;
            }
        }

        public void encloseOBB(OBB otherOBB)
        {
            Vector3 otherRightAxis      = otherOBB.right;
            Vector3 otherUpAxis         = otherOBB.up;
            Vector3 otherExtents        = otherOBB.extents;

            Vector3 otherFaceCenter = otherOBB.center - otherOBB.look * otherExtents.z;
            enclosePoint(otherFaceCenter - otherRightAxis * otherExtents.x + otherUpAxis * otherExtents.y);
            enclosePoint(otherFaceCenter + otherRightAxis * otherExtents.x + otherUpAxis * otherExtents.y);
            enclosePoint(otherFaceCenter + otherRightAxis * otherExtents.x - otherUpAxis * otherExtents.y);
            enclosePoint(otherFaceCenter - otherRightAxis * otherExtents.x - otherUpAxis * otherExtents.y);

            otherFaceCenter = otherOBB.center + otherOBB.look * otherExtents.z;
            enclosePoint(otherFaceCenter + otherRightAxis * otherExtents.x + otherUpAxis * otherExtents.y);
            enclosePoint(otherFaceCenter - otherRightAxis * otherExtents.x + otherUpAxis * otherExtents.y);
            enclosePoint(otherFaceCenter - otherRightAxis * otherExtents.x - otherUpAxis * otherExtents.y);
            enclosePoint(otherFaceCenter + otherRightAxis * otherExtents.x - otherUpAxis * otherExtents.y);
        }

        public OBB calcInwardFaceExtrusion(Box3DFace face, float extrudeAmount)
        {
            Box3DFaceDesc faceDesc = Box3D.getFaceDesc(_center, _size, _rotation, face);

            Vector3 size = _size;
            if (face == Box3DFace.Left || face == Box3DFace.Right) size.x = extrudeAmount;
            else if (face == Box3DFace.Bottom || face == Box3DFace.Top) size.y = extrudeAmount;
            else size.z = extrudeAmount;

            return new OBB(faceDesc.center - faceDesc.plane.normal * extrudeAmount * 0.5f, size, _rotation);
        }

        public OBB calcOutwardFaceExtrusion(Box3DFace face, float extrudeAmount)
        {
            Box3DFaceDesc faceDesc = Box3D.getFaceDesc(_center, _size, _rotation, face);

            Vector3 size = _size;
            if (face == Box3DFace.Left || face == Box3DFace.Right) size.x = extrudeAmount;
            else if (face == Box3DFace.Bottom || face == Box3DFace.Top) size.y = extrudeAmount;
            else size.z = extrudeAmount;

            return new OBB(faceDesc.center + faceDesc.plane.normal * extrudeAmount * 0.5f, size, _rotation);
        }

        public OBB calcFaceExtrusionFromFaceCenter(Box3DFace face, float extrudeAmount)
        {
            Box3DFaceDesc faceDesc = Box3D.getFaceDesc(_center, _size, _rotation, face);

            Vector3 size = _size;
            if (face == Box3DFace.Left || face == Box3DFace.Right) size.x = extrudeAmount;
            else if (face == Box3DFace.Bottom || face == Box3DFace.Top) size.y = extrudeAmount;
            else size.z = extrudeAmount;

            return new OBB(faceDesc.center, size, _rotation);
        }

        public bool containsPoint(Vector3 point, Vector3Int axisMask)
        {
            Vector3 boxRight    = right;
            Vector3 boxUp       = up;
            Vector3 boxLook     = look;

            // Bring point in box local space
            float ptX = point.x;
            float ptY = point.y;
            float ptZ = point.z;

            point.x = (ptX * boxRight.x + ptY * boxRight.y + ptZ * boxRight.z) - (_center.x * boxRight.x + _center.y * boxRight.y + _center.z * boxRight.z);
            point.y = (ptX * boxUp.x + ptY * boxUp.y + ptZ * boxUp.z) - (_center.x * boxUp.x + _center.y * boxUp.y + _center.z * boxUp.z);
            point.z = (ptX * boxLook.x + ptY * boxLook.y + ptZ * boxLook.z) - (_center.x * boxLook.x + _center.y * boxLook.y + _center.z * boxLook.z);

            Vector3 extents;
            extents.x = _size.x * 0.5f;
            extents.y = _size.y * 0.5f;
            extents.z = _size.z * 0.5f;

            if (axisMask.x != 0 && (point.x < -extents.x || point.x > extents.x)) return false;
            if (axisMask.y != 0 && (point.y < -extents.y || point.y > extents.y)) return false;
            if (axisMask.z != 0 && (point.z < -extents.z || point.z > extents.z)) return false;

            return true;
        }

        public bool containsPoint(Vector3 point)
        {
            Vector3 boxRight    = right;
            Vector3 boxUp       = up;
            Vector3 boxLook     = look;

            // Bring point in box local space
            float ptX = point.x;
            float ptY = point.y;
            float ptZ = point.z;

            point.x = (ptX * boxRight.x + ptY * boxRight.y + ptZ * boxRight.z) - (_center.x * boxRight.x + _center.y * boxRight.y + _center.z * boxRight.z);
            point.y = (ptX * boxUp.x + ptY * boxUp.y + ptZ * boxUp.z) - (_center.x * boxUp.x + _center.y * boxUp.y + _center.z * boxUp.z);
            point.z = (ptX * boxLook.x + ptY * boxLook.y + ptZ * boxLook.z) - (_center.x * boxLook.x + _center.y * boxLook.y + _center.z * boxLook.z);

            Vector3 extents;
            extents.x = _size.x * 0.5f;
            extents.y = _size.y * 0.5f;
            extents.z = _size.z * 0.5f;

            if (point.x < -extents.x || point.x > extents.x) return false;
            if (point.y < -extents.y || point.y > extents.y) return false;
            if (point.z < -extents.z || point.z > extents.z) return false;

            return true;
        }

        public bool containsPoint(Vector3 point, Vector3 boxRight, Vector3 boxUp, Vector3 boxLook)
        {
            // Bring point in box local space
            float ptX = point.x;
            float ptY = point.y;
            float ptZ = point.z;

            point.x = (ptX * boxRight.x + ptY * boxRight.y + ptZ * boxRight.z) - (_center.x * boxRight.x + _center.y * boxRight.y + _center.z * boxRight.z);
            point.y = (ptX * boxUp.x + ptY * boxUp.y + ptZ * boxUp.z) - (_center.x * boxUp.x + _center.y * boxUp.y + _center.z * boxUp.z);
            point.z = (ptX * boxLook.x + ptY * boxLook.y + ptZ * boxLook.z) - (_center.x * boxLook.x + _center.y * boxLook.y + _center.z * boxLook.z);

            Vector3 extents;
            extents.x = _size.x * 0.5f;
            extents.y = _size.y * 0.5f;
            extents.z = _size.z * 0.5f;

            if (point.x < -extents.x || point.x > extents.x) return false;
            if (point.y < -extents.y || point.y > extents.y) return false;
            if (point.z < -extents.z || point.z > extents.z) return false;

            return true;
        }

        public bool containsPoints(IEnumerable<Vector3> points, Vector3Int axisMask)
        {
            foreach (var pt in points)
                if (!containsPoint(pt, axisMask)) return false;

            return true;
        }

        public bool containsPoints(IEnumerable<Vector3> points)
        {
            foreach (var pt in points)
                if (!containsPoint(pt)) return false;

            return true;
        }

        public Vector3 calcClosestPoint(Vector3 point)
        {
            Vector3 boxRight    = right;
            Vector3 boxUp       = up;
            Vector3 boxLook     = look;

            // Bring point in box local space
            float ptX = point.x;
            float ptY = point.y;
            float ptZ = point.z;

            point.x = (ptX * boxRight.x + ptY * boxRight.y + ptZ * boxRight.z) - (_center.x * boxRight.x + _center.y * boxRight.y + _center.z * boxRight.z);
            point.y = (ptX * boxUp.x + ptY * boxUp.y + ptZ * boxUp.z) - (_center.x * boxUp.x + _center.y * boxUp.y + _center.z * boxUp.z);
            point.z = (ptX * boxLook.x + ptY * boxLook.y + ptZ * boxLook.z) - (_center.x * boxLook.x + _center.y * boxLook.y + _center.z * boxLook.z);

            Vector3 extents;
            extents.x = _size.x * 0.5f;
            extents.y = _size.y * 0.5f;
            extents.z = _size.z * 0.5f;

            Vector3 closestPt;

            closestPt.x = point.x;
            if (closestPt.x > extents.x) closestPt.x = extents.x;
            else if (closestPt.x < -extents.x) closestPt.x = -extents.x;

            closestPt.y = point.y;
            if (closestPt.y > extents.y) closestPt.y = extents.y;
            else if (closestPt.y < -extents.y) closestPt.y = -extents.y;

            closestPt.z = point.z;
            if (closestPt.z > extents.z) closestPt.z = extents.z;
            else if (closestPt.z < -extents.z) closestPt.z = -extents.z;

            point.x = boxRight.x * closestPt.x + boxUp.x * closestPt.y + boxLook.x * closestPt.z + _center.x;
            point.y = boxRight.y * closestPt.x + boxUp.y * closestPt.y + boxLook.y * closestPt.z + _center.y;
            point.z = boxRight.z * closestPt.x + boxUp.z * closestPt.y + boxLook.z * closestPt.z + _center.z;

            return point;
        }

        public Vector3 calcClosestPoint(Vector3 point, Vector3 boxRight, Vector3 boxUp, Vector3 boxLook)
        {
            // Bring point in box local space
            float ptX = point.x;
            float ptY = point.y;
            float ptZ = point.z;

            point.x = (ptX * boxRight.x + ptY * boxRight.y + ptZ * boxRight.z) - (_center.x * boxRight.x + _center.y * boxRight.y + _center.z * boxRight.z);
            point.y = (ptX * boxUp.x + ptY * boxUp.y + ptZ * boxUp.z) - (_center.x * boxUp.x + _center.y * boxUp.y + _center.z * boxUp.z);
            point.z = (ptX * boxLook.x + ptY * boxLook.y + ptZ * boxLook.z) - (_center.x * boxLook.x + _center.y * boxLook.y + _center.z * boxLook.z);

            Vector3 extents;
            extents.x = _size.x * 0.5f;
            extents.y = _size.y * 0.5f;
            extents.z = _size.z * 0.5f;

            Vector3 closestPt;

            closestPt.x = point.x;
            if (closestPt.x > extents.x) closestPt.x = extents.x;
            else if (closestPt.x < -extents.x) closestPt.x = -extents.x;

            closestPt.y = point.y;
            if (closestPt.y > extents.y) closestPt.y = extents.y;
            else if (closestPt.y < -extents.y) closestPt.y = -extents.y;

            closestPt.z = point.z;
            if (closestPt.z > extents.z) closestPt.z = extents.z;
            else if (closestPt.z < -extents.z) closestPt.z = -extents.z;

            point.x = boxRight.x * closestPt.x + boxUp.x * closestPt.y + boxLook.x * closestPt.z + _center.x;
            point.y = boxRight.y * closestPt.x + boxUp.y * closestPt.y + boxLook.y * closestPt.z + _center.y;
            point.z = boxRight.z * closestPt.x + boxUp.z * closestPt.y + boxLook.z * closestPt.z + _center.z;

            return point;
        }

        public bool raycast(Ray ray, out float t)
        {
            t = 0.0f;

            Matrix4x4 boxMatrix = Matrix4x4.TRS(_center, _rotation, _size.replaceZero(1e-6f));
            Ray modelRay        = boxMatrix.inverse.transformRay(ray);
            if (modelRay.direction.sqrMagnitude == 0.0f) return false;

            Bounds unitCube = new Bounds(Vector3.zero, Vector3.one);
            if (unitCube.IntersectRay(modelRay, out t))
            {
                Vector3 intersectPt = boxMatrix.MultiplyPoint(modelRay.GetPoint(t));
                t = (intersectPt - ray.origin).magnitude;
                return true;
            }

            return false;
        }

        public bool raycast(Ray ray)
        {
            float t;
            return raycast(ray, out t);
        }

        private static List<Vector3> _aabbCorners = new List<Vector3>();
        public bool intersectsTriangle(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            // TODO: Implement SAT?
            AABB aabb = new AABB(Vector3.zero, Vector3.one);

            // Check if the aabb contains any of the 3 triangle points
            Matrix4x4 inverseTransform = transformMatrix.inverse;
            p0 = inverseTransform.MultiplyPoint(p0);
            if (aabb.containsPoint(p0)) return true;
            p1 = inverseTransform.MultiplyPoint(p1);
            if (aabb.containsPoint(p1)) return true;
            p2 = inverseTransform.MultiplyPoint(p2);
            if (aabb.containsPoint(p2)) return true;

            // Check if the triangle edges intersect the AABB
            Vector3 edge    = p1 - p0;
            Ray ray         = new Ray(p0, edge);
            float t;
            if (aabb.raycast(ray, out t))
            {
                if (t >= 0.0f && t <= edge.magnitude) return true;
            }

            edge            = p2 - p0;
            ray             = new Ray(p0, edge);
            if (aabb.raycast(ray, out t))
            {
                if (t >= 0.0f && t <= edge.magnitude) return true;
            }

            edge            = p2 - p1;
            ray             = new Ray(p1, edge);
            if (aabb.raycast(ray, out t))
            {
                if (t >= 0.0f && t <= edge.magnitude) return true;
            }

            // Check if the AABB intersects the triangle plane and if it does, check if
            // any one of the corner points of the AABB lies inside the triangle area.
            Plane trianglePlane = new Plane(p0, p1, p2);
            Box3D.calcCorners(Vector3.zero, Vector3.one, _aabbCorners, false);
            var result          = Box3D.classifyAgainstPlane(_aabbCorners, trianglePlane);
            if (result == PlaneClassifyResult.Spanning || result == PlaneClassifyResult.OnPlane)
            {
                int numPts = _aabbCorners.Count;
                for (int i = 0; i < numPts; ++i)
                {
                    if (Triangle3D.containsPoint(_aabbCorners[i], false, p0, p1, p2)) return true;
                }
            }

            return false;
        }

        #region BoxIntersectsBox Heap Alloc
        private static Vector3[] A = new Vector3[3];
        private static Vector3[] B = new Vector3[3];
        private static float[,] R = new float[3, 3];
        private static float[,] absR = new float[3, 3];
        #endregion
        public bool intersectsOBB(OBB other)
        {
            A[0] = _rotation * Vector3.right;
            A[1] = _rotation * Vector3.up;
            A[2] = _rotation * Vector3.forward;

            B[0] = other._rotation * Vector3.right;
            B[1] = other._rotation * Vector3.up;
            B[2] = other._rotation * Vector3.forward;

            // Note: We're using column major matrices.
            for (int row = 0; row < 3; ++row)
            {
                for (int column = 0; column < 3; ++column)
                {
                    R[row, column] = Vector3.Dot(A[row], B[column]);
                }
            }

            Vector3 extents = _size * 0.5f;
            Vector3 AEx     = new Vector3(extents.x, extents.y, extents.z);
            extents         = other._size * 0.5f;
            Vector3 BEx     = new Vector3(extents.x, extents.y, extents.z);

            // Construct absolute rotation error matrix to account for cases when 2 local axes are parallel
            const float epsilon = 1e-4f;
            for (int row = 0; row < 3; ++row)
            {
                for (int column = 0; column < 3; ++column)
                {
                    absR[row, column] = MathEx.fastAbs(R[row, column]) + epsilon;
                }
            }

            Vector3 trVector    = other._center - _center;
            Vector3 t           = new Vector3(Vector3.Dot(trVector, A[0]), Vector3.Dot(trVector, A[1]), Vector3.Dot(trVector, A[2]));

            // Test extents projection on box A local axes (A0, A1, A2)
            for (int axisIndex = 0; axisIndex < 3; ++axisIndex)
            {
                float bExtents = BEx[0] * absR[axisIndex, 0] + BEx[1] * absR[axisIndex, 1] + BEx[2] * absR[axisIndex, 2];
                if (MathEx.fastAbs(t[axisIndex]) > AEx[axisIndex] + bExtents) return false;
            }

            // Test extents projection on box B local axes (B0, B1, B2)
            for (int axisIndex = 0; axisIndex < 3; ++axisIndex)
            {
                float aExtents = AEx[0] * absR[0, axisIndex] + AEx[1] * absR[1, axisIndex] + AEx[2] * absR[2, axisIndex];
                if (MathEx.fastAbs(t[0] * R[0, axisIndex] +
                                   t[1] * R[1, axisIndex] +
                                   t[2] * R[2, axisIndex]) > aExtents + BEx[axisIndex]) return false;
            }

            // Test axis A0 x B0
            float ra = AEx[1] * absR[2, 0] + AEx[2] * absR[1, 0];
            float rb = BEx[1] * absR[0, 2] + BEx[2] * absR[0, 1];
            if (MathEx.fastAbs(t[2] * R[1, 0] - t[1] * R[2, 0]) > ra + rb) return false;

            // Test axis A0 x B1
            ra = AEx[1] * absR[2, 1] + AEx[2] * absR[1, 1];
            rb = BEx[0] * absR[0, 2] + BEx[2] * absR[0, 0];
            if (MathEx.fastAbs(t[2] * R[1, 1] - t[1] * R[2, 1]) > ra + rb) return false;

            // Test axis A0 x B2
            ra = AEx[1] * absR[2, 2] + AEx[2] * absR[1, 2];
            rb = BEx[0] * absR[0, 1] + BEx[1] * absR[0, 0];
            if (MathEx.fastAbs(t[2] * R[1, 2] - t[1] * R[2, 2]) > ra + rb) return false;

            // Test axis A1 x B0
            ra = AEx[0] * absR[2, 0] + AEx[2] * absR[0, 0];
            rb = BEx[1] * absR[1, 2] + BEx[2] * absR[1, 1];
            if (MathEx.fastAbs(t[0] * R[2, 0] - t[2] * R[0, 0]) > ra + rb) return false;

            // Test axis A1 x B1
            ra = AEx[0] * absR[2, 1] + AEx[2] * absR[0, 1];
            rb = BEx[0] * absR[1, 2] + BEx[2] * absR[1, 0];
            if (MathEx.fastAbs(t[0] * R[2, 1] - t[2] * R[0, 1]) > ra + rb) return false;

            // Test axis A1 x B2
            ra = AEx[0] * absR[2, 2] + AEx[2] * absR[0, 2];
            rb = BEx[0] * absR[1, 1] + BEx[1] * absR[1, 0];
            if (MathEx.fastAbs(t[0] * R[2, 2] - t[2] * R[0, 2]) > ra + rb) return false;

            // Test axis A2 x B0
            ra = AEx[0] * absR[1, 0] + AEx[1] * absR[0, 0];
            rb = BEx[1] * absR[2, 2] + BEx[2] * absR[2, 1];
            if (MathEx.fastAbs(t[1] * R[0, 0] - t[0] * R[1, 0]) > ra + rb) return false;

            // Test axis A2 x B1
            ra = AEx[0] * absR[1, 1] + AEx[1] * absR[0, 1];
            rb = BEx[0] * absR[2, 2] + BEx[2] * absR[2, 0];
            if (MathEx.fastAbs(t[1] * R[0, 1] - t[0] * R[1, 1]) > ra + rb) return false;

            // Test axis A2 x B2
            ra = AEx[0] * absR[1, 2] + AEx[1] * absR[0, 2];
            rb = BEx[0] * absR[2, 1] + BEx[1] * absR[2, 0];
            if (MathEx.fastAbs(t[1] * R[0, 2] - t[0] * R[1, 2]) > ra + rb) return false;

            return true;
        }
    }
}
#endif