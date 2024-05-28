#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
	public class BinarySphereTreeNode<TData>
        where TData : class
    {
        public Vector3                      center;
        public float                        radius;
        public TData                        data;

        public BinarySphereTreeNode<TData>  parent;
        public BinarySphereTreeNode<TData>  firstChild;
        public BinarySphereTreeNode<TData>  secondChild;
        public BinarySphereTreeNode<TData>  stack_Previous;

        // Note: Assumes the node has 2 children (i.e. both child node references are valid).
        public void encloseChildren(float radiusPadding)
        {
            // Note: This seems to be faster than the other approach in which we
            //		 sum the centers and calculate the average.
            // Note: Start with the child with the largest radius in order to account
            //		 for the case where the smaller one is fully contained within its sibling.
            if (firstChild.radius >= secondChild.radius)
            {
                center = firstChild.center;
                radius = firstChild.radius;
                enclose(secondChild);
            }
            else
            {
                center = secondChild.center;
                radius = secondChild.radius;
                enclose(firstChild);
            }
            radius += radiusPadding;
        }

        public void enclose(BinarySphereTreeNode<TData> node)
        {
            Vector3 dir         = node.center - center;
            float l             = dir.magnitude;
            float d             = l + node.radius;
            if (d > radius)
            {
                float newRadius = (radius + d) * 0.5f;
                center          += dir * ((newRadius - radius) / l); // Note: Divide by l to normalize direction vector.
                radius          = newRadius;
            }
        }

        public bool isLeaf() { return data != null; }
    };

    public struct BinarySphereTreeNodeRayHit<TData>
        where TData : class
    {
        private BinarySphereTreeNode<TData> _hitNode;
        private Vector3                     _hitPoint;
        private float                       _hitEnter;

        public BinarySphereTreeNode<TData>  hitNode     { get { return _hitNode; } }
        public Vector3                      hitPoint    { get { return _hitPoint; } }
        public float                        hitEnter    { get { return _hitEnter; } }

        public BinarySphereTreeNodeRayHit(Ray ray, BinarySphereTreeNode<TData> hitNode, float hitEnter)
        {
            _hitNode    = hitNode;
            _hitEnter   = hitEnter;
            _hitPoint   = ray.GetPoint(_hitEnter);
        }
    }

    public class BinarySphereTree<TData>
        where TData : class
    {
        private class NodeStack
        {
            public BinarySphereTreeNode<TData> top;

            public void push(BinarySphereTreeNode<TData> node)
            {
                node.stack_Previous = top;
                top = node;
            }

            public BinarySphereTreeNode<TData> pop()
            {
                var ret = top;
                top = top.stack_Previous;
                return ret;
            }
        }

        private BinarySphereTreeNode<TData>     _root;
        private float                           _nodeRadiusPadding;
        private NodeStack                       _nodeStack = new NodeStack();

        public bool initialize(float nodeRadiusPadding)
        {
            clear();

            _nodeRadiusPadding = nodeRadiusPadding > 0.0f ? nodeRadiusPadding : 0.0f;
            return true;
        }

        public void clear()
        {
            _root           = new BinarySphereTreeNode<TData>();
            _root.center    = Vector3.zero;
            _root.radius    = 0.0f;
        }

        public BinarySphereTreeNode<TData> createLeafNode(Vector3 sphereCenter, float sphereRadius, TData nodeData)
	    {
            BinarySphereTreeNode<TData> newNode = new BinarySphereTreeNode<TData>();
            newNode.center                      = sphereCenter;
		    newNode.radius                      = sphereRadius;
		    newNode.data                        = nodeData;

		    intergrateLeafNode(newNode);
		    return newNode;
	    }

        public void eraseLeafNode(BinarySphereTreeNode<TData> node)
        {
            removeLeafNode(node);
        }

        public void updateLeafNodeSphere(BinarySphereTreeNode<TData> node, Vector3 newSphereCenter, float newSphereRadius)
	    {
            node.center = newSphereCenter;
		    node.radius = newSphereRadius;

            BinarySphereTreeNode<TData> parentNode = node.parent;
            float d = (node.center - parentNode.center).magnitude + node.radius;
		    if (d > parentNode.radius)
		    {
			    removeLeafNode(node);
                intergrateLeafNode(node);
            }
        }

        public bool overlapBox(OBB box, List<BinarySphereTreeNode<TData>> nodes)
        {
            nodes.Clear();

            Vector3 boxRight    = box.right;
            Vector3 boxUp       = box.up;
            Vector3 boxLook     = box.look;

            if (_root.firstChild != null)   _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null)  _nodeStack.push(_root.secondChild);

            while (_nodeStack.top != null)
            {
                var node = _nodeStack.pop();
                if (Sphere.containsPoint(node.center, node.radius, box.calcClosestPoint(node.center, boxRight, boxUp, boxLook)))
                {
                    if (!node.isLeaf())
                    {
                        _nodeStack.push(node.firstChild);
                        _nodeStack.push(node.secondChild);
                    }
                    else nodes.Add(node);
                }
            }

            return nodes.Count != 0;
        }

        public bool overlapBox(AABB box, List<BinarySphereTreeNode<TData>> nodes)
        {
            nodes.Clear();

            if (_root.firstChild != null)   _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null)  _nodeStack.push(_root.secondChild);

            while (_nodeStack.top != null)
            {
                var node = _nodeStack.pop();
                if (Sphere.containsPoint(node.center, node.radius, box.calcClosestPoint(node.center)))
                {
                    if (!node.isLeaf())
                    {
                        _nodeStack.push(node.firstChild);
                        _nodeStack.push(node.secondChild);
                    }
                    else nodes.Add(node);
                }
            }

            return nodes.Count != 0;
        }

        public bool raycastAll(Ray ray, List<BinarySphereTreeNodeRayHit<TData>> hits, bool sort)
        {
            hits.Clear();

            float t;

            if (_root.firstChild != null)   _nodeStack.push(_root.firstChild);
            if (_root.secondChild != null)  _nodeStack.push(_root.secondChild);

            while (_nodeStack.top != null)
            {
                var node = _nodeStack.pop();
                if (Sphere.raycast(node.center, node.radius, ray, out t))
                {
                    if (!node.isLeaf())
                    {
                        _nodeStack.push(node.firstChild);
                        _nodeStack.push(node.secondChild);
                    }
                    else hits.Add(new BinarySphereTreeNodeRayHit<TData>(ray, node, t));
                }
            }

            if (sort)
            {
                hits.Sort(delegate (BinarySphereTreeNodeRayHit<TData> h0, BinarySphereTreeNodeRayHit<TData> h1)
                { return h0.hitEnter.CompareTo(h1.hitEnter); });
            }

            return hits.Count != 0;
        }

        private void intergrateLeafNode(BinarySphereTreeNode<TData> node)
        {
            // Note: The root node doesn't have to have a volume.
            if (_root.firstChild == null)
            {
                node.parent         = _root;
                _root.firstChild    = node;
                return;
            }
            else
            if (_root.secondChild == null)
            {
                node.parent         = _root;
                _root.secondChild   = node;
                return;
            }

            BinarySphereTreeNode<TData> currentNode = _root;
            while (!currentNode.isLeaf())
            {
                if (((node.center - currentNode.firstChild.center).magnitude + currentNode.firstChild.radius) <
                    ((node.center - currentNode.secondChild.center).magnitude + currentNode.secondChild.radius)) currentNode = currentNode.firstChild;
                else currentNode = currentNode.secondChild;
            }

            BinarySphereTreeNode<TData> p = currentNode.parent;
            BinarySphereTreeNode<TData> newParentNode = new BinarySphereTreeNode<TData>();

            if (currentNode == p.firstChild) p.firstChild = newParentNode;
            else p.secondChild = newParentNode;

            newParentNode.parent        = p;
            node.parent                 = newParentNode;
            currentNode.parent          = newParentNode;

            newParentNode.firstChild    = currentNode;
            newParentNode.secondChild   = node;
            encloseChildren(newParentNode);
        }

        private void removeLeafNode(BinarySphereTreeNode<TData> node)
        {
            BinarySphereTreeNode<TData> parentNode = node.parent;
            if (parentNode != _root)
            {
                // Simply make the sibling take the place of the parent
                BinarySphereTreeNode<TData> siblingNode = parentNode.firstChild == node ? parentNode.secondChild : parentNode.firstChild;
                BinarySphereTreeNode<TData> grandpaNode = parentNode.parent;
                siblingNode.parent = grandpaNode;

                if (grandpaNode.firstChild == parentNode) grandpaNode.firstChild = siblingNode;
                else grandpaNode.secondChild = siblingNode;
                encloseChildren(grandpaNode);
            }
            else
            {
                if (_root.firstChild == node) _root.firstChild = _root.secondChild;
                _root.secondChild = null;
            }

            node.parent = null;
        }

        private void encloseChildren(BinarySphereTreeNode<TData> startNode)
        {
            // Note: We stop at the root node. The root node doesn't need to have its volume
            //	     updated. This allows us to assume that all nodes whose spheres need to
            //		 be updated have 2 children. We have no such guarantee with the root node.
            BinarySphereTreeNode<TData> currentNode = startNode;
            while (currentNode != _root)
            {
                currentNode.encloseChildren(_nodeRadiusPadding);
                currentNode = currentNode.parent;
            }
        }
    }
}
#endif