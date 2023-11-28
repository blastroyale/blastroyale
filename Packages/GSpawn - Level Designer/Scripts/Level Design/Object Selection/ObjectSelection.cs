#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectSelection : ScriptableObject
    {
        private enum SelectionChangeReason
        {
            ClickSelect = 0,
            MultiSelect,
            SetSelected,
            Append,
            Replaced,
            SelectPrefabInstances,
            SelectSimilarPrefabInstances,
            DeselectPrefabInstances,
            Delete,
            FilterOutOfView
        }

        // 1-to-1 mapping with ObjectTransformSession.Type
        private ObjectTransformSession[]    _transformSessions          = new ObjectTransformSession[Enum.GetValues(typeof(ObjectTransformSessionType)).Length];
        private ObjectTransformSession      _activeTransformSession     = null;

        // Note: Helps us avoid processing of mouse up events when the mouse is released 
        //       after dragging a window. In that case we don't want to update the selection.
        private bool                        _receivedMouseDown;

        private ObjectBounds.QueryConfig    _selBoxQConfig              = new ObjectBounds.QueryConfig();
        private GameObjectType              _selBoxDrawTypes            = GameObjectType.All & (~GameObjectType.Empty);
        private ObjectSelectionShape[]      _selShapes                  = new ObjectSelectionShape[] { new ObjectSelectionRect(), new ObjectSelectionSegments(), new ObjectSelectionBox() };

        [NonSerialized]
        private SceneRaycastFilter          _replaceRaycastFilter       = new SceneRaycastFilter()
        {
            objectTypes = GameObjectType.Mesh | GameObjectType.Sprite,
            raycastGrid = false
        };

        [SerializeField]
        private ObjectSelectionGizmos       _gizmos;
        [NonSerialized]
        private ObjectSelectionSettings     _settings;
        [NonSerialized]
        private ObjectSelectionGrowSettings _growSettings;
        [NonSerialized]
        private ObjectProjectionSettings    _projectionSettings;
        [NonSerialized]
        private ObjectVertexSnapSettings    _vertexSnapSettings;
        [NonSerialized]
        private ObjectBoxSnapSettings       _boxSnapSettings;
        [NonSerialized]
        private ObjectSurfaceSnapSettings   _surfaceSnapSettings;
        [NonSerialized]
        private ObjectModularSnapSettings   _modularSnapSettings;

        [SerializeField]
        private ObjectSelectionShape.Type   _selectionShapeType         = ObjectSelectionShape.Type.Rect;
        [SerializeField]
        private List<GameObject>            _selectedObjects            = new List<GameObject>();
        [SerializeField]
        private GameObject                  _gizmosPivotObject;

        [NonSerialized]
        private List<GameObject>            _parentsBuffer              = new List<GameObject>();
        [NonSerialized]
        private List<Transform>             _parentsTransformBuffer     = new List<Transform>();
        [NonSerialized]
        private List<GameObject>            _gameObjectBuffer           = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _prefabInstanceBuffer       = new List<GameObject>();
        [NonSerialized]
        private ObjectOutline               _selectionHighlight         = new ObjectOutline();
        [NonSerialized]
        private List<GameObject>            _prefabAssetBuffer          = new List<GameObject>();
        [NonSerialized]
        private HashSet<GameObject>         _prefabAssetSet             = new HashSet<GameObject>();
        [NonSerialized]
        private HashSet<GameObject>         _growSet                    = new HashSet<GameObject>();
        [NonSerialized]
        private List<GameObject>            _growSetAddBuffer           = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _growSetRemoveBuffer        = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _objectOverlapBuffer        = new List<GameObject>();

        private bool                        appendEnabled               { get; set; }
        private bool                        multiDeselectEnabled        { get; set; }

        public IEnumerable<GameObject>      objectCollection            { get { return _selectedObjects; } }
        public int                          numSelectedObjects          { get { return _selectedObjects.Count; } }
        public ObjectSelectionGizmos        gizmos                      { get { if (_gizmos == null) _gizmos = ScriptableObject.CreateInstance<ObjectSelectionGizmos>(); return _gizmos; } }
        public ObjectSelectionShape.Type    selectionShapeType          { get { return _selectionShapeType; } set { getSelectionShape().cancel(); _selectionShapeType = value; PluginInspectorUI.instance.refresh(); } }
        public bool                         multiSelecting              { get { return getSelectionShape().selecting; } }
        public ObjectSelectionSettings      settings
        {
            get
            {
                if (_settings == null) _settings = AssetDbEx.loadScriptableObject<ObjectSelectionSettings>(PluginFolders.settings);
                return _settings;
            }
        }
        public ObjectSelectionGrowSettings  growSettings
        {
            get
            {
                if (_growSettings == null) _growSettings = AssetDbEx.loadScriptableObject<ObjectSelectionGrowSettings>(PluginFolders.settings);
                return _growSettings;
            }
        }
        public ObjectProjectionSettings     projectionSettings 
        { 
            get 
            {
                if (_projectionSettings == null) _projectionSettings = AssetDbEx.loadScriptableObject<ObjectProjectionSettings>(PluginFolders.settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectProjectionSettings).Name); 
                return _projectionSettings;
            } 
        }
        public ObjectVertexSnapSettings     vertexSnapSettings 
        { 
            get 
            { 
                if (_vertexSnapSettings == null) _vertexSnapSettings = AssetDbEx.loadScriptableObject<ObjectVertexSnapSettings>(PluginFolders.settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectVertexSnapSettings).Name);
                return _vertexSnapSettings; 
            } 
        }
        public ObjectBoxSnapSettings        boxSnapSettings
        { 
            get 
            { 
                if (_boxSnapSettings == null) _boxSnapSettings = AssetDbEx.loadScriptableObject<ObjectBoxSnapSettings>(PluginFolders.settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectBoxSnapSettings).Name);
                return _boxSnapSettings; 
            }
        }
        public ObjectSurfaceSnapSettings    surfaceSnapSettings 
        { 
            get 
            { 
                if (_surfaceSnapSettings == null)
                {
                    _surfaceSnapSettings = AssetDbEx.loadScriptableObject<ObjectSurfaceSnapSettings>(PluginFolders.settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectSurfaceSnapSettings).Name);
                    _surfaceSnapSettings.snapSingleTargetToCursor = false;
                }
                
                return _surfaceSnapSettings; 
            } 
        }
        public ObjectModularSnapSettings    modularSnapSettings 
        { 
            get 
            { 
                if (_modularSnapSettings == null) _modularSnapSettings = AssetDbEx.loadScriptableObject<ObjectModularSnapSettings>(PluginFolders.settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectModularSnapSettings).Name);
                return _modularSnapSettings; 
            } 
        }
        public bool                         isAnyTransformSessionActive     { get { return _activeTransformSession != null; } }
        public bool                         clickSelectEnabled              { get; set; }
        public bool                         multiSelectEnabled              { get; set; }
        public bool                         gizmosEnabled                   { get; set; }

        public static ObjectSelection       instance                        { get { return GSpawn.active.objectSelection; } }

        public ObjectSelection()
        {
            _selBoxQConfig.objectTypes      = _selBoxDrawTypes;
            _selBoxQConfig.volumelessSize   = Vector3.one;
        }

        public void onSceneGUI()
        {
            Event e                 = Event.current;
            appendEnabled           = false;
            multiDeselectEnabled    = false;

            if (_activeTransformSession != null)
            {
                if (e.type == EventType.KeyDown && FixedShortcuts.cancelAction(e)) endTransformSession();
                else
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    if (isTransformSessionActive(ObjectTransformSessionType.SurfaceSnap) ||
                        isTransformSessionActive(ObjectTransformSessionType.ModularSnap)) endTransformSession();
                }

                if (_activeTransformSession != null) _activeTransformSession.onSceneGUI();
            }
            else
            {
                if (e.type == EventType.MouseDown)
                {
                    if (FixedShortcuts.selection_ReplaceOnClick(e))
                    {
                        replaceWithPickedObject();
                    }
                }
                
                if (FixedShortcuts.selection_EnableAppend(e)) appendEnabled = true;
                else if (FixedShortcuts.selection_EnableMultiDeselect(e)) multiDeselectEnabled = true;

                // Note: Let the gizmos eat the current GUI event first. Otherwise, there
                //       will be conflicts between event handling (e.g. on MouseUp, the
                //       selection is updated and this can result in the gizmos being
                //       hidden because objects may have been deselected, when in fact the
                //       mouse was released over the gizmo at the end of a drag session).
                if (gizmosEnabled && (selectionShapeType == ObjectSelectionShape.Type.Rect || !multiSelecting)) gizmos.onSceneGUI();

                bool wasMultiSelecting = getSelectionShape().selecting;
                getSelectionShape().onSceneGUI();
                if (wasMultiSelecting && !getSelectionShape().selecting) refreshObjectSelectionUI();

                if (e.type == EventType.MouseUp)
                {
                    // Note: Only proceed if the ALT key is not pressed. If we are orbiting, we 
                    //       don't want to alter the selection.
                    if (e.button == (int)MouseButton.LeftMouse && !getSelectionShape().selecting && !e.alt && _receivedMouseDown)
                    {
                        clickSelect();
                        refreshObjectSelectionUI();
                    }

                    _receivedMouseDown = false;
                }
                else
                if (e.type == EventType.MouseDown) _receivedMouseDown = true;
            }

            drawSelectionHandles();
        }

        public bool canSelectPrefabInstances()
        {
            return !isAnyTransformSessionActive;
        }

        public void getSelectedPrefabs(List<GameObject> prefabAssets)
        {
            prefabAssets.Clear();
            _prefabAssetSet.Clear();
            foreach(var go in _selectedObjects)
            {
                var prefab = go.getPrefabAsset();
                if (prefab != null && !_prefabAssetSet.Contains(prefab))
                {
                    prefabAssets.Add(prefab);
                    _prefabAssetSet.Add(prefab);
                }
            }

            _prefabAssetSet.Clear();
        }

        public void filterOutOfView()
        {
            if (numSelectedObjects == 0) return;

            UndoEx.record(this);
            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(PluginCamera.camera);

            ObjectBounds.QueryConfig boundsQConfig = ObjectBounds.QueryConfig.defaultConfig;
            foreach (var parent in _parentsBuffer)
            {
                var outerMostInstance = parent.getOutermostPrefabInstanceRoot();
                if (outerMostInstance != null)
                {
                    AABB aabb = ObjectBounds.calcHierarchyWorldAABB(outerMostInstance, boundsQConfig);
                    if (aabb.isValid)
                    {
                        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, aabb.toBounds()))
                        {
                            parent.getAllChildrenAndSelf(true, true, _gameObjectBuffer);
                            foreach (var go in _gameObjectBuffer)
                                _selectedObjects.Remove(go);
                        }
                    }
                }
                else
                {
                    AABB aabb = ObjectBounds.calcWorldAABB(parent, boundsQConfig);
                    if (aabb.isValid)
                    {
                        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, aabb.toBounds()))
                        {
                            parent.getAllChildrenAndSelf(true, true, _gameObjectBuffer);
                            foreach (var go in _gameObjectBuffer)
                                _selectedObjects.Remove(go);
                        }
                    }
                }
            }

            onSelectionChanged(SelectionChangeReason.FilterOutOfView);
            refreshObjectSelectionUI();
        }

        public void selectSimilarPrefabInstances()
        {
            if (numSelectedObjects == 0) return;
            getSelectedPrefabs(_prefabAssetBuffer);

            UndoEx.record(this);
            _selectedObjects.Clear();

            PluginScene.instance.findPrefabInstances(_prefabAssetBuffer, _prefabInstanceBuffer);
            _selectedObjects.AddRange(_prefabInstanceBuffer);

            onSelectionChanged(SelectionChangeReason.SelectSimilarPrefabInstances);
            refreshObjectSelectionUI();
        }

        public void selectPrefabInstances(List<GameObject> prefabAssets)
        {
            if (!canSelectPrefabInstances()) return;

            UndoEx.record(this);
            if (!appendEnabled) _selectedObjects.Clear();

            PluginScene.instance.findPrefabInstances(prefabAssets, _prefabInstanceBuffer);
            _selectedObjects.AddRange(_prefabInstanceBuffer);

            onSelectionChanged(SelectionChangeReason.SelectPrefabInstances);
            refreshObjectSelectionUI();
        }

        public bool canDeselectPrefabInstances()
        {
            return !isAnyTransformSessionActive;
        }

        public void deselectPrefabInstances(List<GameObject> prefabAssets)
        {
            if (!canDeselectPrefabInstances()) return;

            UndoEx.record(this);
            GameObjectEx.getOutermostPrefabInstanceRoots(_selectedObjects, prefabAssets, _gameObjectBuffer, null);

            foreach (var go in _gameObjectBuffer)
                _selectedObjects.Remove(go);

            onSelectionChanged(SelectionChangeReason.DeselectPrefabInstances);
            refreshObjectSelectionUI();
        }

        public bool canCreatePrefabFromSelection()
        {
            return numSelectedObjects != 0 && !isAnyTransformSessionActive;
        }

        public GameObject createPrefabFromSelectedObjects(PrefabFromSelectedObjectsCreationSettings prefabCreationSettings)
        {
            if (!canCreatePrefabFromSelection()) return null;

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            return PrefabFactory.create(_parentsBuffer, prefabCreationSettings);
        }

        public bool canReplace()
        {
            return numSelectedObjects != 0 && !isAnyTransformSessionActive;
        }

        public void replaceWithPickedObject()
        {
            if (!canReplace()) return;

            SceneRayHit rayHit = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), _replaceRaycastFilter, ObjectRaycastConfig.defaultConfig);
            if (rayHit.wasObjectHit)
            {
                var prefabInstanceRoot = rayHit.objectHit.hitObject.getOutermostPrefabInstanceRoot();
                if (prefabInstanceRoot != null)
                {
                    var prefab = prefabInstanceRoot.getPrefabAsset();
                    if (prefab != null) replaceSelection(prefab);
                }
                else replaceSelection(rayHit.objectHit.hitObject);
            }
        }

        public void replaceSelection(GameObject replacement)
        {
            if (!canReplace()) return;

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            GameObjectEx.getAllObjectsInHierarchies(_parentsBuffer, true, true, _gameObjectBuffer);

            UndoEx.record(this);
            foreach (var go in _gameObjectBuffer)
                _selectedObjects.Remove(go);

            _gameObjectBuffer.Clear();

            if (replacement.isSceneObject())
            {
                foreach (var selectedParent in _parentsBuffer)
                {
                    var prefabInstanceRoot = selectedParent.getOutermostPrefabInstanceRoot();
                    if (prefabInstanceRoot != null)
                    {
                        GameObject newObject            = GameObject.Instantiate(replacement, prefabInstanceRoot.transform.position, prefabInstanceRoot.transform.rotation);
                        UndoEx.registerCreatedObject(newObject);
                        newObject.transform.localScale  = prefabInstanceRoot.transform.lossyScale;
                        newObject.transform.parent      = prefabInstanceRoot.transform.parent;
                        _gameObjectBuffer.Add(newObject);

                        UndoEx.destroyGameObjectImmediate(prefabInstanceRoot);
                    }
                    else
                    {
                        GameObject newObject            = GameObject.Instantiate(replacement, selectedParent.transform.position, selectedParent.transform.rotation);
                        UndoEx.registerCreatedObject(newObject);
                        newObject.transform.localScale  = selectedParent.transform.lossyScale;
                        newObject.transform.parent      = selectedParent.transform.parent;
                        _gameObjectBuffer.Add(newObject);

                        UndoEx.destroyGameObjectImmediate(selectedParent);
                    }
                }
            }
            else
            {
                // Note: Currently ignored; causes floating point rounding errors to creep in.
                //       Quaternion baseRotation = replacement.transform.rotation;
                foreach (var selectedParent in _parentsBuffer)
                {
                    var prefabInstanceRoot = selectedParent.getOutermostPrefabInstanceRoot();
                    if (prefabInstanceRoot != null)
                    {
                        GameObject newObject            = replacement.instantiatePrefab();
                        UndoEx.registerCreatedObject(newObject);
                        newObject.transform.position    = prefabInstanceRoot.transform.position;
                        newObject.transform.rotation    = prefabInstanceRoot.transform.rotation;// * baseRotation;
                        newObject.transform.localScale  = prefabInstanceRoot.transform.lossyScale;
                        newObject.transform.parent      = prefabInstanceRoot.transform.parent;
                        _gameObjectBuffer.Add(newObject);

                        UndoEx.destroyGameObjectImmediate(prefabInstanceRoot);
                    }
                    else
                    {
                        GameObject newObject            = replacement.instantiatePrefab();
                        UndoEx.registerCreatedObject(newObject);
                        newObject.transform.position    = selectedParent.transform.position;
                        newObject.transform.rotation    = selectedParent.transform.rotation;// * baseRotation;
                        newObject.transform.localScale  = selectedParent.transform.lossyScale;
                        newObject.transform.parent      = selectedParent.transform.parent;
                        _gameObjectBuffer.Add(newObject);

                        UndoEx.destroyGameObjectImmediate(selectedParent);
                    }
                }
            }

            foreach (var go in _gameObjectBuffer)
                _selectedObjects.Add(go);

            onSelectionChanged(SelectionChangeReason.Replaced);
            _gameObjectBuffer.Clear();
            _parentsBuffer.Clear();

            refreshObjectSelectionUI();
        }

        public void replaceSelection(PluginPrefab prefab)
        {
            if (!canReplace()) return;

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            GameObjectEx.getAllObjectsInHierarchies(_parentsBuffer, true, true, _gameObjectBuffer);

            UndoEx.record(this);
            foreach (var go in _gameObjectBuffer)
                _selectedObjects.Remove(go);

            _gameObjectBuffer.Clear();

            // Note: Currently ignored; causes floating point rounding errors to creep in.
            //       Quaternion baseRotation = prefab.prefabAsset.transform.rotation;
            foreach (var parent in _parentsBuffer)
            {
                var prefabInstanceRoot = parent.getOutermostPrefabInstanceRoot();
                if (prefabInstanceRoot != null)
                {
                    GameObject newObject = prefab.spawn(prefabInstanceRoot.transform.position,
                                                        (prefabInstanceRoot.transform.rotation),// * baseRotation).normalized,
                                                        prefabInstanceRoot.transform.lossyScale);
                    UndoEx.registerCreatedObject(newObject);
                    _gameObjectBuffer.Add(newObject);

                    UndoEx.destroyGameObjectImmediate(prefabInstanceRoot);
                }
                else
                {
                    GameObject newObject = prefab.spawn(parent.transform.position,
                                                        (parent.transform.rotation),// * baseRotation).normalized,
                                                        parent.transform.lossyScale);
                    UndoEx.registerCreatedObject(newObject);
                    _gameObjectBuffer.Add(newObject);

                    UndoEx.destroyGameObjectImmediate(parent);
                }
            }

            foreach (var go in _gameObjectBuffer)
                _selectedObjects.Add(go);

            onSelectionChanged(SelectionChangeReason.Replaced);
            _gameObjectBuffer.Clear();
            _parentsBuffer.Clear();

            refreshObjectSelectionUI();
        }

        [NonSerialized]
        private CumulativeProbabilityTable<PluginPrefab> _replacementPrefabTable = new CumulativeProbabilityTable<PluginPrefab>();
        public void replaceSelection(List<PluginPrefab> prefabs)
        {
            if (!canReplace()) return;
            if (prefabs.Count == 0) return;

            _replacementPrefabTable.clear();
            foreach (var prefab in prefabs)
                _replacementPrefabTable.addEntity(prefab, 1.0f);
            _replacementPrefabTable.refresh();

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            GameObjectEx.getAllObjectsInHierarchies(_parentsBuffer, true, true, _gameObjectBuffer);

            UndoEx.record(this);
            foreach (var go in _gameObjectBuffer)
                _selectedObjects.Remove(go);

            _gameObjectBuffer.Clear();

            foreach (var parent in _parentsBuffer)
            {
                PluginPrefab pickedPrefab   = _replacementPrefabTable.pickEntity();
                var prefabInstanceRoot      = parent.getOutermostPrefabInstanceRoot();
                if (prefabInstanceRoot != null)
                {
                    GameObject newObject = pickedPrefab.spawn(prefabInstanceRoot.transform.position,
                                                        (prefabInstanceRoot.transform.rotation),// * baseRotation).normalized,
                                                        prefabInstanceRoot.transform.lossyScale);
                    UndoEx.registerCreatedObject(newObject);
                    _gameObjectBuffer.Add(newObject);

                    UndoEx.destroyGameObjectImmediate(prefabInstanceRoot);
                }
                else
                {
                    GameObject newObject = pickedPrefab.spawn(parent.transform.position,
                                                        (parent.transform.rotation),// * baseRotation).normalized,
                                                        parent.transform.lossyScale);
                    UndoEx.registerCreatedObject(newObject);
                    _gameObjectBuffer.Add(newObject);

                    UndoEx.destroyGameObjectImmediate(parent);
                }
            }

            foreach (var go in _gameObjectBuffer)
                _selectedObjects.Add(go);

            onSelectionChanged(SelectionChangeReason.Replaced);
            _gameObjectBuffer.Clear();
            _parentsBuffer.Clear();

            refreshObjectSelectionUI();
        }

        public void frameSelected()
        {
            // Note: Selection framing depends on the currently active pivot mode. So make sure
            //       that the Unity Editor pivot mode is synced with the gizmos pivot before
            //       framing the selection.
            var oldPivotMode    = Tools.pivotMode;
            Tools.pivotMode     = gizmos.transformPivot == ObjectGizmoTransformPivot.Center ? PivotMode.Center : PivotMode.Pivot;
            UnityEditorCommands.frameSelected(_selectedObjects);
            Tools.pivotMode     = oldPivotMode;
        }

        public void duplicateSelected()
        {
            UnityEditorCommands.duplicate(_selectedObjects);
        }

        public void deleteSelected()
        {
            if (numSelectedObjects == 0) return;

            endActiveTransformSession();

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            _parentsBuffer.RemoveAll(item => 
            {
                var outerPrefabInstance = item.getOutermostPrefabInstanceRoot();
                return outerPrefabInstance != null && outerPrefabInstance != item;
            });

            UndoEx.record(this);
            _gameObjectBuffer.Clear();  
            _selectedObjects.RemoveAll(item => 
            {
                foreach (var parent in _parentsBuffer)
                {
                    if (item.transform.IsChildOf(parent.transform))
                    {
                        _gameObjectBuffer.Add(item);
                        return true;
                    }
                }
                return false;
            });
            onSelectionChanged(SelectionChangeReason.Delete);
            PluginScene.instance.deleteObjects(_parentsBuffer);

            refreshObjectSelectionUI();
        }

        public bool isTransformSessionActive(ObjectTransformSessionType sessionType)
        {
            if (_activeTransformSession == null) return false;
            return _activeTransformSession.sessionType == sessionType;
        }

        public void beginTransformSession(ObjectTransformSessionType sessionType)
        {
            if (isTransformSessionActive(sessionType)) return;

            getSelectionShape().cancel();
            endTransformSession();

            UndoEx.recordGameObjectTransforms(_selectedObjects);
            _activeTransformSession = getTransformSession(sessionType);
            if (!_activeTransformSession.begin())
            {
                _activeTransformSession = null;
            }

            SceneView.RepaintAll();
            if (_activeTransformSession != null) ObjectSelectionUI.instance.setObjectTransformUIEnabled(false);
        }

        public void onLevelDesignToolChanged()
        {
            if (GSpawn.active.levelDesignToolId != LevelDesignToolId.ObjectSelection)
                endActiveTransformSession();

            //onSelectedObjectsMightHaveBeenDeleted(Plugin.active.levelDesignToolId == LevelDesignToolId.ObjectSelection);
        }

        public void endActiveTransformSession()
        {
            endTransformSession();
        }

        public void endActiveTransformSession(ObjectTransformSessionType sessionType)
        {
            if (!isTransformSessionActive(sessionType)) return;
            endTransformSession();
        }

        public void executeSurfaceSnapSessionCommand(ObjectSurfaceSnapSessionCommand command)
        {
            if (isTransformSessionActive(ObjectTransformSessionType.SurfaceSnap))
            {
                var session = (getTransformSession(ObjectTransformSessionType.SurfaceSnap) as ObjectSurfaceSnapSession);
                session.executeCommand(command);
            }
        }

        public void executeModularSnapSessionCommand(ObjectModularSnapSessionCommand command)
        {
            if (isTransformSessionActive(ObjectTransformSessionType.ModularSnap))
            {
                var session = (getTransformSession(ObjectTransformSessionType.ModularSnap) as ObjectModularSnapSession);
                session.executeCommand(command);
            }
        }

        public void drawGizmos()
        {
            if (isTransformSessionActive(ObjectTransformSessionType.BoxSnap) ||
                isTransformSessionActive(ObjectTransformSessionType.SurfaceSnap) ||
                isTransformSessionActive(ObjectTransformSessionType.ModularSnap)) return;

            drawSelectionGizmos();
        }

        public bool isObjectSelected(GameObject gameObject)
        {
            return _selectedObjects.Contains(gameObject);
        }

        public bool anyChildrenSelected(GameObject gameObject)
        {
            gameObject.getAllChildren(false, false, _gameObjectBuffer);
            foreach (var child in _gameObjectBuffer)
                if (isObjectSelected(child)) return true;

            return false;
        }

        public bool canClickSelect()
        {
            return clickSelectEnabled && !multiSelecting;
        }

        public bool canMultiSelect()
        {
            return multiSelectEnabled;
        }

        public bool isObjectSelectable(GameObject gameObject)
        {
            if (gameObject == null || PluginInstanceData.instance.isPlugin(gameObject) || gameObject.couldBePooled()) return false;
            if (SceneVisibilityManager.instance.IsHidden(gameObject, false) ||
                LayerEx.isLayerHidden(gameObject.layer) || LayerEx.isPickingDisabled(gameObject.layer) ||
                SceneVisibilityManager.instance.IsPickingDisabled(gameObject, false)) return false;

            return settings.isGameObjectSelectable(gameObject, GameObjectDataDb.instance.getGameObjectType(gameObject));             
        }

        public Vector3 calcSelectionCenter()
        {
            if (numSelectedObjects == 0) return Vector3.zero;

            Vector3 center = Vector3.zero;
            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);

            foreach (var parent in _parentsBuffer)
                center += parent.transform.position;

            center *= (1.0f / (float)_parentsBuffer.Count);
            return center;
        }

        public void resetRotationToOriginal()
        {
            if (numSelectedObjects == 0) return;
            if (_activeTransformSession != null && !_activeTransformSession.clientCanUpdateTargetTransforms) return;

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            UndoEx.recordGameObjectTransforms(_parentsBuffer);

            foreach (var parent in _parentsBuffer)
                parent.resetRotationToOriginal();

            onObjectTransformsChanged();
            ObjectEvents.onObjectsTransformed();
        }

        public void setRotation(Quaternion rotation)
        {
            if (numSelectedObjects == 0) return;
            if (_activeTransformSession != null && !_activeTransformSession.clientCanUpdateTargetTransforms) return;

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            UndoEx.recordGameObjectTransforms(_parentsBuffer);

            foreach (var parent in _parentsBuffer)
                parent.transform.rotation = rotation;

            onObjectTransformsChanged();
            ObjectEvents.onObjectsTransformed();
        }

        public void resetScaleToOriginal()
        {
            if (numSelectedObjects == 0) return;
            if (_activeTransformSession != null && !_activeTransformSession.clientCanUpdateTargetTransforms) return;

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            UndoEx.recordGameObjectTransforms(_parentsBuffer);

            foreach (var parent in _parentsBuffer)
                parent.resetScaleToOriginal();

            onObjectTransformsChanged();
            ObjectEvents.onObjectsTransformed();
        }

        public void rotate(Vector3 axis, float degrees)
        {
            if (numSelectedObjects == 0) return;
            if (_activeTransformSession != null && !_activeTransformSession.clientCanUpdateTargetTransforms) return;

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            UndoEx.recordGameObjectTransforms(_parentsBuffer);
            foreach (var parent in _parentsBuffer)
                parent.transform.Rotate(axis, degrees, Space.World);

            onObjectTransformsChanged();
            ObjectEvents.onObjectsTransformed();
        }

        public void rotate(Vector3 point, Vector3 axis, float degrees)
        {
            if (numSelectedObjects == 0) return;
            if (_activeTransformSession != null && !_activeTransformSession.clientCanUpdateTargetTransforms) return;

            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            UndoEx.recordGameObjectTransforms(_parentsBuffer);
            foreach (var parent in _parentsBuffer)
                parent.transform.RotateAround(point, axis, degrees);

            onObjectTransformsChanged();
            ObjectEvents.onObjectsTransformed();
        }

        public void onObjectTransformsChanged()
        {
            foreach (var session in _transformSessions)
                session.onTargetTransformsChanged();

            gizmos.onTargetObjectTransformsChanged();
            refreshObjectSelectionUI();
        }

        public void getSelectedObjects(List<GameObject> selectedObjects)
        {
            selectedObjects.Clear();
            selectedObjects.AddRange(_selectedObjects);
        }

        public GameObject getSelectedObject(int index)
        {
            return numSelectedObjects != 0 ? _selectedObjects[index] : null;
        }

        public void setSelectedObject(GameObject gameObject)
        {
            endTransformSession();

            UndoEx.record(this);
            _selectedObjects.Clear();

            _gameObjectBuffer.Clear();
            _gameObjectBuffer.Add(gameObject);
            selectObjects(_gameObjectBuffer, true, true);
            onSelectionChanged(SelectionChangeReason.SetSelected);
            refreshObjectSelectionUI();
        }

        public void setSelectedObjects(List<GameObject> gameObjects)
        {
            endTransformSession();

            UndoEx.record(this);
            _selectedObjects.Clear();

            selectObjects(gameObjects, true, true);
            onSelectionChanged(SelectionChangeReason.SetSelected);
            refreshObjectSelectionUI();
        }

        public void appendObjects(List<GameObject> gameObjects)
        {
            endTransformSession();

            UndoEx.record(this);
            selectObjects(gameObjects, true, true);
            onSelectionChanged(SelectionChangeReason.Append);
            refreshObjectSelectionUI();
        }

        public void setMultiSelectedObjects(List<GameObject> gameObjects, ObjectSelectionShape.Type shapeType, UndoConfig undoConfig)
        {
            if (!canMultiSelect()) return;
            if (undoConfig.allowUndoRedo) UndoEx.record(this);

            if (multiDeselectEnabled && shapeType == ObjectSelectionShape.Type.Rect)
            {
                deselectObjects(gameObjects);
                onSelectionChanged(SelectionChangeReason.MultiSelect);
            }
            else
            {
                if (appendEnabled)
                {
                    selectObjects(gameObjects, true, true);
                    onSelectionChanged(SelectionChangeReason.MultiSelect);
                }
                else
                {
                    _selectedObjects.Clear();
                    selectObjects(gameObjects, false, true);
                    onSelectionChanged(SelectionChangeReason.MultiSelect);
                }
            }

            if (undoConfig.allowUndoRedo && undoConfig.collapseToGroup) Undo.CollapseUndoOperations(undoConfig.groupIndex);
        }

        public void snapAllAxes(PluginGrid grid)
        {
            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            TransformEx.getTransforms(_parentsBuffer, _parentsTransformBuffer);
            UndoEx.recordTransforms(_parentsTransformBuffer);
            grid.snapTransformsAllAxes(_parentsTransformBuffer);
            gizmos.onTargetObjectsUpdated(_gizmosPivotObject);
            ObjectEvents.onObjectsTransformed();
        }

        public void projectOnGrid(PluginGrid grid)
        {
            endTransformSession();

            UndoEx.recordGameObjectTransforms(_selectedObjects);
            GameObjectEx.getParents(_selectedObjects, _parentsBuffer);
            ObjectProjection.projectHierarchiesOnPlane(_parentsBuffer, grid.plane, projectionSettings, null);
            ObjectEvents.onObjectsTransformed();
        }

        public void onObjectsWillBeDeleted(List<GameObject> gameObjects, bool refreshUI)
        {
            UndoEx.record(this);
            if (_selectedObjects.RemoveAll(gameObject => gameObjects.Contains(gameObject)) != 0)
            {
                if (_gizmosPivotObject == null) _gizmosPivotObject = numSelectedObjects != 0 ? _selectedObjects[0] : null;
                gizmos.onTargetObjectsUpdated(_gizmosPivotObject);

                if (refreshUI) refreshObjectSelectionUI();
            }
        }

        public void onSelectedObjectsMightHaveBeenDeleted(bool refreshUI)
        {
            _selectedObjects.RemoveAll(item => item == null);
            if (_gizmosPivotObject == null) _gizmosPivotObject = numSelectedObjects != 0 ? _selectedObjects[0] : null;
            gizmos.onTargetObjectsUpdated(_gizmosPivotObject);

            if (refreshUI) refreshObjectSelectionUI();
        }

        public void onObjectsWillBeDestroyed(List<GameObject> gameObjects, bool refreshUI)
        {
            UndoEx.record(this);
            if (_selectedObjects.RemoveAll(item => gameObjects.Contains(item)) != 0)
            {
                if (_gizmosPivotObject == null) _gizmosPivotObject = numSelectedObjects != 0 ? _selectedObjects[0] : null;
                gizmos.onTargetObjectsUpdated(_gizmosPivotObject);

                if (refreshUI) refreshObjectSelectionUI();
            }
        }

        public void refreshObjectSelectionUI()
        {
            ObjectSelectionUI.instance.refreshObjectTransformUI();
            PluginInspectorUI.instance.refresh();
        }

        private List<Vector3> _originalGrowPositions = new List<Vector3>();
        public void grow()
        {
            if (isAnyTransformSessionActive) return;

            _growSet.Clear();
            _prefabAssetSet.Clear();

            var boundsQConfig                   = ObjectBounds.QueryConfig.defaultConfig;
            boundsQConfig.objectTypes           = GameObjectType.Mesh | GameObjectType.Sprite;
            var objectOverlapFilter             = new ObjectOverlapFilter();
            objectOverlapFilter.objectTypes     = GameObjectType.Mesh | GameObjectType.Sprite;
            var objectOverlapConfig             = ObjectOverlapConfig.defaultConfig;
            objectOverlapConfig.prefabMode      = ObjectOverlapPrefabMode.OnlyPrefabInstanceRoot;

            var frustumPlanes                   = GeometryUtility.CalculateFrustumPlanes(PluginCamera.camera);

            UndoEx.record(this);
            GameObjectEx.getOutermostPrefabInstanceRoots(_selectedObjects, _prefabInstanceBuffer, null);

            bool checkPosConstraints    = growSettings.xPositionConstraint | growSettings.yPositionConstraint | growSettings.zPositionConstraint;

            // Note: Start with a clean slate.
            _selectedObjects.Clear();
            _originalGrowPositions.Clear();
            foreach (var go in _prefabInstanceBuffer)
            {
                _growSet.Add(go);
                go.getAllChildrenAndSelf(true, true, _gameObjectBuffer);
                objectOverlapFilter.addIgnoredObjects(_gameObjectBuffer);

                if (growSettings.usePrefabConstraint)
                    _prefabAssetSet.Add(go.getOutermostPrefabAsset());

                // Select the prefab instance. This ensures that when growing, selection will
                // always contain the top most prefab instance for each object hierarchy, even
                // though the user may have selected children.
                _selectedObjects.Add(go);

                if (checkPosConstraints)
                {
                    OBB obb = ObjectBounds.calcHierarchyWorldOBB(go, boundsQConfig);
                    if (obb.isValid) _originalGrowPositions.Add(obb.center);
                }
            }

            Vector3 gridRight           = PluginScene.instance.grid.right;
            Vector3 gridUp              = PluginScene.instance.grid.up;
            Vector3 gridLook            = PluginScene.instance.grid.look;


            objectOverlapFilter.customFilter = (go) =>
            {
                if (growSettings.usePrefabConstraint && !_prefabAssetSet.Contains(go.getOutermostPrefabAsset())) return false;
                if (growSettings.ignoreOutOfView)
                {
                    AABB aabb = ObjectBounds.calcHierarchyWorldAABB(go, boundsQConfig);
                    if (aabb.isValid)
                    {
                        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, aabb.toBounds())) return false;
                    }
                }

                if (checkPosConstraints)
                {
                    OBB obb             = ObjectBounds.calcHierarchyWorldOBB(go, boundsQConfig);
                    Vector3 obbCenter   = obb.center;

                    int numConditionsToSatisfy = 0;
                    if (growSettings.xPositionConstraint) ++numConditionsToSatisfy;
                    if (growSettings.yPositionConstraint) ++numConditionsToSatisfy;
                    if (growSettings.zPositionConstraint) ++numConditionsToSatisfy;

                    float dot;
                    float xErr = growSettings.xPositionThreshold;
                    float yErr = growSettings.yPositionThreshold;
                    float zErr = growSettings.zPositionThreshold;

                    bool satisfiesAll = false;
                    foreach (var originalPos in _originalGrowPositions)
                    {
                        int numSatisfied = 0;
                        Vector3 vec = obbCenter - originalPos;

                        if (growSettings.xPositionConstraint)
                        {
                            dot = Vector3.Dot(gridRight, vec);
                            if (dot < 0.0f && !growSettings.growLeft) continue;
                            if (dot > 0.0f && !growSettings.growRight) continue;
                            if (!growSettings.useXPositionThreshold || Mathf.Abs(dot) <= xErr) ++numSatisfied;
                        }
                        if (growSettings.yPositionConstraint)
                        {
                            dot = Vector3.Dot(gridUp, vec);
                            if (dot < 0.0f && !growSettings.growDown) continue;
                            if (dot > 0.0f && !growSettings.growUp) continue;
                            if (!growSettings.useYPositionThreshold || Mathf.Abs(dot) <= yErr) ++numSatisfied;
                        }
                        if (growSettings.zPositionConstraint)
                        {
                            dot = Vector3.Dot(gridLook, vec);
                            if (dot < 0.0f && !growSettings.growBackward) continue;
                            if (dot > 0.0f && !growSettings.growForward) continue;
                            if (!growSettings.useZPositionThreshold || Mathf.Abs(dot) <= zErr) ++numSatisfied;
                        }

                        if (numSatisfied == numConditionsToSatisfy)
                        {
                            satisfiesAll = true;
                            break;
                        }
                    }

                    if (!satisfiesAll) return false;
                }

                return true;
            };

            _growSetRemoveBuffer.Clear();
            _growSetAddBuffer.Clear();

            float obbInflate = growSettings.distanceThreshold * 2.0f;  // Note: Multiply by 2 because when inflating it will inflate by half in all directions.
            int numSelected = 0;
            while (_growSet.Count != 0)
            {
                foreach (var go in _growSet)
                {
                    if (canBeIncludedInGrow(go))
                    {
                        // Build the overlap box
                        Vector3 eulerAngles = go.transform.rotation.eulerAngles;
                        OBB overlapOBB = ObjectBounds.calcHierarchyWorldOBB(go, boundsQConfig);
                        overlapOBB.inflate(obbInflate);   

                        // Gather surrounding objects
                        if (PluginScene.instance.overlapBox(overlapOBB, objectOverlapFilter, objectOverlapConfig, _objectOverlapBuffer))
                        {
                            // Extract the prefab instances
                            GameObjectEx.getOutermostPrefabInstanceRoots(_objectOverlapBuffer, _prefabInstanceBuffer, null);

                            // Add the instances to the grow add buffer and select them
                            foreach (var gameObj in _prefabInstanceBuffer)
                            {
                                if (growSettings.rotationConstraintMode != ObjectSelectionGrowRotationConstraintMode.None)
                                {
                                    if (growSettings.rotationConstraintMode == ObjectSelectionGrowRotationConstraintMode.Flexible)
                                    {
                                        if (!gameObj.transform.checkCoordSystemAxesAlignment(overlapOBB.rotation, growSettings.angleThreshold)) continue;
                                    }
                                    else
                                    if (growSettings.rotationConstraintMode == ObjectSelectionGrowRotationConstraintMode.Exact)
                                    {
                                        if (gameObj.transform.rotation.eulerAngles != eulerAngles) continue;
                                    }
                                }

                                _growSetAddBuffer.Add(gameObj);
                                _selectedObjects.Add(gameObj);

                                gameObj.getAllChildrenAndSelf(true, true, _gameObjectBuffer);
                                objectOverlapFilter.addIgnoredObjects(_gameObjectBuffer);

                                ++numSelected;
                                if (growSettings.useMaxCountConstraint && numSelected == growSettings.maxCount) break;
                            }
                        }
                    }

                    // Mark this object for removal
                    _growSetRemoveBuffer.Add(go);

                    if (growSettings.useMaxCountConstraint && numSelected == growSettings.maxCount) break;
                }

                // Remove objects from grow
                foreach (var go in _growSetRemoveBuffer)
                    _growSet.Remove(go);

                // Add more objects for grow
                foreach (var go in _growSetAddBuffer)
                    _growSet.Add(go);

                _growSetRemoveBuffer.Clear();
                _growSetAddBuffer.Clear();

                if (growSettings.useMaxCountConstraint && numSelected == growSettings.maxCount) break;
            }

            _growSet.Clear();
            SceneView.RepaintAll();
        }

        private bool canBeIncludedInGrow(GameObject gameObject)
        {
            GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(gameObject);
            if (objectType == GameObjectType.Terrain) return false;

            if (objectType == GameObjectType.Mesh)
            {
                if (gameObject.isSphericalMesh()) return false;
                if (gameObject.isTerrainMesh()) return false;
            }

            return true;
        }

        private void clickSelect()
        {
            if (!canClickSelect()) return;

            GameObject pickedObject = HandleUtility.PickGameObject(Mouse.instance.position, true);
            if (appendEnabled)
            {
                if (pickedObject != null && isObjectSelectable(pickedObject))
                {
/*
                    if (pickedObject != null && pickedObject.isPartOfPrefabInstance() && (isObjectSelected(pickedObject) || anyChildrenSelected(pickedObject)))
                    {
                        GameObject pickedChild = HandleUtility.PickGameObject(Mouse.instance.position, false);
                        if (pickedChild != null && isObjectSelectable(pickedChild)) pickedObject = pickedChild;
                    }
*/

                    UndoEx.record(this);
                    if (isObjectSelected(pickedObject)) _selectedObjects.Remove(pickedObject);
                    else _selectedObjects.Add(pickedObject);
                    onSelectionChanged(SelectionChangeReason.ClickSelect);
                }
            }
            else
            {
                if (ObjectSelectionPrefs.instance.clickSelectAllowChildSelect)
                {
                    if (pickedObject != null && pickedObject.isPartOfPrefabInstance() && isObjectSelected(pickedObject))
                    {
                        GameObject pickedChild = HandleUtility.PickGameObject(Mouse.instance.position, false);
                        if (pickedChild != null && isObjectSelectable(pickedChild)) pickedObject = pickedChild;
                    }
                }

                UndoEx.record(this);
                _selectedObjects.Clear();
                if (pickedObject != null && isObjectSelectable(pickedObject)) _selectedObjects.Add(pickedObject);
                onSelectionChanged(SelectionChangeReason.ClickSelect);
            }
        }

        private void selectObjects(IEnumerable<GameObject> gameObjects, bool filterAlreadySelected, bool applyRestrictions)
        {
            if (filterAlreadySelected && applyRestrictions)
            {
                foreach (var go in gameObjects)
                {
                    if (isObjectSelectable(go) && !isObjectSelected(go))
                        _selectedObjects.Add(go);
                }
            }
            else
            if (filterAlreadySelected && !applyRestrictions)
            {
                foreach (var go in gameObjects)
                {
                    if (!isObjectSelected(go))
                        _selectedObjects.Add(go);
                }
            }
            else
            if (!filterAlreadySelected && applyRestrictions)
            {
                foreach (var go in gameObjects)
                {
                    if (isObjectSelectable(go))
                        _selectedObjects.Add(go);
                }
            }
            else _selectedObjects.AddRange(gameObjects);
        }

        private void deselectObjects(IEnumerable<GameObject> gameObjects)
        {
            foreach (var go in gameObjects)
                _selectedObjects.Remove(go);
        }

        private void drawSelectionHandles()
        {
            if (numSelectedObjects == 0 || Event.current.type != EventType.Repaint) return;
            if (isTransformSessionActive(ObjectTransformSessionType.BoxSnap) ||
                isTransformSessionActive(ObjectTransformSessionType.SurfaceSnap) ||
                isTransformSessionActive(ObjectTransformSessionType.ModularSnap)) return;

            _selectionHighlight.setGatherObjects(_selectedObjects);
            _selectionHighlight.drawHandles(ObjectSelectionPrefs.instance.parentHighlightColor, ObjectSelectionPrefs.instance.childHighlightColor, ObjectSelectionPrefs.instance.highlightOpacity);
        }

        private void drawSelectionGizmos()
        {
            foreach (var selectedObject in  _selectedObjects)
            {
                // Note: Should not happen but it seems that sometimes, drawSelectionGizmos is called before
                //       _selectedObjects has a chance to be cleaned up of null objects (e.g. after objects have
                //       been deleted in the scene).
                if (selectedObject == null) return;

                if (LayerEx.isLayerHidden(selectedObject.layer) || !selectedObject.activeInHierarchy) return;

                Camera camera = selectedObject.getCamera();
                if (camera != null && camera.enabled)
                {
                    GizmosEx.saveColor();
                    Gizmos.color = ObjectSelectionPrefs.instance.cameraGizmoColor;
                    if (!camera.orthographic) Gizmos.DrawFrustum(camera.transform.position, camera.fieldOfView, camera.farClipPlane, camera.nearClipPlane, camera.aspect);
                    else
                    {
                        GizmosEx.saveMatrix();
                        Vector3 scale = new Vector3(camera.orthographicSize * 2.0f * camera.aspect, camera.orthographicSize * 2.0f, camera.farClipPlane - camera.nearClipPlane);
                        Gizmos.matrix = Matrix4x4.TRS(camera.transform.position + camera.transform.forward * (camera.nearClipPlane + (camera.farClipPlane - camera.nearClipPlane) * 0.5f), camera.transform.rotation, scale);
                        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                        GizmosEx.restoreMatrix();
                    }
                    GizmosEx.restoreColor();
                    return;
                }
            }
        }

        private ObjectSelectionShape getSelectionShape()
        {
            return _selShapes[(int)_selectionShapeType];
        }

        private void onSelectionChanged(SelectionChangeReason changeReason)
        {
            if (changeReason == SelectionChangeReason.MultiSelect && !multiDeselectEnabled && !appendEnabled)
            {
                if (_gizmosPivotObject == null || !isObjectSelected(_gizmosPivotObject))
                    _gizmosPivotObject = numSelectedObjects > 0 ? _selectedObjects[0] : null;
            }
            else _gizmosPivotObject = numSelectedObjects > 0 ? _selectedObjects[numSelectedObjects - 1] : null;
       
            gizmos.onTargetObjectsUpdated(_gizmosPivotObject);
            SceneView.RepaintAll();
        }

        private void endTransformSession()
        {
            if (_activeTransformSession != null) _activeTransformSession.end();

            _activeTransformSession = null;
            SceneView.RepaintAll();
            gizmos.onTargetObjectsUpdated(_gizmosPivotObject);
            ObjectSelectionUI.instance.setObjectTransformUIEnabled(true);
        }

        private ObjectTransformSession getTransformSession(ObjectTransformSessionType sessionType)
        {
            return _transformSessions[(int)sessionType];
        }

        private void OnEnable()
        {
            if (!FileSystem.folderExists(PluginFolders.settings))
                return;

            EditorApplication.hierarchyChanged  += onHierarchyChanged;
            Selection.selectionChanged          += onUnitySelectionChanged;
            Undo.undoRedoPerformed              += onUndoRedo;

            clickSelectEnabled  = true;
            multiSelectEnabled  = true;
            gizmosEnabled       = true;
            gizmos.bindTargetObjects(_selectedObjects);

            int numSessions = _transformSessions.Length;
            for (int i = 0; i < numSessions; ++i)
            {
                _transformSessions[i] = ObjectTransformSession.create((ObjectTransformSessionType)i);
                _transformSessions[i].bindTargetObjects(_selectedObjects);
            }

            var projectionSession   = (getTransformSession(ObjectTransformSessionType.Projection) as ObjectProjectionSession);
            projectionSession.sharedSettings = projectionSettings;
            projectionSession.projected += onProjected;

            var vertexSnapSession   = (getTransformSession(ObjectTransformSessionType.VertexSnap)) as ObjectVertexSnapSession;
            vertexSnapSession.sharedSettings = vertexSnapSettings;

            var boxSnapSession      = (getTransformSession(ObjectTransformSessionType.BoxSnap)) as ObjectBoxSnapSession;
            boxSnapSession.sharedSettings = boxSnapSettings;

            var surfaceSnapSession  = (getTransformSession(ObjectTransformSessionType.SurfaceSnap)) as ObjectSurfaceSnapSession;
            surfaceSnapSession.sharedSettings = surfaceSnapSettings;

            var modularSnapSession  = (getTransformSession(ObjectTransformSessionType.ModularSnap)) as ObjectModularSnapSession;
            modularSnapSession.sharedSettings = modularSnapSettings;
            modularSnapSettings.snapSingleTargetToCursor = true;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged  -= onHierarchyChanged;
            Selection.selectionChanged          -= onUnitySelectionChanged;
            Undo.undoRedoPerformed              -= onUndoRedo;

            if (!FileSystem.folderExists(PluginFolders.settings))
                return;

            var projectionSession               = (getTransformSession(ObjectTransformSessionType.Projection) as ObjectProjectionSession);
            projectionSession.projected         -= onProjected;
        }

        private void OnDestroy()
        {
            int numSessions = _transformSessions.Length;
            for (int i = 0; i < numSessions; ++i)
            {
                ScriptableObjectEx.destroyImmediate(_transformSessions[i]);
                _transformSessions[i] = null;
            }

            ScriptableObjectEx.destroyImmediate(_gizmos);
        }

        private void onHierarchyChanged()
        {
            _selectedObjects.RemoveAll(item => item == null);
            if (_gizmosPivotObject == null) _gizmosPivotObject = numSelectedObjects != 0 ? _selectedObjects[0] : null;
            gizmos.onTargetObjectsUpdated(_gizmosPivotObject);
        }

        private void onUnitySelectionChanged()
        {
            if (GSpawn.active == null) return;

            // Note: We need to refresh the gizmos in case the selected objects
            //       have been transformed with Unity's gizmos. Also, update the
            //       pivot object in case the selection change event was fired 
            //       because objects were deleted from the scene via the Unity
            //       interface.
            _selectedObjects.RemoveAll(item => item == null);
            if (_gizmosPivotObject == null) _gizmosPivotObject = numSelectedObjects != 0 ? _selectedObjects[0] : null;
            gizmos.onTargetObjectsUpdated(_gizmosPivotObject);

            endActiveTransformSession();
        }

        private void onUndoRedo()
        {
            gizmos.onTargetObjectsUpdated(_gizmosPivotObject);

            foreach (var session in _transformSessions)
                session.onUndoRedo();

            refreshObjectSelectionUI();
        }

        private void onProjected()
        {
            endTransformSession();
        }      
    }
}
#endif