#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public enum ObjectModularSnapSessionCommandId
    {
        VerticalStep = 0,
        ResetVerticalStep,
        ResetVerticalStepToOriginal,
        ToggleSnapHalfSpace,
        ToggleObject2ObjectSnap,
        ToggleGridSnapObjectClimb,
    }

    public struct ObjectModularSnapSessionCommand
    {
        public ObjectModularSnapSessionCommandId    id;
        public VerticalStepDirection                verticalStepDirection;
    }

    public class ObjectModularSnapSession : ObjectTransformSession
    {
        public enum SnapMode
        {
            Grid,
            ObjectToObject
        }

        public enum SnapHalfSpace
        {
            InFront,
            Behind
        }

        public enum TransformMode
        {
            Snap = 0,
            Scale,
            Rotate,
        }

        private TransformMode                           _transformMode                  = TransformMode.Snap;
        private bool                                    _snapAxisLocked                 = false;
        private bool                                    _switchSnapAxis                 = false;
        private Vector3                                 _lockedSnapAxis                 = Vector3.zero;
        private Vector3Int                              _lockedSnapAxisMask             = Vector3Int.zero;
        private List<Vector3>                           _lockedSnapOrigins              = new List<Vector3>();

        [SerializeField]
        private SnapMode                                _snapMode                       = SnapMode.Grid;
        [SerializeField]
        private SnapHalfSpace                           _snapHalfSpace                  = SnapHalfSpace.InFront;

        private List<ObjectModularSnapTargetParent>     _modularSnapTargetParents       = new List<ObjectModularSnapTargetParent>();
        private ObjectTransformSessionSurface           _surface                        = new ObjectTransformSessionSurface();

        private SceneRaycastFilter                      _raycastFilter                  = new SceneRaycastFilter();
        private ObjectToObjectSnap.Config               _object2ObjectSnapConfig        = new ObjectToObjectSnap.Config();
        private ObjectBounds.QueryConfig                _boxRenderQConfig               = new ObjectBounds.QueryConfig();

        [SerializeField]
        private ObjectProjectionSettings                _projectionSettings;
        private int                                     _mouseDeltaCaptureId;

        [NonSerialized]
        private ObjectOverlapFilter                     _highlightOverlapFilter     = new ObjectOverlapFilter();
        [NonSerialized]
        private ObjectOutline                           _alignmentOutline           = new ObjectOutline();
        [NonSerialized]
        private List<GameObject>                        _hintObjectBuffer           = new List<GameObject>();
        [NonSerialized]
        private Vector3[]                               _highlightOverlapAxes       = new Vector3[4];
        [NonSerialized]
        private Vector3[]                               _highlightProjectionAxes    = new Vector3[4];
        [NonSerialized]
        private Color[]                                 _distanceLabelColors        = new Color[4];
        [NonSerialized]
        private List<GameObject>                        _gameObjectBuffer           = new List<GameObject>();

        private ObjectProjectionSettings                projectionSettings
        {
            get
            {
                if (_projectionSettings == null)
                {
                    _projectionSettings                 = ScriptableObject.CreateInstance<ObjectProjectionSettings>();
                    UndoEx.saveEnabledState();
                    UndoEx.enabled                      = false;
                    _projectionSettings.alignAxis       = false;
                    _projectionSettings.embedInSurface  = true;
                    UndoEx.restoreEnabledState();
                }
                return _projectionSettings;
            }
        }

        public ObjectModularSnapSettings                sharedSettings                  { get; set; }
        public SnapMode                                 snapMode                        { get { return _snapMode; } }
        public SnapHalfSpace                            snapHalfSpace                   { get { return _snapHalfSpace; } }
        public override string                          sessionName                     { get { return "Modular Snap"; } }
        public override ObjectTransformSessionType      sessionType                     { get { return ObjectTransformSessionType.ModularSnap; } }
        public override bool                            clientCanUpdateTargetTransforms { get { return true; } }

        public ObjectModularSnapSession()
        {
            _boxRenderQConfig.objectTypes       = GameObjectType.Mesh | GameObjectType.Sprite | GameObjectType.Terrain;
            _boxRenderQConfig.volumelessSize    = Vector3.zero;
            _boxRenderQConfig.includeInactive   = false;
            _boxRenderQConfig.includeInvisible  = false;

            _highlightOverlapFilter.objectTypes = GameObjectType.All & (~GameObjectType.Terrain);
        }

        public void executeCommand(ObjectModularSnapSessionCommand command)
        {
            if (command.id == ObjectModularSnapSessionCommandId.VerticalStep) verticalStep(command.verticalStepDirection);
            else if (command.id == ObjectModularSnapSessionCommandId.ToggleSnapHalfSpace)
                setSnapHalfSpace(snapHalfSpace == SnapHalfSpace.Behind ? SnapHalfSpace.InFront : SnapHalfSpace.Behind);
            else if (command.id == ObjectModularSnapSessionCommandId.ToggleObject2ObjectSnap)
                setSnapMode(snapMode == SnapMode.ObjectToObject ? SnapMode.Grid : SnapMode.ObjectToObject);
            else if (command.id == ObjectModularSnapSessionCommandId.ResetVerticalStep) resetVerticalStep();
            else if (command.id == ObjectModularSnapSessionCommandId.ResetVerticalStepToOriginal) resetVerticalStepToOriginal();
            else if (command.id == ObjectModularSnapSessionCommandId.ToggleGridSnapObjectClimb)
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled = false;
                sharedSettings.gridSnapClimb = !sharedSettings.gridSnapClimb;
                if (!sharedSettings.gridSnapClimb) 
                    ObjectModularSnapTargetParent.calcOriginalVerticalStep(_modularSnapTargetParents, getVerticalStepBasePlane(), PluginScene.instance.grid.activeSettings.cellSizeY);

                PluginInspectorUI.instance.targetEditor.Repaint();
                UndoEx.restoreEnabledState();
            }
        }

        public int getVerticalStep(GameObject targetParent)
        {
            foreach(var target in _modularSnapTargetParents)
            {
                if (target.gameObject == targetParent) return target.verticalStep;
            }

            return 0;
        }

        public void setVerticalStep(GameObject targetParent, int verticalStep)
        {
            foreach (var target in _modularSnapTargetParents)
            {
                if (target.gameObject == targetParent)
                {
                    target.verticalStep = verticalStep;
                    snap();
                    ObjectEvents.onObjectsTransformed();
                }
            }
        }

        public void verticalStep(VerticalStepDirection stepDirection)
        {
            if (!isActive || snapMode != SnapMode.Grid) return;

            PluginGrid grid     = PluginScene.instance.grid;
            Plane basePlane     = getVerticalStepBasePlane();
            Vector3 stepVector  = grid.up * (stepDirection == VerticalStepDirection.Up ? grid.activeSettings.cellSizeY : -grid.activeSettings.cellSizeY);
            foreach (var parent in _modularSnapTargetParents)
            {
                parent.transform.position += stepVector;
                parent.updateVerticalStep(basePlane, grid.activeSettings.cellSizeY);
            }

            ObjectEvents.onObjectsTransformed();
        }

        public void setSnapMode(SnapMode snapMode)
        {
            if (!isActive || _snapMode == snapMode) return;

            _snapMode = snapMode;
            snapSingleTargetToCursor();
            snap();

            // Note: This must be done. Otherwise, objects might be pushed upwards or downwards
            //       along the grid normal depending on how much they have been offset vertically
            //       during grid snap.
            if (_snapMode == SnapMode.ObjectToObject)
                ObjectModularSnapTargetParent.updateSurfaceAnchorDirections(_modularSnapTargetParents, _surface.pickPoint);
        }

        public void setSnapHalfSpace(SnapHalfSpace halfSpace)
        {
            if (!isActive || halfSpace == _snapHalfSpace) return;

            _snapHalfSpace = halfSpace;
            foreach (var parent in _modularSnapTargetParents)
                parent.verticalStep = -parent.verticalStep;

            snap();
        }

        public void resetVerticalStep()
        {
            if (!isActive || snapMode != SnapMode.Grid) return;

            ObjectModularSnapTargetParent.resetVerticalStep(_modularSnapTargetParents);
            snap();
        }

        public void resetVerticalStepToOriginal()
        {
            if (!isActive || snapMode != SnapMode.Grid) return;
    
            ObjectModularSnapTargetParent.resetVerticalStepToOriginal(_modularSnapTargetParents);
            snap();
        }

        public override void onUndoRedo()
        {
            if (!isActive) return;

            removeNullObjects();
            if (!isActive) return;

            ObjectModularSnapTargetParent.create(_targetParents, _modularSnapTargetParents);
            onTargetTransformsChanged();
        }

        public override void onTargetTransformsChanged()
        {
            if (isActive)
            {
                ObjectModularSnapTargetParent.updateVerticalStep(_modularSnapTargetParents, getVerticalStepBasePlane(), PluginScene.instance.grid.activeSettings.cellSizeY);
                if(_snapMode == SnapMode.Grid) snapToGrid();
                ObjectModularSnapTargetParent.updateSurfaceAnchorDirections(_modularSnapTargetParents, _surface.pickPoint);
                snap();
            }
        }

        protected override void drawUIHandles()
        {
            if (!ObjectTransformSessionPrefs.instance.modularSnapShowInfoText) return;

            Camera camera                           = PluginCamera.camera;
            Transform cameraTransform               = camera.transform;
            ObjectBounds.QueryConfig boundsQCOnfig  = ObjectBounds.QueryConfig.defaultConfig;

            if (_targetParents.Count == 1)
            {
                Handles.BeginGUI();
                foreach (var targetParent in _targetParents)
                {
                    var obb         = ObjectBounds.calcHierarchyWorldOBB(targetParent, boundsQCOnfig);
                    string label    = targetParent.name + "\n";
                    label           += targetParent.transform.position.ToString("F3");

                    Vector3 labelPos = obb.center - cameraTransform.up * obb.extents.magnitude - cameraTransform.right * 1.8f;
                    Handles.Label(labelPos, label, GUIStyleDb.instance.sceneViewInfoLabel);
                }
                Handles.EndGUI();
            }
        }

        protected override void draw()
        {
            var sessionPrefs = ObjectTransformSessionPrefs.instance;
            if (!sessionPrefs.modularSnapDrawObjectPivotProjectionLines && 
                !sessionPrefs.modularSnapDrawObjectPivotTicks &&
                !sessionPrefs.modularSnapDrawProjectedObjectPivotTicks) return;

            HandlesEx.saveColor();
            PluginGrid grid = PluginScene.instance.grid;

            foreach(var targetParent in _modularSnapTargetParents)
            {
                Vector3 targetPivot             = targetParent.transform.position;
                Vector3 projectedTargetPivot    = grid.plane.projectPoint(targetPivot);

                if (snapMode == SnapMode.Grid)
                {
                    if (sessionPrefs.modularSnapDrawObjectPivotProjectionLines)
                    {
                        Handles.color = sessionPrefs.modularSnapObjectPivotProjectionLineColor;
                        Handles.DrawLine(targetPivot, projectedTargetPivot);
                    }
                    if (sessionPrefs.modularSnapDrawObjectPivotTicks)
                    {
                        Handles.color = sessionPrefs.modularSnapObjectPivotTickColor;
                        Handles.DotHandleCap(0, targetPivot, Quaternion.identity, HandleUtility.GetHandleSize(targetPivot) * sessionPrefs.modularSnapObjectPivotTickSize, EventType.Repaint);
                    }
                    if (sessionPrefs.modularSnapDrawProjectedObjectPivotTicks)
                    {
                        Handles.color = sessionPrefs.modularSnapProjectedObjectPivotTickColor;
                        Handles.DotHandleCap(0, projectedTargetPivot, Quaternion.identity, HandleUtility.GetHandleSize(projectedTargetPivot) * sessionPrefs.modularSnapProjectedObjectPivotTickSize, EventType.Repaint);
                    }
                }

                if (sessionPrefs.modularSnapDrawObjectBoxes)
                {
                    OBB worldOBB = ObjectBounds.calcHierarchyWorldOBB(targetParent.gameObject, _boxRenderQConfig);
                    if (worldOBB.isValid)
                    {
                        HandlesEx.saveMatrix();
                        Handles.color   = sessionPrefs.modularSnapObjectBoxWireColor;
                        Handles.matrix  = worldOBB.transformMatrix;
                        //Handles.DrawWireCube(Vector3.zero, Vector3.one);
                        HandlesEx.drawUnitWireCube();
                        HandlesEx.restoreMatrix();
                    }
                }
            }

            HandlesEx.restoreColor();

            if (_snapMode != SnapMode.ObjectToObject &&
                (ObjectTransformSessionPrefs.instance.modularSnapDrawAlingmentHighlights ||
                 ObjectTransformSessionPrefs.instance.modularSnapShowAlignmentHints))
            {
                Vector3 gridOrigin  = PluginScene.instance.grid.origin;
                Vector3 gridRight   = PluginScene.instance.grid.right;
                Vector3 gridUp      = PluginScene.instance.grid.up;
                Vector3 gridLook    = PluginScene.instance.grid.look;

                _highlightOverlapAxes[0] = gridRight;
                _highlightOverlapAxes[1] = -gridRight;
                _highlightOverlapAxes[2] = gridLook;
                _highlightOverlapAxes[3] = -gridLook;

                _highlightProjectionAxes[0] = gridLook;
                _highlightProjectionAxes[1] = gridLook;
                _highlightProjectionAxes[2] = gridRight;
                _highlightProjectionAxes[3] = gridRight;

                _distanceLabelColors[0] = GridPrefs.instance.xAxisColor;
                _distanceLabelColors[1] = GridPrefs.instance.xAxisColor;
                _distanceLabelColors[2] = GridPrefs.instance.zAxisColor;
                _distanceLabelColors[3] = GridPrefs.instance.zAxisColor;

                bool showAlignmentHints     = ObjectTransformSessionPrefs.instance.modularSnapShowAlignmentHints;
                float highlightRadius       = ObjectTransformSessionPrefs.instance.modularSnapAlignmentHighlightRadius;
                GUIStyle labelStyle         = new GUIStyle(GUIStyleDb.instance.sceneViewInfoLabel);
                labelStyle.fontStyle        = FontStyle.Bold;
                var overlapConfig           = ObjectOverlapConfig.defaultConfig;

                foreach (var targetParent in _targetParents)
                {
                    Transform targetTransform               = targetParent.transform;
                    ObjectBounds.QueryConfig boundsQConfig  = ObjectBounds.QueryConfig.defaultConfig;
                    boundsQConfig.volumelessSize            = new Vector3(0.1f, 0.1f, 0.1f);
                    OBB hierarchyOBB = ObjectBounds.calcHierarchyWorldOBB(targetParent, boundsQConfig);
                    if (hierarchyOBB.isValid)
                    {
                        float sizeAlongGridUp = Vector3Ex.getSizeAlongAxis(hierarchyOBB.size, hierarchyOBB.rotation, gridUp);

                        _highlightOverlapFilter.customFilter = (GameObject go) =>
                        {
                            if (TileRuleGridDb.instance.isObjectChildOfTileRuleGrid(go)) return false;
                            
                            Transform transform = go.transform;
                            foreach (var target in _targetParents)
                            {
                                if (go == target || transform.IsChildOf(target.transform)) return false;
                            }

                            return true;
                        };

                        for (int axis = 0; axis < _highlightOverlapAxes.Length; ++axis)
                        {
                            Vector3 overlapAxis     = _highlightOverlapAxes[axis];
                            Vector3 projectionAxis  = _highlightProjectionAxes[axis];

                            Vector3 overlapOBBSize  = new Vector3(1.0f, sizeAlongGridUp, 1.0f);
                            if (axis < 2) overlapOBBSize.x = highlightRadius;
                            else overlapOBBSize.z   = highlightRadius;

                            OBB overlapOBB = new OBB(hierarchyOBB.center + overlapAxis * highlightRadius * 0.5f, PluginScene.instance.grid.rotation);
                            overlapOBB.size = overlapOBBSize;
                            PluginScene.instance.overlapBox(overlapOBB, _highlightOverlapFilter, ObjectOverlapConfig.defaultConfig, _alignmentOutline.objectGather);

                            // Debug 
                            /*HandlesEx.saveMatrix();
                            Handles.matrix = overlapOBB.transformMatrix;
                            HandlesEx.drawUnitWireCube();
                            HandlesEx.restoreMatrix();*/

                            float d0 = Vector3.Dot((targetTransform.position - gridOrigin), projectionAxis);
                            _alignmentOutline.objectGather.RemoveAll(go =>
                            {
                                // Note: Reject if sitting below target position. This can be really confusing when
                                //       placing objects on top of other objects.
                                if (Vector3.Dot(go.transform.position - targetTransform.position, gridUp) < 0.0f) return true;

                                float d1 = Vector3.Dot((go.transform.position - gridOrigin), projectionAxis);
                                return Mathf.Abs((d1 - d0)) > 1e-5f;
                            });

                            GameObjectEx.getOutermostPrefabInstanceRoots(_alignmentOutline.objectGather, _hintObjectBuffer, null);
                  
                            if (_alignmentOutline.objectGather.Count != 0)
                            {
                                // Note: Use 'drawHandles' instead of 'drawHandlesIndividually'. That one is slow when large numbers of objects are drawn.
                                if (ObjectTransformSessionPrefs.instance.modularSnapDrawAlingmentHighlights)
                                    _alignmentOutline.drawHandles(_distanceLabelColors[axis]);

                                if (showAlignmentHints)
                                {
                                    GUIEx.saveContentColor();
                                    GUI.contentColor = _distanceLabelColors[axis];

                                    _hintObjectBuffer.Sort((GameObject g0, GameObject g1) =>
                                    {
                                        float d0 = (g0.transform.position - targetTransform.position).magnitude;
                                        float d1 = (g1.transform.position - targetTransform.position).magnitude;
                                        return d0.CompareTo(d1);
                                    });

                                    // Note: Draw from end to start in order to draw closer object hints over ones which are further away.
                                    int numHints = Mathf.Min(ObjectTransformSessionPrefs.instance.modularSnapMaxNumAlignmentHints, _hintObjectBuffer.Count);
                                    for (int i = numHints - 1; i >= 0; --i)
                                    {
                                        GameObject go = _hintObjectBuffer[i];
                                        float d = (go.transform.position - targetTransform.position).magnitude;
                                        Handles.BeginGUI();
                                        Handles.Label(go.transform.position, go.name + "\nDistance: " + d, labelStyle);
                                        Handles.EndGUI();
                                    }
                                    GUIEx.restoreContentColor();
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_projectionSettings);
        }

        protected override bool onCanBegin()
        {
            return sharedSettings != null;
        }

        private bool _recordTransformsOnSnap = false;
        protected override bool onBegin()
        {
            GameObjectEx.getAllObjectsInHierarchies(_targetParents, true, true, _gameObjectBuffer);
            _raycastFilter.setIgnoredObjects(_gameObjectBuffer);

            bool surfaceReady = updateSurface();
            if (!surfaceReady) return false;

            ObjectModularSnapTargetParent.create(_targetParents, _modularSnapTargetParents);
            ObjectModularSnapTargetParent.calcOriginalVerticalStep(_modularSnapTargetParents, getVerticalStepBasePlane(), PluginScene.instance.grid.activeSettings.cellSizeY);

            _recordTransformsOnSnap = true;
            snapSingleTargetToCursor();
            snap();
            ObjectModularSnapTargetParent.updateSurfaceAnchorDirections(_modularSnapTargetParents, _surface.pickPoint);

            if (ObjectTransformSessionPrefs.instance.modularSnapRotationRelativeToGrid)
            {
                Vector3 destAxis = PluginScene.instance.grid.up;
                foreach (var parent in _targetParents)
                {
                    Transform parentTransform = parent.transform;
                    int axisIndex = parentTransform.findIndexOfMostAlignedAxis(destAxis);
                    float dot = Vector3Ex.absDot(destAxis, parentTransform.getLocalAxis(axisIndex));
                    if (Mathf.Abs(1.0f - dot) > 1e-5f)
                    {
                        parentTransform.alignAxis(axisIndex, destAxis);
                    }
                }
            }

            return true;
        }

        protected override void onEnd()
        {
            _surface.makeInvalid();
            _modularSnapTargetParents.Clear();
        }

        private bool _prevGridSnapClimbEnabled = false;
        protected override void update()
        {
            Event e = Event.current;

            // Note: Not useful with modular snapping and can create confusion and subtle errors.
            //if (FixedShortcuts.enableMouseRotateObjects(e)) setTransformMode(TransformMode.Rotate);
            //else if (FixedShortcuts.enableMouseScaleObjects(e)) setTransformMode(TransformMode.Scale);

            if (!SceneView.lastActiveSceneView.hasFocus)
                SceneView.lastActiveSceneView.Focus();

            if (!Mouse.instance.hasMoved && !e.isScrollWheel && (_prevGridSnapClimbEnabled == sharedSettings.gridSnapClimb)) return;
            _prevGridSnapClimbEnabled = sharedSettings.gridSnapClimb;

            if (_transformMode != TransformMode.Snap)
                setTransformMode(TransformMode.Snap);

            if (_transformMode == TransformMode.Snap && !e.isScrollWheel)
            {
                bool oldSnapAxisLocked  = _snapAxisLocked;
                bool oldInvertSnapAxis  = _switchSnapAxis;
                _snapAxisLocked         = FixedShortcuts.modularSnap_EnableLockSnapAxis(e);
                _switchSnapAxis         = FixedShortcuts.modularSnap_EnableLockSnapAxisInvert(e);

                if ((!oldSnapAxisLocked && _snapAxisLocked) || (_snapAxisLocked && (oldInvertSnapAxis != _switchSnapAxis)))
                {
                    _lockedSnapOrigins.Clear();
                    foreach (var p in _modularSnapTargetParents)
                        _lockedSnapOrigins.Add(p.transform.position);
                }

                if (_snapAxisLocked)
                {
                    _switchSnapAxis = FixedShortcuts.modularSnap_EnableLockSnapAxisInvert(e);
                    calcSnapAxisLockData();
                }
            }
            else
            {
                _snapAxisLocked = false;
                _switchSnapAxis = false;
            }

            if (_snapMode == SnapMode.Grid)
            {
                if (FixedShortcuts.modularSnap_VerticalStep_ScrollWheel(e))
                {
                    if (e.delta.y > 0) verticalStep(VerticalStepDirection.Down);
                    else verticalStep(VerticalStepDirection.Up);

                    e.disable();
                }
                else
                if (FixedShortcuts.modularSnap_Rotate_ScrollWheel(e))
                {
                    float angle         = e.delta.y * InputPrefs.instance.scrollRotationStep;
                    Quaternion rotation = Quaternion.AngleAxis(angle, InputPrefs.instance.getRotationAxis(1));

                    UndoEx.recordGameObjectTransforms(_targetParents);

                    if (_targetParents.Count == 1)
                    {                      
                        _targetParents[0].transform.rotateAround(rotation, _targetParents[0].transform.position);
                    }
                    else
                    {
                        Vector3 center = Vector3.zero;
                        foreach (var targetParent in _targetParents)
                            center += targetParent.transform.position;

                        center /= (float)_targetParents.Count;
                        foreach (var targetParent in _targetParents)
                        {
                            targetParent.transform.rotateAround(rotation, center);
                        }
                    }
                    onTargetTransformsChanged();
                    e.disable();
                }
            }

            updateSurface();
            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Invalid) return;

            if (_transformMode == TransformMode.Snap)
            {
                if (_recordTransformsOnSnap)
                {
                    UndoEx.recordGameObjectTransforms(_targetParents);
                    _recordTransformsOnSnap = false;
                }

                foreach (var parent in _modularSnapTargetParents) 
                    parent.transform.position = (_surface.pickPoint + parent.surfaceAnchorDirection);
                snap();
            }
            else
            if (_transformMode == TransformMode.Scale)
            {
                UndoEx.recordGameObjectTransforms(_targetParents);
                scale();
            }
            else
            if (_transformMode == TransformMode.Rotate)
            {
                UndoEx.recordGameObjectTransforms(_targetParents);
                rotate();
            }
        }

        private void calcSnapAxisLockData()
        {
            var grid            = PluginScene.instance.grid;
            Vector3 camLook     = PluginCamera.camera.transform.forward;
            Vector3 gridRight   = grid.right;
            Vector3 gridLook    = grid.look;
            float d0            = Vector3Ex.absDot(camLook, gridRight);
            float d1            = Vector3Ex.absDot(camLook, gridLook);
            if (d0 < d1)
            {
                _lockedSnapAxisMask = _switchSnapAxis ? new Vector3Int(0, 0, 1) : new Vector3Int(1, 0, 0);
                _lockedSnapAxis     = _switchSnapAxis ? gridLook : gridRight;
            }
            else
            {
                _lockedSnapAxisMask = _switchSnapAxis ? new Vector3Int(1, 0, 0) : new Vector3Int(0, 0, 1);
                _lockedSnapAxis     = _switchSnapAxis ? gridRight : gridLook;
            }
        }

        private void snapSingleTargetToCursor()
        {
            if (_modularSnapTargetParents.Count == 1 && sharedSettings.snapSingleTargetToCursor)
            {
                if (_snapMode == SnapMode.Grid)
                {
                    float t;
                    Ray pickRay             = PluginCamera.camera.getCursorRay();
                    PluginGrid grid         = PluginScene.instance.grid;
                    if (grid.plane.Raycast(pickRay, out t))
                    {
                        Vector3 point       = grid.snapAxes(pickRay.GetPoint(t), new Vector3Int(1, 0, 1));
                        Vector3 moveVector  = point - grid.plane.projectPoint(_modularSnapTargetParents[0].transform.position);
                        _modularSnapTargetParents[0].transform.position += moveVector;
                    }
                }
                else _modularSnapTargetParents[0].transform.position = _surface.pickPoint;
                ObjectEvents.onObjectsTransformed();
            }
        }

        private void setTransformMode(TransformMode transformMode)
        {
            if (!isActive) return;

            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Invalid ||
                _transformMode == transformMode) return;

            if (_transformMode == TransformMode.Scale) Mouse.instance.removeDeltaCapture(_mouseDeltaCaptureId);

            _transformMode = transformMode;
            if (_transformMode == TransformMode.Scale)
            {
                _mouseDeltaCaptureId = Mouse.instance.createDeltaCapture(Mouse.instance.position);
                ObjectModularSnapTargetParent.storeLocalScaleSnapshots(_modularSnapTargetParents);
            }

            updateSurfaceAnchorDirections();
        }

        private Plane getVerticalStepBasePlane()
        {
            var grid = PluginScene.instance.grid;
            return grid.plane;
        }

        private void snap()
        {
            if (_snapMode == SnapMode.Grid) snapToGrid();
            else object2ObjectSnap();

            ObjectEvents.onObjectsTransformed();
        }

        private void snapToGrid()
        {
            PluginGrid grid = PluginScene.instance.grid;
            Plane gridPlane = grid.plane;

            // Note: Don't grid snap climb when snap axis is locked. Causes the targets to jump
            //       around when an object is hovered with the mouse cursor.
            if (sharedSettings.gridSnapClimb && !_snapAxisLocked &&
                _surface.surfaceType != ObjectTransformSessionSurface.Type.Grid)
            {
                if (_surface.surfaceType != ObjectTransformSessionSurface.Type.Invalid)
                {
                    if (_targetParents.Count == 1 && sharedSettings.snapSingleTargetToCursor)
                    {
                        _targetParents[0].transform.position = _surface.pickPoint;
                        projectTargets();

/*
                        if (_snapAxisLocked)
                            _targetParents[0].transform.position = 
                                Vector3Ex.projectOnSegment(_targetParents[0].transform.position, _lockedSnapOrigins[0], _lockedSnapOrigins[0] + _lockedSnapAxis);*/
                    }
                    else
                    {
                        Plane surfacePlane  = _surface.plane;
                        int numParents      = _modularSnapTargetParents.Count;
                        for (int i = 0; i < numParents; ++i)
                        {
                            var parent                      = _modularSnapTargetParents[i];
                            parent.transform.position       = surfacePlane.projectPoint(parent.transform.position);
                            /*if (_snapAxisLocked)
                                parent.transform.position   = Vector3Ex.projectOnSegment(parent.transform.position, _lockedSnapOrigins[i], _lockedSnapOrigins[i] + _lockedSnapAxis);*/
                        }

                        if (!_snapAxisLocked) projectTargets();
                    }
              
                    grid.snapObjectsAxes(_targetObjects, _snapAxisLocked ? _lockedSnapAxisMask : Vector3Int.one);
                }
            }
            else
            {
                Vector3Int axesMask = _snapAxisLocked ? _lockedSnapAxisMask : Vector3Int.one;
                int numParents      = _modularSnapTargetParents.Count;
                for (int i = 0; i < numParents; ++i)
                {
                    var parent                  = _modularSnapTargetParents[i];
                    Vector3 pos                 = parent.transform.position;
                    Vector3 projectedPos        = gridPlane.projectPoint(pos);
                    parent.transform.position   += (projectedPos - pos);

                    if (_snapAxisLocked)
                        parent.transform.position = Vector3Ex.projectOnSegment(parent.transform.position, _lockedSnapOrigins[i], _lockedSnapOrigins[i] + _lockedSnapAxis);

                    grid.snapObjectAxes(parent.gameObject, axesMask);

                    // Note: Vector3Ex.projectOnSegment already took care of this.
                    if (!_snapAxisLocked) parent.transform.position   += grid.up * parent.verticalStep * grid.activeSettings.cellSize.y;
                }
            }
        }

        private void object2ObjectSnap()
        {
            projectTargets();
            _object2ObjectSnapConfig.snapRadius         = sharedSettings.snapRadius;
            _object2ObjectSnapConfig.destinationLayers  = sharedSettings.destinationLayers;
            ObjectToObjectSnap.snap(_targetParents, _object2ObjectSnapConfig);
        }

        private void projectTargets()
        {
            projectionSettings.halfSpace        = snapHalfSpace == SnapHalfSpace.InFront ? ObjectProjectionHalfSpace.InFront : ObjectProjectionHalfSpace.Behind;
            projectionSettings.projectAsUnit    = true;

            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Grid)
                ObjectProjection.projectHierarchiesOnPlane(_targetParents, _surface.plane, projectionSettings, null);
            else
            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Object)
                ObjectProjection.projectHierarchiesOnObject(_targetParents, _surface.surfaceObject, _surface.objectType, _surface.plane, projectionSettings, null);

        }

        private void scale()
        {
            if (!Mouse.instance.hasMoved || !FixedShortcuts.checkMouseScaleDelay()) return;

            foreach (var targetParent in _modularSnapTargetParents)
            {
                float scaleFactor   = 1.0f + Mouse.instance.deltaFromCapture(_mouseDeltaCaptureId).x * InputPrefs.instance.mouseScaleSensitivity;
                Vector3 newScale    = targetParent.localScaleSnapshot * scaleFactor;
                targetParent.transform.setLocalScaleFromPivot(newScale, targetParent.transform.position);
            }

            updateSurfaceAnchorDirections();
        }

        private void rotate()
        {
            if (!Mouse.instance.hasMoved || !FixedShortcuts.checkMouseRotateDelay()) return;

            float rotationAmount = Mouse.instance.delta.x * InputPrefs.instance.mouseRotationSensitivity;
            Vector3 rotationAxis = calcRotationAxis();

            foreach (var targetParent in _modularSnapTargetParents)
                targetParent.transform.Rotate(rotationAxis, rotationAmount, Space.World);

            updateSurfaceAnchorDirections();
        }

        private Vector3 calcRotationAxis()
        {
            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Object)
            {
                if (_surface.objectType == GameObjectType.Terrain) return _surface.surfaceObject.transform.up;
                else return PluginScene.instance.grid.up;
            }
            else
            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Grid) return PluginScene.instance.grid.up;
            else return Vector3.up;
        }

        private bool updateSurface()
        {
            _raycastFilter.raycastGrid      = sharedSettings.allowsGridSurface;
            _raycastFilter.raycastObjects   = (sharedSettings.allowsObjectSurface && _snapMode == SnapMode.ObjectToObject) || (sharedSettings.gridSnapClimb && !_snapAxisLocked);
            _raycastFilter.objectTypes      = GameObjectType.Mesh | GameObjectType.Terrain | GameObjectType.Sprite;

            _surface.pickFromScene(_raycastFilter);
            return _surface.surfaceType != ObjectTransformSessionSurface.Type.Invalid;
        }

        private void updateSurfaceAnchorDirections()
        {
            if (_modularSnapTargetParents.Count > 1 || !sharedSettings.snapSingleTargetToCursor)
                ObjectModularSnapTargetParent.updateSurfaceAnchorDirections(_modularSnapTargetParents, _surface.pickPoint);
        }
    }
}
#endif