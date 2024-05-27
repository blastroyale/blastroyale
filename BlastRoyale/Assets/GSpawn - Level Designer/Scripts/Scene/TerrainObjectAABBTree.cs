#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace GSpawn
{
    public class TerrainObjectAABBTree
    {
        private class Node
        {
            public Bounds       aabb            = new Bounds();
            public OBB          obb             = new OBB(Vector3.zero, Vector3.zero, Quaternion.identity);  
            public float        radius;
            public GameObject   terrainObject;
            public bool         isLeaf;

            public Node         parent;
            public Node         firstChild;
            public Node         secondChild;
            public Node         stack_Previous;

            // Note: Assumes the node has 2 children (i.e. both child node references are valid).
            public void encloseChildren()
            {
                aabb.min    = firstChild.aabb.min;
                aabb.min    = Vector3.Min(aabb.min, secondChild.aabb.min);

                aabb.max    = firstChild.aabb.max;
                aabb.max    = Vector3.Max(aabb.max, secondChild.aabb.max);

                radius      = aabb.extents.magnitude;
                obb         = new OBB(aabb);
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
                var ret             = top;
                top                 = top.stack_Previous;
                return ret;
            }
        }

        private Node                            _root;
        private NodeStack                       _nodeStack              = new NodeStack();
        private Dictionary<Terrain, Node>       _unityTerrainMap        = new Dictionary<Terrain, Node>();
        private Dictionary<GameObject, Node>    _terrainMeshMap         = new Dictionary<GameObject, Node>();
        private ObjectBounds.QueryConfig        _terrainBoundsQConfig   = ObjectBounds.QueryConfig.defaultConfig;

        public TerrainObjectAABBTree()
        {
            clear();
            _terrainBoundsQConfig.objectTypes = GameObjectType.Terrain | GameObjectType.Mesh; 
        }

        public void getTerrains(TerrainCollection terrains)
        {
            terrains.clear();

            foreach (var pair in _unityTerrainMap)
                terrains.unityTerrains.Add(pair.Key);

            foreach (var pair in _terrainMeshMap)
                terrains.terrainMeshes.Add(pair.Key);
        }

        public void getUnityTerrains(List<Terrain> unityTerrains)
        {
            unityTerrains.Clear();
            foreach (var pair in _unityTerrainMap)
                unityTerrains.Add(pair.Key);
        }

        public void getTerrainMeshes(List<GameObject> terrainMeshes)
        {
            terrainMeshes.Clear();
            foreach (var pair in _terrainMeshMap)
                terrainMeshes.Add(pair.Key);
        }

        public void clear()
        {
            _root           = new Node();
            _root.aabb      = new Bounds(Vector3.zero, Vector3.zero);
            _root.radius    = 0.0f;
        }

        public void handleNullRefs()
        {
            var newUnityTerrainMap = new Dictionary<Terrain, Node>();
            foreach (var pair in _unityTerrainMap)
            {
                if (pair.Key != null) newUnityTerrainMap.Add(pair.Key, pair.Value);
                else eraseLeafNode(pair.Value);
            }

            _unityTerrainMap.Clear();
            _unityTerrainMap = newUnityTerrainMap;

            var newTerrainMeshMap = new Dictionary<GameObject, Node>();
            foreach (var pair in _terrainMeshMap)
            {
                if (pair.Key != null) newTerrainMeshMap.Add(pair.Key, pair.Value);
                else eraseLeafNode(pair.Value);
            }

            _terrainMeshMap.Clear();
            _terrainMeshMap = newTerrainMeshMap;
        }

        public void registerTerrainObject(GameObject terrainObject)
        {
            if (!canRegisterTerrainObject(terrainObject)) return;

            AABB aabb               = ObjectBounds.calcWorldAABB(terrainObject, _terrainBoundsQConfig);
            if (!aabb.isValid) return;

            var leafNode            = new Node();
            leafNode.terrainObject  = terrainObject;
            leafNode.isLeaf         = true;
            leafNode.aabb           = aabb.toBounds();
            leafNode.obb            = new OBB(leafNode.aabb);
            leafNode.radius         = leafNode.aabb.extents.magnitude;
            integrateLeafNode(leafNode);

            if (terrainObject.isTerrainMesh()) _terrainMeshMap.Add(terrainObject, leafNode);
            else _unityTerrainMap.Add(terrainObject.getTerrain(), leafNode);
        }

        public void unregisterTerrainObject(GameObject terrainObject)
        {
            Node node       = null;
            Terrain terrain = terrainObject.getTerrain();
            if (terrain != null && _unityTerrainMap.TryGetValue(terrain, out node))
            {
                _unityTerrainMap.Remove(terrain);
                eraseLeafNode(node);
                return;
            }

            if (_terrainMeshMap.TryGetValue(terrainObject, out node))
            {
                _terrainMeshMap.Remove(terrainObject);
                eraseLeafNode(node);
                return;
            }
        }

        public void onTerrainObjectTransformChanged(GameObject terrainObject)
        {
            Node node               = null;
            Terrain unityTerrain    = terrainObject.getTerrain();
            if (unityTerrain != null)
            {
                if (!_unityTerrainMap.TryGetValue(unityTerrain, out node))
                {
                    if (!_terrainMeshMap.TryGetValue(terrainObject, out node))
                        return;
                }
            }
            else
            {
                if (!_terrainMeshMap.TryGetValue(terrainObject, out node))
                    return;
            }

            AABB terrainAABB = ObjectBounds.calcWorldAABB(terrainObject, _terrainBoundsQConfig);
            if (terrainAABB.isValid) updateNodeAABB(node, terrainAABB);
        }

        public bool canRegisterTerrainObject(GameObject terrainObject)
        {
            if (terrainObject.couldBePooled()) return false;

            Terrain terrain = terrainObject.getTerrain();
            if (terrain != null && _unityTerrainMap.ContainsKey(terrain)) return false;
            if (_terrainMeshMap.ContainsKey(terrainObject)) return false;

            GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(terrainObject);
            if (objectType == GameObjectType.Terrain) return true;
            if (objectType == GameObjectType.Mesh && terrainObject.isTerrainMesh()) return true;
   
            return false;
        }

        public bool overlapBox(OBB box, TerrainObjectOverlapFilter overlapFilter, TerrainObjectOverlapConfig overlapConfig, TerrainCollection terrains)
        {
            terrains.clear();

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
                        GameObject terrainObject = node.terrainObject;
                        if (overlapFilter.filterObject(terrainObject))
                        {
                            if (terrainObject.isTerrainMesh()) terrains.terrainMeshes.Add(terrainObject);
                            else terrains.unityTerrains.Add(terrainObject.getTerrain());
                        }
                    }
                }
            }

            return terrains.unityTerrains.Count != 0 || terrains.terrainMeshes.Count != 0;
        }

        private void integrateLeafNode(Node node)
        {
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
                if (((node.aabb.center - currentNode.firstChild.aabb.center).magnitude + currentNode.firstChild.radius) <
                    ((node.aabb.center - currentNode.secondChild.aabb.center).magnitude + currentNode.secondChild.radius)) currentNode = currentNode.firstChild;
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
            Node currentNode = startNode;
            while (currentNode != _root)
            {
                currentNode.encloseChildren();
                currentNode = currentNode.parent;
            }
        }

        private void eraseLeafNode(Node node)
        {
            Node parentNode = node.parent;
            if (parentNode != _root)
            {
                Node siblingNode = parentNode.firstChild == node ? parentNode.secondChild : parentNode.firstChild;
                Node grandpaNode = parentNode.parent;
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

        private void updateNodeAABB(Node node, AABB newAABB)
        {
            node.aabb   = newAABB.toBounds();
            node.obb    = new OBB(node.aabb);
            node.radius = node.aabb.extents.magnitude;

            Node parentNode = node.parent;
            float d = (node.aabb.center - parentNode.aabb.center).magnitude + node.radius;
            if (d > parentNode.radius)
            {
                eraseLeafNode(node);
                integrateLeafNode(node);
            }
        }
    }
}
#endif