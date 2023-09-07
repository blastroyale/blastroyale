#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectExtrudeGizmo : PluginGizmo
    {
        public delegate void    VerticalAxisExtrudeSpawnHandler             (ObjectExtrudeGizmo extrudeGizmo, List<GameObject> spawnedParents);
        public delegate void    ExtrudeSpawnHandler                         (ObjectExtrudeGizmo extrudeGizmo, List<GameObject> spawnedParents);

        public event            VerticalAxisExtrudeSpawnHandler             verticalAxisExtrudeSpawn;
        public event            ExtrudeSpawnHandler                         extrudeSpawn;

        private class Slider
        {
            public Vector3  position;
            public Vector3  direction;
            public int      controlId;
            public bool     visible;
        }

        private class SglAxisSlider : Slider
        {
            public enum Id
            {
                Right = 0,
                Left,
                Top,
                Bottom,
                Forward,
                Back
            }

            public float    dragSign;
            public Id       sliderId;
            public int      axis;

            public bool     isXAxis             { get { return axis == 0; } }
            public bool     isYAxis             { get { return axis == 1; } }
            public bool     isZAxis             { get { return axis == 2; } }
            public Vector3  dragDirection       { get { return direction * dragSign; } }
        }

        private class DblAxisSlider : Slider
        {
            public enum Id
            {
                RightForward = 0,
                RightBack,
                LeftBack,
                LeftForward,
                TopRight,
                BottomRight,
                BottomLeft,
                TopLeft,
                ForwardTop,
                ForwardBottom,
                BackBottom,
                BackTop
            }

            public float    dragSign0;
            public float    dragSign1;
            public int      axis0;
            public int      axis1;
            public Id       sliderId;

            public bool     isXZAxis { get { return axis0 == 0 && axis1 == 2; } }
            public bool     isYXAxis { get { return axis0 == 1 && axis1 == 0; } }
            public bool     isZYAxis { get { return axis0 == 2 && axis1 == 1; } }
        }

        private static readonly float               _minBoxSize = 1e-6f;
        private Vector3                             _dragStartPosition;

        private SglAxisSlider[]                     _sglAxisSliders             = new SglAxisSlider[6];
        private DblAxisSlider[]                     _dblAxisSliders             = new DblAxisSlider[12];
        private SglAxisSlider                       _hoveredSglAxisSlider       = null;
        private DblAxisSlider                       _hoveredDblAxisSlider       = null;

        private ObjectGrid3D                        _grid                       = new ObjectGrid3D();
        private Vector3Int                          _targetObjectsCellCoords    = Vector3Int.zero;
        private bool                                _spawnOnRepaint;

        private IEnumerable<GameObject>             _targetObjects;
        private List<GameObject>                    _targetParents              = new List<GameObject>();
        private List<GameObject>                    _spawnedParents             = new List<GameObject>();

        private Dictionary<GameObject, Vector3>     _spawnAnchorMap             = new Dictionary<GameObject, Vector3>();
        [NonSerialized]
        private ObjectBounds.QueryConfig            _targetBoundsQConfig        = new ObjectBounds.QueryConfig()
        {
            objectTypes = GameObjectType.Sprite | GameObjectType.Mesh | GameObjectType.Terrain,
            includeInactive = false,
            includeInvisible = false,
            volumelessSize = Vector3.zero
        };
        [NonSerialized]
        private ObjectBounds.QueryConfig            _overlapBoundsQConfig       = new ObjectBounds.QueryConfig()
        {
            objectTypes = GameObjectType.Sprite | GameObjectType.Mesh | GameObjectType.Terrain,
            includeInactive = false,
            includeInvisible = false,
            volumelessSize = Vector3.zero
        };
        [NonSerialized]
        private ObjectOverlapFilter                 _overlapFilter              = new ObjectOverlapFilter();
        [NonSerialized]
        private TerrainObjectOverlapFilter          _terrainOverlapFilter       = new TerrainObjectOverlapFilter();

        [SerializeField]
        private Vector3                             _boxSize = Vector3.zero;
        [SerializeField]
        private ObjectExtrudeSpace                  _lastExtrudeSpace;
        [NonSerialized]
        private ObjectExtrudeGizmoSettings          _sharedSettings;
        [SerializeField]
        private ObjectProjectionSettings            _terrainProjectionSettings;

        [NonSerialized]
        private List<GameObject>                    _verticalAxisObjectsBuffer  = new List<GameObject>();
        [NonSerialized]
        private TerrainCollection                   _terrains                   = new TerrainCollection();

        private ObjectProjectionSettings            terrainProjectionSettings
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
                    _terrainProjectionSettings.projectAsUnit    = false;
                    _terrainProjectionSettings.alignAxis        = false;
                    UndoEx.restoreEnabledState();
                }
                return _terrainProjectionSettings;
            }
        }

        public Vector3                              boxCenter                   { get { return position; } }
        public Quaternion                           boxRotation                 { get { return rotation; } }
        public Vector3                              boxSize                     { get { return _boxSize; } }
        public Vector3                              boxExtents                  { get { return _boxSize * 0.5f; } }
        public Vector3Int                           numExtrudeCells             { get { return extruding ? _grid.numCells : Vector3Int.zero; } }
        public bool                                 extruding                   { get { return _hoveredSglAxisSlider != null || _hoveredDblAxisSlider != null; } }
        public ObjectExtrudeGizmoSettings           sharedSettings              { get { return _sharedSettings; } set { _sharedSettings = value; } }

        public ObjectExtrudeGizmo()
        {
            for (int index = 0; index < _sglAxisSliders.Length; ++index)
                _sglAxisSliders[index] = new SglAxisSlider();

            for (int index = 0; index < _dblAxisSliders.Length; ++index)
                _dblAxisSliders[index] = new DblAxisSlider();

            int axis            = 0;
            var sglSliderIds    = Enum.GetValues(typeof(SglAxisSlider.Id));
            foreach(var sliderId in sglSliderIds)
            {
                _sglAxisSliders[(int)sliderId].axis         = axis / 2;
                _sglAxisSliders[(int)sliderId].sliderId     = (SglAxisSlider.Id)sliderId;
                ++axis;
            }

            var dblSliderIds    = Enum.GetValues(typeof(DblAxisSlider.Id));
            int[] axesPairs     = new int[] {0, 2, 0, 2, 0, 2, 0, 2, 1, 0, 1, 0, 1, 0, 1, 0, 2, 1, 2, 1, 2, 1, 2, 1};
            int sliderIndex     = 0;
            foreach(var sliderId in dblSliderIds)
            {
                var slider      = _dblAxisSliders[(int)sliderId];
                slider.sliderId = (DblAxisSlider.Id)sliderId;
                slider.axis0    = axesPairs[sliderIndex * 2];
                slider.axis1    = axesPairs[sliderIndex * 2 + 1];

                ++sliderIndex;
            }

            _grid.isCellMasked          = isGridCellMasked;
            _overlapFilter.objectTypes  = GameObjectType.Mesh | GameObjectType.Sprite;
            _overlapFilter.customFilter = (GameObject go) => { return !go.isTerrainMesh() && !go.isSphericalMesh(); };
        }

        public void getTargetParents(List<GameObject> targetParents)
        {
            targetParents.Clear();
            GameObjectEx.getParents(_targetObjects, targetParents);
        }

        public void getExtrudeCellsOBBs(List<OBB> obbs)
        {
            obbs.Clear();
            if (!_grid.hasCells) return;

            for (int x = 0; x < _grid.numCellsX; ++x)
            {
                for (int y = 0; y < _grid.numCellsY; ++y)
                {
                    for (int z = 0; z < _grid.numCellsZ; ++z)
                    {
                        obbs.Add(_grid.calcCellOBB(x, y, z));
                    }
                }
            }
        }

        public void bindTargetObjects(IEnumerable<GameObject> targetObjects)
        {
            _lastExtrudeSpace = sharedSettings.extrudeSpace;
            _targetObjects = targetObjects;
            fitToTargetObjects();
        }

        public void onTargetObjectsUpdated()
        {
            fitToTargetObjects();
        }

        public void onTargetObjectTransformsChanged()
        {
            fitToTargetObjects();
        }

        public void projectObjectsBasedOnProjectionMode(List<GameObject> gameObjects)
        {
            if (sharedSettings.projectionMode == ObjectExtrudeGizmoProjectionMode.Terrains)
            {
                // Note: Record transform because the original is also included and we need to
                //       be able to undo/redo its transform change.
                UndoEx.recordGameObjectTransforms(gameObjects);
                OBB obb = ObjectBounds.calcHierarchiesWorldOBB(gameObjects, _targetBoundsQConfig);
                PluginScene.instance.terrainsOverlapBox(obb, _terrainOverlapFilter, TerrainObjectOverlapConfig.defaultConfig, _terrains);
                ObjectProjection.projectHierarchiesOnTerrainsAsUnit(gameObjects, _terrains, terrainProjectionSettings);
            }
        }

        protected override void doOnSceneGUI()
        {
            if (_lastExtrudeSpace != sharedSettings.extrudeSpace)
            {
                fitToTargetObjects();
                _lastExtrudeSpace = sharedSettings.extrudeSpace;
            }
            if (_boxSize.magnitude == 0.0f) return;

            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                if (_spawnOnRepaint)
                {
                    spawn();
                    _spawnOnRepaint = false;
                }
            }
            else
            if (e.type == EventType.MouseDown)
            {
                _dragStartPosition = position;

                if (e.button == (int)MouseButton.LeftMouse)
                    refreshSpawnAnchors();
            }
            else
            if (e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow)
            {
                _dragStartPosition = position;
                if (e.button == (int)MouseButton.LeftMouse && extruding)
                {
                    _hoveredSglAxisSlider = null;
                    _hoveredDblAxisSlider = null;
                    _spawnOnRepaint = true;
                }
            }

            updateSglAxisSliders();
            updateDblAxisSliders();

            drawWireBox();
            drawSglAxisSliders();
            drawDblAxisSliders();

            if (extruding)
            {
                updateSpawnGrid();
                drawSpawnGrid();
            }

            drawUIHandles();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_terrainProjectionSettings);
        }

        private void fitToTargetObjects()
        {
            GameObjectEx.getParents(_targetObjects, _targetParents);
            if (sharedSettings.extrudeSpace == ObjectExtrudeSpace.Global)
            {
                AABB worldAABB = ObjectBounds.calcHierarchiesWorldAABB(_targetParents, _targetBoundsQConfig);
                setAABB(worldAABB);
            }
            else
            {
                OBB worldOBB = ObjectBounds.calcHierarchiesWorldOBB(_targetParents, _targetBoundsQConfig);
                setOBB(worldOBB);
            }
        }

        private void refreshSpawnAnchors()
        {
            _spawnAnchorMap.Clear();
            foreach (var parent in _targetParents)
                _spawnAnchorMap.Add(parent, parent.transform.position - _dragStartPosition);
        }

        [NonSerialized]
        private List<GameObject> _overlappedObjectsBuffer = new List<GameObject>();
        private void spawn()
        {
            if (sharedSettings.avoidOverlaps)
            {
                _overlapFilter.clearIgnoredObjects();
                _overlapFilter.addIgnoredObjects(_targetObjects);
            }

            var sceneGrid = PluginScene.instance.grid;

            _spawnedParents.Clear();
            for (int cellX = 0; cellX < _grid.numCellsX; ++cellX)
            {
                for (int cellZ = 0; cellZ < _grid.numCellsZ; ++cellZ)
                {
                    _verticalAxisObjectsBuffer.Clear();

                    // Note: Traverse the Y axis last in order to gather columns of spawned
                    //       objects in a single buffer for terrain projection.
                    for (int cellY = 0; cellY < _grid.numCellsY; ++cellY)
                    {
                        Vector3 cellPosition = _grid.calcCellPosition(cellX, cellY, cellZ);

                        // Note: If this is the cell in which the target objects reside,
                        //       add it to the buffer to allow the to be projected later.
                        if (sharedSettings.projectionMode != ObjectExtrudeGizmoProjectionMode.None &&
                            _targetObjectsCellCoords.matchCoords(cellX, cellY, cellZ)) _verticalAxisObjectsBuffer.AddRange(_targetParents);

                        if (isGridCellMasked(cellX, cellY, cellZ, cellPosition)) continue;

                        foreach (var targetParent in _targetParents)
                        {
                            var spawnedParent = UnityEditorCommands.duplicate(targetParent);
                            if (spawnedParent != null)
                            {
                                spawnedParent.transform.position = cellPosition + _spawnAnchorMap[targetParent];
                                OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(spawnedParent, _overlapBoundsQConfig);

                                if (sharedSettings.avoidOverlaps)
                                {
                                    OBB overlapOBB = hierarchyWorldOBB;
                                    if (overlapOBB.isValid)
                                    {
                                        overlapOBB.inflate(-1e-1f);
                                        if (PluginScene.instance.overlapBox(overlapOBB, _overlapFilter, ObjectOverlapConfig.defaultConfig, _overlappedObjectsBuffer))
                                        {
                                            bool foundOverlap = false;
                                            foreach (var overlapped in _overlappedObjectsBuffer)
                                            {
                                                if (overlapped == spawnedParent || overlapped.transform.IsChildOf(spawnedParent.transform)) continue;

                                                if (overlapped.getMesh() != null)
                                                {
                                                    if (spawnedParent.meshHierarchyIntersectsMeshTriangles(overlapped, -1e-1f))
                                                    {
                                                        foundOverlap = true;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    foundOverlap = true;
                                                    break;
                                                }
                                            }

                                            if (foundOverlap)
                                                GameObject.DestroyImmediate(spawnedParent);
                                        }                                           
                                    }
                                }

                                if (spawnedParent != null)
                                {
                                    _spawnedParents.Add(spawnedParent);
                                    _verticalAxisObjectsBuffer.Add(spawnedParent);
                                }
                            }
                        }
                    }

                    if (_verticalAxisObjectsBuffer.Count != 0)
                    {
                        projectObjectsBasedOnProjectionMode(_verticalAxisObjectsBuffer);

                        if (verticalAxisExtrudeSpawn != null)
                            verticalAxisExtrudeSpawn(this, _verticalAxisObjectsBuffer);
                    }
                }
            }

            if (_spawnedParents.Count != 0 && extrudeSpawn != null) extrudeSpawn(this, _spawnedParents);
            _spawnedParents.Clear();
        }

        private void updateSglAxisSliders()
        {
            Vector3 right       = boxRotation * Vector3.right;
            Vector3 up          = boxRotation * Vector3.up;
            Vector3 look        = boxRotation * Vector3.forward;

            var slider          = _sglAxisSliders[(int)SglAxisSlider.Id.Left];
            slider.direction    = -right;
            slider.position     = boxCenter + slider.direction * boxExtents.x;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > _minBoxSize;

            slider              = _sglAxisSliders[(int)SglAxisSlider.Id.Right];
            slider.direction    = right;
            slider.position     = boxCenter + slider.direction * boxExtents.x;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > _minBoxSize;

            slider              = _sglAxisSliders[(int)SglAxisSlider.Id.Top];
            slider.direction    = up;
            slider.position     = boxCenter + slider.direction * boxExtents.y;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.y > _minBoxSize;

            slider              = _sglAxisSliders[(int)SglAxisSlider.Id.Bottom];
            slider.direction    = -up;
            slider.position     = boxCenter + slider.direction * boxExtents.y;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.y > _minBoxSize;

            slider              = _sglAxisSliders[(int)SglAxisSlider.Id.Forward];
            slider.direction    = look;
            slider.position     = boxCenter + slider.direction * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.z > _minBoxSize;

            slider              = _sglAxisSliders[(int)SglAxisSlider.Id.Back];
            slider.direction    = -look;
            slider.position     = boxCenter + slider.direction * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.z > _minBoxSize;
        }

        private void updateDblAxisSliders()
        {
            Vector3 right       = boxRotation * Vector3.right;
            Vector3 up          = boxRotation * Vector3.up;
            Vector3 look        = boxRotation * Vector3.forward;

            var slider          = _dblAxisSliders[(int)DblAxisSlider.Id.RightForward];
            slider.direction    = (right + look).normalized;
            slider.position     = boxCenter + right * boxExtents.x + look * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > _minBoxSize && boxSize.z > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.RightBack];
            slider.direction    = (right - look).normalized;
            slider.position     = boxCenter + right * boxExtents.x - look * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > _minBoxSize && boxSize.z > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.LeftBack];
            slider.direction    = (-right - look).normalized;
            slider.position     = boxCenter - right * boxExtents.x - look * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > _minBoxSize && boxSize.z > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.LeftForward];
            slider.direction    = (-right + look).normalized;
            slider.position     = boxCenter - right * boxExtents.x + look * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > 0.0f && boxSize.z > 0.0f;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.TopLeft];
            slider.direction    = (up - right).normalized;
            slider.position     = boxCenter + up * boxExtents.y - right * boxExtents.x;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > _minBoxSize && boxSize.y > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.TopRight];
            slider.direction    = (up + right).normalized;
            slider.position     = boxCenter + up * boxExtents.y + right * boxExtents.x;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > _minBoxSize && boxSize.y > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.BottomRight];
            slider.direction    = (-up + right).normalized;
            slider.position     = boxCenter - up * boxExtents.y + right * boxExtents.x;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > _minBoxSize && boxSize.y > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.BottomLeft];
            slider.direction    = (-up - right).normalized;
            slider.position     = boxCenter - up * boxExtents.y - right * boxExtents.x;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.x > _minBoxSize && boxSize.y > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.ForwardTop];
            slider.direction    = (up + look).normalized;
            slider.position     = boxCenter + up * boxExtents.y + look * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.y > _minBoxSize && boxSize.z > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.ForwardBottom];
            slider.direction    = (-up + look).normalized;
            slider.position     = boxCenter - up * boxExtents.y + look * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.y > _minBoxSize && boxSize.z > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.BackBottom];
            slider.direction    = (-up - look).normalized;
            slider.position     = boxCenter - up * boxExtents.y - look * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.y > _minBoxSize && boxSize.z > _minBoxSize;

            slider              = _dblAxisSliders[(int)DblAxisSlider.Id.BackTop];
            slider.direction    = (up - look).normalized;
            slider.position     = boxCenter + up * boxExtents.y - look * boxExtents.z;
            slider.controlId    = GUIUtility.GetControlID(FocusType.Passive);
            slider.visible      = boxSize.y > _minBoxSize && boxSize.z > _minBoxSize;
        }

        private void updateSpawnGrid()
        {
            _grid.origin            = _dragStartPosition;
            _grid.cellSize          = _boxSize;
            _grid.cellRotation      = boxRotation;
            _grid.right             = boxRotation * Vector3.right;
            _grid.up                = boxRotation * Vector3.up;
            _grid.look              = boxRotation * Vector3.forward;
            _grid.padding           = sharedSettings.padding;

            Vector3 toPosition      = position - _dragStartPosition;
            _grid.numCells          = Vector3Int.one;

            if (_boxSize[0] > 0.0f) _grid.numCellsX = 1 + Mathf.RoundToInt(toPosition.absDot(boxRotation * Vector3.right) / calcDragSize(0));
            if (_boxSize[1] > 0.0f) _grid.numCellsY = 1 + Mathf.RoundToInt(toPosition.absDot(boxRotation * Vector3.up) / calcDragSize(1));
            if (_boxSize[2] > 0.0f) _grid.numCellsZ = 1 + Mathf.RoundToInt(toPosition.absDot(boxRotation * Vector3.forward) / calcDragSize(2));

            if (_hoveredSglAxisSlider != null)
            {
                if (_hoveredSglAxisSlider.isXAxis)
                {
                    _grid.right = _hoveredSglAxisSlider.dragDirection;
                    _targetObjectsCellCoords = new Vector3Int(0, 0, 0);
                }
                else if (_hoveredSglAxisSlider.isYAxis)
                {
                    _grid.up = _hoveredSglAxisSlider.dragDirection;
                    _targetObjectsCellCoords = new Vector3Int(0, 0, 0);
                }
                else if (_hoveredSglAxisSlider.isZAxis)
                {
                    _grid.look = _hoveredSglAxisSlider.dragDirection;
                    _targetObjectsCellCoords = new Vector3Int(0, 0, 0);
                }
            }
            else
            if (_hoveredDblAxisSlider != null)
            {
                _targetObjectsCellCoords = Vector3Int.zero;
                if (_hoveredDblAxisSlider.isXZAxis)
                {
                    _grid.right = rotation * Vector3.right * _hoveredDblAxisSlider.dragSign0;
                    _grid.look  = rotation * Vector3.forward * _hoveredDblAxisSlider.dragSign1;

                    _targetObjectsCellCoords.x = 0;
                    _targetObjectsCellCoords.z = 0;
                }
                else
                if (_hoveredDblAxisSlider.isYXAxis)
                {
                    _grid.up    = rotation * Vector3.up * _hoveredDblAxisSlider.dragSign0;
                    _grid.right = rotation * Vector3.right * _hoveredDblAxisSlider.dragSign1;

                    _targetObjectsCellCoords.y = 0;
                    _targetObjectsCellCoords.x = 0;
                }
                else
                if (_hoveredDblAxisSlider.isZYAxis)
                {
                    _grid.look  = rotation * Vector3.forward * _hoveredDblAxisSlider.dragSign0;
                    _grid.up    = rotation * Vector3.up * _hoveredDblAxisSlider.dragSign1;

                    _targetObjectsCellCoords.z = 0;
                    _targetObjectsCellCoords.y = 0;
                }
            }
        }

        private void drawSpawnGrid()
        {
            _grid.cellFillColor = GizmoPrefs.instance.extrudeCellFillColor;
            _grid.cellWireColor = GizmoPrefs.instance.extrudeCellWireColor;
            _grid.draw();
        }

        private void drawWireBox()
        {
            HandlesEx.saveMatrix();
            HandlesEx.saveColor();

            Handles.matrix      = Matrix4x4.TRS(position, rotation, _boxSize);
            Handles.color       = GizmoPrefs.instance.extrudeWireColor;
            //Handles.DrawWireCube(Vector3.zero, Vector3.one);
            HandlesEx.drawUnitWireCube();

            HandlesEx.restoreColor();
            HandlesEx.restoreMatrix();
        }

        private void drawSglAxisSliders()
        {
            HandlesEx.saveColor();
            HandlesEx.saveMatrix();

            Vector3 padding         = sharedSettings.padding;
            _hoveredSglAxisSlider   = null;
            foreach (var slider in _sglAxisSliders)
            {
                if (!slider.visible) continue;

                Color sliderColor                       = GizmoPrefs.instance.extrudeXAxisColor;
                if (slider.isYAxis) sliderColor         = GizmoPrefs.instance.extrudeYAxisColor;
                else if (slider.isZAxis) sliderColor    = GizmoPrefs.instance.extrudeZAxisColor;

                Handles.color = sliderColor;
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.Slider(slider.controlId, slider.position, slider.direction, getSglAxisHandleSize(), Handles.ConeHandleCap, 0.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    slider.dragSign = Mathf.Sign(Vector3.Dot(slider.direction, (newPos - _dragStartPosition)));

                    float t;
                    Ray ray = PluginCamera.camera.getCursorRay();
                    Plane dragPlane = PlaneEx.createPlaneWithMostAlignedNormal(slider.axis, rotation, PluginCamera.camera.transform.forward, slider.position);
                    if (dragPlane.Raycast(ray, out t))
                    {
                        Vector3 intersectPt = ray.GetPoint(t);
                        Vector3 direction   = (intersectPt - position);
                        Vector3 moveVector  = Vector3.zero;

                        float dot = Vector3.Dot(direction, slider.direction);
                        float dragSize = calcDragSize(slider.axis);
                        if (Mathf.Abs(dot) >= dragSize)
                        {
                            int numCells = Mathf.RoundToInt(Mathf.Abs(dot) / dragSize);
                            moveVector += slider.direction * dragSize * numCells * Mathf.Sign(dot);
                        }
    
                        if (moveVector.magnitude > 0.0f)
                        {
                            UndoEx.record(this);
                            position += moveVector;
                            //moveTargets(moveVector);
                        }
                    }
                }
            }

            HandlesEx.restoreMatrix();
            HandlesEx.restoreColor();

            foreach (var slider in _sglAxisSliders)
            {
                if (slider.controlId == GUIUtility.hotControl)
                {
                    _hoveredSglAxisSlider = slider;
                    break;
                }    
            }
        }

        private void drawDblAxisSliders()
        {
            HandlesEx.saveColor();

            _hoveredDblAxisSlider = null;
            foreach (var slider in _dblAxisSliders)
            {
                if (!slider.visible) continue;

                if (slider.isZYAxis) Handles.color      = GizmoPrefs.instance.extrudeXAxisColor;
                else if (slider.isYXAxis) Handles.color = GizmoPrefs.instance.extrudeZAxisColor;
                else Handles.color                      = GizmoPrefs.instance.extrudeYAxisColor;

                EditorGUI.BeginChangeCheck();
                Handles.Slider(slider.controlId, slider.position, slider.direction, getDblAxisHandleSize(), Handles.DotHandleCap, 0.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 axis0       = Vector3Ex.createAxis(slider.axis0, rotation);
                    Vector3 axis1       = Vector3Ex.createAxis(slider.axis1, rotation);

                    slider.dragSign0    = Mathf.Sign(Vector3.Dot((position - _dragStartPosition), axis0));
                    slider.dragSign1    = Mathf.Sign(Vector3.Dot((position - _dragStartPosition), axis1));

                    float t;
                    Ray ray = PluginCamera.camera.getCursorRay();
                    Plane dragPlane = new Plane(Vector3.Cross(axis0, axis1).normalized, _dragStartPosition);
                    if (dragPlane.Raycast(ray, out t))
                    {
                        Vector3 intersectPt = ray.GetPoint(t);
                        Vector3 direction   = (intersectPt - position);
                        Vector3 moveVector  = Vector3.zero;

                        float dot0 = Vector3.Dot(direction, axis0);
                        float dragSize0 = calcDragSize(slider.axis0);
                        if (Mathf.Abs(dot0) >= dragSize0)
                        {
                            int numCells = Mathf.RoundToInt(Mathf.Abs(dot0) / dragSize0);
                            moveVector += axis0 * dragSize0 * numCells * Mathf.Sign(dot0);
                        }

                        float dot1 = Vector3.Dot(direction, axis1);
                        float dragSize1 = calcDragSize(slider.axis1);
                        if (Mathf.Abs(dot1) >= _boxSize[slider.axis1])
                        {
                            int numCells = Mathf.RoundToInt(Mathf.Abs(dot1) / dragSize1);
                            moveVector += axis1 * dragSize1 * numCells * Mathf.Sign(dot1);
                        }

                        if (moveVector.magnitude > 0.0f)
                        {
                            UndoEx.record(this);
                            position += moveVector;
                            //moveTargets(moveVector);
                        }
                    }
                }
            }

            HandlesEx.restoreColor();

            foreach (var slider in _dblAxisSliders)
            {
                if (slider.controlId == GUIUtility.hotControl)
                {
                    _hoveredDblAxisSlider = slider;
                    break;
                }
            }
        }

        private float calcDragSize(int axis)
        {
            return _boxSize[axis] + sharedSettings.padding[axis];
        }

        private void drawUIHandles()
        {
            if (_grid.hasCells && extruding)
            {
                if (GizmoPrefs.instance.extrudeShowInfoText)
                {
                    Handles.BeginGUI();
                    Vector3 labelPos = _grid.calcCellPosition(0, 0, 0);
                    labelPos -= _grid.right * _grid.cellSize.x * 0.5f;
                    labelPos -= _grid.up * _grid.cellSize.y * 0.5f;
                    labelPos -= _grid.look * _grid.cellSize.z * 0.5f;
                    Handles.Label(labelPos, "X: " + _grid.numCellsX + ", Y:" + _grid.numCellsY + ", Z: " + _grid.numCellsZ, GUIStyleDb.instance.sceneViewInfoLabel);
                    Handles.EndGUI();
                }
            }
        }

        private float getSglAxisHandleSize()
        {
            return HandleUtility.GetHandleSize(position) * GizmoPrefs.instance.extrudeSglHandleSize;
        }

        private float getDblAxisHandleSize()
        {
            return HandleUtility.GetHandleSize(position) * GizmoPrefs.instance.extrudeDblAxisSize;
        }

        private void setAABB(AABB aabb)
        {
            if (!aabb.isValid)
            {
                _boxSize = Vector3.zero;
                return;
            }

            _boxSize = aabb.size.abs();
            rotation = Quaternion.identity;
            position = aabb.center;
        }

        private void setOBB(OBB obb)
        {
            if (!obb.isValid)
            {
                _boxSize = Vector3.zero;
                return;
            }

            _boxSize = obb.size.abs();
            rotation = obb.rotation;
            position = obb.center;
        }

        private void moveTargets(Vector3 moveVector)
        {
            UndoEx.recordGameObjectTransforms(_targetParents);
            foreach (var parent in _targetParents)
                parent.transform.position += moveVector;
        }

        private bool isGridCellMasked(int cellX, int cellY, int cellZ, Vector3 cellPosition)
        {
            // Note: Ignore the cell that encapsulates the target objects.
            if (_targetObjectsCellCoords.matchCoords(cellX, cellY, cellZ)) return true;
            return false;

            // Note: This was used back when pattern step was used instead of padding.
            /*Vector3Int patternStep = sharedSettings.patternStep;
            if (patternStep.x != 0)
            {
                int groupIndex = (int)(cellX / (float)patternStep.x);
                if ((groupIndex % 2) != 0) return true;
            }
            if (patternStep.y != 0)
            {
                int groupIndex = (int)(cellY / (float)patternStep.y);
                if ((groupIndex % 2) != 0) return true;
            }
            if (patternStep.z != 0)
            {
                int groupIndex = (int)(cellZ / (float)patternStep.z);
                if ((groupIndex % 2) != 0) return true;
            }

            return false;*/
        }
    }
}
#endif