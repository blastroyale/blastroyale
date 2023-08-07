#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class SegmentsObjectSpawn : ObjectSpawnTool
    {
        [SerializeField]
        private ObjectSpawnExtensionPlane           _extensionPlane     = new ObjectSpawnExtensionPlane();
        [NonSerialized]
        private bool                                _isBuildingSegments = false;
        [NonSerialized]
        private List<ObjectSpawnCellSegment>        _segments           = new List<ObjectSpawnCellSegment>();
        [NonSerialized]
        private ObjectSpawnCellSegment              _lastSegment;
        [NonSerialized]
        private ObjectSpawnCellSegment              _penultimateSegment;
        [NonSerialized]
        private int                                 _currentHeight;
        [NonSerialized]
        private List<int>                           _randomHeightValues = new List<int>();
        [NonSerialized]
        private OBB                                 _refOBB;
        [NonSerialized]
        private ObjectOverlapFilter                 _overlapFilter      = new ObjectOverlapFilter();

        [NonSerialized]
        private bool                                _rotateAtCorners;
        [NonSerialized]
        private float                               _horizontalPadding;
        [NonSerialized]
        private SegmentsObjectSpawnCornerConnection _cornerConnection;
        [NonSerialized]
        private int                                 _indexOfFirstStackInPenultimateSegment;
        [NonSerialized]
        private List<int>                           _heightPattern              = new List<int>();
        [NonSerialized]
        private IntPatternSampler                   _heightPatternSampler       = null;

        [NonSerialized]
        private TerrainCollection                   _terrainCollection          = new TerrainCollection();
        [NonSerialized]
        private List<GameObject>                    _gameObjectBuffer           = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>                    _allChildrenAndSeflBuffer   = new List<GameObject>();
        [NonSerialized]
        private List<OBB>                           _cellOBBBuffer              = new List<OBB>();
        [NonSerialized]
        private List<OBB>                           _mirroredCellOBBBuffer      = new List<OBB>();
        [NonSerialized]
        private List<MirroredObjectList>            _mirroredObjectListBuffer   = new List<MirroredObjectList>();

        [NonSerialized]
        private ObjectModularSnapSettings           _modularSnapSettings;
        [SerializeField]
        private ObjectModularSnapSession            _modularSnapSession;
        [NonSerialized]
        private SceneRaycastFilter                  _pickPrefabRaycastFilter;
        [NonSerialized]
        private ObjectProjectionSettings            _terrainProjectionSettings;

        [SerializeField]
        private ObjectMirrorGizmo                   _mirrorGizmo;
        [NonSerialized]
        private ObjectMirrorGizmoSettings           _mirrorGizmoSettings;

        private ObjectProjectionSettings            terrainProjectionSettings
        {
            get
            {
                if (_terrainProjectionSettings == null)
                {
                    _terrainProjectionSettings                  = CreateInstance<ObjectProjectionSettings>();
                    UndoEx.saveEnabledState();
                    UndoEx.enabled = false;
                    _terrainProjectionSettings.halfSpace        = ObjectProjectionHalfSpace.InFront;
                    _terrainProjectionSettings.embedInSurface   = true;
                    _terrainProjectionSettings.alignAxis        = false;
                    _terrainProjectionSettings.projectAsUnit    = true;
                    UndoEx.restoreEnabledState();
                }
                return _terrainProjectionSettings;
            }
        }
        private SegmentsObjectSpawnSettingsProfile  settings    { get { return SegmentsObjectSpawnSettingsProfileDb.instance.activeProfile; } }     
        private ObjectModularSnapSession            modularSnapSession
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

        public ObjectModularSnapSettings            modularSnapSettings
        {
            get
            {
                if (_modularSnapSettings == null) _modularSnapSettings = AssetDbEx.loadScriptableObject<ObjectModularSnapSettings>(PluginFolders.settings, typeof(SegmentsObjectSpawn).Name + "_" + typeof(ObjectModularSnapSettings).Name);
                return _modularSnapSettings;
            }
        }
        public ObjectMirrorGizmoSettings            mirrorGizmoSettings
        {
            get
            {
                if (_mirrorGizmoSettings == null) _mirrorGizmoSettings = AssetDbEx.loadScriptableObject<ObjectMirrorGizmoSettings>(PluginFolders.settings, typeof(SegmentsObjectSpawn).Name + "_" + typeof(ObjectMirrorGizmoSettings).Name);
                return _mirrorGizmoSettings;
            }
        }
        public override ObjectSpawnToolId           spawnToolId                     { get { return ObjectSpawnToolId.Segments; } }
        public override bool                        requiresSpawnGuide              { get { return true; } }
        public override bool                        canChangeSpawnGuideTransform    { get { return !_isBuildingSegments; } }
        public override ObjectMirrorGizmo           mirrorGizmo                     { get { return _mirrorGizmo; } }
        public bool                                 isBuildingSegments              { get { return _isBuildingSegments; } }
        public int                                  currentHeight                   { get { return _currentHeight; } }

        public SegmentsObjectSpawn()
        {
            _overlapFilter.customFilter = new Func<GameObject, bool>((GameObject go) => { return !go.isTerrainMesh() && !go.isSphericalMesh(); });
        }

        public override void setSpawnGuidePrefab(PluginPrefab prefab)
        {
            onCancelSegmentsBuild();
            spawnGuide.usePrefab(prefab, modularSnapSession);
        }

        public override void onNoLongerActive()
        {
            onCancelSegmentsBuild();
            spawnGuide.destroyGuide();
        }

        public void executeModularSnapSessionCommand(ObjectModularSnapSessionCommand command)
        {
            if (!_isBuildingSegments)
                modularSnapSession.executeCommand(command);
        }

        public void nextExtensionPlane()
        {
            if (!_isBuildingSegments && spawnGuide.isPresentInScene)
                _extensionPlane.setRefOBBFace(Box3D.getNextFace(_extensionPlane.refOBBFace));
        }

        public void stepBack()
        {
            if (!_isBuildingSegments || _segments.Count < 4) return;

            _segments.RemoveAt(_segments.Count - 1);
            _segments.RemoveAt(_segments.Count - 1);

            _lastSegment = _segments[_segments.Count - 1];
            _penultimateSegment = _segments[_segments.Count - 2];
        }

        public void setCurrentHeight(int height)
        {
            if (!_isBuildingSegments) return;

            float sizeAlongHeight = Vector3Ex.getSizeAlongAxis(_refOBB.size, _refOBB.rotation, _extensionPlane.planeNormal);
            if (sizeAlongHeight < 1e-4f) return;

            int oldHeight   = _currentHeight;
            _currentHeight  = height;
            if (settings.heightMode == SegmentsObjectSpawnHeightMode.Constant)
            {
                foreach (var segment in _segments)
                {
                    segment.setHeight(_currentHeight);
                    jitterSegment(segment, 0);
                }
            }
            else
            if (settings.heightMode == SegmentsObjectSpawnHeightMode.Random)
            {
                foreach (var segment in _segments)
                {
                    int numStacks = segment.numStacks;
                    for (int i = 0; i < numStacks; ++i)
                    {
                        var stack           = segment.getStack(i);
                        var randomHeight    = stack.height - oldHeight;
                        stack.setHeight(_currentHeight + randomHeight);
                    }
                    jitterSegment(segment, 0);
                }
            }
            else
            if (settings.heightMode == SegmentsObjectSpawnHeightMode.Pattern)
            {
                foreach (var segment in _segments)
                {
                    updatePatternSegmentHeight(segment);
                    jitterSegment(segment, 0);
                }
            }

            applyFillMode(0);
            detectOccludedCells();
        }

        public void raiseCurrentHeight()
        {
            setCurrentHeight(_currentHeight + settings.heightRaiseAmount);
        }

        public void lowerCurrentHeight()
        {
            setCurrentHeight(_currentHeight - settings.heightLowerAmount);
        }

        protected override void doOnSceneGUI()
        {
            Event e = Event.current;

            _mirrorGizmo.onSceneGUI();
            if (!_isBuildingSegments)
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
                        if (e.noShiftCtrlCmdAlt()) onBeginSegmentsBuild();
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
                    onCancelSegmentsBuild();
                    return;
                }

                if (FixedShortcuts.objectSpawnStructure_UpdateHeightByScrollWheel(e))
                {
                    e.disable();
                    if (e.delta.y < 0) setCurrentHeight(_currentHeight + settings.heightRaiseAmount);
                    else setCurrentHeight(_currentHeight - settings.heightLowerAmount);
                }
                else 
                if (e.isRightMouseButtonDownEvent() && FixedShortcuts.selectionSegments_EnableStepBack(e))
                {
                    stepBack();
                    e.disable();
                }
                else
                if (e.isLeftMouseButtonDownEvent())
                {
                    if (FixedShortcuts.structureBuild_EnableCommitOnLeftClick(e))
                    {
                        onEndSegmentsBuild();
                        return;
                    }
                    else appendSegmentsOnLeftMouseButtonDown();
                }

                if (e.isMouseMoveEvent()) updateSegments();
            }
        }

        protected override void draw()
        {
            if (!isSpawnGuidePresentInScene && !_isBuildingSegments) return;

            _extensionPlane.borderColor = ObjectSpawnPrefs.instance.segmentsSpawnExtensionPlaneBorderColor;
            _extensionPlane.fillColor   = ObjectSpawnPrefs.instance.segmentsSpawnExtensionPlaneFillColor;
            _extensionPlane.draw();

            ObjectSpawnCellSegment.DrawConfig drawConfig = new ObjectSpawnCellSegment.DrawConfig();
            drawConfig.drawGranular     = true;
            drawConfig.cellWireColor    = ObjectSpawnPrefs.instance.segmentsSpawnCellWireColor;

            int numSegments = _segments.Count;
            for (int i = 0; i < numSegments; ++i)
            {
                var segment = _segments[i];
                segment.draw(drawConfig);
            }
        }

        protected override void onEnabled()
        {
            Undo.undoRedoPerformed  += onUndoRedo;
            _pickPrefabRaycastFilter = createDefaultPrefabPickRaycastFilter();
            modularSnapSession.sharedSettings = modularSnapSettings;

            if (_mirrorGizmo == null)
            {
                _mirrorGizmo = ScriptableObject.CreateInstance<ObjectMirrorGizmo>();
                _mirrorGizmo.enabled = false;
            }

            _mirrorGizmo.sharedSettings = mirrorGizmoSettings;
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
            foreach(var segment in _segments)
            {
                int numStacks = segment.numStacks;
                for (int stackIndex = 0; stackIndex < numStacks; ++stackIndex)
                {
                    var stack       = segment.getStack(stackIndex);
                    int numCells    = stack.numCells;
                    for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                    {
                        var cell = stack.getCell(cellIndex);
                        if (cell.isGoodForSpawn) obbs.Add(cell.objectOBB);
                    }
                }
            }
        }

        private void onBeginSegmentsBuild()
        {
            if (settings.fillMode == SegmentsObjectSpawnFillMode.Border)
            {
                if (settings.beginBorderWidth == 0 &&
                    settings.endBorderWidth == 0 &&
                    settings.topBorderWidth == 0 &&
                    settings.bottomBorderWidth == 0)
                {
                    Debug.LogError("Segments fill mode is set to Border, but all border widths are 0. You must define at least one border width to be >= 1.");
                    return;
                }
            }

            if (settings.heightMode == SegmentsObjectSpawnHeightMode.Pattern)
            {
                _heightPatternSampler = IntPatternSampler.create(settings.heightPatternWrapMode);
                if (settings.heightPattern.numValues == 0)
                {
                    Debug.LogWarning("The selected height pattern is empty. The default pattern will be used instead.");
                    IntPatternDb.instance.defaultPattern.getValues(_heightPattern);
                }
                else settings.heightPattern.getValues(_heightPattern);
            }

            _refOBB                                 = calcRefOBB();
            _currentHeight                          = settings.defaultHeight;
            _indexOfFirstStackInPenultimateSegment  = 0;
            _rotateAtCorners                        = settings.rotateAtCorners;
            _horizontalPadding                      = settings.horizontalPadding;
            _cornerConnection                       = settings.cornerConnection;
            if (!_rotateAtCorners)
            {
                float size0 = Vector3Ex.getSizeAlongAxis(_refOBB.size, _refOBB.rotation, _extensionPlane.right);
                float size1 = Vector3Ex.getSizeAlongAxis(_refOBB.size, _refOBB.rotation, _extensionPlane.look);
                if (size0 < 1e-5f || size1 < 1e-5f) _rotateAtCorners = true;
            }

            Vector3 majorAxis = Vector3.zero;
            /*if (settings.majorAxis == SegmentsObjectSpawnMajorAxis.ViewAligned)
                majorAxis = ObjectSpawnCellSegment.pickInitialExtensionAxisByViewAlignment(_extensionPlane, calcSpawnGuideWorldOBB());*/
            /*else*/ if (settings.majorAxis == SegmentsObjectSpawnMajorAxis.Longest)
                majorAxis = ObjectSpawnCellSegment.pickInitialExtensionAxisByLongestAxis(_extensionPlane, calcSpawnGuideWorldOBB());
            else if (settings.majorAxis == SegmentsObjectSpawnMajorAxis.Shortest)
                majorAxis = ObjectSpawnCellSegment.pickInitialExtensionAxisByShortestAxis(_extensionPlane, calcSpawnGuideWorldOBB());

            _penultimateSegment = new ObjectSpawnCellSegment(_refOBB, _extensionPlane.planeNormal, majorAxis);
            _penultimateSegment.setHorizontalPadding(_horizontalPadding);
            _penultimateSegment.setVerticalPadding(settings.verticalPadding);
            _penultimateSegment.setLength(1);

            if (settings.heightMode != SegmentsObjectSpawnHeightMode.Pattern) updateConstantOrRandomSegmentHeight(_penultimateSegment, 0);
            else updatePatternSegmentHeight(_penultimateSegment);

            jitterSegment(_penultimateSegment, 0);

            _lastSegment = new ObjectSpawnCellSegment(_refOBB, _extensionPlane.planeNormal, _penultimateSegment.rightAxis);
            _lastSegment.setHorizontalPadding(_horizontalPadding);
            _lastSegment.setVerticalPadding(settings.verticalPadding);
            if (_rotateAtCorners) _lastSegment.makeObjectRotation90DegreesRelativeToSegment(_penultimateSegment);

            _segments.Add(_penultimateSegment);
            _segments.Add(_lastSegment);

            SegmentsObjectSpawnSettingsProfileDbUI.instance.setEnabled(false);
            _isBuildingSegments = true;
            spawnGuide.setGuideObjectActive(false);

            applyFillMode(0);
        }

        private void onEndSegmentsBuild()
        {
            spawnObjects();

            _lastSegment        = null;
            _penultimateSegment = null;
            _segments.Clear();

            SegmentsObjectSpawnSettingsProfileDbUI.instance.setEnabled(true);
            _isBuildingSegments = false;
            spawnGuide.setGuideObjectActive(true);
        }

        private void onCancelSegmentsBuild()
        {
            _lastSegment        = null;
            _penultimateSegment = null;
            _segments.Clear();

            SegmentsObjectSpawnSettingsProfileDbUI.instance.setEnabled(true);
            _isBuildingSegments = false;
            spawnGuide.setGuideObjectActive(true);
        }

        private void updateSegments()
        {
            Vector3 extPlaneIntersectPt;
            if (!_extensionPlane.cursorRaycast(out extPlaneIntersectPt)) return;

            Vector3 toIntersectPt = extPlaneIntersectPt - _penultimateSegment.startPosition;
            if (Vector3.Dot(toIntersectPt, _penultimateSegment.extensionAxis) < 0.0f)
                _penultimateSegment.setExtensionAxis(-_penultimateSegment.extensionAxis);

            int oldLength = _penultimateSegment.snapLengthToCursor(_extensionPlane, settings.maxSegmentLength, false);

            if (settings.heightMode != SegmentsObjectSpawnHeightMode.Pattern) updateConstantOrRandomSegmentHeight(_penultimateSegment, oldLength);
            else updatePatternSegmentHeight(_penultimateSegment);

            jitterSegment(_penultimateSegment, oldLength);
            connectLastToPenultimateSegment(_penultimateSegment, _lastSegment);

            toIntersectPt = extPlaneIntersectPt - _penultimateSegment.endPosition;
            if (Vector3.Dot(toIntersectPt, _lastSegment.extensionAxis) < 0.0f)
            {
                _lastSegment.setExtensionAxis(-_lastSegment.extensionAxis);

                if (_rotateAtCorners)
                    _lastSegment.makeObjectRotation90DegreesRelativeToSegment(_penultimateSegment);

                if (_cornerConnection == SegmentsObjectSpawnCornerConnection.Gap)
                    connectLastToPenultimateSegment(_penultimateSegment, _lastSegment);
            }

            oldLength = _lastSegment.snapLengthToCursor(_extensionPlane, settings.maxSegmentLength, true);

            if (settings.heightMode != SegmentsObjectSpawnHeightMode.Pattern) updateConstantOrRandomSegmentHeight(_lastSegment, oldLength);
            else updatePatternSegmentHeight(_lastSegment);

            jitterSegment(_lastSegment, oldLength);

            applyFillMode(0);
            detectOccludedCells();
        }

        private void appendSegmentsOnLeftMouseButtonDown()
        {
            var newPenultimateSegment   = new ObjectSpawnCellSegment(_lastSegment.refObjectOBB, _extensionPlane.planeNormal, _lastSegment.extensionAxis);
            var newLastSegment          = new ObjectSpawnCellSegment(_lastSegment.refObjectOBB, _extensionPlane.planeNormal, _lastSegment.rightAxis);

            newPenultimateSegment.setHorizontalPadding(_horizontalPadding);
            newPenultimateSegment.setVerticalPadding(settings.verticalPadding);
            newLastSegment.setHorizontalPadding(_horizontalPadding);
            newLastSegment.setVerticalPadding(settings.verticalPadding);

            _lastSegment.removeLastStack(); 
            newPenultimateSegment.connectToParallelSegmentEnd(_lastSegment, _horizontalPadding);
            int oldLength = newPenultimateSegment.snapLengthToCursor(_extensionPlane, settings.maxSegmentLength, false);

            if (settings.heightMode != SegmentsObjectSpawnHeightMode.Pattern) updateConstantOrRandomSegmentHeight(newPenultimateSegment, oldLength);
            else updatePatternSegmentHeight(newPenultimateSegment);

            jitterSegment(newPenultimateSegment, oldLength);

            if (_rotateAtCorners) newLastSegment.makeObjectRotation90DegreesRelativeToSegment(newPenultimateSegment);
            connectLastToPenultimateSegment(newPenultimateSegment, newLastSegment);

            _indexOfFirstStackInPenultimateSegment += _penultimateSegment.numStacks;
            _indexOfFirstStackInPenultimateSegment += _lastSegment.numStacks;

            _penultimateSegment = newPenultimateSegment;
            _lastSegment        = newLastSegment;

            _segments.Add(newPenultimateSegment);
            _segments.Add(newLastSegment);

            applyFillMode(0);
            detectOccludedCells();
        }

        private void detectOccludedCells()
        {
            if (_segments.Count > 2)
            {
                detectOccludedCellsInSegment(_penultimateSegment, 0, _segments.Count - 3);
                detectOccludedCellsInSegment(_lastSegment, 0, _segments.Count - 2);
            }
            else
            {
                // If we have 2 segments, we still need to check for overlap between the last and penultimate segment
                // if the corner connection is set to Overlap.
                if (_cornerConnection == SegmentsObjectSpawnCornerConnection.Overlap)
                    detectOccludedCellsInSegment(_lastSegment, 0, 0);
            }
        }

        private void detectOccludedCellsInSegment(ObjectSpawnCellSegment segment, int firstSegmentIndex, int lastSegmentIndex)
        {
            // Reset occlusion state for all cells in the segment
            segment.setAllCellsOccluded(false);

            // Store segment data for easy access. This is the segment that will
            // have its cells occluded when they intersect cells in other segments.
            OBB segmentOBB = segment.obb;
            if (!segmentOBB.isValid) return;
            int numStacks = segment.numStacks;
            if (numStacks == 0) return;

            // Store the cell size along the segment's extension axis here. We will
            // need it when we perform the cell occlusion test.
            float cellSizeAlongExtAxis = segment.calcCellSizeAlongAxis(segment.extensionAxis);

            // Loop through each segment in the specified range and check for occlusions
            for (int otherSegmentIndex = firstSegmentIndex; otherSegmentIndex <= lastSegmentIndex; ++otherSegmentIndex)
            {
                // Store the other segment's OBB for easy access
                var otherSegment = _segments[otherSegmentIndex];
                OBB otherSegmentOBB = otherSegment.obb;

                // If the other segment's OBB is invalid or if the segments' OBBs don't intersect, move on to the next segment.
                if (!otherSegmentOBB.isValid) continue;
                if (!segmentOBB.intersectsOBB(otherSegmentOBB)) continue;       // Note: Broad-Phase Level 1

                // Loop through each stack in the segment
                int numOtherStacks = otherSegment.numStacks;
                for (int stackIndex = 0; stackIndex < numStacks; ++stackIndex)
                {
                    // Store stack data for easy access and move on if the stack OBB is invalid
                    var stack = segment.getStack(stackIndex);
                    OBB stackOBB = stack.obb;
                    if (!stackOBB.isValid) continue;
                    int numCells = stack.numCells;

                    // If this stack OBB doesn't intersect the segment's OBB, move on.
                    if (!stackOBB.intersectsOBB(otherSegmentOBB)) continue;     // Note: Broad-Phase Level 2

                    // Now loop through each stack in the segment we are testing against
                    for (int otherStackIndex = 0; otherStackIndex < numOtherStacks; ++otherStackIndex)
                    {
                        // Store data easy access and move on if the stack's OBB is invalid
                        var otherStack = otherSegment.getStack(otherStackIndex);
                        OBB otherStackOBB = otherStack.obb;
                        if (!otherStackOBB.isValid) continue;

                        // Note: Broad-Phase Level 3
                        //       We only proceed if the stacks occlude each other.
                        float d = (stack.startPosition - otherStack.startPosition).magnitude;
                        if (d < 1e-5f)
                        {
                            // Stack positions are close enough. Check size. If the sizes don't match, we can move on.
                            float otherCellSize = otherSegment.calcCellSizeAlongAxis(segment.extensionAxis);
                            if (Mathf.Abs(cellSizeAlongExtAxis - otherCellSize) > 1e-5f) continue;
                        }

                        // At this point we know some kind of overlap exists between the cells in the 2 stacks.
                        // Loop through each cell in the stack and check against the cells in the other stack.                 
                        int numOtherCells = otherStack.numCells;
                        for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                        {
                            // Store cell OBB and move on if not valid. We also move on if the cell is 
                            // not used for spawning.
                            var cell = stack.getCell(cellIndex);
                            if (!cell.isGoodForSpawn) continue;
                            OBB cellOBB = cell.objectOBB;
                            if (!cellOBB.isValid) continue;

                            // Loop through the other stack's cells
                            for (int otherCellIndex = 0; otherCellIndex < numOtherCells; ++otherCellIndex)
                            {
                                // Store cell OBB and move on if not valid. We also move on if the cell is
                                // not being used for spawning.
                                var otherCell = otherStack.getCell(otherCellIndex);
                                if (!otherCell.isGoodForSpawn) continue;
                                OBB otherCellOBB = otherCell.objectOBB;
                                if (!otherCellOBB.isValid) continue;
           
                                // A cell is occluded if there is a perfect overlap between the 2.
                                // First, check if the distance between the 2 cells is small enough.
                                d = (cellOBB.center - otherCellOBB.center).magnitude;
                                if (d < 1e-5f)
                                {
                                    // Now make sure the sizes match along one of the segment's extension axes
                                    float otherCellSize = otherSegment.calcCellSizeAlongAxis(segment.extensionAxis);
                                    if (Mathf.Abs(cellSizeAlongExtAxis - otherCellSize) < 1e-5f) cell.occluded = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void connectLastToPenultimateSegment(ObjectSpawnCellSegment penultimateSegment, ObjectSpawnCellSegment lastSegment)
        {
            if (_cornerConnection == SegmentsObjectSpawnCornerConnection.Normal)
                lastSegment.normalConnectAtCorner(penultimateSegment, _horizontalPadding);
            else if (_cornerConnection == SegmentsObjectSpawnCornerConnection.Overlap)
                lastSegment.overlapConnectAtCorner(penultimateSegment, _horizontalPadding);
            else if (_cornerConnection == SegmentsObjectSpawnCornerConnection.Gap)
                lastSegment.gapConnectAtCorner(penultimateSegment, _horizontalPadding);
        }

        [NonSerialized]
        private MirroredObjectList _spawnObjects_MirroredObjectList = new MirroredObjectList();
        private void spawnObjects()
        {
            skipCellsBeforeSpawn();

            _spawnObjects_MirroredObjectList.objects.Clear();
            _gameObjectBuffer.Clear();

            if (settings.projectionMode == SegmentsObjectSpawnProjectionMode.Terrains)
                PluginScene.instance.findAllTerrains(_terrainCollection);

            bool projectOnTerrains = settings.projectionMode == SegmentsObjectSpawnProjectionMode.Terrains &&
                (_terrainCollection.unityTerrains.Count != 0 || _terrainCollection.terrainMeshes.Count != 0);

            float obbInflateAmount  = getOverlapCheckOBBInflateAmount();
            var overlapConfig       = getOverlapCheckConfig();
            prepareSceneObjectOverlapFilter();

            foreach (var segment in _segments)
            {
                int numStacks = segment.numStacks;
                for (int stackIndex = 0; stackIndex < numStacks; ++stackIndex)
                {
                    ObjectSpawnCellStack stack = segment.getStack(stackIndex);
                    int numCells = stack.numCells;
                    for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                    {
                        ObjectSpawnCell cell = stack.getCell(cellIndex);
                        if (!cell.isGoodForSpawn) continue;

                        // Note: Useful for checking for overlaps in case we are using jittering and
                        //       objects are pushed over each other.
                        if (settings.avoidOverlaps &&
                            settings.jitterMode != SegmentsObjectSpawnJitterMode.None)
                        {
                            OBB cellOBB = cell.objectOBB;
                            cellOBB.inflate(obbInflateAmount);
                            if (PluginScene.instance.overlapBox(cellOBB, _overlapFilter, overlapConfig))
                                continue;
                        }

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
                GameObject go = gameObjects[i];
                OBB obb = ObjectBounds.calcHierarchyWorldOBB(go, boundsQConfig);

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
            if (settings.prefabPickMode == SegmentsObjectSpawnPrefabPickMode.SpawnGuide)
            {
                Vector3 objectPos = ObjectPositionCalculator.calcRootPosition(spawnGuide.gameObject,
                    _refOBB, cell.objectOBBCenter, spawnGuide.lossyScale, cell.objectOBBRotation);
                return spawnGuide.spawn(objectPos, cell.objectOBBRotation, spawnGuide.lossyScale);
            }
            else
            if (settings.prefabPickMode == SegmentsObjectSpawnPrefabPickMode.Random)
            {
                var pool            = settings.randomPrefabProfile;
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
            if (settings.prefabPickMode == SegmentsObjectSpawnPrefabPickMode.HeightRange)
            {
                var pool                = settings.heightRangePrefabProfile;
                IntRangePrefab prefab   = pool.pickPrefab(stack.cellIndexToHeight(cellIndex));
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

        private float getOverlapCheckOBBInflateAmount()
        {
            return -1e-1f;
        }

        private void skipCellsBeforeSpawn()
        {
            float obbInflateAmount  = getOverlapCheckOBBInflateAmount();
            var overlapConfig       = getOverlapCheckConfig();
            prepareSceneObjectOverlapFilter();

            foreach (var segment in _segments)
            {
                int numStacks = segment.numStacks;
                for (int stackIndex = 0; stackIndex < numStacks; ++stackIndex)
                {
                    bool checkSceneOverlaps     = settings.avoidOverlaps;
                    ObjectSpawnCellStack stack  = segment.getStack(stackIndex);
                    if (checkSceneOverlaps)
                    {
                        OBB stackOBB = stack.obb;
                        stackOBB.inflate(obbInflateAmount);
                        if (stackOBB.isValid && !PluginScene.instance.overlapBox(stackOBB, _overlapFilter, overlapConfig))
                            checkSceneOverlaps = false;
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

                        if (checkSceneOverlaps)
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

        private void updateConstantOrRandomSegmentHeight(ObjectSpawnCellSegment segment, int firstStackIndex)
        {
            if (firstStackIndex < segment.numStacks)
            {
                if (settings.heightMode == SegmentsObjectSpawnHeightMode.Constant) segment.setHeight(_currentHeight, firstStackIndex);
                else if (settings.heightMode == SegmentsObjectSpawnHeightMode.Random)
                {
                    generateRandomHeightValues(segment.numStacks - firstStackIndex, _randomHeightValues);
                    segment.setHeight(_randomHeightValues, _currentHeight, firstStackIndex);
                }
                else if (settings.heightMode == SegmentsObjectSpawnHeightMode.Pattern)
                {
                    int heightValueIndex = _indexOfFirstStackInPenultimateSegment;
                    if (segment == _lastSegment) heightValueIndex += _penultimateSegment.numStacks;

                    int numStacks = segment.numStacks;
                    for (int stackIndex = 0; stackIndex < numStacks; ++stackIndex)
                    {
                        int heightValue = _currentHeight + _heightPatternSampler.sample(_heightPattern, heightValueIndex++);
                        segment.setStackHeight(heightValue, stackIndex);
                    }
                }
            }
        }

        private void updatePatternSegmentHeight(ObjectSpawnCellSegment segment)
        {
            int heightValueIndex = 0;
            if (segment == _penultimateSegment) heightValueIndex = _indexOfFirstStackInPenultimateSegment;
            else if (segment == _lastSegment) heightValueIndex = _indexOfFirstStackInPenultimateSegment + _penultimateSegment.numStacks;
            else
            {
                foreach (var seg in _segments)
                {
                    if (seg == segment) break;
                    heightValueIndex += seg.numStacks;
                }
            }

            int numStacks = segment.numStacks;
            for (int stackIndex = 0; stackIndex < numStacks; ++stackIndex)
            {
                int heightValue = _currentHeight + _heightPatternSampler.sample(_heightPattern, heightValueIndex++);
                segment.setStackHeight(heightValue, stackIndex);
            }
        }

        private void generateRandomHeightValues(int numValues, List<int> heightValues)
        {
            heightValues.Clear();
            if (settings.heightMode == SegmentsObjectSpawnHeightMode.Random)
            {
                for (int i = 0; i < numValues; ++i)
                    heightValues.Add(UnityEngine.Random.Range(settings.minRandomHeight, settings.maxRandomHeight + 1));
            }
        }

        private void applyFillMode(int firstSegmentIndex)
        {
            if (settings.fillMode == SegmentsObjectSpawnFillMode.Solid) return;

            int numSegments = _segments.Count;
            if (settings.fillMode == SegmentsObjectSpawnFillMode.Border)
            {
                // Handle top and bottom borders
                for (int segmentIndex = firstSegmentIndex; segmentIndex < numSegments; ++segmentIndex)
                {
                    var segment = _segments[segmentIndex];
                    segment.setAllCellsOutOfScope(false);

                    int numStacks = segment.numStacks;
                    for (int stackIndex = 0; stackIndex < numStacks; ++stackIndex)
                    {
                        var stack = segment.getStack(stackIndex);
                        bool stackScopeRes = false;

                        if (firstSegmentIndex == 0 && segmentIndex == 0)
                        {
                            if (stackIndex <= (settings.beginBorderWidth - 1)) stackScopeRes |= true;
                        }
                        else
                        if (segmentIndex == numSegments - 1)
                        {
                            if (stackIndex >= (numStacks - settings.endBorderWidth)) stackScopeRes |= true;
                        }
                        else
                        {
                            // Check if we are dealing with a dead-end
                            if (numSegments > 2 && segmentIndex % 2 == 0 && segment.numStacks > 1)
                            {
                                var prevSegment = _segments[segmentIndex - 1];
                                if (Vector3.Dot(prevSegment.extensionAxis, segment.extensionAxis) < 0.0f)
                                {
                                    if (stackIndex <= (settings.segmentCapBorderWidth - 1)) stackScopeRes |= true;
                                }
                            }
                        }

                        int numCells = stack.numCells;
                        for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                        {
                            bool cellScopRes = cellIndex <= (settings.bottomBorderWidth - 1);
                            cellScopRes |= cellIndex >= (numCells - settings.topBorderWidth);
                            cellScopRes |= stackScopeRes;

                            if (!cellScopRes) stack.getCell(cellIndex).outOfScope = true;
                        }
                    }
                }
            }
        }

        private void jitterSegment(ObjectSpawnCellSegment segment, int firstStackIndex)
        {
            if (settings.jitterMode == SegmentsObjectSpawnJitterMode.None || firstStackIndex >= segment.numStacks) return;

            Vector3 jitterAxis = segment.rightAxis;
            if (UnityEngine.Random.Range(0.0f, 1.0f) >= 0.5f) jitterAxis = -jitterAxis;

            if (settings.jitterMode == SegmentsObjectSpawnJitterMode.All)
            {
                int numStacks = segment.numStacks;
                for (int stackIndex = firstStackIndex; stackIndex < numStacks; ++stackIndex)
                {
                    var stack = segment.getStack(stackIndex);
                    int numCells = stack.numCells;
                    float cellSizeAlongHeightAxis = stack.calcCellSizeAlongAxis(stack.heightAxis);
                    for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                    {
                        float jitterAmount = UnityEngine.Random.Range(settings.minJitter, settings.maxJitter);
                        var cell = stack.getCell(cellIndex);
                        cell.setObjectOBBCenter(stack.calcCellPosition(cellIndex, cellSizeAlongHeightAxis) + jitterAxis * jitterAmount);
                    }
                }
            }
            else
            if (settings.jitterMode == SegmentsObjectSpawnJitterMode.HeightRange)
            {
                int numStacks       = segment.numStacks;
                for (int stackIndex = firstStackIndex; stackIndex < numStacks; ++stackIndex)
                {
                    var stack       = segment.getStack(stackIndex);
                    int numCells    = stack.numCells;
                    float cellSizeAlongHeightAxis = stack.calcCellSizeAlongAxis(stack.heightAxis);
                    for (int cellIndex = 0; cellIndex < numCells; ++cellIndex)
                    {
                        int cellHeight = stack.cellIndexToHeight(cellIndex);
                        if (cellHeight >= settings.minJitterHeight && cellHeight <= settings.maxJitterHeight)
                        {
                            float jitterAmount = UnityEngine.Random.Range(settings.minJitter, settings.maxJitter);
                            var cell = stack.getCell(cellIndex);
                            cell.setObjectOBBCenter(stack.calcCellPosition(cellIndex, cellSizeAlongHeightAxis) + jitterAxis * jitterAmount);
                        }
                    }
                }
            }
        }

        private void updateExtensionPlane()
        {
            _extensionPlane.set(calcSpawnGuideWorldOBB(), _extensionPlane.refOBBFace,
                ObjectSpawnPrefs.instance.segmentsSpawnExtensionPlaneInflateAmount);
        }

        private OBB calcRefOBB()
        {
            OBB refOBB = calcSpawnGuideWorldOBB();
            if (settings.cellMode == SegmentsObjectSpawnCellMode.Grid)
            {
                if (settings.useSceneGridCellSize) refOBB.size = PluginScene.instance.grid.activeSettings.cellSize;
                else refOBB.size = settings.gridCellSize;
            }
            else
            {
                if (refOBB.size.magnitude < 1e-5f) refOBB = new OBB(spawnGuide.position, Vector3Ex.create(settings.volumlessObjectSize), spawnGuide.rotation);
            }

            return refOBB;
        }

        private void onUndoRedo()
        {
            onCancelSegmentsBuild();
        }
    }
}
#endif