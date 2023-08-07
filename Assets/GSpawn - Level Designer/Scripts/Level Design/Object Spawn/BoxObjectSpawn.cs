#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public enum BoxObjectSpawnHeightPatternDirection
    {
        LeftToRight = 0,
        RightToLeft,
        BackToFront,
        FrontToBack
    }

    public class BoxObjectSpawn : ObjectSpawnTool
    {
        [SerializeField]
        private ObjectSpawnExtensionPlane               _extensionPlane             = new ObjectSpawnExtensionPlane();
        [SerializeField]
        private BoxObjectSpawnHeightPatternDirection    _heightPatternDirection     = BoxObjectSpawnHeightPatternDirection.LeftToRight;

        [NonSerialized]
        private ObjectSpawnCellBox          _cellBox;
        [NonSerialized]
        private bool                        _isBuildingBox              = false;
        [NonSerialized]
        private int                         _currentHeight;
        [NonSerialized]
        private OBB                         _refOBB;
        [NonSerialized]
        private ObjectOverlapFilter         _overlapFilter              = new ObjectOverlapFilter();
        [NonSerialized]
        private List<int>                   _heightPattern              = new List<int>();
        [NonSerialized]
        private IntPatternSampler           _heightPatternSampler       = null;

        [NonSerialized]
        private TerrainCollection           _terrainCollection          = new TerrainCollection();
        [NonSerialized]
        private List<GameObject>            _gameObjectBuffer           = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _allChildrenAndSeflBuffer   = new List<GameObject>();
        [NonSerialized]
        private List<OBB>                   _cellOBBBuffer              = new List<OBB>();
        [NonSerialized]
        private List<OBB>                   _mirroredCellOBBBuffer      = new List<OBB>();
        [NonSerialized]
        private List<MirroredObjectList>    _mirroredObjectListBuffer   = new List<MirroredObjectList>();

        [NonSerialized]
        private ObjectModularSnapSettings   _modularSnapSettings;
        [SerializeField]
        private ObjectModularSnapSession    _modularSnapSession;
        [NonSerialized]
        private SceneRaycastFilter          _pickPrefabRaycastFilter;
        [NonSerialized]
        private ObjectProjectionSettings    _terrainProjectionSettings;

        [SerializeField]
        private ObjectMirrorGizmo           _mirrorGizmo;
        [NonSerialized]
        private ObjectMirrorGizmoSettings   _mirrorGizmoSettings;

        private ObjectProjectionSettings    terrainProjectionSettings
        {
            get
            {
                if (_terrainProjectionSettings == null)
                {
                    _terrainProjectionSettings                  = CreateInstance<ObjectProjectionSettings>();
                    UndoEx.saveEnabledState();
                    UndoEx.enabled                              = false;
                    _terrainProjectionSettings.halfSpace        = ObjectProjectionHalfSpace.InFront;
                    _terrainProjectionSettings.embedInSurface   = true;
                    _terrainProjectionSettings.alignAxis        = false;
                    _terrainProjectionSettings.projectAsUnit    = true;
                    UndoEx.restoreEnabledState();
                }
                return _terrainProjectionSettings;
            }
        }
        private BoxObjectSpawnSettingsProfile   settings         { get { return BoxObjectSpawnSettingsProfileDb.instance.activeProfile; } }
        private ObjectModularSnapSession        modularSnapSession
        {
            get
            {
                if (_modularSnapSession == null)
                {
                    _modularSnapSession = CreateInstance<ObjectModularSnapSession>();
                    _modularSnapSession.sharedSettings = modularSnapSettings;
                }
                return _modularSnapSession;
            }
        }

        public ObjectModularSnapSettings        modularSnapSettings
        {
            get
            {
                if (_modularSnapSettings == null) _modularSnapSettings = AssetDbEx.loadScriptableObject<ObjectModularSnapSettings>(PluginFolders.settings, typeof(BoxObjectSpawn).Name + "_" + typeof(ObjectModularSnapSettings).Name);
                return _modularSnapSettings;
            }
        }
        public ObjectMirrorGizmoSettings        mirrorGizmoSettings
        {
            get
            {
                if (_mirrorGizmoSettings == null) _mirrorGizmoSettings = AssetDbEx.loadScriptableObject<ObjectMirrorGizmoSettings>(PluginFolders.settings, typeof(BoxObjectSpawn).Name + "_" + typeof(ObjectMirrorGizmoSettings).Name);
                return _mirrorGizmoSettings;
            }
        }
        public override ObjectSpawnToolId       spawnToolId                     { get { return ObjectSpawnToolId.Box; } }
        public override bool                    requiresSpawnGuide              { get { return true; } }
        public override ObjectMirrorGizmo       mirrorGizmo                     { get { return _mirrorGizmo; } }
        public override bool                    canChangeSpawnGuideTransform    { get { return !_isBuildingBox; } }
        public bool                             isBuildingBox                   { get { return _isBuildingBox; } }

        public BoxObjectSpawn()
        {
            _overlapFilter.customFilter = new Func<GameObject, bool>((GameObject go) => { return !go.isTerrainMesh() && !go.isSphericalMesh(); });
        }

        public override void setSpawnGuidePrefab(PluginPrefab prefab)
        {
            onCancelBoxBuild();
            spawnGuide.usePrefab(prefab, modularSnapSession);
        }

        public override void onNoLongerActive()
        {
            onCancelBoxBuild();
            spawnGuide.destroyGuide();
        }

        public void executeModularSnapSessionCommand(ObjectModularSnapSessionCommand command)
        {
            if (!_isBuildingBox)
                modularSnapSession.executeCommand(command);
        }

        public void nextHeightPatternDirection()
        {
            int newDir = ((int)_heightPatternDirection + 1) % Enum.GetValues(typeof(BoxObjectSpawnHeightPatternDirection)).Length;
            _heightPatternDirection = (BoxObjectSpawnHeightPatternDirection)newDir;

            updateBox();
            EditorUtility.SetDirty(this);
        }

        public void previousHeightPatternDirection()
        {
            int newDir = ((int)_heightPatternDirection - 1);
            if (newDir < 0) newDir = Enum.GetValues(typeof(BoxObjectSpawnHeightPatternDirection)).Length - 1;
            _heightPatternDirection = (BoxObjectSpawnHeightPatternDirection)newDir;

            updateBox();
            EditorUtility.SetDirty(this);
        }

        public void nextExtensionPlane()
        {
            if (!_isBuildingBox && spawnGuide.isPresentInScene)
                _extensionPlane.setRefOBBFace(Box3D.getNextFace(_extensionPlane.refOBBFace));
        }

        public void setCurrentHeight(int height)
        {
            if (!_isBuildingBox) return;

            float sizeAlongHeight = Vector3Ex.getSizeAlongAxis(_refOBB.size, _refOBB.rotation, _extensionPlane.planeNormal);
            if (sizeAlongHeight < 1e-4f) return;

            int oldHeight   = _currentHeight;
            _currentHeight  = height;
            if (settings.heightMode == BoxObjectSpawnHeightMode.Constant)
                _cellBox.setHeight(_currentHeight);
            else
            if (settings.heightMode == BoxObjectSpawnHeightMode.Random)
            {
                int numRows         = _cellBox.numRows;
                int numColumns      = _cellBox.numColumns;
                for (int row = 0; row < numRows; ++row)
                {
                    for (int col = 0; col < numColumns; ++col)
                    {
                        var stack           = _cellBox.getStack(col, row);
                        var randomHeight    = stack.height - oldHeight;
                        stack.setHeight(_currentHeight + randomHeight);
                    }
                }
            }
            else
            if (settings.heightMode == BoxObjectSpawnHeightMode.Pattern)
            {
                updateBoxHeight(_cellBox.size);
            }

            applyCornerMode();
            applyFillMode();
        }

        public void raiseCurrentHeight()
        {
            if (!_isBuildingBox || settings.heightMode != BoxObjectSpawnHeightMode.Constant) return;
            setCurrentHeight(_currentHeight + settings.heightRaiseAmount);
        }

        public void lowerCurrentHeight()
        {
            if (!_isBuildingBox || settings.heightMode != BoxObjectSpawnHeightMode.Constant) return;
            setCurrentHeight(_currentHeight - settings.heightRaiseAmount);
        }

        protected override void doOnSceneGUI()
        {
            Event e = Event.current;

            _mirrorGizmo.onSceneGUI();
            if (!_isBuildingBox)
            {
                spawnGuide.onSceneGUI();
                if (FixedShortcuts.enablePickSpawnGuidePrefabFromScene(e))
                {
                    if (e.isLeftMouseButtonDownEvent())
                    {
                        var prefabPickResult = PluginScene.instance.pickPrefab(PluginCamera.camera.getCursorRay(), _pickPrefabRaycastFilter, ObjectRaycastConfig.defaultConfig);
                        if (prefabPickResult != null)
                        {
                            setSpawnGuidePrefab(prefabPickResult.pickedPluginPrefab);
                            spawnGuide.setRotationAndScale(prefabPickResult.pickedObject.transform.rotation, prefabPickResult.pickedObject.transform.lossyScale);
                        }
                    }
                }

                if (modularSnapSession.isActive)
                {
                    updateExtensionPlane();
                    if (e.isLeftMouseButtonDownEvent())
                    {
                        if (e.noShiftCtrlCmdAlt()) onBeginBoxBuild();
                        else
                        {
                            if (FixedShortcuts.extensionPlane_EnablePickOnClick(e))
                            {
                                GameObjectEx.getAllChildrenAndSelf(spawnGuide.gameObject, false, false, _gameObjectBuffer);
                                _extensionPlane.pickRefOBBFaceWithCursor(_gameObjectBuffer);
                            }
                        }
                    }

                    if (FixedShortcuts.extensionPlane_ChangeByScrollWheel(e))
                    {
                        e.disable();
                        if (e.delta.y < 0) _extensionPlane.setRefOBBFace(Box3D.getNextFace(_extensionPlane.refOBBFace));
                        else _extensionPlane.setRefOBBFace(Box3D.getPreviousFace(_extensionPlane.refOBBFace));
                    }

                    if (_mirrorGizmo.enabled)
                    {
                        _mirrorGizmo.mirrorOBB(spawnGuide.calcWorldOBB(), _cellOBBBuffer);
                        _mirrorGizmo.drawMirroredOBBs(_cellOBBBuffer);
                    }
                }
            }
            else
            {
                if (_mirrorGizmo.enabled)
                {
                    getAllCellOBBs(_cellOBBBuffer);
                    _mirrorGizmo.mirrorOBBs(_cellOBBBuffer, _mirroredCellOBBBuffer);
                    _mirrorGizmo.drawMirroredOBBs(_mirroredCellOBBBuffer);
                }

                if (FixedShortcuts.cancelAction(e) || spawnGuide.gameObject == null)
                {
                    onCancelBoxBuild();
                    return;
                }

                if (FixedShortcuts.objectSpawnStructure_UpdateHeightByScrollWheel(e))
                {
                    e.disable();
                    if (e.delta.y < 0) setCurrentHeight(_currentHeight + settings.heightRaiseAmount);
                    else setCurrentHeight(_currentHeight - settings.heightLowerAmount);
                }

                if (settings.heightMode == BoxObjectSpawnHeightMode.Pattern)
                {
                    if (FixedShortcuts.boxObjectSpawn_ChangePatternDirectionByScrollWheel(e))
                    {
                        e.disable();
                        if (e.delta.y < 0) nextHeightPatternDirection();
                        else previousHeightPatternDirection();
                    }
                }

                if (e.isLeftMouseButtonDownEvent() && SceneViewEx.containsCursor(e)) onEndBoxBuild();
                else if (e.isMouseMoveEvent()) updateBox();
            }
        }

        protected override void draw()
        {
            if (!isSpawnGuidePresentInScene && !_isBuildingBox) return;

            _extensionPlane.borderColor     = ObjectSpawnPrefs.instance.boxSpawnExtensionPlaneBorderColor;
            _extensionPlane.fillColor       = ObjectSpawnPrefs.instance.boxSpawnExtensionPlaneFillColor;
            _extensionPlane.draw();

            if (_cellBox != null)
            {
                var drawConfig              = new ObjectSpawnCellBox.DrawConfig();
                drawConfig.cellWireColor    = ObjectSpawnPrefs.instance.boxSpawnCellWireColor;
                drawConfig.xAxisColor       = ObjectSpawnPrefs.instance.boxSpawnXAxisColor;
                drawConfig.yAxisColor       = ObjectSpawnPrefs.instance.boxSpawnYAxisColor;
                drawConfig.zAxisColor       = ObjectSpawnPrefs.instance.boxSpawnZAxisColor;
                drawConfig.xAxisLength      = ObjectSpawnPrefs.instance.boxSpawnXAxisLength;
                drawConfig.yAxisLength      = ObjectSpawnPrefs.instance.boxSpawnYAxisLength;
                drawConfig.zAxisLength      = ObjectSpawnPrefs.instance.boxSpawnZAxisLength;
                drawConfig.drawInfoText     = ObjectSpawnPrefs.instance.boxSpawnShowInfoText;
                drawConfig.hasUniformHeight = settings.heightMode == BoxObjectSpawnHeightMode.Constant;
                drawConfig.height           = _currentHeight;
                drawConfig.drawGranular     = true;

                _cellBox.draw(drawConfig);
            }
        }

        protected override void onEnabled()
        {
            Undo.undoRedoPerformed              += onUndoRedo;
            _pickPrefabRaycastFilter            = createDefaultPrefabPickRaycastFilter();
            modularSnapSession.sharedSettings   = modularSnapSettings;

            if (_mirrorGizmo == null)
            {
                _mirrorGizmo                    = ScriptableObject.CreateInstance<ObjectMirrorGizmo>();
                _mirrorGizmo.enabled            = false;
            }

            _mirrorGizmo.sharedSettings         = mirrorGizmoSettings;
        }

        protected override void onDisabled()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
            ScriptableObjectEx.destroyImmediate(_modularSnapSession);
            ScriptableObjectEx.destroyImmediate(_terrainProjectionSettings);
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_modularSnapSession);
            ScriptableObjectEx.destroyImmediate(_terrainProjectionSettings);
        }

        private void getAllCellOBBs(List<OBB> obbs)
        {
            obbs.Clear();

            int numRows     = _cellBox.numRows;
            int numColumns  = _cellBox.numColumns;
            for (int row = 0; row < numRows; ++row)
            {
                for (int col = 0; col < numColumns; ++col)
                {
                    var stack       = _cellBox.getStack(col, row);
                    int numCells    = stack.numCells;
                    for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                    {
                        var cell = stack.getCell(cellIndex);
                        if (cell.isGoodForSpawn) obbs.Add(cell.objectOBB);
                    }
                }
            }                 
        }

        private void updateExtensionPlane()
        {
            _extensionPlane.set(calcSpawnGuideWorldOBB(), _extensionPlane.refOBBFace,
                                ObjectSpawnPrefs.instance.boxSpawnExtensionPlaneInflateAmount);
        }

        private void onBeginBoxBuild()
        {
            if (settings.heightMode == BoxObjectSpawnHeightMode.Pattern)
            {
                _heightPatternSampler = IntPatternSampler.create(settings.heightPatternWrapMode);
                if (settings.heightPattern.numValues == 0)
                {
                    Debug.LogWarning("The selected height pattern is empty. The default pattern will be used instead.");
                    IntPatternDb.instance.defaultPattern.getValues(_heightPattern);
                }
                else settings.heightPattern.getValues(_heightPattern);
            }

            _refOBB = calcRefOBB();
            _currentHeight = settings.defaultHeight;

            _cellBox = new ObjectSpawnCellBox(_refOBB, _extensionPlane.planeNormal, _extensionPlane.right);

            var oldBoxSize = _cellBox.setSize(1, 1);
            _cellBox.setHorizontalPadding(settings.horizontalPadding);
            _cellBox.setVerticalPadding(settings.verticalPadding);
            updateBoxHeight(oldBoxSize);
            applyCornerMode();
            applyFillMode();

            BoxObjectSpawnSettingsProfileDbUI.instance.setEnabled(false);
            _isBuildingBox = true;
            spawnGuide.setGuideObjectActive(false);
        }

        private void onCancelBoxBuild()
        {
            _cellBox = null;

            BoxObjectSpawnSettingsProfileDbUI.instance.setEnabled(true);
            _isBuildingBox = false;
            spawnGuide.setGuideObjectActive(true);
        }

        private void onEndBoxBuild()
        {
            spawnObjects();
            _cellBox = null;

            BoxObjectSpawnSettingsProfileDbUI.instance.setEnabled(true);
            _isBuildingBox = false;
            spawnGuide.setGuideObjectActive(true);
        }

        private void updateBox()
        {
            if (!_extensionPlane.cursorRaycast()) return;

            var oldBoxSize = _cellBox.snapSizeAndExtensionAxesToCursor(_extensionPlane, 
                FixedShortcuts.boxObjectSpawn_EnableEqualSize(Event.current), calcBoxMaxSize());

            updateBoxHeight(oldBoxSize);
            applyCornerMode();
            applyFillMode();
        }

        private Vector2Int calcBoxMaxSize()
        {
            Vector2Int maxSize = new Vector2Int(settings.maxSize, settings.maxSize);
            if (settings.heightMode == BoxObjectSpawnHeightMode.Pattern && settings.constrainSizeToHeightPattern)
            {
                if (_heightPatternDirection == BoxObjectSpawnHeightPatternDirection.LeftToRight ||
                    _heightPatternDirection == BoxObjectSpawnHeightPatternDirection.RightToLeft)
                {
                    if (_heightPattern.Count < settings.maxSize)
                        maxSize.x = _heightPattern.Count;
                }
                else
                {
                    if (_heightPattern.Count < settings.maxSize)
                        maxSize.y = _heightPattern.Count;
                }
            }

            return maxSize;
        }

        [NonSerialized]
        private MirroredObjectList _spawnObjects_MirroredObjectList = new MirroredObjectList();
        private void spawnObjects()
        {
            skipCellsBeforeSpawn();

            _spawnObjects_MirroredObjectList.objects.Clear();
            _gameObjectBuffer.Clear();

            if (settings.projectionMode == BoxObjectSpawnProjectionMode.Terrains)
                PluginScene.instance.findAllTerrains(_terrainCollection);

            bool projectOnTerrains = settings.projectionMode == BoxObjectSpawnProjectionMode.Terrains &&
                (_terrainCollection.unityTerrains.Count != 0 || _terrainCollection.terrainMeshes.Count != 0);

            int numRows     = _cellBox.numRows;
            int numColumns  = _cellBox.numColumns;

            for (int row = 0; row < numRows; ++row)
            {
                for (int col = 0; col < numColumns; ++col)
                {
                    ObjectSpawnCellStack stack  = _cellBox.getStack(col, row);
                    int numCells                = stack.numCells;

                    for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                    {
                        ObjectSpawnCell cell = stack.getCell(cellIndex);
                        if (!cell.isGoodForSpawn) continue;

                        GameObject go = spawnObjectInCell(stack, cell, cellIndex);
                        if (go != null) _gameObjectBuffer.Add(go);
                    }

                    if (projectOnTerrains) ObjectProjection.projectHierarchiesOnTerrainsAsUnit(_gameObjectBuffer, _terrainCollection, terrainProjectionSettings);

                    ObjectEvents.onObjectsSpawned(_gameObjectBuffer);   // Note: Needed to handle avoidOverlaps with mirroring enabled (bottom of function).

                    if (_mirrorGizmo.enabled)
                    {
                        if (projectOnTerrains)
                        {
                            _mirrorGizmo.mirrorObjectsOrganized_NoDuplicateCommand(_gameObjectBuffer, _mirroredObjectListBuffer);
                            foreach (var list in _mirroredObjectListBuffer)
                            {
                                ObjectProjection.projectHierarchiesOnTerrainsAsUnit(list.objects, _terrainCollection, terrainProjectionSettings);
                                if (settings.avoidOverlaps) _spawnObjects_MirroredObjectList.objects.AddRange(list.objects);
                            }
                        }
                        else
                        {
                            if (settings.avoidOverlaps) _mirrorGizmo.mirrorObjects_NoDuplicateCommand(_gameObjectBuffer, _spawnObjects_MirroredObjectList, true);
                            else _mirrorGizmo.mirrorObjects(_gameObjectBuffer);
                        }
                    }

                    _gameObjectBuffer.Clear();
                }
            }

            if (settings.avoidOverlaps && _spawnObjects_MirroredObjectList.objects.Count != 0)
            {
                prepareSceneObjectOverlapFilter();
                ObjectEvents.onObjectsSpawned(_spawnObjects_MirroredObjectList.objects);
                destroyObjectsWhichOverlapScene(_spawnObjects_MirroredObjectList.objects);
            }
        }

        private void destroyObjectsWhichOverlapScene(List<GameObject> gameObjects)
        {
            ObjectOverlapConfig overlapConfig       = ObjectOverlapConfig.defaultConfig;
            ObjectBounds.QueryConfig boundsQConfig  = ObjectBounds.QueryConfig.defaultConfig;
            boundsQConfig.objectTypes               = GameObjectType.Mesh | GameObjectType.Sprite;

            var oldIgnoredHierarchy = _overlapFilter.ignoredHierarchy;
            for (int i = 0; i < gameObjects.Count;)
            {
                GameObject go   = gameObjects[i];
                OBB obb         = ObjectBounds.calcHierarchyWorldOBB(go, boundsQConfig);

                if (obb.isValid)
                {  
                    _overlapFilter.ignoredHierarchy = go;
                    obb.inflate(-1e-2f);
                    if (PluginScene.instance.overlapBox(obb, _overlapFilter, overlapConfig))
                    {
                        ObjectEvents.onObjectWillBeDestroyed(go);
                        GameObject.DestroyImmediate(go);
                        gameObjects.RemoveAt(i);
                        continue;
                    }
                }

                ++i;
            }

            _overlapFilter.ignoredHierarchy = oldIgnoredHierarchy;
        }

        private GameObject spawnObjectInCell(ObjectSpawnCellStack stack, ObjectSpawnCell cell, int cellIndex)
        {
            if (settings.prefabPickMode == BoxObjectSpawnPrefabPickMode.SpawnGuide)
            {
                Vector3 objectPos = ObjectPositionCalculator.calcRootPosition(spawnGuide.gameObject,
                    _refOBB, cell.objectOBBCenter, spawnGuide.lossyScale, cell.objectOBBRotation);
                return spawnGuide.spawn(objectPos, cell.objectOBBRotation, spawnGuide.lossyScale);
            }
            else
            if (settings.prefabPickMode == BoxObjectSpawnPrefabPickMode.Random)
            {
                var pool = settings.randomPrefabProfile;
                RandomPrefab prefab = pool.pickPrefab();
                if (prefab != null)
                {
                    PluginPrefab pluginPrefab = prefab.pluginPrefab;
                    GameObject prefabAsset = pluginPrefab.prefabAsset;

                    Vector3 objectPos = ObjectPositionCalculator.calcRootPosition(prefabAsset, ObjectBounds.calcHierarchyWorldOBB(prefabAsset, spawnGuide.worldOBBQConfig),
                        cell.objectOBBCenter, prefabAsset.transform.lossyScale, cell.objectOBBRotation);
                    return pluginPrefab.spawn(objectPos, cell.objectOBBRotation, prefabAsset.transform.lossyScale);
                }
            }
            else
            if (settings.prefabPickMode == BoxObjectSpawnPrefabPickMode.HeightRange)
            {
                var pool = settings.heightRangePrefabProfile;
                IntRangePrefab prefab = pool.pickPrefab(stack.cellIndexToHeight(cellIndex));
                if (prefab != null)
                {
                    PluginPrefab pluginPrefab = prefab.pluginPrefab;
                    GameObject prefabAsset = pluginPrefab.prefabAsset;

                    Vector3 objectPos = ObjectPositionCalculator.calcRootPosition(prefabAsset, ObjectBounds.calcHierarchyWorldOBB(prefabAsset, spawnGuide.worldOBBQConfig),
                        cell.objectOBBCenter, prefabAsset.transform.lossyScale, cell.objectOBBRotation);
                    return pluginPrefab.spawn(objectPos, cell.objectOBBRotation, prefabAsset.transform.lossyScale);
                }
            }

            return null;
        }

        private float getOverlapCheckOBBInflateAmount()
        {
            return -1e-1f;
        }

        private ObjectOverlapConfig getOverlapCheckConfig()
        {
            return ObjectOverlapConfig.defaultConfig;
        }

        private void prepareSceneObjectOverlapFilter()
        {
            _overlapFilter.clearIgnoredObjects();
            spawnGuide.gameObject.getAllChildrenAndSelf(true, true, _allChildrenAndSeflBuffer);
            _overlapFilter.setIgnoredObjects(_allChildrenAndSeflBuffer);
            _overlapFilter.objectTypes      = GameObjectType.Mesh | GameObjectType.Sprite;
            _overlapFilter.ignoredHierarchy = null;
        }

        private void skipCellsBeforeSpawn()
        {
            float obbInflateAmount  = getOverlapCheckOBBInflateAmount();
            var overlapConfig       = getOverlapCheckConfig();
            prepareSceneObjectOverlapFilter();

            int numRows             = _cellBox.numRows;
            int numColumns          = _cellBox.numColumns;
            for (int row = 0; row < numRows; ++row)
            {
                for (int col = 0; col < numColumns; ++col)
                {
                    bool avoidOverlaps = settings.avoidOverlaps;
                    ObjectSpawnCellStack stack = _cellBox.getStack(col, row);
                    if (avoidOverlaps)
                    {
                        OBB stackOBB = stack.obb;
                        stackOBB.inflate(obbInflateAmount);
                        if (stackOBB.isValid && !PluginScene.instance.overlapBox(stackOBB, _overlapFilter, overlapConfig))
                            avoidOverlaps = false;
                    }

                    int numCells = stack.numCells;
                    for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                    {
                        ObjectSpawnCell cell = stack.getCell(cellIndex);
                        if (!cell.isGoodForSpawn) continue;

                        if (Probability.evalChance(settings.objectSkipChance))
                        {
                            cell.skipped = true;
                            continue;
                        }

                        if (avoidOverlaps)
                        {
                            OBB cellOBB = cell.objectOBB;
                            if (cellOBB.isValid)
                            {
                                cellOBB.inflate(obbInflateAmount);
                                if (PluginScene.instance.overlapBox(cellOBB, _overlapFilter, overlapConfig))
                                {
                                    cell.skipped = true;
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void updateBoxHeight(Vector2Int oldSize)
        {
            if (settings.heightMode == BoxObjectSpawnHeightMode.Constant) _cellBox.setHeight(_currentHeight);
            else if (settings.heightMode == BoxObjectSpawnHeightMode.Random)
            {
                // Note: If the old box size is >= than the current size along both axes, keep the old values.
                if (oldSize.x >= _cellBox.numColumns && oldSize.y >= _cellBox.numRows) return;

                int numColumns = _cellBox.numColumns;
                int numRows = _cellBox.numRows;
                for (int row = 0; row < numRows; ++row)
                {
                    for (int col = 0; col < numColumns; ++col)
                    {
                        if (col >= oldSize.x || row >= oldSize.y)
                        {
                            var stack = _cellBox.getStack(col, row);
                            stack.setHeight(_currentHeight + UnityEngine.Random.Range(settings.minRandomHeight, settings.maxRandomHeight + 1));
                        }
                    }
                }
            }
            else if (settings.heightMode == BoxObjectSpawnHeightMode.Pattern)
            {
                if (_heightPatternDirection == BoxObjectSpawnHeightPatternDirection.LeftToRight ||
                    _heightPatternDirection == BoxObjectSpawnHeightPatternDirection.RightToLeft)
                {
                    int numColumns      = _cellBox.numColumns;
                    int numRows         = _cellBox.numRows;
                    int startCol        = _heightPatternDirection == BoxObjectSpawnHeightPatternDirection.LeftToRight ? 0 : numColumns - 1;
                    int endCol          = _heightPatternDirection == BoxObjectSpawnHeightPatternDirection.LeftToRight ? numColumns - 1 : 0;
                    int colAdd          = _heightPatternDirection == BoxObjectSpawnHeightPatternDirection.LeftToRight ? 1 : -1;
                    int pastEndCol      = endCol + colAdd;
                    for (int row = 0; row < numRows; ++row)
                    {
                        int heightValIndex = 0;
                        for (int col = startCol; col != pastEndCol; col += colAdd)
                        {
                            var stack = _cellBox.getStack(col, row);
                            stack.setHeight(_currentHeight + _heightPatternSampler.sample(_heightPattern, heightValIndex++));
                        }
                    }
                }
                else
                if (_heightPatternDirection == BoxObjectSpawnHeightPatternDirection.BackToFront || 
                    _heightPatternDirection == BoxObjectSpawnHeightPatternDirection.FrontToBack)
                {
                    int numColumns      = _cellBox.numColumns;
                    int numRows         = _cellBox.numRows;
                    int startRow        = _heightPatternDirection == BoxObjectSpawnHeightPatternDirection.FrontToBack ? 0 : numRows - 1;
                    int endRow          = _heightPatternDirection == BoxObjectSpawnHeightPatternDirection.FrontToBack ? numRows - 1 : 0;
                    int rowAdd          = _heightPatternDirection == BoxObjectSpawnHeightPatternDirection.FrontToBack ? 1 : -1;
                    int pastEndRow      = endRow + rowAdd;
                    int heightValIndex  = 0;
                    for (int row = startRow; row != pastEndRow; row += rowAdd)
                    {
                        int heightVal = _heightPatternSampler.sample(_heightPattern, heightValIndex++);
                        for (int col = 0; col < numColumns; ++col)
                        {
                            var stack = _cellBox.getStack(col, row);
                            stack.setHeight(_currentHeight + heightVal);
                        }
                    }
                }
            }
        }

        private bool canApplyCornerGaps()
        {
            return _cellBox.numRows > (settings.cornerGapSize * 2) &&
                   _cellBox.numColumns > (settings.cornerGapSize * 2);
        }

        private void applyCornerMode()
        {
            if (settings.cornerMode == BoxObjectSpawnCornerMode.Gap &&
                canApplyCornerGaps())
            {
                int numRows     = _cellBox.numRows;
                int numColumns  = _cellBox.numColumns;
                for (int row = 0; row < numRows; ++row)
                {
                    for (int col = 0; col < numColumns; ++col)
                    {
                        var stack = _cellBox.getStack(col, row);
                        stack.setAllCellsSkipped(false);

                        bool colRes = col < settings.cornerGapSize || col >= (numColumns - settings.cornerGapSize);
                        bool rowRes = row < settings.cornerGapSize || row >= (numRows - settings.cornerGapSize);
                        if (colRes && rowRes) stack.setAllCellsSkipped(true);
                    }
                }
            }
        }

        private void applyFillMode()
        {
            if (settings.fillMode == BoxObjectSpawnFillMode.Border)
            {
                bool canGapCorners = settings.cornerMode == BoxObjectSpawnCornerMode.Gap && canApplyCornerGaps();

                int numRows     = _cellBox.numRows;
                int numColumns  = _cellBox.numColumns;
                for (int row = 0; row < numRows; ++row)
                {
                    for (int col = 0; col < numColumns; ++col)
                    {
                        var stack = _cellBox.getStack(col, row);
                        stack.setAllCellsOutOfScope(false);

                        int numCells = stack.numCells;
                        for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                        {
                            var cell = stack.getCell(cellIndex);
                            bool cellRes = cellIndex >= settings.borderWidth && cellIndex < (numCells - settings.borderWidth);

                            int actualBorderWidth = settings.borderWidth;
                            if (canGapCorners) actualBorderWidth += settings.cornerGapSize;

                            // Note: Handle special case where the cell sits in the corner. If that is so,
                            //       we can skip all other tests.
                            if (canGapCorners)
                            {
                                if (row < settings.cornerGapSize && col == settings.cornerGapSize) continue;
                                if (row < settings.cornerGapSize && col == (numColumns - settings.cornerGapSize - 1)) continue;

                                if (row == settings.cornerGapSize && col <= settings.cornerGapSize) continue;
                                if (row == settings.cornerGapSize && col >= (numColumns - settings.cornerGapSize - 1)) continue;

                                if (row > (numRows - settings.cornerGapSize - 1) && col == settings.cornerGapSize) continue;
                                if (row > (numRows - settings.cornerGapSize - 1) && col == (numColumns - settings.cornerGapSize - 1)) continue;

                                if (row == (numRows - settings.cornerGapSize - 1) && col <= settings.cornerGapSize) continue;
                                if (row == (numRows - settings.cornerGapSize - 1) && col >= (numColumns - settings.cornerGapSize - 1)) continue;
                            }

                            if (col == 0 || col == numColumns - 1)
                            {
                                if (cellRes &&
                                    row >= actualBorderWidth && row < (numRows - actualBorderWidth)) cell.outOfScope = true;
                            }
                            else
                            if (row == 0 || row == numRows - 1)
                            {
                                if (cellRes &&
                                    col >= actualBorderWidth && col < (numColumns - actualBorderWidth)) cell.outOfScope = true;
                            }
                            else
                            {
                                if (cellIndex == 0 || cellIndex == numCells - 1)
                                {
                                    if ((col >= settings.borderWidth && col < (numColumns - settings.borderWidth)) &&
                                        (row >= settings.borderWidth && row < (numRows - settings.borderWidth))) cell.outOfScope = true;
                                }
                                else cell.outOfScope = true;
                            }
                        }
                    }
                }
            }
            else if(settings.fillMode == BoxObjectSpawnFillMode.Hollow)
            {
                bool canGapCorners  = canApplyCornerGaps();
                int numRows         = _cellBox.numRows;
                int numColumns      = _cellBox.numColumns;
                for (int row = 0; row < numRows; ++row)
                {
                    for (int col = 0; col < numColumns; ++col)
                    {
                        var stack = _cellBox.getStack(col, row);
                        stack.setAllCellsOutOfScope(true);

                        bool stackRes;
                        if (settings.cornerMode == BoxObjectSpawnCornerMode.Normal || !canGapCorners)
                        {
                            stackRes = col == 0 || col == numColumns - 1;
                            stackRes |= (row == 0 || row == numRows - 1);
                        }
                        else
                        {
                            if (row < settings.cornerGapSize || row >= (numRows - settings.cornerGapSize))
                            {
                                stackRes = col == settings.cornerGapSize || col == numColumns - settings.cornerGapSize - 1;
                                stackRes |= (row == 0 || row == numRows - 1);
                            }
                            else
                            {
                                if (row == settings.cornerGapSize || row == (numRows - settings.cornerGapSize - 1))
                                    stackRes = col < settings.cornerGapSize || col >= numColumns - settings.cornerGapSize;
                                else stackRes = col == 0 || col == numColumns - 1;
                            }
                        }

                        int numCells = stack.numCells;
                        for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                        {
                            var cell = stack.getCell(cellIndex);
                            if (stackRes || (cellIndex == 0 || cellIndex == numCells - 1))
                                cell.outOfScope = false;
                        }
                    }
                }
            }
        }

        private OBB calcRefOBB()
        {
            OBB refOBB = calcSpawnGuideWorldOBB();
            if (settings.cellMode == BoxObjectSpawnCellMode.Grid)
            {
                if (settings.useSceneGridCellSize) refOBB.size = PluginScene.instance.grid.activeSettings.cellSize;
                else refOBB.size = settings.gridCellSize;
            }
            else
            {
                if (refOBB.size.magnitude < 1e-5f) refOBB = new OBB(spawnGuide.position, Vector3Ex.create(settings.volumlessObjectSize), spawnGuide.rotation);
                else
                {
                    // Note: If the size of the OBB is 0 along one of the extension axes, use the volumeless object size instead.
                    Vector3 obbSize = refOBB.size;
                    float size0     = Vector3Ex.getSizeAlongAxis(obbSize, refOBB.rotation, _extensionPlane.right);
                    float size1     = Vector3Ex.getSizeAlongAxis(obbSize, refOBB.rotation, _extensionPlane.look);
                    if (size0 < 1e-5f || size1 < 1e-5f)
                    {
                        if (obbSize.x < 1e-5f) obbSize.x = settings.volumlessObjectSize;
                        if (obbSize.y < 1e-5f) obbSize.y = settings.volumlessObjectSize;
                        if (obbSize.z < 1e-5f) obbSize.z = settings.volumlessObjectSize;
                        refOBB.size = obbSize;
                    }
                }
            }

            return refOBB;
        }

        private void onUndoRedo()
        {
            onCancelBoxBuild();
        }
    }
}
#endif