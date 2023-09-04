#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class TerrainCollection
    {
        public List<Terrain>        unityTerrains = new List<Terrain>();
        public List<GameObject>     terrainMeshes = new List<GameObject>();

        public void clear()
        {
            unityTerrains.Clear();
            terrainMeshes.Clear();
        }
    }

    public class PluginScene : ScriptableObject
    {
        public class PrefabPickResult
        {
            public PluginPrefab pickedPluginPrefab;
            public GameObject   pickedObject;
        }

        [SerializeField]
        private PluginGrid                  _grid;
        [SerializeField]
        private List<PluginGrid>            _grids                  = new List<PluginGrid>();
        [NonSerialized]
        private SceneRaycastFilter          _gridSnapRaycastFilter  = new SceneRaycastFilter()
        {
            objectTypes = GameObjectType.Mesh | GameObjectType.Sprite | GameObjectType.Terrain,
            raycastGrid = false
        };
        [NonSerialized]
        private Func<GameObject, bool>      _prefabInstanceFilter   = new Func<GameObject, bool>((GameObject go) => { return !go.couldBePooled(); });
        [NonSerialized]
        private List<GameObject>            _sceneObjects           = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _gameObjectBuffer       = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _rootBuffer             = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _objectDeleteBuffer     = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _childrenAndSelfBuffer  = new List<GameObject>();
        [NonSerialized]
        private List<ObjectRayHit>          _objectHitBuffer        = new List<ObjectRayHit>();
        [NonSerialized]
        private List<Vector3>               _vector3Buffer          = new List<Vector3>();
        [NonSerialized]
        private List<GameObject>            _prefabAssetBuffer      = new List<GameObject>();
        [NonSerialized]
        private SceneObjectTree             _objectTree             = new SceneObjectTree();
        [NonSerialized]
        private TerrainObjectAABBTree       _terrainObjectTree      = new TerrainObjectAABBTree();

        public PluginGrid                   grid
        {
            get
            {
                if (_grid == null) _grid = ScriptableObject.CreateInstance<PluginGrid>();
                return _grid;
            }
        }
        public bool                         snapGridToPickedObjectEnabled           { get; set; }

        public static PluginScene           instance                                { get { return GSpawn.active.pluginScene; } }
        public static float                 terrainOverlapBoxVerticalSize           { get { return 2000.0f; } }

        public void refreshObjectTrees()
        {
            _objectTree.clear();
            _terrainObjectTree.clear();

            getSceneObjects(_sceneObjects);
            addObjectsToTrees(_sceneObjects);
        }

        public void onSceneGUI(SceneView sceneView)
        {
            // Note: For larger scenes, it's slow to do this here.
            //getSceneObjects(_sceneObjects);
            foreach (var sceneObject in _sceneObjects)
            {
                // Note: Could happen due to the fact that onSceneGUI seems to be called
                //       before '_sceneObjects' has a chance to be updated when an object
                //       is deleted.
                if (sceneObject == null) continue;

                Transform objectTransform = sceneObject.transform;
                if (objectTransform.hasChanged)
                {
                    _objectTree.onGameObjectTransformChanged(sceneObject);
                    objectTransform.hasChanged = false;

                    if (sceneObject.isTerrain()) _terrainObjectTree.onTerrainObjectTransformChanged(sceneObject);
                }
            }

            bool drawGrid = GSpawn.active.levelDesignToolId != LevelDesignToolId.ObjectSpawn ||
                            (ObjectSpawn.instance.activeTool.spawnToolId != ObjectSpawnToolId.TileRules);
            if (drawGrid)
            {
                Event e = Event.current;
                if (FixedShortcuts.grid_enableChangeCellSize(e) && e.isScrollWheel)
                {
                    float step          = 0.25f;
                    float sign          = Mathf.Sign(-e.delta.y);
                    Vector3 cellSize    = grid.activeSettings.cellSize;

                    cellSize.x          += step * sign;
                    cellSize.z          += step * sign;

                    if (cellSize.x < step) cellSize.x = step;
                    if (cellSize.z < step) cellSize.z = step;

                    grid.activeSettings.cellSize = Vector3Ex.roundCorrectError(cellSize, 1e-5f);

                    e.disable();
                }

                drawGridHandles(sceneView.camera);
            }

            if (snapGridToPickedObjectEnabled)
            {
                Event e = Event.current;
                if (e.type == EventType.MouseDown) snapGridToPickedObject(e.clickCount == 2);
            }
        }

        public void destroyPhysicsSimulationMonos(bool showMessage)
        {
            var simObjects = FindObjectsOfType<PhysicsSimulationObjectMono>(true);
            foreach (var simObject in simObjects)
            {
                simObject.onExitSimulation();
                PhysicsSimulationObjectMono.DestroyImmediate(simObject);
            }

            if (showMessage)
            {
                EditorUtility.DisplayDialog("Physics Simulation Object Script Removal", simObjects.Length + " physics simulation object scripts " + "have been removed.", "Ok");
            }
        }

        public void onObjectSpawned(GameObject gameObject)
        {
            gameObject.getAllChildrenAndSelf(true, true, _childrenAndSelfBuffer);

            foreach (var go in _childrenAndSelfBuffer)
            {
                _sceneObjects.Add(go);
                _objectTree.registerGameObject(go);

                if (go.isTerrain()) _terrainObjectTree.registerTerrainObject(go);
            }
        }

        public void onObjectsWillBeDestroyed(List<GameObject> gameObjects)
        {
            foreach (var go in gameObjects)
                onObjectWillBeDestroyed(go);
        }

        public void onObjectWillBeDestroyed(GameObject gameObject)
        {
            gameObject.getAllChildrenAndSelf(true, true, _childrenAndSelfBuffer);

            foreach(var go in _childrenAndSelfBuffer)
            {
                _sceneObjects.Remove(go);
                _objectTree.unregisterGameObject(go);

                if (go.isTerrain()) _terrainObjectTree.unregisterTerrainObject(go);
            }
        }

        public void onObjectLayerChangedTerrainMeshStatus(PluginObjectLayer objectLayer)
        {
            if (objectLayer.isTerrainMesh)
            {
                foreach (var sceneObject in _sceneObjects)
                {
                    if (sceneObject.layer == objectLayer.layerIndex && sceneObject.isTerrainMesh())
                        _terrainObjectTree.registerTerrainObject(sceneObject);
                }
            }
            else
            {
                foreach (var sceneObject in _sceneObjects)
                {
                    if (sceneObject.layer == objectLayer.layerIndex && sceneObject.getMesh() != null && sceneObject.getTerrain() == null)
                        _terrainObjectTree.unregisterTerrainObject(sceneObject);
                }
            }
        }

        public void findAllTerrains(TerrainCollection terrains)
        {
            _terrainObjectTree.getTerrains(terrains);
        }

        public void findUnityTerrains(List<Terrain> unityTerrains)
        {
            _terrainObjectTree.getUnityTerrains(unityTerrains);
        }

        public void findTerrainMeshes(List<GameObject> terrainMeshes)
        {
            _terrainObjectTree.getTerrainMeshes(terrainMeshes);
        }

        public PrefabPickResult pickPrefab(Ray ray, SceneRaycastFilter raycastFilter, ObjectRaycastConfig raycastConfig)
        {
            var rayHit = raycastClosest(ray, raycastFilter, raycastConfig);
            if (!rayHit.wasObjectHit) return null;

            GameObject prefabAsset = rayHit.objectHit.hitObject.getOutermostPrefabAsset();
            if (prefabAsset == null) return null;
         
            PluginPrefab pluginPrefab = PrefabLibProfileDb.instance.getPrefab(prefabAsset);
            if (pluginPrefab == null) return null;

            return new PrefabPickResult() { pickedPluginPrefab = pluginPrefab, pickedObject = rayHit.objectHit.hitObject };
        }

        public void snapGridToPickedObject(bool snapToClosestBoundary)
        {
            SceneRayHit rayHit = raycastClosest(PluginCamera.camera.getCursorRay(), _gridSnapRaycastFilter, ObjectRaycastConfig.defaultConfig);
            if (rayHit != null && rayHit.wasObjectHit)
            {
                if (snapToClosestBoundary)
                {
                    ObjectBounds.QueryConfig boundsQConfig  = ObjectBounds.QueryConfig.defaultConfig;
                    boundsQConfig.objectTypes               = GameObjectType.Mesh | GameObjectType.Sprite | GameObjectType.Terrain;
                    OBB worldOBB                            = ObjectBounds.calcWorldOBB(rayHit.objectHit.hitObject, boundsQConfig);
                    if (worldOBB.isValid)
                    {
                        worldOBB.calcCorners(_vector3Buffer, false);

                        Plane slicePlane                    = new Plane(grid.up, worldOBB.center);
                        PlaneClassifyResult ptLocation      = slicePlane.classifyPoint(rayHit.objectHit.hitPoint);

                        int snapDestIndex = -1;
                        if (ptLocation == PlaneClassifyResult.Behind) snapDestIndex = slicePlane.findIndexOfFurthestPointBehind(_vector3Buffer);
                        else snapDestIndex = slicePlane.findIndexOfFurthestPointInFront(_vector3Buffer);
                        if (snapDestIndex >= 0) grid.snapToPoint(_vector3Buffer[snapDestIndex]);
                    }
                }
                else grid.snapToPoint(rayHit.objectHit.hitPoint);
            }
        }

        public void drawGizmos()
        {
        }

        public bool overlapBox(OBB box, ObjectOverlapFilter overlapFilter, ObjectOverlapConfig overlapConfig, List<GameObject> overlappedObjects)
        {
            return _objectTree.overlapBox(box, overlapFilter, overlapConfig, overlappedObjects);
        }

        public bool overlapBox(OBB box, ObjectOverlapFilter overlapFilter, ObjectOverlapConfig overlapConfig)
        {
            return _objectTree.overlapBox(box, overlapFilter, overlapConfig);
        }

        [NonSerialized]
        private List<GameObject> _overlapBuffer = new List<GameObject>();
        public bool overlapBoxAgainstMeshTriangles(OBB box, ObjectOverlapFilter overlapFilter, ObjectOverlapConfig overlapConfig)
        {
            if (_objectTree.overlapBox(box, overlapFilter, overlapConfig, _overlapBuffer))
            {
                int numOverlapped = _overlapBuffer.Count;
                for (int i = 0; i < numOverlapped; ++i)
                {
                    if (_overlapBuffer[i].obbIntersectsMeshTriangles(box)) return true;
                }
            }

            return false;
        }

        public bool terrainsOverlapBox(OBB box, TerrainObjectOverlapFilter overlapFilter, TerrainObjectOverlapConfig overlapConfig, TerrainCollection terrains)
        {
            return _terrainObjectTree.overlapBox(box, overlapFilter, overlapConfig, terrains);
        }

        public SceneRayHit raycastClosest(Ray ray, SceneRaycastFilter raycastFilter, ObjectRaycastConfig raycastConfig)
        {
            ObjectRayHit closestObjectHit = null;
            if (raycastFilter == null || raycastFilter.raycastObjects)
            {
                _objectTree.raycastAll(ray, raycastFilter, raycastConfig, true, _objectHitBuffer);
                closestObjectHit = _objectHitBuffer.Count != 0 ? _objectHitBuffer[0] : null;
            }

            return new SceneRayHit(closestObjectHit, (raycastFilter == null || raycastFilter.raycastGrid) ? raycastGrid(ray) : null);
        }

        public bool raycastAll(Ray ray, SceneRaycastFilter raycastFilter, ObjectRaycastConfig raycastConfig, bool sort, List<ObjectRayHit> objectHits)
        {
            return _objectTree.raycastAll(ray, raycastFilter, raycastConfig, sort, objectHits);
        }

        public GridRayHit raycastGrid(Ray ray)
        {
            float t;
            Plane gridPlane = grid.plane;
            if (gridPlane.Raycast(ray, out t)) return new GridRayHit(ray, grid, t);

            return null;
        }

        public TileRuleGridRayHit raycastClosestTileRuleGrid(Ray ray, List<TileRuleGrid> grids)
        {
            TileRuleGridRayHit closestHit = null;
            foreach (var grid in grids)
            {
                var hit = grid.raycast(ray);
                if (hit != null)
                {
                    if (closestHit == null ||
                        closestHit.hitEnter > hit.hitEnter)
                    {
                        closestHit = hit;
                    }
                }
            }

            return closestHit;
        }

        public void getSceneObjects(List<GameObject> sceneObjects)
        {
            sceneObjects.Clear();

            var activeScene = SceneEx.getCurrent();
            if (activeScene.isLoaded)
            {
                _rootBuffer.Clear();
                if (_rootBuffer.Capacity <= activeScene.rootCount) _rootBuffer.Capacity = activeScene.rootCount + 1;
                activeScene.GetRootGameObjects(_rootBuffer);

                foreach (var root in _rootBuffer)
                {
                    root.getAllChildrenAndSelf(true, true, _childrenAndSelfBuffer);
                    sceneObjects.AddRange(_childrenAndSelfBuffer);
                }
            }
        }

        public void setObjectGroupActive(ObjectGroup objectGroup, bool active, bool allowUndoRedo)
        {
            if (allowUndoRedo) UndoEx.recordGameObject(objectGroup.gameObject);
            objectGroup.gameObject.SetActive(active);
        }

        [NonSerialized] List<GameObject> _nonGroupChildBuffer = new List<GameObject>();
        public void setObjectGroupChildrenActive(ObjectGroup objectGroup, bool active, bool applyActionFilters)
        {            
            objectGroup.getAllNonGroupChildren(_nonGroupChildBuffer);

            if (applyActionFilters)
            {
                var actionFilters = ObjectGroupDb.instance.actionFilters;
                foreach (var go in _nonGroupChildBuffer)
                {
                    if (actionFilters.filterObject(go))
                    {
                        UndoEx.recordGameObject(go);
                        go.SetActive(active);
                    }
                }
            }
            else
            {
                foreach (var go in _nonGroupChildBuffer)
                {
                    UndoEx.recordGameObject(go);
                    go.SetActive(active);
                }
            }
        }

        public void setObjectGroupsActive(List<ObjectGroup> objectGroups, bool active, bool allowUndoRedo)
        {
            if (allowUndoRedo)
            {
                foreach (var objectGroup in objectGroups)
                {
                    UndoEx.recordGameObject(objectGroup.gameObject);
                    objectGroup.gameObject.SetActive(active);
                }
            }
            else
            {
                foreach (var objectGroup in objectGroups)
                    objectGroup.gameObject.SetActive(active);
            }
        }

        public void setLayerActive(int layer, bool active, bool allowUndoRedo)
        {
            if (allowUndoRedo) UndoEx.recordGameObjects(_sceneObjects);
            foreach (var gameObject in _sceneObjects)
            {
                if (gameObject.layer == layer && !PluginInstanceData.instance.isPlugin(gameObject) && !gameObject.couldBePooled())
                        gameObject.SetActive(active);
            }
        }

        public void setLayersActive(List<int> layers, bool active, bool allowUndoRedo)
        {
            if (allowUndoRedo) UndoEx.recordGameObjects(_sceneObjects);
            foreach(var gameObject in _sceneObjects)
            {
                if (layers.Contains(gameObject.layer) && !PluginInstanceData.instance.isPlugin(gameObject) && !gameObject.couldBePooled())
                    gameObject.SetActive(active);
            }
        }

        public void setLayerVisible(int layer, bool visible, bool allowUndoRedo)
        {
            foreach (var gameObject in _sceneObjects)
            {
                if (gameObject.layer == layer && !PluginInstanceData.instance.isPlugin(gameObject) && !gameObject.couldBePooled())
                    gameObject.setVisible(visible, allowUndoRedo);
            }
        }

        public void setLayersVisible(List<int> layers, bool visible, bool allowUndoRedo)
        {
            foreach (var gameObject in _sceneObjects)
            {
                if (layers.Contains(gameObject.layer) && !PluginInstanceData.instance.isPlugin(gameObject) && !gameObject.couldBePooled())
                    gameObject.setVisible(visible, allowUndoRedo);
            }
        }

        public void setPrefabInstancesActive(List<PluginPrefab> prefabs, bool active, bool allowUndoRedo)
        {
            findPrefabInstances(prefabs, _gameObjectBuffer);
            if (allowUndoRedo) UndoEx.recordGameObjects(_gameObjectBuffer);

            foreach (var gameObject in _gameObjectBuffer)
            {
                if (!gameObject.couldBePooled())
                    gameObject.SetActive(active);
            }
        }

        public void setPrefabInstancesActive(List<GameObject> prefabAssets, bool active, bool allowUndoRedo)
        {
            findPrefabInstances(prefabAssets, _gameObjectBuffer);
            if (allowUndoRedo) UndoEx.recordGameObjects(_gameObjectBuffer);

            foreach (var gameObject in _gameObjectBuffer)
            {
                // Note: Don't affect objects hidden in hierarchy because those can
                //       be pooled objects.
                if (!gameObject.couldBePooled())
                    gameObject.SetActive(active);
            }
        }

        public void deletePrefabInstances(List<PluginPrefab> prefabs)
        {
            findPrefabInstances(prefabs, _gameObjectBuffer);
            deleteObjects(_gameObjectBuffer);
        }

        public void deleteObjects(List<GameObject> gameObjects)
        {
            // Note: Deleting the objects might affect the selection.
            ObjectSelection.instance.onObjectsWillBeDeleted(gameObjects, true);

            GameObjectEx.getParents(gameObjects, _rootBuffer);
            _rootBuffer.RemoveAll(item => TileRuleGridDb.instance.isObjectChildOfTileRuleGrid(item));
            GameObjectEx.getAllObjectsInHierarchies(_rootBuffer, true, true, _objectDeleteBuffer);

            _objectTree.unregisterGameObjects(_objectDeleteBuffer);
            foreach(var go in _objectDeleteBuffer)
            {
                if (go.isTerrain()) _terrainObjectTree.unregisterTerrainObject(go);
            }

            UnityEditorCommands.softDelete(_rootBuffer);
        }

        public void deleteObject(GameObject gameObject)
        {
            if (TileRuleGridDb.instance.isObjectChildOfTileRuleGrid(gameObject)) return;

            _gameObjectBuffer.Clear();
            _gameObjectBuffer.Add(gameObject);
            deleteObjects(_gameObjectBuffer);
        }

        public void deleteLayers(List<int> layers)
        {
            _gameObjectBuffer.Clear();
            foreach (var gameObject in _sceneObjects)
            {
                if (layers.Contains(gameObject.layer) && !PluginInstanceData.instance.isPlugin(gameObject) && !gameObject.couldBePooled()) 
                    _gameObjectBuffer.Add(gameObject);
            }

            deleteObjects(_gameObjectBuffer);
        }

        public void deleteLayer(int layer)
        {
            _gameObjectBuffer.Clear();
            foreach (var gameObject in _sceneObjects)
            {
                if (gameObject.layer == layer && !PluginInstanceData.instance.isPlugin(gameObject) && !gameObject.couldBePooled()) 
                    _gameObjectBuffer.Add(gameObject);
            }

            deleteObjects(_gameObjectBuffer);
        }

        public void findPrefabInstances(List<PluginPrefab> prefabs, List<GameObject> instances)
        {
            PluginPrefab.getPrefabAssets(prefabs, _prefabAssetBuffer);
            findPrefabInstances(_prefabAssetBuffer, instances);
        }

        public void findPrefabInstances(List<GameObject> prefabAssets, List<GameObject> instances)
        {
            GameObjectEx.getOutermostPrefabInstanceRoots(_sceneObjects, prefabAssets, instances, _prefabInstanceFilter);
        }

        public void findPrefabInstances(GameObject prefabAsset, List<GameObject> instances)
        {
            GameObjectEx.getOutermostPrefabInstanceRoots(_sceneObjects, prefabAsset, instances, _prefabInstanceFilter);
        }

        private void drawGridHandles(Camera camera)
        {
            var config = new GridHandles.DrawConfig();
            foreach(var sceneGrid in _grids)
            {
                config.cellSizeX    = sceneGrid.activeSettings.cellSizeX;
                config.cellSizeZ    = sceneGrid.activeSettings.cellSizeZ;
                config.fillColor    = GridPrefs.instance.fillColor;
                config.wireColor    = GridPrefs.instance.wireColor;
                config.right        = sceneGrid.right;
                config.look         = sceneGrid.look;
                config.origin       = sceneGrid.origin;
                config.planeNormal  = sceneGrid.planeNormal;

                config.drawCoordSystem          = GridPrefs.instance.drawCoordSystem;
                if (config.drawCoordSystem)
                {
                    config.finiteAxisLength     = GridPrefs.instance.finiteAxisLength;
                    config.infiniteXAxis        = GridPrefs.instance.infiniteXAxis;
                    config.infiniteYAxis        = GridPrefs.instance.infiniteYAxis;
                    config.infiniteZAxis        = GridPrefs.instance.infiniteZAxis;
                    config.xAxisColor           = GridPrefs.instance.xAxisColor;
                    config.yAxisColor           = GridPrefs.instance.yAxisColor;
                    config.zAxisColor           = GridPrefs.instance.zAxisColor;
                }

                GridHandles.drawInfinite(config, camera);
            }

            if (FixedShortcuts.grid_enableChangeCellSize(Event.current))
            {
                Transform cameraTransform = PluginCamera.camera.transform;

                Handles.BeginGUI();
                var cellSize        = grid.activeSettings.cellSize;
                string labelText    = string.Format("Cell size: <{0}, {1}, {2}>", cellSize.x, cellSize.y, cellSize.z);
                Handles.Label(cameraTransform.position + cameraTransform.forward * (camera.nearClipPlane + 1e-3f), labelText, GUIStyleDb.instance.sceneViewInfoLabel);
                Handles.EndGUI();
            }
        }

        private void OnEnable()
        {
            if (!_grids.Contains(grid)) _grids.Add(grid);

            EditorApplication.hierarchyChanged += onSceneObjectsChanged;
            onSceneObjectsChanged();

            // Note: Just to be sure.
            destroyPhysicsSimulationMonos(false);
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= onSceneObjectsChanged;
        }

        private void OnDestroy()
        {
            UndoEx.record(this);
            var sceneGrids = new List<PluginGrid>(_grids);
            _grids.Clear();

            foreach (var sceneGrid in sceneGrids)
                ScriptableObjectEx.destroyImmediate(sceneGrid);

            EditorApplication.hierarchyChanged -= onSceneObjectsChanged;
        }

        private void onSceneObjectsChanged()
        {
            if (GSpawn.active == null) return;

            _objectTree.handleNullRefs();
            _terrainObjectTree.handleNullRefs();

            getSceneObjects(_sceneObjects);
            addObjectsToTrees(_sceneObjects);
        }

        private void addObjectsToTrees(List<GameObject> gameObjects)
        {
            foreach (var go in gameObjects)
            {
                _objectTree.registerGameObject(go);

                GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(go);
                if (objectType == GameObjectType.Terrain) _terrainObjectTree.registerTerrainObject(go);
                else if (objectType == GameObjectType.Mesh && go.isTerrainMesh()) _terrainObjectTree.registerTerrainObject(go);
            }
        }
    }
}
#endif