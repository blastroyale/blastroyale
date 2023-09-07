#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class PluginMeshTriangle
    {
        private Vector3[]       _verts = new Vector3[3];
        private Vector3         _normal;
        private Vector3         _flippedNormal;
        private int             _triangleIndex;
        private float           _area;

        public Vector3[]        verts           { get { return _verts; } }
        public Vector3          v0              { get { return _verts[0]; } }
        public Vector3          v1              { get { return _verts[1]; } }
        public Vector3          v2              { get { return _verts[2]; } }
        public Vector3          normal          { get { return _normal; } }
        public Plane            plane           { get { return new Plane(_normal, _verts[0]); } }
        public Vector3          flippedNormal   { get { return _flippedNormal; } }
        public int              triangleIndex   { get { return _triangleIndex; } }
        public float            area            { get { return _area; } }

        public PluginMeshTriangle()
        {
        }

        public PluginMeshTriangle(Vector3 vert0, Vector3 vert1, Vector3 vert2, int triangleIndex)
        {
            _verts[0] = vert0;
            _verts[1] = vert1;
            _verts[2] = vert2;

            _normal         = Vector3.Cross((vert1 - vert0), (vert2 - vert0));
            _area           = _normal.magnitude * 0.5f;
            _normal.Normalize();
            _flippedNormal  = -_normal;
            _triangleIndex  = triangleIndex;
        }

        public void getTransformedTriangle_VertsAndNormal(Transform transform, PluginMeshTriangle transformedTriangle)
        {
            transformedTriangle._triangleIndex  = _triangleIndex;
            transformedTriangle._verts[0]       = transform.TransformPoint(_verts[0]);
            transformedTriangle._verts[1]       = transform.TransformPoint(_verts[1]);
            transformedTriangle._verts[2]       = transform.TransformPoint(_verts[2]);
            transformedTriangle._normal         = transform.TransformDirection(_normal);
            transformedTriangle._flippedNormal  = -transformedTriangle._normal;
        }

        public bool raycast(Ray ray, out float t)
        {
            t = 0.0f;
            float prjDir = Vector3.Dot(ray.direction, _normal);
            if (Mathf.Abs(prjDir) < 1e-5f) return false;

            float d = Vector3.Dot(ray.origin - _verts[0], _normal);
            t = d / -prjDir;

            if (t < 0.0f) return false;
            return containsPoint(ray.GetPoint(t));
        }

        public bool containsPoint(Vector3 pt)
        {
            Vector3 e = _verts[1] - _verts[0];
            Vector3 edgeNormal = Vector3.Cross(e, _normal).normalized;
            if (Vector3.Dot(pt - _verts[0], edgeNormal) > 0.0f) return false;

            e = _verts[2] - _verts[1];
            edgeNormal = Vector3.Cross(e, _normal).normalized;
            if (Vector3.Dot(pt - _verts[1], edgeNormal) > 0.0f) return false;

            e = _verts[0] - _verts[2];
            edgeNormal = Vector3.Cross(e, _normal).normalized;
            if (Vector3.Dot(pt - _verts[2], edgeNormal) > 0.0f) return false;

            return true;
        }

        public bool intersectsTriangle(PluginMeshTriangle otherTriangle)
        {
            // Check 'this' edges against other plane
            float t;
            Vector3 edge    = _verts[1] - _verts[0];
            Ray ray         = new Ray(_verts[0], edge);
            if (otherTriangle.raycast(ray, out t))
            {
                if (t <= edge.magnitude) return true;
            }
            edge            = _verts[2] - _verts[0];
            ray             = new Ray(_verts[0], edge);
            if (otherTriangle.raycast(ray, out t))
            {
                if (t <= edge.magnitude) return true;
            }
            edge            = _verts[2] - _verts[1];
            ray             = new Ray(_verts[1], edge);
            if (otherTriangle.raycast(ray, out t))
            {
                if (t <= edge.magnitude) return true;
            }

            // Check other edges against 'this' plane
            edge            = otherTriangle._verts[1] - otherTriangle._verts[0];
            ray             = new Ray(otherTriangle._verts[0], edge);
            if (raycast(ray, out t))
            {
                if (t <= edge.magnitude) return true;
            }
            edge            = otherTriangle._verts[2] - otherTriangle._verts[0];
            ray             = new Ray(otherTriangle._verts[0], edge);
            if (raycast(ray, out t))
            {
                if (t <= edge.magnitude) return true;
            }
            edge            = otherTriangle._verts[2] - otherTriangle._verts[1];
            ray             = new Ray(otherTriangle._verts[1], edge);
            if (raycast(ray, out t))
            {
                if (t <= edge.magnitude) return true;
            }

            // Check if 'this' has at least one point on the plane of the other triangle
            Plane trianglePlane = otherTriangle.plane;
            for (int i = 0; i < 3; ++i)
            {
                float d = trianglePlane.GetDistanceToPoint(_verts[i]);
                if (Mathf.Abs(d) < 1e-5f && otherTriangle.containsPoint(_verts[i])) return true;
            }

            // Check if other has at least one point on the plane of 'this' triangle
            trianglePlane = plane;
            for (int i = 0; i < 3; ++i)
            {
                float d = trianglePlane.GetDistanceToPoint(otherTriangle._verts[i]);
                if (Mathf.Abs(d) < 1e-5f && containsPoint(otherTriangle._verts[i])) return true;
            }

            return false;
        }
    }

    public class PluginMesh
    {
        private Mesh                    _unityMesh;
        private Vector3[]               _vertices;
        private int[]                   _vertIndices;
        private int                     _numTriangles;
        private PluginMeshAABBTree      _tree;
        private AABB                    _aabb;
        private PluginMeshTriangle[]    _triangles;

        public Mesh                     unityMesh       { get { return _unityMesh; } }
        public Vector3[]                vertices        { get { return _vertices.Clone() as Vector3[]; } }
        public int                      numTriangles    { get { return _numTriangles; } }
        public AABB                     aabb            { get { return _aabb; } }

        public PluginMesh(Mesh unityMesh)
        {
            _unityMesh      = unityMesh;
            _vertices       = _unityMesh.vertices;
            _vertIndices    = _unityMesh.triangles;
            _numTriangles   = (int)(_vertIndices.Length / 3);

            _triangles      = new PluginMeshTriangle[_numTriangles];
            for (int triIndex = 0; triIndex < _numTriangles; ++triIndex)
            {
                int baseIndex   = triIndex * 3;
                int i0          = _vertIndices[baseIndex];
                int i1          = _vertIndices[baseIndex + 1];
                int i2          = _vertIndices[baseIndex + 2];

                _triangles[triIndex] = new PluginMeshTriangle(_vertices[i0], _vertices[i1], _vertices[i2], triIndex);
            }

            _tree = new PluginMeshAABBTree();
            _tree.build(this);
            _aabb = new AABB(_unityMesh.bounds);
        }

        public PluginMeshTriangle getTriangle(int triangleIndex)
        {
            return _triangles[triangleIndex];
        }

        public void getTriangleVerts(int triangleIndex, Vector3[] verts)
        {
            _triangles[triangleIndex].verts.CopyTo(verts, 0);
        }

        public void getTriangleVerts(int triangleIndex, Vector3[] verts, Transform meshTransform)
        {
            verts[0] = meshTransform.TransformPoint(_triangles[triangleIndex].verts[0]);
            verts[1] = meshTransform.TransformPoint(_triangles[triangleIndex].verts[1]);
            verts[2] = meshTransform.TransformPoint(_triangles[triangleIndex].verts[2]);
        }

        public void getTriangleVerts(int triangleIndex, Vector3[] verts, Matrix4x4 meshTransform)
        {
            verts[0] = meshTransform.MultiplyPoint(_triangles[triangleIndex].verts[0]);
            verts[1] = meshTransform.MultiplyPoint(_triangles[triangleIndex].verts[1]);
            verts[2] = meshTransform.MultiplyPoint(_triangles[triangleIndex].verts[2]);
        }

        public void getTriangleVerts(int triangleIndex, Vector3[] verts, Transform meshTransform, bool flipWindingOrder)
        {
            verts[0] = meshTransform.TransformPoint(_triangles[triangleIndex].verts[0]);
            if (!flipWindingOrder)
            {
                verts[1] = meshTransform.TransformPoint(_triangles[triangleIndex].verts[1]);
                verts[2] = meshTransform.TransformPoint(_triangles[triangleIndex].verts[2]);
            }
            else
            {
                verts[1] = meshTransform.TransformPoint(_triangles[triangleIndex].verts[2]);
                verts[2] = meshTransform.TransformPoint(_triangles[triangleIndex].verts[1]);
            }
        }

        public bool raycastClosest(Ray ray, Transform meshTransform, MeshRaycastConfig raycastConfig, out MeshRayHit rayHit)
        {
            return _tree.raycastClosest(ray, meshTransform, raycastConfig, out rayHit);
        }

        public void vertsOverlapBox(OBB box, Transform meshTransform, List<Vector3> verts)
        {
            _tree.vertsOverlapBox(box, meshTransform, verts);
        }

        public void modelVertsOverlapBox(OBB box, List<Vector3> verts)
        {
            _tree.modelVertsOverlapBox(box, verts);
        }

        public void modelVertsOverlapBox(AABB box, List<Vector3> verts)
        {
            _tree.modelVertsOverlapBox(box, verts);
        }

        public bool trianglesOverlapBox(OBB box, Transform meshTransform)
        {
            return _tree.trianglesOverlapBox(box, meshTransform);
        }

        public bool trianglesOverlapBox(OBB box, Transform meshTransform, List<PluginMeshTriangle> triangles)
        {
            return _tree.trianglesOverlapBox(box, meshTransform, triangles);
        }

        [NonSerialized]
        private List<PluginMeshTriangle>    _otherTriangles             = new List<PluginMeshTriangle>();
        [NonSerialized]
        private List<PluginMeshTriangle>    _thisTriangles              = new List<PluginMeshTriangle>();
        [NonSerialized]
        private PluginMeshTriangle          _otherTransformTriangle     = new PluginMeshTriangle();
        [NonSerialized]
        private PluginMeshTriangle          _thisTransformTriangle      = new PluginMeshTriangle();
        public bool trianglesIntersectTriangles(Transform thisTransform, float thisOBBInflate, PluginMesh otherMesh, Transform otherTransform)
        {
            OBB thisOBB     = ObjectBounds.calcMeshWorldOBB(thisTransform.gameObject);
            if (!thisOBB.isValid) return false;
            thisOBB.inflate(thisOBBInflate);
            OBB otherOBB    = ObjectBounds.calcMeshWorldOBB(otherTransform.gameObject);
            if (!otherOBB.isValid) return false;

            if (otherMesh.trianglesOverlapBox(thisOBB, otherTransform, _otherTriangles))
            {
                int numOtherTriangles = _otherTriangles.Count;;
                for (int i = 0; i < numOtherTriangles; ++i) 
                {
                    var otherTriangle       = _otherTriangles[i];
                    otherTriangle.getTransformedTriangle_VertsAndNormal(otherTransform, _otherTransformTriangle);
                    OBB otherTriangleOBB    = new OBB(new AABB(_otherTransformTriangle.verts));
                    if (trianglesOverlapBox(otherTriangleOBB, thisTransform, _thisTriangles))
                    {
                        int numTriangles    = _thisTriangles.Count;
                        for (int j = 0; j < numTriangles; ++j) 
                        {
                            _thisTriangles[j].getTransformedTriangle_VertsAndNormal(thisTransform, _thisTransformTriangle);
                            if (_otherTransformTriangle.intersectsTriangle(_thisTransformTriangle))
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
#endif