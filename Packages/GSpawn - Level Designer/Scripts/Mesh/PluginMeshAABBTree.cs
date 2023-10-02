#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class PluginMeshAABBTree
    {
        private class Node
        {
            // Note: Center and radius are also stored for optimization purposes.
            public Bounds               aabb        = new Bounds();
            public OBB                  obb         = new OBB(Vector3.zero, Vector3.zero, Quaternion.identity);  // Note: Useful during overlap checks.
            public Vector3              center;
            public float                radius;       

            public PluginMeshTriangle   meshTriangle;
            public bool                 isLeaf;

            public Node                 parent;
            public Node                 firstChild;
            public Node                 secondChild;
            public Node                 stack_Previous;

            // Note: Assumes the node has 2 children (i.e. both child node references are valid).
            public void encloseChildren()
            {
                aabb.min    = firstChild.aabb.min;
                aabb.min    = Vector3.Min(aabb.min, secondChild.aabb.min);

                aabb.max    = firstChild.aabb.max;
                aabb.max    = Vector3.Max(aabb.max, secondChild.aabb.max);

                radius      = aabb.extents.magnitude;

                obb         = new OBB(aabb);
                center      = obb.center;
            }
        }

        private class NodeStack
        {
            public Node top;

            public void push(Node node)
            {
                node.stack_Previous = top;
                top                 = node;
            }

            public Node pop()
            {
                var ret     = top;
                top         = top.stack_Previous;
                return ret;
            }
        }

        private PluginMesh      _mesh;
        private Node            _root;
        private NodeStack       _nodeStack = new NodeStack();

        public bool build(PluginMesh mesh)
        {
            _mesh = mesh;
            _root = new Node();

            int numTriangles = mesh.numTriangles;
            for (int triIndex = 0; triIndex < numTriangles; ++triIndex)
            {
                integrateTriangle(_mesh.getTriangle(triIndex));
            }

            return true;
        }

        public void debugDraw(Color aabbColor, Matrix4x4 meshTransform)
        {
            HandlesEx.saveColor();
            HandlesEx.saveMatrix();

            Handles.color = aabbColor;

            if (_root.firstChild != null) _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null) _nodeStack.push(_root.secondChild);

            while (_nodeStack.top != null)
            {
                var node = _nodeStack.pop();

                if (!node.isLeaf)
                {
                    _nodeStack.push(node.firstChild);
                    _nodeStack.push(node.secondChild);
                }

                Handles.matrix = meshTransform * Matrix4x4.TRS(node.aabb.center, Quaternion.identity, node.aabb.extents * 2.0f);
                HandlesEx.drawUnitWireCube();
            }

            HandlesEx.restoreMatrix();
            HandlesEx.restoreColor();
        }

        public bool raycastClosest(Ray ray, Transform meshTransform, MeshRaycastConfig raycastConfig, out MeshRayHit rayHit)
        {
            Ray modelRay = meshTransform.worldToLocalMatrix.transformRay(ray);

            Node hitLeaf = null;
            float minT = float.MaxValue;
            float t;

            if (_root.firstChild != null) _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null) _nodeStack.push(_root.secondChild);

            if (raycastConfig.canHitCameraCulledFaces)
            {
                while (_nodeStack.top != null)
                {
                    var node = _nodeStack.pop();
                    if (node.aabb.IntersectRay(modelRay))
                    {
                        if (!node.isLeaf)
                        {
                            _nodeStack.push(node.firstChild);
                            _nodeStack.push(node.secondChild);
                        }
                        else
                        {
                            if (node.meshTriangle.raycast(modelRay, out t))
                            {
                                if (t < minT)
                                {
                                    minT = t;
                                    hitLeaf = node;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Camera camera   = PluginCamera.camera;
                Vector3 camPos  = meshTransform.InverseTransformPoint(camera.transform.position);
                Vector3 camLook = meshTransform.worldToLocalMatrix.MultiplyVector(camera.transform.forward);

                if (!camera.orthographic)
                {
                    while (_nodeStack.top != null)
                    {
                        var node = _nodeStack.pop();
                        if (node.aabb.IntersectRay(modelRay))
                        {
                            if (!node.isLeaf)
                            {
                                _nodeStack.push(node.firstChild);
                                _nodeStack.push(node.secondChild);
                            }
                            else
                            {
                                if (Vector3.Dot(node.meshTriangle.v0 - camPos, node.meshTriangle.normal) < 0.0f)
                                {
                                    if (node.meshTriangle.raycast(modelRay, out t))
                                    {
                                        if (t < minT)
                                        {
                                            minT = t;
                                            hitLeaf = node;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    while (_nodeStack.top != null)
                    {
                        var node = _nodeStack.pop();
                        if (node.aabb.IntersectRay(modelRay))
                        {
                            if (!node.isLeaf)
                            {
                                _nodeStack.push(node.firstChild);
                                _nodeStack.push(node.secondChild);
                            }
                            else
                            {
                                if (Vector3.Dot(camLook, node.meshTriangle.normal) < 0.0f)
                                {
                                    if (node.meshTriangle.raycast(modelRay, out t))
                                    {
                                        if (t < minT)
                                        {
                                            minT = t;
                                            hitLeaf = node;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (hitLeaf == null)
            {
                rayHit = new MeshRayHit();
                return false;
            }

            bool flipTriangle   = raycastConfig.flipNegativeScaleTriangles && (meshTransform.lossyScale.countNegative() != 0);
            Vector3 hitPoint    = meshTransform.TransformPoint(modelRay.GetPoint(minT));
            Vector3 hitNormal;
            if (flipTriangle) 
            {
                Camera camera   = PluginCamera.camera;
                Vector3 camPos  = camera.transform.position;
                Vector3 camLook = camera.transform.forward;

                Vector3 v0      = meshTransform.TransformPoint(hitLeaf.meshTriangle.v0);
                Vector3 v1      = meshTransform.TransformPoint(hitLeaf.meshTriangle.v1);
                Vector3 v2      = meshTransform.TransformPoint(hitLeaf.meshTriangle.v2);
                hitNormal       = Vector3.Cross((v1 - v0), (v2 - v0)).normalized;

                if (!camera.orthographic)
                {
                    if (Vector3.Dot(hitPoint - camPos, hitNormal) >= 0.0f) hitNormal = -hitNormal;
                }
                else
                {
                    if (Vector3.Dot(camLook, hitNormal) >= 0.0f) hitNormal = -hitNormal;
                }
            }
            else hitNormal = meshTransform.TransformDirection(hitLeaf.meshTriangle.normal);
    
            rayHit = new MeshRayHit(ray, (hitPoint - ray.origin).magnitude, hitLeaf.meshTriangle.triangleIndex, hitNormal);
            return true;
        }

        public bool vertsOverlapBox(OBB box, Transform meshTransform, List<Vector3> verts)
        {
            verts.Clear();

            OBB modelOBB        = new OBB(box);
            modelOBB.transform(meshTransform.worldToLocalMatrix);

            Vector3 boxRight    = modelOBB.right;
            Vector3 boxUp       = modelOBB.up;
            Vector3 boxLook     = modelOBB.look;
            
            if (_root.firstChild != null) _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null) _nodeStack.push(_root.secondChild);

            while (_nodeStack.top != null)
            {
                var node = _nodeStack.pop();

                // Note: It's faster to use a sphere-OBB intersection test than OBB-OBB intersection test.
                Vector3 closestPt = modelOBB.calcClosestPoint(node.center, boxRight, boxUp, boxLook);
                if (Sphere.containsPoint(node.center, node.radius, closestPt))
                //if (node.obb.intersectsOBB(modelOBB))
                {
                    if (!node.isLeaf)
                    {
                        _nodeStack.push(node.firstChild);
                        _nodeStack.push(node.secondChild);
                    }
                    else
                    {
                        PluginMeshTriangle meshTriangle = node.meshTriangle;

                        if (modelOBB.containsPoint(meshTriangle.v0, boxRight, boxUp, boxLook)) verts.Add(meshTransform.TransformPoint(meshTriangle.v0));
                        if (modelOBB.containsPoint(meshTriangle.v1, boxRight, boxUp, boxLook)) verts.Add(meshTransform.TransformPoint(meshTriangle.v1));
                        if (modelOBB.containsPoint(meshTriangle.v2, boxRight, boxUp, boxLook)) verts.Add(meshTransform.TransformPoint(meshTriangle.v2));
                    }
                }
            }

            return verts.Count != 0;
        }

        public bool modelVertsOverlapBox(OBB box, List<Vector3> verts)
        {
            verts.Clear();

            Vector3 boxRight    = box.right;
            Vector3 boxUp       = box.up;
            Vector3 boxLook     = box.look;

            if (_root.firstChild != null) _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null) _nodeStack.push(_root.secondChild);

            while (_nodeStack.top != null)
            {
                var node = _nodeStack.pop();
                if (node.obb.intersectsOBB(box))
                {
                    if (!node.isLeaf)
                    {
                        _nodeStack.push(node.firstChild);
                        _nodeStack.push(node.secondChild);
                    }
                    else
                    {
                        PluginMeshTriangle meshTriangle = node.meshTriangle;

                        if (box.containsPoint(meshTriangle.v0, boxRight, boxUp, boxLook)) verts.Add(meshTriangle.v0);
                        if (box.containsPoint(meshTriangle.v1, boxRight, boxUp, boxLook)) verts.Add(meshTriangle.v1);
                        if (box.containsPoint(meshTriangle.v2, boxRight, boxUp, boxLook)) verts.Add(meshTriangle.v2);
                    }
                }
            }

            return verts.Count != 0;
        }

        public bool modelVertsOverlapBox(AABB box, List<Vector3> verts)
        {
            verts.Clear();

            if (_root.firstChild != null) _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null) _nodeStack.push(_root.secondChild);

            Bounds bounds = box.toBounds();
            while (_nodeStack.top != null)
            {
                var node = _nodeStack.pop();
                if (node.aabb.Intersects(bounds))
                {
                    if (!node.isLeaf)
                    {
                        _nodeStack.push(node.firstChild);
                        _nodeStack.push(node.secondChild);
                    }
                    else
                    {
                        PluginMeshTriangle meshTriangle = node.meshTriangle;

                        if (box.containsPoint(meshTriangle.v0)) verts.Add(meshTriangle.v0);
                        if (box.containsPoint(meshTriangle.v1)) verts.Add(meshTriangle.v1);
                        if (box.containsPoint(meshTriangle.v2)) verts.Add(meshTriangle.v2);
                    }
                }
            }

            return verts.Count != 0;
        }

        public bool trianglesOverlapBox(OBB box, Transform meshTransform)
        {
            OBB modelOBB = new OBB(box);
            modelOBB.transform(meshTransform.worldToLocalMatrix);

            Vector3 boxRight    = modelOBB.right;
            Vector3 boxUp       = modelOBB.up;
            Vector3 boxLook     = modelOBB.look;

            if (_root.firstChild != null) _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null) _nodeStack.push(_root.secondChild);

            while (_nodeStack.top != null)
            {
                var node = _nodeStack.pop();

                // Note: It's faster to use a sphere-OBB intersection test than OBB-OBB intersection test.
                Vector3 closestPt = modelOBB.calcClosestPoint(node.center, boxRight, boxUp, boxLook);
                if (Sphere.containsPoint(node.center, node.radius, closestPt))
                //if (node.obb.intersectsOBB(modelOBB))
                {
                    if (!node.isLeaf)
                    {
                        _nodeStack.push(node.firstChild);
                        _nodeStack.push(node.secondChild);
                    }
                    else
                    {
                        PluginMeshTriangle meshTriangle = node.meshTriangle;
                        if (modelOBB.intersectsTriangle(meshTriangle.v0, meshTriangle.v1, meshTriangle.v2))
                        {
                            _nodeStack.top = null;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool trianglesOverlapBox(OBB box, Transform meshTransform, List<PluginMeshTriangle> triangles)
        {
            triangles.Clear();

            OBB modelOBB = new OBB(box);
            modelOBB.transform(meshTransform.worldToLocalMatrix);

            Vector3 boxRight    = modelOBB.right;
            Vector3 boxUp       = modelOBB.up;
            Vector3 boxLook     = modelOBB.look;

            if (_root.firstChild != null) _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null) _nodeStack.push(_root.secondChild);

            while (_nodeStack.top != null)
            {
                var node = _nodeStack.pop();

                // Note: It's faster to use a sphere-OBB intersection test than OBB-OBB intersection test.
                Vector3 closestPt = modelOBB.calcClosestPoint(node.center, boxRight, boxUp, boxLook);
                if (Sphere.containsPoint(node.center, node.radius, closestPt))
                //if (node.obb.intersectsOBB(modelOBB))
                {
                    if (!node.isLeaf)
                    {
                        _nodeStack.push(node.firstChild);
                        _nodeStack.push(node.secondChild);
                    }
                    else
                    {
                        PluginMeshTriangle meshTriangle = node.meshTriangle;
                        if (modelOBB.intersectsTriangle(meshTriangle.v0, meshTriangle.v1, meshTriangle.v2))
                            triangles.Add(meshTriangle);
                    }
                }
            }

            return triangles.Count != 0;
        }

        private void integrateTriangle(PluginMeshTriangle meshTriangle)
        {
            var node            = new Node();
            node.meshTriangle   = meshTriangle;
            node.isLeaf         = true;
            node.aabb.min       = meshTriangle.verts[0];
            node.aabb.min       = Vector3.Min(node.aabb.min, meshTriangle.verts[1]);
            node.aabb.min       = Vector3.Min(node.aabb.min, meshTriangle.verts[2]);
            node.aabb.max       = meshTriangle.verts[0];
            node.aabb.max       = Vector3.Max(node.aabb.max, meshTriangle.verts[1]);
            node.aabb.max       = Vector3.Max(node.aabb.max, meshTriangle.verts[2]);
            node.obb            = new OBB(node.aabb);
            node.center         = node.aabb.center;
            node.radius         = node.aabb.extents.magnitude;
            
            // Note: The root node doesn't have to have a volume.
            if (_root.firstChild == null)
            {
                node.parent = _root;
                _root.firstChild = node;
                return;
            }
            else
            if (_root.secondChild == null)
            {
                node.parent = _root;
                _root.secondChild = node;
                return;
            }

            Node currentNode = _root;
            while (!currentNode.isLeaf)
            {
                if (((node.center - currentNode.firstChild.center).magnitude + currentNode.firstChild.radius) <
                    ((node.center - currentNode.secondChild.center).magnitude + currentNode.secondChild.radius)) currentNode = currentNode.firstChild;
                else currentNode = currentNode.secondChild;
            }

            Node p              = currentNode.parent;
            Node newParentNode  = new Node();

            if (currentNode == p.firstChild) p.firstChild = newParentNode;
            else p.secondChild = newParentNode;

            newParentNode.parent        = p;
            node.parent                 = newParentNode;
            currentNode.parent          = newParentNode;

            newParentNode.firstChild    = currentNode;
            newParentNode.secondChild   = node;
            encloseChildren(newParentNode);
        }

        private void encloseChildren(Node startNode)
        {
            // Note: We stop at the root node. The root node doesn't need to have its volume
            //	     updated. This allows us to assume that all nodes whose spheres need to
            //		 be updated have 2 children. We have no such guarantee with the root node.
            Node currentNode = startNode;
            while (currentNode != _root)
            {
                currentNode.encloseChildren();
                currentNode = currentNode.parent;
            }
        }
    }
}
#endif