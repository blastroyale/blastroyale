#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public enum ObjectSpawnCurveEditMode
    {
        None = 0,
        SelectControlPoints,
        InsertControlPoints,
    }

    public enum ObjectSpawnCurveRefreshReason
    {
        CurvePrefabProfileChanged = 0,
        CurvePrefabUsedStateChanged,
        CurvePrefabSpawnChanceChanged,
        CurvePrefabsDeleted,
        UseDefaultSettings,
        Refresh,
        Other
    }

    public struct ObjectSpawnCurvePOI
    {
        public Vector3  pointOnSegment;
        public int      sampleSegmentIndex;
        public bool     isValid;
    }

    public class ObjectSpawnCurveObjectData
    {
        public GameObject           gameObject;
        public ObjectSpawnCurvePOI  fwPOI;

        // Note: Doesn't matter if curve prefab is destroyed. This will be refreshed every time the curve is updated.
        public CurvePrefab          curvePrefab;

        public Vector3              rightAxis;
        public Vector3              upAxis;
        public Vector3              forwardAxis;
        public float                upSize;
        public float                forwardSize;
        public OBB                  spawnOBB;
        public float                scale;

        public float getUpSize()
        {
            return upSize * scale;
        }

        public float getForwardSize()
        {
            return forwardSize * scale;
        }

        public void generateScale(CurvePrefab curvePrefab)
        {
            if (curvePrefab.randomizeScale) scale = UnityEngine.Random.Range(curvePrefab.minRandomScale, curvePrefab.maxRandomScale);
            else scale = 1.0f;
        }
    }

    [Serializable]
    public class ObjectSpawnCurveLane
    {
        [NonSerialized]
        public List<ObjectSpawnCurveObjectData>     spawnedObjectData = new List<ObjectSpawnCurveObjectData>();
        [NonSerialized]
        public ObjectSpawnCurveObjectData           prevObjectData;

        // Note: Serialize prefab info to avoid loosing prefabs when switching to playmode and back.
        [SerializeField]
        public List<CurvePrefab>                    usedCurvePrefabs = new List<CurvePrefab>();
        [SerializeField]
        public int                                  nextCurvePrefab;

        public CurvePrefab pickNextPrefab(CurvePrefabProfile prefabProfile)
        {
            CurvePrefab curvePrefab;
            if (nextCurvePrefab < usedCurvePrefabs.Count)
            {
                curvePrefab = usedCurvePrefabs[nextCurvePrefab];
                if (curvePrefab == null)
                {
                    // Note: Replace null entry with a new prefab.
                    curvePrefab                         = prefabProfile.pickPrefab();
                    usedCurvePrefabs[nextCurvePrefab]   = curvePrefab;
                }
                ++nextCurvePrefab;
            }
            else
            {
                curvePrefab = prefabProfile.pickPrefab();

                // Add this prefab to the used prefab sequence
                ++nextCurvePrefab;
                usedCurvePrefabs.Add(curvePrefab);
            }

            return curvePrefab;
        }
    }

    public class ObjectSpawnCurve : ScriptableObject, IUIItemStateProvider
    {
        private class RayHit
        {
            public int          segmentIndex;
            public Vector3      pointOnSegment;
        }

        private class PrefabData
        {
            public GameObject   prefabAsset;
            public CurvePrefab  curvePrefab;
            public OBB          obb;
            public Vector3      upAxis;
            public Vector3      forwardAxis;
            public float        upSize;
            public float        forwardSize;
        }

        [SerializeField]
        private PluginGuid                  _guid                       = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private string                      _curveName                  = string.Empty;
        [SerializeField]
        private CurveObjectSpawnSettings    _settings;

        [SerializeField]
        private List<ObjectSpawnCurveLane>  _lanes                      = new List<ObjectSpawnCurveLane>();
        [SerializeField]
        private List<GameObject>            _spawnedObjects             = new List<GameObject>();
        // Note: We need to store the spawned objects in a separate non-serialized
        //       list in order to be able to handle Undo/Redo properly.
        [NonSerialized]
        private List<GameObject>            _spawnedObjectsBuffer       = new List<GameObject>();
        [SerializeField]
        private PrefabInstancePool          _prefabInstancePool;

        [SerializeField]
        private CatmullRomSpline3D          _spline                     = new CatmullRomSpline3D();
        [NonSerialized]
        private ObjectSpawnCurveEditMode    _editMode                   = ObjectSpawnCurveEditMode.None;
        [SerializeField]
        private List<int>                   _selectedCtrlPointIndices   = new List<int>();

        [SerializeField]
        private ObjectProjectionSettings    _terrainProjectionSettings;

        [NonSerialized]
        private Quaternion                  _gizmoRotation              = Quaternion.identity;
        [NonSerialized]
        private Vector3                     _gizmoScale                 = Vector3.one;

        [NonSerialized]
        private bool                        _draggingMoveGizmo;
        [NonSerialized]
        private Vector3                     _moveGizmoDragAreaCenter;

        [NonSerialized]
        private ObjectBounds.QueryConfig    _curvePrefabBoundsQConfig   = ObjectBounds.QueryConfig.defaultConfig;
        [NonSerialized]
        private ObjectBounds.QueryConfig    _spawnedObjectBoundsQConfig = ObjectBounds.QueryConfig.defaultConfig;

        [NonSerialized]
        private OBB[]                       _laneOBBs                   = new OBB[2];
        [NonSerialized]
        private Vector3[]                   _laneOffsetAxes             = new Vector3[2];

        [NonSerialized]
        private List<Vector3>               _samplePoints               = new List<Vector3>();
        [NonSerialized]
        private List<Vector3>               _curveDrawPoints            = new List<Vector3>();

        [NonSerialized]
        private Dictionary<CurvePrefab, PrefabData>     _curvePrefabDataMap         = new Dictionary<CurvePrefab, PrefabData>();
        [NonSerialized]
        private SceneRaycastFilter                      _projectionRaycastFilter    = new SceneRaycastFilter()
        {
            objectTypes = GameObjectType.Mesh | GameObjectType.Terrain | GameObjectType.Sprite,
            raycastGrid = true,
            raycastObjects = true
        };

        [NonSerialized]
        private ObjectOverlapFilter         _overlapFilter              = new ObjectOverlapFilter();
        [NonSerialized]
        private List<Vector3>               _vector3Buffer              = new List<Vector3>();
        [NonSerialized]
        private List<GameObject>            _overlapIgnoreObjectBuffer  = new List<GameObject>();
        [NonSerialized]
        private TerrainCollection           _terrains                   = new TerrainCollection();
        [NonSerialized]
        private TerrainObjectOverlapFilter  _terrainOverlapFilter       = new TerrainObjectOverlapFilter();

        [SerializeField]
        private bool                        _uiSelected                 = false;
        [NonSerialized]
        private CopyPasteMode               _uiCopyPasteMode            = CopyPasteMode.None;

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
                    _terrainProjectionSettings.projectAsUnit    = false;
                    UndoEx.restoreEnabledState();
                }
                return _terrainProjectionSettings;
            }
        }

        public PluginGuid               guid                        { get { return _guid; } }
        public int                      numControlPoints            { get { return _spline.numControlPoints; } }
        public int                      numSegments                 { get { return _spline.numSegments; } }
        public int                      numSampleSegments           { get { return _samplePoints.Count - 1; } }
        public int                      numSpawnedObjects           { get { return _spawnedObjects.Count; } }
        public int                      numSelectedControlPoints    { get { return _selectedCtrlPointIndices.Count; } }
        public ObjectSpawnCurveEditMode editMode                    { get { return _editMode; } set { _editMode = value; } }
        public CurveObjectSpawnSettings settings                    { get { return _settings; } }
        public string                   curveName                   { get { return _curveName; } set { if (!string.IsNullOrEmpty(value)) { UndoEx.record(this); _curveName = value; } } }
        public bool                     uiSelected                  { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode            uiCopyPasteMode             { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }

        public ObjectSpawnCurve()
        {
            _curvePrefabBoundsQConfig.objectTypes   = GameObjectType.All;
            _spawnedObjectBoundsQConfig.objectTypes = _curvePrefabBoundsQConfig.objectTypes;

            _overlapFilter.objectTypes              = GameObjectType.Mesh | GameObjectType.Sprite;
            _overlapFilter.customFilter             = new Func<GameObject, bool>((GameObject go) => { return !go.isTerrainMesh() && !go.isSphericalMesh(); });
        }

        public static int calcNumSpawnedObjects(List<ObjectSpawnCurve> curves)
        {
            int numSpawnedObjects = 0;
            foreach (var curve in curves)
                numSpawnedObjects += curve.numSpawnedObjects;

            return numSpawnedObjects;
        }

        public GameObject getSpawnedObject(int index)
        {
            return _spawnedObjects[index];
        }

        public AABB calcWorldAABB()
        {
            if (numControlPoints == 0) return AABB.getInvalid();

            int lastCtrlPt = numControlPoints - 2;
            AABB aabb = new AABB(_spline.getControlPoint(1), Vector3.zero);
            for (int i = 2; i <= lastCtrlPt; ++i)
                aabb.enclosePoint(_spline.getControlPoint(i));

            return aabb;
        }

        public void copy(ObjectSpawnCurve src)
        {
            if (this == src) return;

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;
            settings.copy(src.settings);

            _spline.copy(src._spline);
            onControlPointsDirty();

            UndoEx.restoreEnabledState();
        }

        public void frame()
        {
            if (numControlPoints == 0) return;
            SceneViewEx.frame(calcWorldAABB().toBounds(), false);
        }

        public void ignoreSpawnedObjectsDuringRaycast(SceneRaycastFilter sceneRaycastFilter)
        {
            foreach (var lane in _lanes)
            {
                foreach (var objectData in lane.spawnedObjectData)
                    sceneRaycastFilter.addIgnoredObject(objectData.gameObject);
            }
        }

        public void refresh(ObjectSpawnCurveRefreshReason refreshReason)
        {
            if (refreshReason == ObjectSpawnCurveRefreshReason.UseDefaultSettings ||
                refreshReason == ObjectSpawnCurveRefreshReason.CurvePrefabProfileChanged ||
                refreshReason == ObjectSpawnCurveRefreshReason.CurvePrefabsDeleted)
            {
                // Note: We want to generate a new set of prefabs because the curve prefab profile has changed
                //       and a new set of prefabs is now available.
                foreach (var lane in _lanes)
                    lane.usedCurvePrefabs.Clear();

                // Note: Before clearing the prefab instance pool, destroy the spawned game objects.
                //       This will release any pooled objects and allow the pool to destroy all
                //       such objects.
                destroySpawnedObjectsNoUndoRedo();
                _prefabInstancePool.clear();
            }
            else
            if (refreshReason == ObjectSpawnCurveRefreshReason.Refresh ||
                refreshReason == ObjectSpawnCurveRefreshReason.CurvePrefabUsedStateChanged ||
                refreshReason == ObjectSpawnCurveRefreshReason.CurvePrefabSpawnChanceChanged)
            {
                // Note: We want to generate a new set of prefabs.
                foreach (var lane in _lanes)
                    lane.usedCurvePrefabs.Clear();
            }

            spawnObjects();
        }

        public void clear()
        {
            _spline.clear();
            _selectedCtrlPointIndices.Clear();
            onControlPointsDirty();
        }

        public void selectAllControlPoints()
        {
            UndoEx.record(this);

            // Note: Don't select the first and last control points.
            //       These are the dummy points inserted to make the curve work more intuitively.
            _selectedCtrlPointIndices.Clear();
            for (int i = 1; i < numControlPoints - 1; ++i)
                _selectedCtrlPointIndices.Add(i);
        }

        public void projectSelectedControlPoints()
        {
            UndoEx.record(this);

            _projectionRaycastFilter.clearIgnoredObjects();
            ignoreSpawnedObjectsDuringRaycast(_projectionRaycastFilter);

            var rayHit = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), _projectionRaycastFilter, ObjectRaycastConfig.defaultConfig);
            if (rayHit != null && rayHit.anyHit)
            {
                if (rayHit.wasObjectHit && !rayHit.wasGridHit) projectSelectedControlPointsOnObject(rayHit.objectHit);
                else if (rayHit.wasGridHit && !rayHit.wasObjectHit) projectedSelectedControlPointsOnGrid(rayHit.gridHit);
                else
                {
                    if (Mathf.Abs(rayHit.objectHit.hitEnter - rayHit.gridHit.hitEnter) < 1e-5f) projectSelectedControlPointsOnObject(rayHit.objectHit);
                    else
                    {
                        if (rayHit.objectHit.hitEnter < rayHit.gridHit.hitEnter) projectSelectedControlPointsOnObject(rayHit.objectHit);
                        else projectedSelectedControlPointsOnGrid(rayHit.gridHit);
                    }
                }

                onControlPointsDirty();
            }
        }

        public void removeLastPoint()
        {
            _spline.removeLastControlPoint();
            _selectedCtrlPointIndices.Clear();
            onControlPointsDirty();
        }

        public void removeSelectedControlPoints()
        {
            if (numControlPoints > 4)
            {
                UndoEx.record(this);

                // Sort the selected indices list to make things easier
                _selectedCtrlPointIndices.Sort(delegate (int i0, int i1)
                { return i0.CompareTo(i1); });

                // Loop through each selected index
                for (int i = 0; i < _selectedCtrlPointIndices.Count;)
                {
                    // Fetch the selected point index and validate it
                    int ptIndex = _selectedCtrlPointIndices[i];
                    if (ptIndex > 1 && ptIndex < numControlPoints - 2)
                    {
                        // Remove the point                    
                        _spline.removeControlPoint(ptIndex);
                        _selectedCtrlPointIndices.RemoveAt(i);

                        // Remap the selected indices
                        // Note: This step is made easier by sorting the list beforehand.
                        for (int j = i; j < _selectedCtrlPointIndices.Count; ++j)
                            --_selectedCtrlPointIndices[j];
                    }
                    else ++i;
                }

                onControlPointsDirty();
            }
        }

        public void addControlPoint(Vector3 pt)
        {
            _spline.addControlPoint(pt);
            onControlPointsDirty();
        }

        public void setControlPoint(int index, Vector3 pt)
        {
            _spline.setControlPoint(index, pt);
            onControlPointsDirty();
        }

        public void setPenultimateControlPoint(Vector3 pt)
        {
            int index = _spline.numControlPoints - 2;
            _spline.setControlPoint(index, pt);
            onControlPointsDirty();
        }

        public void setLastControlPoint(Vector3 pt)
        {
            int index = _spline.numControlPoints - 1;
            _spline.setControlPoint(index, pt);
            onControlPointsDirty();
        }

        public void destroySpawnedObjectsNoUndoRedo()
        {
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            int numObjects = _spawnedObjects.Count;
            for (int i = 0; i < numObjects; ++i)
            {
                var go = _spawnedObjects[i];
                if (go != null)
                {
                    var prefabAsset = go.getOutermostPrefabAsset();
                    if (prefabAsset != null) _prefabInstancePool.releasePrefabInstance(prefabAsset, go);
                    else GameObject.DestroyImmediate(go);
                }
            }

            _spawnedObjects.Clear();
            _spawnedObjectsBuffer.Clear();

            foreach (var lane in _lanes)
                lane.spawnedObjectData.Clear();

            UndoEx.restoreEnabledState();
        }

        public bool isControlPointSelected(int ptIndex)
        {
            return _selectedCtrlPointIndices.Contains(ptIndex);
        }

        public void draw()
        {
            if (_editMode != ObjectSpawnCurveEditMode.InsertControlPoints)  drawCurve();
            if (_editMode == ObjectSpawnCurveEditMode.InsertControlPoints)  drawSegments();
            drawControlPoints();
            if (_editMode == ObjectSpawnCurveEditMode.InsertControlPoints)  drawInsertedControlPoint();

            if (_editMode == ObjectSpawnCurveEditMode.SelectControlPoints)
            {
                var activeGizmoId = ObjectSpawn.instance.curveObjectSpawn.activeGizmoId;
                if (activeGizmoId == ObjectSpawnCurveGizmoId.Move)          drawPositionHandles();
                else if (activeGizmoId == ObjectSpawnCurveGizmoId.Rotate)   drawRotationHandles();
                else if (activeGizmoId == ObjectSpawnCurveGizmoId.Scale)    drawScaleHandles();
            }

            Event e = Event.current;
            if (e.isLeftMouseButtonDownEvent() && GUIUtility.hotControl == 0)
            {
                // Note: Not really necessary and it causes control points in other
                //       curves to get deselected when clicking on control point handles.
                /*
                UndoEx.record(this);
                _selectedCtrlPointIndices.Clear();*/
            }
            else if (FixedShortcuts.cancelAction(e))
            {
                if (_editMode == ObjectSpawnCurveEditMode.InsertControlPoints)
                    _editMode = ObjectSpawnCurveEditMode.SelectControlPoints;
            }
        }

        private void drawCurve()
        {
            HandlesEx.saveColor();
            Handles.color = ObjectSpawnPrefs.instance.curveSpawnCurveColor;

            float step = Mathf.Lerp(0.3f, 0.05f, ObjectSpawnPrefs.instance.curveSmoothness);
            _spline.evalPositions(_curveDrawPoints, step);
            int numPoints = _curveDrawPoints.Count;
            for (int i = 0; i < numPoints - 1; ++i)
                Handles.DrawLine(_curveDrawPoints[i], _curveDrawPoints[i + 1]);

            HandlesEx.restoreColor();
        }

        private void drawControlPoints()
        {
            HandlesEx.saveColor();
            float tickSize  = ObjectSpawnPrefs.instance.curveSpawnTickSize;
            int numPoints   = _spline.numControlPoints;

            // Note: Don't draw the last dummy point because we don't want to override
            //       the penultimate point (see selectAllControlPoints).
            //       However, draw the first control point because we need to be able to
            //       see something when creating a curve (initially, it will have only 2
            //       control points - if we start from 1, no points will be rendered).
            for (int ptIndex = 0; ptIndex < numPoints - 1; ++ptIndex)
            {
                Vector3 controlPoint    = _spline.getControlPoint(ptIndex);
                float tickDrawSize      = HandleUtility.GetHandleSize(controlPoint) * tickSize;
                float tickPickSize      = tickDrawSize;

                if (numSelectedControlPoints != 0 && isControlPointSelected(ptIndex))
                    Handles.color = ObjectSpawnPrefs.instance.curveSpawnSelectedTickColor;
                else Handles.color = ObjectSpawnPrefs.instance.curveSpawnTickColor;

                // Note: If this is the last point and edit mode is none, it might mean
                //       we are building the curve. In that case, use Handles.DotHandleCap
                //       because otherwise, camera pan won't work (i.e. Handles.Button
                //       will eat the event because the cursor hovers the button).
                bool btnPressed = false;
                if (ptIndex == numPoints - 2 && _editMode == ObjectSpawnCurveEditMode.None)
                    Handles.DotHandleCap(0, controlPoint, Quaternion.identity, tickDrawSize, EventType.Repaint);
                else btnPressed = Handles.Button(controlPoint, Quaternion.identity, tickDrawSize, tickPickSize, Handles.DotHandleCap);

                if (btnPressed)
                {
                    if (_editMode == ObjectSpawnCurveEditMode.SelectControlPoints)
                    {
                        UndoEx.record(this);

                        if (FixedShortcuts.selection_EnableAppend(Event.current))
                        {
                            if (isControlPointSelected(ptIndex)) _selectedCtrlPointIndices.Remove(ptIndex);
                            else _selectedCtrlPointIndices.Add(ptIndex);
                        }
                        else
                        {
                            _selectedCtrlPointIndices.Clear();
                            _selectedCtrlPointIndices.Add(ptIndex);
                        }
                    }
                }
            }

            HandlesEx.restoreColor();
        }

        private void drawSegments()
        {
            HandlesEx.saveColor();
            Handles.color = ObjectSpawnPrefs.instance.curveSpawnSegmentColor;

            for (int i = 1; i < numSegments - 1; ++i)
                Handles.DrawLine(_spline.getControlPoint(i), _spline.getControlPoint(i + 1));

            HandlesEx.restoreColor();
        }

        private void drawInsertedControlPoint()
        {
            RayHit rayHit = raycastSegments(PluginCamera.camera.getCursorRay());
            if (rayHit != null)
            {
                float tickSize      = ObjectSpawnPrefs.instance.curveSpawnTickSize;
                float tickDrawSize  = HandleUtility.GetHandleSize(rayHit.pointOnSegment) * tickSize;
                Handles.DotHandleCap(0, rayHit.pointOnSegment, Quaternion.identity, tickDrawSize, EventType.Repaint);

                if (Event.current.isLeftMouseButtonDownEvent())
                {
                    UndoEx.record(this);
                    int insertIndex = rayHit.segmentIndex + 1;
                    _spline.insertControlPoint(insertIndex, rayHit.pointOnSegment);
                    _editMode = ObjectSpawnCurveEditMode.SelectControlPoints;

                    onControlPointsDirty();
                    Event.current.disable();

                    _selectedCtrlPointIndices.Clear();
                    _selectedCtrlPointIndices.Add(insertIndex);

                    // Remap selected indices
                    for (int i = insertIndex + 1; i < numSelectedControlPoints; ++i)
                        ++_selectedCtrlPointIndices[i];
                }
            }
        }

        private RayHit raycastSegments(Ray ray)
        {
            const float segmentThickness    = 1.0f;
            int closestSegmentIndex         = -1;
            float minT                      = float.MaxValue;
            for (int segIndex = 1; segIndex < numSegments - 1; ++segIndex)
            {
                OBB segOBB = OBB.createFromSegment(_spline.getSegmentStart(segIndex), _spline.getSegmentEnd(segIndex), segmentThickness);

                float t;
                if (segOBB.raycast(ray, out t))
                {
                    if (t < minT)
                    {
                        minT                = t;
                        closestSegmentIndex = segIndex;
                    }
                }
            }

            if (closestSegmentIndex < 0) return null;

            Vector3 ptOnSegment = Vector3Ex.projectOnSegment(ray.GetPoint(minT),
                _spline.getSegmentStart(closestSegmentIndex), _spline.getSegmentEnd(closestSegmentIndex));
            return new RayHit() { segmentIndex = closestSegmentIndex, pointOnSegment = ptOnSegment };
        }

        private bool notDraggingGizmos()
        {
            return Event.current.type != EventType.Used && !Mouse.instance.isButtonDown((int)MouseButton.LeftMouse);
        }

        private void drawPositionHandles()
        {
            bool ctrlPointsDirty = false;
            if (numSelectedControlPoints > 0)
            {
                int lastSelectedPtIndex = numSelectedControlPoints - 1;
                EditorGUI.BeginChangeCheck();
                Vector3 lastSelectedCtrlPt = _spline.getControlPoint(_selectedCtrlPointIndices[lastSelectedPtIndex]);
                if (!_draggingMoveGizmo) _moveGizmoDragAreaCenter = lastSelectedCtrlPt;
                Vector3 newPos = Handles.PositionHandle(lastSelectedCtrlPt, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    _draggingMoveGizmo      = true;

                    float maxDragRadius     = ObjectSpawnPrefs.instance.curveSpawnMoveGizmoDragRadius;
                    Vector3 toNewPos        = newPos - _moveGizmoDragAreaCenter;
                    if (toNewPos.magnitude >= maxDragRadius)
                        newPos = _moveGizmoDragAreaCenter + toNewPos.normalized * maxDragRadius;

                    ctrlPointsDirty         = true;
                    UndoEx.record(this);

                    bool snapToGrid         = FixedShortcuts.curveObjectSpawn_EnableControlPointSnapToGrid(Event.current);
                    Vector3 moveOffset      = newPos - _spline.getControlPoint(_selectedCtrlPointIndices[lastSelectedPtIndex]);
                    foreach (var selectedPtIndex in _selectedCtrlPointIndices)
                        moveSelectedControlPoint(selectedPtIndex, moveOffset, snapToGrid);

                    if (!snapToGrid && numSelectedControlPoints == numControlPoints - 2 &&
                        settings.projectionMode == CurveObjectSpawnProjectionMode.None)
                    {
                        ctrlPointsDirty = false;
                        foreach (var go in _spawnedObjects)
                            go.transform.position += moveOffset;
                    }
                }
            }

            if (ctrlPointsDirty) onControlPointsDirty();

            if (notDraggingGizmos())
                _draggingMoveGizmo = false;
        }

        private void drawRotationHandles()
        {
            Vector3 ctrlPtCenter    = _spline.calcControlPointCenter(true);

            EditorGUI.BeginChangeCheck();
            Quaternion newRotation  = Handles.RotationHandle(_gizmoRotation, ctrlPtCenter);
            if (EditorGUI.EndChangeCheck())
            {
                UndoEx.record(this);
                Quaternion relativeRotation = QuaternionEx.createRelativeRotation(_gizmoRotation, newRotation);
                _gizmoRotation              = newRotation;

                int numCtrlPoints           = numControlPoints;
                for (int i = 1; i < numCtrlPoints - 1; ++i)
                {
                    Vector3 pt              = _spline.getControlPoint(i);
                    Vector3 toPt            = pt - ctrlPtCenter;
                    toPt                    = relativeRotation * toPt;
                    _spline.setControlPoint(i, ctrlPtCenter + toPt);
                    syncDummyControlPointIfNecessary(i);
                }

                // Note: If no projection is used and no overlap checking is necessary, just
                //       rotate the objects around the center. Otherwise, we need to refresh
                //       the curve because we want to project the objects and avoid overlaps.
                if (settings.projectionMode == CurveObjectSpawnProjectionMode.None &&
                    !settings.avoidOverlaps)
                {
                    foreach (var go in _spawnedObjects)
                    {
                        Transform objectTransform = go.gameObject.transform;
                        UndoEx.recordTransform(objectTransform);
                        objectTransform.rotateAround(relativeRotation, ctrlPtCenter);
                    }
                }
                else onControlPointsDirty();
            }

            if (notDraggingGizmos())
                _gizmoRotation = Quaternion.identity;
        }

        private void drawScaleHandles()
        {
            Vector3 ctrlPtCenter        = _spline.calcControlPointCenter(true);

            EditorGUI.BeginChangeCheck();
            Vector3 newScale            = Handles.ScaleHandle(_gizmoScale, ctrlPtCenter, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                UndoEx.record(this);
                Vector3 relativeScale   = Vector3.Scale(newScale, _gizmoScale.replaceZero(1.0f).getInverse());
                _gizmoScale             = newScale;

                int numCtrlPoints = numControlPoints;
                for (int i = 1; i < numCtrlPoints - 1; ++i)
                {
                    Vector3 pt          = _spline.getControlPoint(i);
                    Vector3 toPt        = pt - ctrlPtCenter;
                    toPt = Vector3.Scale(toPt, relativeScale);
                    _spline.setControlPoint(i, ctrlPtCenter + toPt);
                    syncDummyControlPointIfNecessary(i);
                }

                onControlPointsDirty();
            }

            if (notDraggingGizmos())
                _gizmoScale = Vector3.one;
        }

        private void moveSelectedControlPoint(int selectedPtIndex, Vector3 moveOffset, bool snapToGrid)
        {
            Vector3 newPos          = _spline.getControlPoint(selectedPtIndex) + moveOffset;
            if (snapToGrid) newPos  = PluginScene.instance.grid.snapAllAxes(newPos);

            if (selectedPtIndex > 1 && selectedPtIndex < numControlPoints - 2)
                _spline.setControlPoint(selectedPtIndex, newPos);
            else
            {
                if (selectedPtIndex <= 1)
                {
                    _spline.setControlPoint(0, newPos);
                    _spline.setControlPoint(1, newPos);
                }
                else
                {
                    _spline.setControlPoint(numControlPoints - 2, newPos);
                    _spline.setControlPoint(numControlPoints - 1, newPos);
                }
            }
        }

        private void onControlPointsDirty()
        {
            spawnObjects();
        }

        private void syncDummyControlPointIfNecessary(int dirtyPointIndex)
        {
            if (dirtyPointIndex > 1 && dirtyPointIndex < (numControlPoints - 2)) return;

            if (dirtyPointIndex == 0) _spline.setControlPoint(1, _spline.getControlPoint(0));
            else if (dirtyPointIndex == 1) _spline.setControlPoint(0, _spline.getControlPoint(1));
            else if (dirtyPointIndex == numControlPoints - 2) _spline.setControlPoint(numControlPoints - 1, _spline.getControlPoint(numControlPoints - 2));
            else if (dirtyPointIndex == numControlPoints - 1) _spline.setControlPoint(numControlPoints - 2, _spline.getControlPoint(numControlPoints - 1));
        }

        private void projectedSelectedControlPointsOnGrid(GridRayHit rayHit)
        {
            projectSelectedControlPointsOnPlane(rayHit.hitPlane);
        }

        private void projectSelectedControlPointsOnPlane(Plane plane)
        {
            int numPts = numSelectedControlPoints;
            for (int i = 0; i < numPts; ++i)
            {
                int ptIndex = _selectedCtrlPointIndices[i];
                projectControlPointOnPlane(plane, ptIndex);
            }
        }

        private void projectControlPointOnPlane(Plane plane, int ptIndex)
        {
            Vector3 ctrlPt = _spline.getControlPoint(ptIndex);
            _spline.setControlPoint(ptIndex, plane.projectPoint(ctrlPt));
            syncDummyControlPointIfNecessary(ptIndex);
        }

        private void projectSelectedControlPointsOnObject(ObjectRayHit rayHit)
        {
            GameObject hitObject        = rayHit.hitObject;
            GameObjectType objectType   = GameObjectDataDb.instance.getGameObjectType(hitObject);
            if (objectType == GameObjectType.Terrain)
            {
                Terrain terrain     = hitObject.getTerrain();
                float terrainYPos   = terrain.transform.position.y;

                // Note: When projecting onto a terrain, we must handle the special case where
                //       there are more terrains in the scene arranged in a grid-like manner.
                //       So we will collect all terrain objects that overlap with the curve
                //       and project each point on the terrain in whose area it resides.
                overlapTerrains(_terrains);

                int numPts = numSelectedControlPoints;
                for (int i = 0; i < numPts; ++i)
                {
                    int ptIndex         = _selectedCtrlPointIndices[i];
                    Vector3 controlPt   = _spline.getControlPoint(ptIndex);

                    if (terrain.isWorldPointInsideTerrainArea(controlPt)) projectControlPointOnTerrain(terrain, terrainYPos, ptIndex);
                    else projectControlPointOnTerrains(_terrains, hitObject, ptIndex);
                }
            }
            else
            if (objectType == GameObjectType.Mesh)
            {
                if (hitObject.isTerrainMesh())
                {
                    PluginMesh terrainMesh = PluginMeshDb.instance.getPluginMesh(hitObject.getMesh());

                    // Note: See GameObject.Terrain branch.
                    overlapTerrains(_terrains);

                    int numPts = numSelectedControlPoints;
                    for (int i = 0; i < numPts; ++i)
                    {
                        int ptIndex         = _selectedCtrlPointIndices[i];
                        Vector3 controlPt   = _spline.getControlPoint(ptIndex);

                        if (TerrainMeshUtil.isWorldPointInsideTerrainArea(hitObject, controlPt))
                            projectControlPointOnTerrainMesh(hitObject, terrainMesh, ptIndex);
                        else projectControlPointOnTerrains(_terrains, hitObject, ptIndex);
                    }
                }
                else
                if (hitObject.isSphericalMesh())
                {
                    PluginMesh sphericalMesh    = PluginMeshDb.instance.getPluginMesh(hitObject.getMesh());
                    int numPts                  = numSelectedControlPoints;
                    for (int i = 0; i < numPts; ++i)
                    {
                        int ptIndex             = _selectedCtrlPointIndices[i];
                        projectControlPointOnSphericalMesh(hitObject, sphericalMesh, ptIndex);
                    }
                }
                else projectSelectedControlPointsOnPlane(rayHit.hitPlane);
            }
            else if (objectType == GameObjectType.Sprite) projectSelectedControlPointsOnPlane(rayHit.hitPlane);
        }

        private void projectControlPointOnTerrains(TerrainCollection terrains, GameObject ignoredTerrain, int ptIndex)
        {
            Vector3 controlPt   = _spline.getControlPoint(ptIndex);
            bool projected      = false;

            foreach (var terrain in terrains.unityTerrains)
            {
                if (terrain.gameObject != ignoredTerrain && terrain.isWorldPointInsideTerrainArea(controlPt))
                {
                    projectControlPointOnTerrain(terrain, terrain.transform.position.y, ptIndex);
                    projected = true;
                    break;
                }
            }

            if (!projected)
            {
                foreach (var go in terrains.terrainMeshes)
                {
                    if (TerrainMeshUtil.isWorldPointInsideTerrainArea(go, controlPt))
                    {
                        projectControlPointOnTerrainMesh(go, PluginMeshDb.instance.getPluginMesh(go.getMesh()), ptIndex);
                        projected = true;
                        break;
                    }
                }
            }
        }

        private void projectControlPointOnTerrain(Terrain terrain, float terrainYPos, int ptIndex)
        {
            Vector3 ctrlPt = _spline.getControlPoint(ptIndex);
            _spline.setControlPoint(ptIndex, terrain.projectPoint(terrainYPos, ctrlPt));
            syncDummyControlPointIfNecessary(ptIndex);
        }

        private void projectControlPointOnTerrainMesh(GameObject terrainObject, PluginMesh terrainMesh, int ptIndex)
        {
            Vector3 ctrlPt = _spline.getControlPoint(ptIndex);
            _spline.setControlPoint(ptIndex, TerrainMeshUtil.projectPoint(terrainObject, terrainMesh, ctrlPt));
            syncDummyControlPointIfNecessary(ptIndex);
        }

        private void projectControlPointOnSphericalMesh(GameObject sphereObject, PluginMesh sphericalMesh, int ptIndex)
        {
            Vector3 ctrlPt = _spline.getControlPoint(ptIndex);
            _spline.setControlPoint(ptIndex, SphericalMeshUtil.projectPoint(sphereObject, sphericalMesh, ctrlPt));
            syncDummyControlPointIfNecessary(ptIndex);
        }

        private void spawnObjects()
        {
            // Destroy the previous bulk of objects and exit if we don't have enough segments
            destroySpawnedObjectsNoUndoRedo();
            if (numSegments < 3) return;

            // Cache prefab data needed during object spawn
            fillCurvePrefabDataMap();

            // If the prefab data map is empty, it means no prefabs are being used, so we can exit
            if (_curvePrefabDataMap.Count == 0) return;

            // Evaluate the sample points that are used to approximate the curve
            evalSamplePoints();

            CurvePrefabProfile curvePrefabProfile   = settings.curvePrefabProfile;
            int sampleSegmentIndex                  = 0;
            Vector3 curveUpAxis                     = getCurveUpAxis();
            ObjectSpawnCurvePOI prevPOI             = new ObjectSpawnCurvePOI();
            prevPOI.pointOnSegment                  = _samplePoints[0];
            prevPOI.sampleSegmentIndex              = sampleSegmentIndex;
            prevPOI.isValid                         = true;

            // Note: Disable Undo/Redo because this function will be called
            //       from onUndoRedo and we can't record while Undo/Redo is
            //       being handled.
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            // Clear overlap filter
            _overlapFilter.clearIgnoredObjects();

            // Init lanes
            updateLaneListCapacity();
            foreach (var lane in _lanes)
            {
                lane.nextCurvePrefab        = 0;
                lane.prevObjectData         = null;
                if (lane.usedCurvePrefabs.Count == 0)
                    lane.usedCurvePrefabs.Add(curvePrefabProfile.pickPrefab());
            }

            // Cache this here
            _spawnedObjectBoundsQConfig.volumelessSize = Vector3Ex.create(settings.volumlessObjectSize);

            CurvePrefab curvePrefab;
            while (true)
            {
                // Pick the next prefab in the main lane.
                // Note: We are working with the main lane only. Other lanes
                //       will be handled at the end of each iteration.
                ObjectSpawnCurveLane mainLane   = getMainLane();
                curvePrefab                     = mainLane.pickNextPrefab(curvePrefabProfile);

                // Store data for easy access
                PrefabData prefabData           = _curvePrefabDataMap[curvePrefab];

                // Create the spawn data for the object we are about to spawn
                var objectData                  = new ObjectSpawnCurveObjectData();
                objectData.curvePrefab          = curvePrefab;
                objectData.forwardSize          = prefabData.forwardSize;
                objectData.upSize               = prefabData.upSize;
                objectData.generateScale(curvePrefab);

                // Generate the length value (prefab size + padding)
                float length                    = objectData.getForwardSize();
                if (settings.paddingMode == CurveObjectSpawnPaddingMode.Constant) length += settings.padding;
                else if (settings.paddingMode == CurveObjectSpawnPaddingMode.Random) length += UnityEngine.Random.Range(settings.minRandomPadding, settings.maxRandomPadding);

                // Find the point on the curve where the prefab's forward extremity should reside
                objectData.fwPOI                = findFowardPOI(prevPOI, length, numSampleSegments, ref sampleSegmentIndex);
                if (!objectData.fwPOI.isValid) break;

                // Calculate the OBB that is used to establish the object transform details
                objectData.forwardAxis          = (objectData.fwPOI.pointOnSegment - prevPOI.pointOnSegment).normalized;
                objectData.rightAxis            = calcObjectRightAxis(curveUpAxis, objectData.forwardAxis);
                objectData.upAxis               = calcObjectUpAxis(objectData.rightAxis, objectData.forwardAxis);
                objectData.spawnOBB             = calcObjectSpawnOBB(objectData, mainLane, prefabData, prevPOI);

                // Note: Apply lane padding to the main lane. But only if its random. When lane padding is Constant,
                //       We want the main lane to always sit in the middle, extend along the curve so that the other lanes
                //       move away from the curve in a symmetrical fashion.
                if (settings.lanePaddingMode == CurveObjectSpawnLanePaddingMode.Random)
                {
                    float lanePadding = UnityEngine.Random.Range(settings.minRandomLanePadding, settings.maxRandomLanePadding);
                    objectData.spawnOBB.center = objectData.spawnOBB.center + objectData.rightAxis * lanePadding;
                }

                // Try to spawn a game object
                trySpawnObject(objectData, prefabData, curvePrefabProfile, mainLane);

                // Regardless of whether an object was spawned or not, spawn object in parallel lanes
                spawnObjectsInParallelLanes(objectData, curvePrefabProfile);

                // Update previous POI
                prevPOI = objectData.fwPOI;
            }

            UndoEx.restoreEnabledState();
        }

        private bool trySpawnObject(ObjectSpawnCurveObjectData objectData, PrefabData prefabData, CurvePrefabProfile curvePrefabProfile, ObjectSpawnCurveLane lane)
        {
            // Store data for easy access
            bool isMainLabe = lane == getMainLane();
            var curvePrefab = objectData.curvePrefab;
            var prefabAsset = curvePrefab.prefabAsset;

            // Ensure no overlap with previous object
            if (settings.tryFixOverlap && isMainLabe && lane.prevObjectData != null)
                tryFixOverlap(lane.prevObjectData, objectData);

            // Calculate object position based on OBB center
            Vector3 objectScale = Vector3.Scale(Vector3Ex.create(objectData.scale), prefabAsset.transform.lossyScale);
            Vector3 position    = ObjectPositionCalculator.calcRootPosition(prefabAsset, prefabData.obb,
                    objectData.spawnOBB.center, objectScale, objectData.spawnOBB.rotation);

            // Spawn object
            lane.prevObjectData = null;      // Reset this to null and only set it if an object is spawned.
            if (settings.objectSkipChance == 0.0f || !Probability.evalChance(settings.objectSkipChance))
            {
                GameObject spawnedObject    = _prefabInstancePool.acquirePrefabInstance(curvePrefab.prefabAsset, position,
                    objectData.spawnOBB.rotation, objectScale);

                objectData.gameObject       = spawnedObject;
                curvePrefab.pluginPrefab.attachInstanceToObjectGroup(spawnedObject);
                updateObjectTransform(objectData);

                // Handle overlaps.
                // Note: We need to handle them here because this is where we have access to the final OBB of the object.
                if (settings.avoidOverlaps)
                {
                    if (checkForOverlaps(objectData, lane))
                    {
                        _prefabInstancePool.releasePrefabInstance(curvePrefab.prefabAsset, spawnedObject);
                        return false;
                    }

                    // Note: Add this spawned hierarchy to the ignored object list.
                    spawnedObject.getAllChildrenAndSelf(false, false, _overlapIgnoreObjectBuffer);
                    _overlapFilter.addIgnoredObjects(_overlapIgnoreObjectBuffer);
                }

                lane.spawnedObjectData.Add(objectData);
                _spawnedObjects.Add(spawnedObject);
                _spawnedObjectsBuffer.Add(spawnedObject);   // Note: Add to secondary list to allow Undo/Redo to work correctly.
                lane.prevObjectData = objectData;

                return true;
            }

            return false;
        }

        private void spawnObjectsInParallelLanes(ObjectSpawnCurveObjectData mainLaneObjectData, CurvePrefabProfile curvePrefabProfile)
        {
            _laneOffsetAxes[0]  = mainLaneObjectData.rightAxis;
            _laneOffsetAxes[1]  = -_laneOffsetAxes[0];

            _laneOBBs[0]        = mainLaneObjectData.spawnOBB;
            _laneOBBs[1]        = mainLaneObjectData.spawnOBB;

            // Generate the lane padding
            float lanePadding   = settings.lanePadding;
            if (settings.lanePaddingMode == CurveObjectSpawnLanePaddingMode.Random)
                lanePadding     = UnityEngine.Random.Range(settings.minRandomLanePadding, settings.maxRandomLanePadding);

            // Generate the number of lanes
            int numLanes        = settings.numLanes;
            if (settings.laneMode == CurveObjectSpawnLaneMode.Random)
                numLanes        = UnityEngine.Random.Range(settings.minRandomNumLanes, settings.maxRandomNumLanes + 1);

            // Note: Jump over the main lane which is always at index 0.
            for (int laneIndex = 1; laneIndex < numLanes; ++laneIndex)
            {
                int arrayIndex          = (laneIndex - 1) % 2;
                Vector3 rightAxis       = _laneOffsetAxes[arrayIndex];
                OBB obb                 = _laneOBBs[arrayIndex];
           
                var lane                = _lanes[laneIndex];
                var curvePrefab         = lane.pickNextPrefab(curvePrefabProfile);
                var prefabData          = _curvePrefabDataMap[curvePrefab];

                var objectData          = new ObjectSpawnCurveObjectData();
                objectData.curvePrefab  = curvePrefab;
                objectData.forwardSize  = prefabData.forwardSize;
                objectData.upSize       = prefabData.upSize;
                objectData.forwardAxis  = mainLaneObjectData.forwardAxis;
                objectData.rightAxis    = mainLaneObjectData.rightAxis;
                objectData.fwPOI        = mainLaneObjectData.fwPOI;    // Irrelevant in this case but let's go ahead and store it
                objectData.upAxis       = calcObjectUpAxis(objectData.rightAxis, objectData.forwardAxis);
                objectData.generateScale(curvePrefab);

                // Note: Pass the forward POI of the main lane spawn data. It's irrelevant since the center
                //       of the OBB will be calculated later.
                objectData.spawnOBB     = calcObjectSpawnOBB(objectData, lane, prefabData, mainLaneObjectData.fwPOI);

                // Push the OBB in the right position
                float size0 = Vector3Ex.getSizeAlongAxis(obb.size, obb.rotation, rightAxis);
                float size1 = Vector3Ex.getSizeAlongAxis(objectData.spawnOBB.size, objectData.spawnOBB.rotation, rightAxis);
                objectData.spawnOBB.center = obb.center + rightAxis * ((size0 + size1) * 0.5f + lanePadding);
                trySpawnObject(objectData, prefabData, curvePrefabProfile, lane);

                _laneOBBs[arrayIndex] = objectData.spawnOBB;
            }
        }

        private void updateLaneListCapacity()
        {
            int numLanesNeeded  = settings.numLanes;
            if (settings.laneMode == CurveObjectSpawnLaneMode.Random)
                numLanesNeeded  = settings.maxRandomNumLanes;

            if (_lanes.Count < numLanesNeeded)
            {
                int numLanesToCreate = numLanesNeeded - _lanes.Count;
                for (int i = 0; i < numLanesToCreate; ++i)
                    _lanes.Add(new ObjectSpawnCurveLane());
            }
            else
            if (_lanes.Count > numLanesNeeded)
            {
                while (_lanes.Count > numLanesNeeded)
                    _lanes.RemoveAt(_lanes.Count - 1);
            }
        }

        private ObjectSpawnCurveLane getMainLane()
        {
            return _lanes[0];
        }

        private bool checkForOverlaps(ObjectSpawnCurveObjectData objectData, ObjectSpawnCurveLane lane)
        {
            OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(objectData.gameObject, _spawnedObjectBoundsQConfig);
            _overlapFilter.ignoredHierarchy = objectData.gameObject;
            if (PluginScene.instance.overlapBox(hierarchyWorldOBB, _overlapFilter, ObjectOverlapConfig.defaultConfig))
                return true;

            return false;
        }

        private void tryFixOverlap(ObjectSpawnCurveObjectData prevObjectData, ObjectSpawnCurveObjectData currentObjectData)
        {
            OBB prevOBB             = prevObjectData.spawnOBB;
            OBB currentOBB          = currentObjectData.spawnOBB;

            Box3DFace prevFace      = Box3D.findMostAlignedFace(prevOBB.center, prevOBB.size, prevOBB.rotation, prevObjectData.forwardAxis);
            Box3DFace currentFace   = Box3D.findMostAlignedFace(currentOBB.center, currentOBB.size, currentOBB.rotation, -currentObjectData.forwardAxis);

            Plane prevFacePlane     = Box3D.calcFacePlane(prevOBB.center, prevOBB.size, prevOBB.rotation, prevFace);
            Box3D.calcFaceCorners(currentOBB.center, currentOBB.size, currentOBB.rotation, currentFace, _vector3Buffer);
            int furthestPtBehind    = prevFacePlane.findIndexOfFurthestPointBehind(_vector3Buffer);

            if (furthestPtBehind >= 0)
            {
                Vector3 projectedPt = prevFacePlane.projectPoint(_vector3Buffer[furthestPtBehind]);
                Vector3 offset = projectedPt - _vector3Buffer[furthestPtBehind];

                // Note: When 'tryFixOverlap' and 'avoidOverlaps' are true, it can happen that
                //       the algorithm is stuck in an infinite loop not being able to spawn the next
                //       object. Not sure how this happens exactly, but this fixes it.
                // Note: No longer needed. Now, tryFixOverlap is only called if the previous object
                //       data is not null (i.e. if the previous position on the curve contains
                //       a game object).
                //if (Vector3.Dot(prevObjectData.forwardAxis, currentObjectData.forwardAxis) + 1.0f < 1e-5f)
                {
                    currentObjectData.spawnOBB.center += offset;
                    currentObjectData.fwPOI.pointOnSegment += offset;
                }
            }
        }

        private void updateObjectTransform(ObjectSpawnCurveObjectData objectData)
        {
            CurvePrefab curvePrefab     = objectData.curvePrefab;
            GameObject spawnedObject    = objectData.gameObject;

            // Note: Project before we offset along up axis.
            if (settings.projectionMode == CurveObjectSpawnProjectionMode.Terrains)
                projectObjectOnTerrains(objectData);

            if (curvePrefab.upAxisOffsetMode == CurvePrefabUpAxisOffsetMode.Constant)
            {
                if (curvePrefab.upAxisOffset != 0.0f) spawnedObject.transform.position += objectData.upAxis * curvePrefab.upAxisOffset;
            }
            else
            if (curvePrefab.upAxisOffsetMode == CurvePrefabUpAxisOffsetMode.Random)
            {
                spawnedObject.transform.position += objectData.upAxis * UnityEngine.Random.Range(curvePrefab.minRandomUpAxisOffset, curvePrefab.maxRandomUpAxisOffset);
            }
        }

        private void projectObjectOnTerrains(ObjectSpawnCurveObjectData objectData)
        {
            OBB overlapOBB      = objectData.spawnOBB;
            overlapOBB.size     = overlapOBB.size.replace(1, PluginScene.terrainOverlapBoxVerticalSize);
            PluginScene.instance.terrainsOverlapBox(overlapOBB, _terrainOverlapFilter, TerrainObjectOverlapConfig.defaultConfig, _terrains);

            terrainProjectionSettings.alignAxis             = objectData.curvePrefab.alignUpAxisWhenProjected;
            terrainProjectionSettings.invertAlignmentAxis   = objectData.curvePrefab.invertUpAxis;
            terrainProjectionSettings.alignmentAxis         = objectData.curvePrefab.upAxis;
            ObjectProjection.projectHierarchyOnTerrains(objectData.gameObject, objectData.spawnOBB, _terrains, terrainProjectionSettings);

            // Note: Need to do this again if up axis alignment is used, because
            //       the alignment might have canceled the random rotation.
            if (objectData.curvePrefab.randomizeForwardAxisRotation && objectData.curvePrefab.alignUpAxisWhenProjected)
            {
                Quaternion randRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(objectData.curvePrefab.minRandomForwardAxisRotation,
                    objectData.curvePrefab.maxRandomForwardAxisRotation), objectData.forwardAxis);
                objectData.gameObject.transform.rotation = randRotation * objectData.gameObject.transform.rotation;
            }
        }

        private OBB calcObjectSpawnOBB(ObjectSpawnCurveObjectData objectData, ObjectSpawnCurveLane lane, PrefabData prefabData, ObjectSpawnCurvePOI prevPOI)
        {
            bool isMainLane     = (lane == getMainLane());
            var curvePrefab     = objectData.curvePrefab;

            OBB obb             = new OBB();
            obb.center          = prevPOI.pointOnSegment;
            obb.center          += objectData.upAxis * objectData.getUpSize() * 0.5f;
            obb.center          += objectData.forwardAxis * objectData.getForwardSize() * 0.5f;

            if (curvePrefab.randomizeScale)
                obb.size        = Vector3.Scale(prefabData.obb.size, Vector3Ex.create(objectData.scale));
            else obb.size       = prefabData.obb.size;

            Quaternion rotation = Quaternion.identity;
            if (curvePrefab.alignAxes)
            {
                Quaternion upAlignmentRotation = prefabData.prefabAsset.transform.calcAlignmentRotation(prefabData.upAxis, objectData.upAxis);
                // Note: When aligning the forward axis, we need to rotate the prefab's forward axis by the previous
                //       rotation in order to simulate a prefab rotation.
                //       Also, we need to handle the case where the prefab's forward axis is at 180 degrees
                //       from the desired forward axis AFTER the up alignment rotation is applied. This is
                //       necessary to handle loops.
                if (Vector3.Dot(upAlignmentRotation * prefabData.forwardAxis, objectData.forwardAxis) + 1.0f < 1e-5f)
                {
                    upAlignmentRotation = Quaternion.AngleAxis(180.0f, objectData.upAxis) * upAlignmentRotation;
                }
                rotation = prefabData.prefabAsset.transform.calcAlignmentRotation(upAlignmentRotation * prefabData.forwardAxis, objectData.forwardAxis) * upAlignmentRotation;
            }
            obb.rotation = rotation;

            if (curvePrefab.randomizeForwardAxisRotation)
            {
                Quaternion randRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(curvePrefab.minRandomForwardAxisRotation,
                    curvePrefab.maxRandomForwardAxisRotation), objectData.forwardAxis);
                obb.rotation = randRotation * obb.rotation;
            }

            if (curvePrefab.randomizeUpAxisRotation)
            {
                Quaternion randRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(curvePrefab.minRandomUpAxisRotation, curvePrefab.maxRandomUpAxisRotation), objectData.upAxis);
                obb.rotation = randRotation * obb.rotation;
            }

            if (isMainLane && curvePrefab.jitterMode != CurvePrefabJitterMode.None)
            {
                float jitter = curvePrefab.jitter;
                if (curvePrefab.jitterMode == CurvePrefabJitterMode.Random)
                    jitter = UnityEngine.Random.Range(curvePrefab.minRandomJitter, curvePrefab.maxRandomJitter);

                Vector3 jitterAxis = objectData.rightAxis;
                if (Probability.evalChance(0.5f)) jitterAxis = -jitterAxis;
                obb.center += jitterAxis * jitter;
            }

            return obb;
        }

        private Vector3 calcObjectRightAxis(Vector3 curveUpAxis, Vector3 forwardAxis)
        {
            Vector3 rightAxis = Vector3.Cross(curveUpAxis, forwardAxis);
            if (rightAxis.magnitude < 1e-5f)
            {
                Vector3 newCurveUpAxis;
                if (settings.curveUpAxis == CurveObjectSpawnUpAxis.Y) newCurveUpAxis = Vector3.forward;
                else if (settings.curveUpAxis == CurveObjectSpawnUpAxis.Z) newCurveUpAxis = Vector3.right;
                else newCurveUpAxis = Vector3.up;
                rightAxis = Vector3.Cross(newCurveUpAxis, forwardAxis).normalized;
            }

            return rightAxis;
        }

        private Vector3 calcObjectUpAxis(Vector3 rightAxis, Vector3 forwardAxis)
        {
            return Vector3.Cross(forwardAxis, rightAxis).normalized;
        }

        private ObjectSpawnCurvePOI findFowardPOI(ObjectSpawnCurvePOI prevPOI, float length, int numSampleSegments, ref int segmentIndex)
        {
            ObjectSpawnCurvePOI fwPOI   = new ObjectSpawnCurvePOI();
            fwPOI.isValid               = false;

            while (segmentIndex < numSampleSegments)
            {
                fwPOI = findFowardPOI(prevPOI, length, segmentIndex);
                if (!fwPOI.isValid) ++segmentIndex;
                else break;
            }

            if (segmentIndex == numSampleSegments) return fwPOI;
            fwPOI.isValid = true;
            return fwPOI;
        }

        private ObjectSpawnCurvePOI findFowardPOI(ObjectSpawnCurvePOI prevFWExtremity, float length, int segmentIndex)
        {
            ObjectSpawnCurvePOI poi     = new ObjectSpawnCurvePOI();
            poi.isValid                 = false;
            poi.sampleSegmentIndex      = segmentIndex;

            Vector3 p0;
            if (prevFWExtremity.sampleSegmentIndex == segmentIndex) p0 = prevFWExtremity.pointOnSegment;
            else p0 = _samplePoints[segmentIndex];
            Vector3 p1 = _samplePoints[segmentIndex + 1];

            if (!Vector3Ex.calcPointOnSegment(prevFWExtremity.pointOnSegment, p0, p1, length, out poi.pointOnSegment)) return poi;

            poi.isValid = true;
            return poi;
        }

        private void fillCurvePrefabDataMap()
        {
            _curvePrefabDataMap.Clear();
            _curvePrefabBoundsQConfig.volumelessSize = Vector3Ex.create(settings.volumlessObjectSize);

            // Loop through each prefab
            int numCurvePrefabs = settings.curvePrefabProfile.numPrefabs;
            for (int i = 0; i < numCurvePrefabs; ++i)
            {
                // Retrieve the current prefab and calculate its data.
                // Note: Ignore the prefab if it's not being used.
                CurvePrefab curvePrefab     = settings.curvePrefabProfile.getPrefab(i);
                if (curvePrefab.used)
                {
                    // Create a new prefab data record
                    PrefabData prefabData       = new PrefabData();
                    prefabData.prefabAsset      = curvePrefab.prefabAsset;
                    prefabData.curvePrefab      = curvePrefab;
                    _curvePrefabDataMap.Add(curvePrefab, prefabData);

                    // Calculate the prefab OBB
                    prefabData.obb              = ObjectBounds.calcHierarchyWorldOBB(curvePrefab.prefabAsset, _curvePrefabBoundsQConfig);

                    // Extract info about the prefab's up axis
                    GameObject prefabAsset      = curvePrefab.prefabAsset;
                    AxisDescriptor upAxisDesc   = prefabAsset.transform.flexiToLocalAxisDesc(prefabData.obb, curvePrefab.upAxis, curvePrefab.invertUpAxis);
                    prefabData.upSize           = prefabData.obb.size[upAxisDesc.index];
                    prefabData.upAxis           = prefabAsset.transform.getLocalAxis(upAxisDesc);

                    // Extract info about the prefab's forward axis
                    AxisDescriptor forwardAxisDesc  = prefabAsset.transform.flexiToLocalAxisDesc(prefabData.obb, curvePrefab.forwardAxis, curvePrefab.invertForwardAxis);
                    prefabData.forwardSize          = prefabData.obb.size[forwardAxisDesc.index];
                    prefabData.forwardAxis          = prefabAsset.transform.getLocalAxis(forwardAxisDesc);
                }
            }
        }

        private Vector3 getCurveUpAxis()
        {
            if (settings.curveUpAxis == CurveObjectSpawnUpAxis.Y) return settings.invertUpAxis ? Vector3.down : Vector3.up;
            if (settings.curveUpAxis == CurveObjectSpawnUpAxis.Z) return settings.invertUpAxis ? Vector3.back : Vector3.forward;
            return settings.invertUpAxis ? Vector3.left : Vector3.right;
        }

        private void evalSamplePoints()
        {
            _samplePoints.Clear();
            _spline.evalPositions(_samplePoints, settings.step);
        }

        private void overlapTerrains(TerrainCollection terrains)
        {
            terrains.clear();

            // Note: Use a large enough value along the Y axis for the size.
            OBB overlapOBB  = new OBB(calcWorldAABB());
            overlapOBB.size = overlapOBB.size.replace(1, PluginScene.terrainOverlapBoxVerticalSize);

            PluginScene.instance.terrainsOverlapBox(overlapOBB, _terrainOverlapFilter, TerrainObjectOverlapConfig.defaultConfig, terrains);
        }

        private void OnEnable()
        {
            if (_settings == null) _settings = ScriptableObject.CreateInstance<CurveObjectSpawnSettings>();
            if (_prefabInstancePool == null) _prefabInstancePool = ScriptableObject.CreateInstance<PrefabInstancePool>();
            else _prefabInstancePool.hidePrefabInstancesInInspector();

            Undo.undoRedoPerformed += onUndoRedo;
            EditorApplication.playModeStateChanged += onPlayModeStateChanged;

            _spawnedObjectsBuffer.Clear();
            foreach (var go in _spawnedObjects)
                _spawnedObjectsBuffer.Add(go);

            // Init lanes
            for (int i = 0; i < 1; ++i)
                _lanes.Add(new ObjectSpawnCurveLane());

            // Note: Always start out in control point selection mode.
            _editMode = ObjectSpawnCurveEditMode.SelectControlPoints;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
            EditorApplication.playModeStateChanged -= onPlayModeStateChanged;
        }

        private void onPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            _prefabInstancePool.hidePrefabInstancesInInspector();
        }

        private void OnDestroy()
        {
            if (_settings != null) UndoEx.destroyObjectImmediate(_settings);
            if (_terrainProjectionSettings != null) UndoEx.destroyObjectImmediate(_terrainProjectionSettings);
            if (_prefabInstancePool != null)
            {
                // Note: Need to clear instance pool before destruction. Otherwise, Undo/Redo won't work.
                _prefabInstancePool.clear();
                UndoEx.destroyObjectImmediate(_prefabInstancePool);
            }
        }

        // Note: This function has to be like this in order for Undo/Redo to work.
        private void onUndoRedo()
        {
            if (uiSelected && GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn
                && ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve && GSpawn.isActiveSelected)
            {
                // Note: Destroy previous objects. This works because the secondary object list is not serialized and
                //       the old objects are still there.
                foreach (var go in _spawnedObjectsBuffer)
                {
                    if (go != null) GameObject.DestroyImmediate(go);
                }

                _spawnedObjects.Clear();
                _spawnedObjectsBuffer.Clear();

                foreach (var lane in _lanes)
                    lane.spawnedObjectData.Clear();

                refresh(ObjectSpawnCurveRefreshReason.Other);
            }
        }
    }
}
#endif