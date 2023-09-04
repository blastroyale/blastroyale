#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class SceneObjectTree
    {
        [NonSerialized]
        private List<BinarySphereTreeNode<GameObject>>          _overlappedNodes        = new List<BinarySphereTreeNode<GameObject>>();
        [NonSerialized]
        private List<BinarySphereTreeNodeRayHit<GameObject>>    _nodeHitBuffer          = new List<BinarySphereTreeNodeRayHit<GameObject>>();
        [NonSerialized]
        private List<Vector3>                                   _fullOverlapCorners     = new List<Vector3>();
        [NonSerialized]
        private HashSet<GameObject>                             _prefabInstanceRoots    = new HashSet<GameObject>();
        [NonSerialized]
        private ObjectBounds.QueryConfig                        _nodeBoundsQConfig;
        [NonSerialized]
        private ObjectBounds.QueryConfig                        _objectBoundsQConfig;
        [NonSerialized]
        private GameObjectType                                  _recognizedObjectTypes  = GameObjectType.Mesh | GameObjectType.Sprite | GameObjectType.Terrain | 
                                                                                          GameObjectType.Light | GameObjectType.ParticleSystem | GameObjectType.Camera | GameObjectType.Empty;
        [NonSerialized]
        private BinarySphereTree<GameObject>                                _tree       = new BinarySphereTree<GameObject>();
        [NonSerialized]
        private Dictionary<GameObject, BinarySphereTreeNode<GameObject>>    _nodeMap    = new Dictionary<GameObject, BinarySphereTreeNode<GameObject>>();

        public SceneObjectTree()
        {
            _nodeBoundsQConfig                      = new ObjectBounds.QueryConfig();
            _nodeBoundsQConfig.volumelessSize       = Vector3.one * 0.1f;
            _nodeBoundsQConfig.objectTypes          = _recognizedObjectTypes;
            _nodeBoundsQConfig.includeInactive      = true;
            _nodeBoundsQConfig.includeInvisible     = true;

            _objectBoundsQConfig                    = new ObjectBounds.QueryConfig();
            _objectBoundsQConfig.volumelessSize     = Vector3.one;
            _objectBoundsQConfig.objectTypes        = _recognizedObjectTypes;

            _tree.initialize(0.0f);
        }

        public void clear()
        {
            _tree.clear();
            _nodeMap.Clear();
        }

        public bool overlapBox(OBB box, ObjectOverlapFilter overlapFilter, ObjectOverlapConfig overlapConfig)
        {
            if (!_tree.overlapBox(box, _overlappedNodes)) return false;

            if (overlapConfig.requireFullOverlap)
            {
                foreach (var node in _overlappedNodes)
                {
                    GameObject gameObject = node.data;
                    if (gameObject == null || !gameObject.activeInHierarchy || LayerEx.isLayerHidden(gameObject.layer) ||
                        SceneVisibilityManager.instance.IsHidden(gameObject, false) ||
                       (overlapFilter != null && !overlapFilter.filterObject(gameObject))) continue;
      
                    OBB worldOBB = ObjectBounds.calcWorldOBB(gameObject, _objectBoundsQConfig);
                    if (worldOBB.isValid)
                    {
                        worldOBB.calcCorners(_fullOverlapCorners, false);
                        if (box.containsPoints(_fullOverlapCorners))
                        {
                            if (overlapConfig.prefabMode != ObjectOverlapPrefabMode.None)
                            {
                                GameObject prefabRoot = gameObject.getOutermostPrefabInstanceRoot();
                                if (prefabRoot != null || overlapConfig.prefabMode != ObjectOverlapPrefabMode.OnlyPrefabInstanceRoot) return true;
                            }
                            else return true;
                        }
                    }
                }
            }
            else
            {
                foreach (var node in _overlappedNodes)
                {
                    GameObject gameObject = node.data;
                    if (gameObject == null || !gameObject.activeInHierarchy || LayerEx.isLayerHidden(gameObject.layer) ||
                        SceneVisibilityManager.instance.IsHidden(gameObject, false) ||
                        (overlapFilter != null && !overlapFilter.filterObject(gameObject))) continue;

                    OBB worldOBB = ObjectBounds.calcWorldOBB(gameObject, _objectBoundsQConfig);
                    if (worldOBB.isValid && worldOBB.intersectsOBB(box))
                    {
                        if (overlapConfig.prefabMode != ObjectOverlapPrefabMode.None)
                        {
                            GameObject prefabRoot = gameObject.getOutermostPrefabInstanceRoot();
                            if (prefabRoot != null || overlapConfig.prefabMode != ObjectOverlapPrefabMode.OnlyPrefabInstanceRoot) return true;
                        }
                        else return true;
                    }
                }
            }

            return false;
        }

        public bool overlapBox(OBB box, ObjectOverlapFilter overlapFilter, ObjectOverlapConfig overlapConfig, List<GameObject> overlappedObjects)
        {
            overlappedObjects.Clear();
            if (!_tree.overlapBox(box, _overlappedNodes)) return false;
         
            _prefabInstanceRoots.Clear();
            if (overlapConfig.requireFullOverlap)
            {
                foreach (var node in _overlappedNodes)
                {
                    GameObject gameObject = node.data;
                    // Note: This should not be necessary, but seems to happen when holding down ALT key (e.g. during object segments selection).
                    if (gameObject == null) continue;
              
                    if (!gameObject.activeInHierarchy || LayerEx.isLayerHidden(gameObject.layer) ||
                        SceneVisibilityManager.instance.IsHidden(gameObject, false) ||
                       (overlapFilter != null && !overlapFilter.filterObject(gameObject))) continue;

                    OBB worldOBB = ObjectBounds.calcWorldOBB(gameObject, _objectBoundsQConfig);
                    if (worldOBB.isValid)
                    {   
                        worldOBB.calcCorners(_fullOverlapCorners, false);
                        if (box.containsPoints(_fullOverlapCorners))
                        {
                            if (overlapConfig.prefabMode != ObjectOverlapPrefabMode.None)
                            {
                                GameObject prefabRoot = gameObject.getOutermostPrefabInstanceRoot();
                                if (prefabRoot != null)
                                {
                                    if (!_prefabInstanceRoots.Contains(prefabRoot))
                                    {
                                        _prefabInstanceRoots.Add(prefabRoot);
                                        overlappedObjects.Add(prefabRoot);
                                    }
                                }
                                else
                                if (overlapConfig.prefabMode != ObjectOverlapPrefabMode.OnlyPrefabInstanceRoot)
                                {
                                    overlappedObjects.Add(gameObject);
                                }
                            }
                            else overlappedObjects.Add(gameObject);
                        }
                    }
                }
            }
            else
            {
                foreach (var node in _overlappedNodes)
                {
                    GameObject gameObject = node.data;
                    // Note: This should not be necessary, but seems to happen when holding down ALT key (e.g. during object path selection).
                    if (gameObject == null) continue;

                    if (!gameObject.activeInHierarchy || LayerEx.isLayerHidden(gameObject.layer) ||
                        SceneVisibilityManager.instance.IsHidden(gameObject, false) ||
                        (overlapFilter != null && !overlapFilter.filterObject(gameObject))) continue;
              
                    OBB worldOBB = ObjectBounds.calcWorldOBB(gameObject, _objectBoundsQConfig);
                    if (worldOBB.isValid && worldOBB.intersectsOBB(box))
                    {
                        if (overlapConfig.prefabMode != ObjectOverlapPrefabMode.None)
                        {
                            GameObject prefabRoot = gameObject.getOutermostPrefabInstanceRoot();
                            if (prefabRoot != null)
                            {
                                if (!_prefabInstanceRoots.Contains(prefabRoot))
                                {
                                    _prefabInstanceRoots.Add(prefabRoot);
                                    overlappedObjects.Add(prefabRoot);
                                }
                            }
                            else 
                            if (overlapConfig.prefabMode != ObjectOverlapPrefabMode.OnlyPrefabInstanceRoot)
                                overlappedObjects.Add(gameObject);
                        }
                        else overlappedObjects.Add(gameObject);
                    }
                }
            }

            return overlappedObjects.Count != 0;
        }

        public bool raycastAll(Ray ray, SceneRaycastFilter raycastFilter, ObjectRaycastConfig raycastConfig, bool sort, List<ObjectRayHit> objectHits)
        {
            objectHits.Clear();
            if (!_tree.raycastAll(ray, _nodeHitBuffer, false)) return false;

            OBB worldOBB = new OBB();
            if (raycastConfig.raycastPrecision == ObjectRaycastPrecision.BestFit)
            {
                foreach (var hit in _nodeHitBuffer)
                {
                    GameObject gameObject = hit.hitNode.data;
                    if (gameObject == null) continue;       // Note: This should not be necessary, but seems to happen with prefab drag and drop in scene view.

                    if (!gameObject.activeInHierarchy || LayerEx.isLayerHidden(gameObject.layer) ||
                        SceneVisibilityManager.instance.IsHidden(gameObject, false) ||
                        (raycastFilter != null && !raycastFilter.filterObject(gameObject))) continue;

                    // ---------------------------------- Mesh ---------------------------------- //
                    Mesh mesh = gameObject.getMesh();
                    if (mesh != null)
                    {
                        if (gameObject.isMeshOrSkinnedMeshRendererEnabled())
                        {
                            PluginMesh pluginMesh = PluginMeshDb.instance.getPluginMesh(mesh);
                            MeshRayHit meshRayHit;
                            if (pluginMesh.raycastClosest(ray, gameObject.transform, raycastConfig.meshConfig, out meshRayHit))
                                objectHits.Add(new ObjectRayHit(ray, gameObject, meshRayHit));
                        }
                        continue;
                    }

                    // ---------------------------------- Terrain ---------------------------------- //
                    TerrainCollider terrainCollider = gameObject.getTerrainCollider();
                    if (terrainCollider != null)
                    {
                        if (gameObject.isTerrainEnabled())
                        {
                            RaycastHit raycastHit;
                            if (terrainCollider.Raycast(ray, out raycastHit, float.MaxValue))
                            {
                                Vector3 hitNormal = raycastHit.normal;
                                if (raycastConfig.terrainConfig.useInterpolatedNormal)
                                    hitNormal = terrainCollider.gameObject.getTerrain().getInterpolatedNormal(raycastHit.point);

                                objectHits.Add(new ObjectRayHit(ray, gameObject, hitNormal, raycastHit.distance));
                            }
                        }
                        continue;
                    }

                    // ---------------------------------- Sprite ---------------------------------- //
                    Sprite sprite = gameObject.getSprite();
                    if (sprite != null)
                    {
                        if (gameObject.isSpriteRendererEnabled())
                        {
                            worldOBB = ObjectBounds.calcSpriteWorldOBB(gameObject);
                            if (worldOBB.isValid)
                            {
                                float t;
                                if (worldOBB.raycast(ray, out t))
                                {
                                    Vector3 hitPt               = ray.GetPoint(t);
                                    Box3DFace faceClosestToPt   = Box3D.findFaceClosestToPoint(hitPt, worldOBB.center, worldOBB.size, worldOBB.rotation);
                                    Vector3 faceNormal          = Box3D.calcFaceNormal(worldOBB.center, worldOBB.size, worldOBB.rotation, faceClosestToPt);
                                    objectHits.Add(new ObjectRayHit(ray, gameObject, faceNormal, t));
                                }
                            }
                        }
                        continue;
                    }

                    // ---------------------------------- Misc ---------------------------------- //
                    worldOBB = ObjectBounds.calcWorldOBB(gameObject, _objectBoundsQConfig);
                    if (worldOBB.isValid)
                    {
                        float t;
                        if (worldOBB.raycast(ray, out t))
                        {
                            Vector3 hitPt               =  ray.GetPoint(t);
                            Box3DFace faceClosestToPt   = Box3D.findFaceClosestToPoint(hitPt, worldOBB.center, worldOBB.size, worldOBB.rotation);
                            Vector3 faceNormal          = Box3D.calcFaceNormal(worldOBB.center, worldOBB.size, worldOBB.rotation, faceClosestToPt);
                            objectHits.Add(new ObjectRayHit(ray, gameObject, faceNormal, t));
                        }
                    }
                }
            }
            else
            if (raycastConfig.raycastPrecision == ObjectRaycastPrecision.Box)
            {
                foreach (var hit in _nodeHitBuffer)
                {
                    GameObject gameObject = hit.hitNode.data;
                    if (!gameObject.activeInHierarchy || LayerEx.isLayerHidden(gameObject.layer) ||
                        SceneVisibilityManager.instance.IsHidden(gameObject, false) ||
                        !raycastFilter.filterObject(gameObject)) continue;

                    worldOBB = ObjectBounds.calcWorldOBB(gameObject, _objectBoundsQConfig);
                    if (worldOBB.isValid)
                    {
                        float t;
                        if (worldOBB.raycast(ray, out t))
                        {
                            Vector3 hitPt               = ray.GetPoint(t);
                            Box3DFace faceClosestToPt   = Box3D.findFaceClosestToPoint(hitPt, worldOBB.center, worldOBB.size, worldOBB.rotation);
                            Vector3 faceNormal          = Box3D.calcFaceNormal(worldOBB.center, worldOBB.size, worldOBB.rotation, faceClosestToPt);
                            objectHits.Add(new ObjectRayHit(ray, gameObject, faceNormal, t));
                        }
                    }
                }
            }

            if (sort) ObjectRayHit.sortByHitDistance(objectHits);
            return objectHits.Count != 0;
        }

        public void registerGameObjects(IEnumerable<GameObject> gameObjects)
        {
            foreach (var gameObject in gameObjects)
                registerGameObject(gameObject);
        }

        public void registerGameObject(GameObject gameObject)
        {
            if (canRegisterGameObject(gameObject))
            {
                AABB objectAABB = ObjectBounds.calcWorldAABB(gameObject, _nodeBoundsQConfig);
                if (objectAABB.isValid)
                {
                    Sphere sphere = objectAABB.getEnclosingSphere();
                    var node = _tree.createLeafNode(sphere.center, sphere.radius, gameObject);
                    _nodeMap.Add(gameObject, node);
                }
            }
        }

        public void unregisterGameObjects(IEnumerable<GameObject> gameObjects)
        {
            foreach (var gameObject in gameObjects)
                unregisterGameObject(gameObject);
        }

        public void unregisterGameObject(GameObject gameObject)
        {
            BinarySphereTreeNode<GameObject> objectNode = null;
            if (_nodeMap.TryGetValue(gameObject, out objectNode))
            {
                _nodeMap.Remove(gameObject);
                _tree.eraseLeafNode(objectNode);
            }
        }

        public void onGameObjectTransformChanged(GameObject gameObject)
        {
            if (!_nodeMap.ContainsKey(gameObject)) return;

            AABB objectAABB = ObjectBounds.calcWorldAABB(gameObject, _nodeBoundsQConfig);
            if (objectAABB.isValid)
            {
                Sphere sphere = objectAABB.getEnclosingSphere();
                var node = _nodeMap[gameObject];
                _tree.updateLeafNodeSphere(node, sphere.center, sphere.radius);
            }
        }

        public void handleNullRefs()
        {
            foreach (var pair in _nodeMap)
            {
                if (pair.Key == null && pair.Value.parent != null)
                    _tree.eraseLeafNode(pair.Value);
            }

            var newMap = new Dictionary<GameObject, BinarySphereTreeNode<GameObject>>();
            foreach (var pair in _nodeMap)
                if (pair.Key != null) newMap.Add(pair.Key, pair.Value);

            _nodeMap.Clear();
            _nodeMap = newMap;
        }

        private bool canRegisterGameObject(GameObject gameObject)
        {
            if (gameObject.couldBePooled()) return false;
            if (_nodeMap.ContainsKey(gameObject)) return false;

            GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(gameObject);
            if ((objectType & _recognizedObjectTypes) != 0) return true;

            return false;
        }
    }
}
#endif