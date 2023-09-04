#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public enum ObjectSurfaceSnapSessionCommandId
    {
        SetOffsetFromSurface = 0,
        ToggleAxisAlignment
    }

    public struct ObjectSurfaceSnapSessionCommand
    {
        public ObjectSurfaceSnapSessionCommandId    id;
        public float                                appliedOffsetFromSurface;
    }

    public class ObjectSurfaceSnapSession : ObjectTransformSession
    {
        public enum TransformMode
        {
            Snap = 0,
            Scale,
            Rotate,
            OrbitAroundAnchor,
            OffsetFromSurface,
            AdjustAnchor,
            OffsetFromAnchor
        }

        private List<ObjectSurfaceSnapTargetParent>     _surfaceSnapTargetParents           = new List<ObjectSurfaceSnapTargetParent>();

        private float                                   _appliedOffsetFromSurface           = 0.0f;
        private float                                   _totalOffsetFromSurface             = 0.0f;
        private TransformMode                           _transformMode                      = TransformMode.Snap;
        private bool                                    _isSurfaceLocked                    = false;
        private ObjectTransformSessionSurface           _surface                            = new ObjectTransformSessionSurface();
        private SceneRaycastFilter                      _surfacePickRaycastFilter           = new SceneRaycastFilter();
        [SerializeField]
        private ObjectProjectionSettings                _projectionSettings;
        private ObjectBounds.QueryConfig                _boxRenderQConfig                   = new ObjectBounds.QueryConfig();
        private int                                     _mouseDeltaCaptureId;

        private List<ObjectProjectionResult>            _projectionResultBuffer             = new List<ObjectProjectionResult>();
        [NonSerialized]
        private List<GameObject>                        _objectsIgnoredAsSurface            = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>                        _gameObjectBuffer                   = new List<GameObject>();

        private ObjectProjectionSettings                projectionSettings
        { 
            get 
            {
                if (_projectionSettings == null)
                {
                    _projectionSettings = ScriptableObject.CreateInstance<ObjectProjectionSettings>();
                    UndoEx.saveEnabledState();
                    UndoEx.enabled = false;
                    _projectionSettings.halfSpace = ObjectProjectionHalfSpace.InFront;
                    UndoEx.restoreEnabledState();
                }
                return _projectionSettings;
            } 
        }

        public ObjectSurfaceSnapSettings                sharedSettings                      { get; set; }
        public float                                    appliedOffsetFromSurface            { get { return _appliedOffsetFromSurface; } }
        public TransformMode                            transformMode                       { get { return _transformMode; } }
        public bool                                     isSurfaceValid                      { get { return _surface.surfaceType != ObjectTransformSessionSurface.Type.Invalid; } }
        public bool                                     isSurfaceLocked                     { get { return _isSurfaceLocked; } }
        public bool                                     isSurfaceUnityTerrain               { get { return _surface.surfaceType == ObjectTransformSessionSurface.Type.Object && _surface.objectType == GameObjectType.Terrain; } }
        public bool                                     isSurfaceTerrainMesh                { get { return _surface.surfaceType == ObjectTransformSessionSurface.Type.Object && _surface.objectType == GameObjectType.Mesh && _surface.surfaceObject.isTerrainMesh(); } }
        public bool                                     isSurfaceTerrain                    { get { return isSurfaceUnityTerrain || isSurfaceTerrainMesh; } }
        public bool                                     isSurfaceMesh                       { get { return _surface.surfaceType == ObjectTransformSessionSurface.Type.Object && _surface.objectType == GameObjectType.Mesh; } }
        public bool                                     isSurfaceGrid                       { get { return _surface.surfaceType == ObjectTransformSessionSurface.Type.Grid; } }
        public GameObject                               surfaceObject                       { get { return _surface.surfaceObject; } }
        public Vector3                                  surfacePickPoint                    { get { return _surface.pickPoint; } }
        public Vector3                                  surfacePickNormal                   { get { return _surface.pickPointNormal; } }
        public override string                          sessionName                         { get { return "Surface Snap"; } }
        public override ObjectTransformSessionType      sessionType                         { get { return ObjectTransformSessionType.SurfaceSnap; } }

        public ObjectSurfaceSnapSession()
        {
            _boxRenderQConfig.objectTypes       = GameObjectType.Mesh | GameObjectType.Sprite;
            _boxRenderQConfig.volumelessSize    = Vector3.zero;
            _boxRenderQConfig.includeInactive   = false;
            _boxRenderQConfig.includeInvisible  = false;
        }

        public Vector3 getNoAlignmentRotationAxis()
        {
            return calcNoAlignmentRotationAxis();
        }

        public void snapObjects()
        {
            snap();
        }

        public void setSurfaceLocked(bool locked)
        {
            _isSurfaceLocked = locked;
        }

        public void clearObjectsIgnoredAsSurface()
        {
            _objectsIgnoredAsSurface.Clear();
        }

        public void addObjectHierarchyIgnoredAsSurface(GameObject gameObject)
        {
            gameObject.getAllChildrenAndSelf(true, true, _gameObjectBuffer);
            _objectsIgnoredAsSurface.AddRange(_gameObjectBuffer);
        }

        public override void onUndoRedo()
        {
            if (isActive)
            {
                removeNullObjects();
                if (!isActive) return;

                ObjectSurfaceSnapTargetParent.create(_targetParents, _surfaceSnapTargetParents);

                if (_surface.surfaceType != ObjectTransformSessionSurface.Type.Invalid)
                {
                    updateSurfaceAnchorDirections();
                    snap();
                }
            }
        }

        public void executeCommand(ObjectSurfaceSnapSessionCommand command)
        {
            if (command.id == ObjectSurfaceSnapSessionCommandId.SetOffsetFromSurface) setAppliedOffsetFromSurface(command.appliedOffsetFromSurface);
            else if (command.id == ObjectSurfaceSnapSessionCommandId.ToggleAxisAlignment)
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled = false;
                sharedSettings.alignAxis = !sharedSettings.alignAxis;

                if (!sharedSettings.alignAxis)
                {
                    foreach (var targetParent in _targetParents)
                        targetParent.resetRotationToOriginal();
                }
                UndoEx.restoreEnabledState();

                PluginInspectorUI.instance.refresh();
            }
        }

        public void setAppliedOffsetFromSurface(float offset)
        {
            if (!isActive) return;

            _appliedOffsetFromSurface = offset;
            updateTotalOffsetFromSurface();
            if (_surface.surfaceType != ObjectTransformSessionSurface.Type.Invalid)
            {
                updateSurfaceAnchorDirections();
                snap();
            }
        }

        public void projectTargetsOnSurface()
        {
            if (_surface.surfaceType != ObjectTransformSessionSurface.Type.Invalid)
                projectTragets();
        }

        public void setTransformMode(TransformMode transformMode)
        {
            if (!isActive) return;

            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Invalid || 
                _transformMode == transformMode) return;

            if (_transformMode == TransformMode.Scale) Mouse.instance.removeDeltaCapture(_mouseDeltaCaptureId);

            _transformMode = transformMode;
            if (_transformMode == TransformMode.Scale)
            {
                _mouseDeltaCaptureId = Mouse.instance.createDeltaCapture(Mouse.instance.position);
                ObjectSurfaceSnapTargetParent.storeLocalScaleSnapshots(_surfaceSnapTargetParents);
            }
            else
            if (_transformMode == TransformMode.OffsetFromAnchor)
            {
                _mouseDeltaCaptureId = Mouse.instance.createDeltaCapture(Mouse.instance.position);
                ObjectSurfaceSnapTargetParent.storeSurfaceAnchorDirectionSnapshots(_surfaceSnapTargetParents);
            }

            updateSurfaceAnchorDirections();
        }

        protected override void update()
        {
            Event e = Event.current;
            if (FixedShortcuts.enableMouseRotateObjects(e))             setTransformMode(TransformMode.Rotate);
            else if (FixedShortcuts.enableMouseScaleObjects(e))         setTransformMode(TransformMode.Scale);
            else if (FixedShortcuts.enableMouseOffsetFromSurface(e))    setTransformMode(TransformMode.OffsetFromSurface);
            else if (FixedShortcuts.enableMouseOffsetFromPoint(e))      setTransformMode(TransformMode.OffsetFromAnchor);
            else if (FixedShortcuts.enableMouseOrbitAroundPoint(e))     setTransformMode(TransformMode.OrbitAroundAnchor);
            else if (FixedShortcuts.enableMouseAdjustAnchor(e))         setTransformMode(TransformMode.AdjustAnchor);
            else setTransformMode(TransformMode.Snap);

            if (!SceneView.lastActiveSceneView.hasFocus)
                SceneView.lastActiveSceneView.Focus();

            if (!Mouse.instance.hasMoved) return;
            if (_transformMode != TransformMode.OrbitAroundAnchor && _transformMode != TransformMode.OffsetFromAnchor) updateSurface();
            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Invalid) return;
            updateTotalOffsetFromSurface();
         
            if (_transformMode == TransformMode.Snap)
            {
                if (_recordTransformsOnSnap)
                {
                    UndoEx.recordGameObjectTransforms(_targetParents);
                    _recordTransformsOnSnap = false;
                }
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
            else
            if (_transformMode == TransformMode.OrbitAroundAnchor)
            {
                UndoEx.recordGameObjectTransforms(_targetParents);
                orbitAroundAnchor();
            }
            else
            if (_transformMode == TransformMode.OffsetFromSurface)
            {
                UndoEx.recordGameObjectTransforms(_targetParents);
                offsetFromSurface();
            }
            else
            if (_transformMode == TransformMode.OffsetFromAnchor)
            {
                UndoEx.recordGameObjectTransforms(_targetParents);
                offsetFromAnchor();
            }
            else
            if (_transformMode == TransformMode.AdjustAnchor)
            {
                updateSurfaceAnchorDirections();
            }
        }

        protected override bool onCanBegin()
        {
            return sharedSettings != null;
        }

        private bool _recordTransformsOnSnap = false;
        protected override bool onBegin()
        {
            bool surfaceReady           = updateSurface();
            if (!surfaceReady) return false;

            _appliedOffsetFromSurface   = 0.0f;
            updateTotalOffsetFromSurface();
            _transformMode              = TransformMode.Snap;
            _surfaceSnapTargetParents.Clear();

            _recordTransformsOnSnap = true;
            ObjectSurfaceSnapTargetParent.create(_targetParents, _surfaceSnapTargetParents);
            if (_surfaceSnapTargetParents.Count == 1 && sharedSettings.snapSingleTargetToCursor) _surfaceSnapTargetParents[0].transform.position = _surface.pickPoint;

            updateSurfaceAnchorDirections();
            snap();

            return true;
        }

        protected override void onEnd()
        {
            _surface.makeInvalid();
            _surfaceSnapTargetParents.Clear();
            _appliedOffsetFromSurface   = 0.0f;
            _totalOffsetFromSurface     = 0.0f;
            _transformMode              = TransformMode.Snap;
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_projectionSettings);
        }

        protected override void drawUIHandles()
        {
            if (!ObjectTransformSessionPrefs.instance.surfaceSnapShowInfoText) return;

            Camera camera                           = PluginCamera.camera;
            Transform cameraTransform               = camera.transform;
            ObjectBounds.QueryConfig boundsQCOnfig  = ObjectBounds.QueryConfig.defaultConfig;

            // Note: Only draw when there's one.
            if (_targetParents.Count == 1)
            {
                Handles.BeginGUI();
                foreach (var targetParent in _targetParents)
                {
                    var obb         = ObjectBounds.calcHierarchyWorldOBB(targetParent, boundsQCOnfig);
                    string label    = targetParent.name + "\n";
                    label           += targetParent.transform.position.ToString("F3");
                    label           += "\n";
                    label           += "Offset: " + _appliedOffsetFromSurface + sharedSettings.implicitOffsetFromSurface;

                    Handles.Label(obb.center - cameraTransform.up * obb.extents.magnitude - cameraTransform.right * 1.8f, label, GUIStyleDb.instance.sceneViewInfoLabel);
                }
                Handles.EndGUI();
            }
        }

        protected override void draw()
        {
            HandlesEx.saveColor();

            var sessionPrefs = ObjectTransformSessionPrefs.instance;
            foreach (var targetParent in _targetParents)
            {
                Vector3 targetPivot = targetParent.transform.position;
                if (sessionPrefs.surfaceSnapDrawAnchorLines)
                {
                    Handles.color = sessionPrefs.surfaceSnapAnchorLineColor;
                    Handles.DrawLine(targetPivot, _surface.pickPoint);
                }

                if (sessionPrefs.surfaceSnapDrawObjectPivotTicks)
                {
                    Handles.color = sessionPrefs.surfaceSnapObjectPivotTickColor;
                    Handles.DotHandleCap(0, targetPivot, Quaternion.identity, HandleUtility.GetHandleSize(targetPivot) * sessionPrefs.surfaceSnapObjectPivotTickSize, EventType.Repaint);
                }

                if (sessionPrefs.surfaceSnapDrawObjectBoxes)
                {
                    OBB worldOBB = ObjectBounds.calcHierarchyWorldOBB(targetParent, _boxRenderQConfig);
                    if (worldOBB.isValid)
                    {
                        HandlesEx.saveMatrix();
                        Handles.color   = sessionPrefs.surfaceSnapObjectBoxWireColor;
                        Handles.matrix  = worldOBB.transformMatrix;
                        //Handles.DrawWireCube(Vector3.zero, Vector3.one);
                        HandlesEx.drawUnitWireCube();
                        HandlesEx.restoreMatrix();
                    }
                }
            }          

            if (sessionPrefs.surfaceSnapDrawAnchorTick)
            {
                Handles.color = sessionPrefs.surfaceSnapAnchorTickColor;
                Handles.DotHandleCap(0, _surface.pickPoint, Quaternion.identity, HandleUtility.GetHandleSize(_surface.pickPoint) * sessionPrefs.surfaceSnapAnchorTickSize, EventType.Repaint);
            }

            HandlesEx.restoreColor();
        }

        private void snap()
        {
            foreach (var parent in _surfaceSnapTargetParents)
                parent.transform.position = (_surface.pickPoint + parent.surfaceAnchorDirection);

            projectTragets();
        }

        private void projectTragets()
        {
            if (sharedSettings.snapMode == ObjectSurfaceSnapMode.Pivot)
            {
                ObjectBounds.QueryConfig boundsQConfig = new ObjectBounds.QueryConfig()
                {
                    volumelessSize      = Vector3.zero,
                    objectTypes         = GameObjectType.All & (~GameObjectType.Terrain),
                    includeInactive     = false,
                    includeInvisible    = false
                };

                foreach (var parent in _surfaceSnapTargetParents)
                {
                    var parentTransform         = parent.transform;
                    parentTransform.position    = _surface.plane.projectPoint(parentTransform.position);
                    if (sharedSettings.alignAxis)
                    {
                        OBB obb = ObjectBounds.calcHierarchyWorldOBB(parent.gameObject, boundsQConfig);
                        if (obb.isValid)
                        {
                            Vector3 alignmentAxis = parentTransform.flexiToLocalAxis(obb, sharedSettings.alignmentAxis, sharedSettings.invertAlignmentAxis);
                            parentTransform.alignAxis(alignmentAxis, _surface.plane.normal, obb.center);
                        }
                    }
                    parentTransform.position += _surface.plane.normal * _totalOffsetFromSurface;

                    Plane projectionPlane = _surface.plane;
                    if (isSurfaceTerrain)
                    {
                        if (!sharedSettings.alignAxis)
                        {
                            if (isSurfaceUnityTerrain) projectionPlane = new Plane(Vector3.up, parentTransform.position);
                            else projectionPlane = new Plane(ObjectPrefs.instance.getTerrainMeshUp(_surface.surfaceObject), parentTransform.position);
                        }
                    }

                    parent.projectionResult = new ObjectProjectionResult()
                    {
                        wasProjected = true,
                        projectionPlane = projectionPlane,
                        projectedPosition = parent.transform.position,
                    };
                }
            }
            else
            {
                projectionSettings.alignAxis            = sharedSettings.alignAxis;
                projectionSettings.alignmentAxis        = sharedSettings.alignmentAxis;
                projectionSettings.invertAlignmentAxis  = sharedSettings.invertAlignmentAxis;
                projectionSettings.projectAsUnit        = false;
                projectionSettings.inFrontOffset        = _totalOffsetFromSurface;
                projectionSettings.embedInSurface       = sharedSettings.embedInSurface;

                if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Grid)
                {
                    foreach (var targetParent in _surfaceSnapTargetParents)
                        targetParent.projectionResult = ObjectProjection.projectHierarchyOnPlane(targetParent.gameObject, _surface.plane, projectionSettings);
                }
                else
                if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Object)
                {
                    foreach (var targetParent in _surfaceSnapTargetParents)
                        targetParent.projectionResult = ObjectProjection.projectHierarchyOnObject(targetParent.gameObject, _surface.surfaceObject, _surface.objectType, _surface.plane, projectionSettings);
                }
            }

            ObjectEvents.onObjectsTransformed();
        }

        private void scale()
        {
            if (!Mouse.instance.hasMoved || !FixedShortcuts.checkMouseScaleDelay()) return;

            foreach (var targetParent in _surfaceSnapTargetParents)
            {
                if (!targetParent.projectionResult.wasProjected) continue;
                float scaleFactor   = 1.0f + Mouse.instance.deltaFromCapture(_mouseDeltaCaptureId).x * InputPrefs.instance.mouseScaleSensitivity;
                Vector3 newScale    = targetParent.localScaleSnapshot * scaleFactor;
                targetParent.transform.setLocalScaleFromPivot(newScale, targetParent.projectionResult.projectedPosition + targetParent.projectionResult.projectionPlane.normal * _totalOffsetFromSurface);
            }
            updateSurfaceAnchorDirections();
        }

        private void rotate()
        {
            if (!Mouse.instance.hasMoved || !FixedShortcuts.checkMouseRotateDelay()) return;

            float rotationAmount        = Mouse.instance.delta.x * InputPrefs.instance.mouseRotationSensitivity;
            Vector3 noAlignRotationAxis = calcNoAlignmentRotationAxis();
            foreach (var targetParent in _surfaceSnapTargetParents)
            {
                if (!targetParent.projectionResult.wasProjected) continue;
                if (sharedSettings.alignAxis) targetParent.transform.Rotate(targetParent.projectionResult.projectionPlane.normal, rotationAmount, Space.World);
                else targetParent.transform.Rotate(noAlignRotationAxis, rotationAmount, Space.World);
            }

            updateSurfaceAnchorDirections();
        }

        private void orbitAroundAnchor()
        {
            if (!Mouse.instance.hasMoved || !FixedShortcuts.checkMouseOrbitDelay()) return;

            float rotationAmount    = Mouse.instance.delta.x * InputPrefs.instance.mouseRotationSensitivity;
            Quaternion rotation     = sharedSettings.alignAxis ? Quaternion.AngleAxis(rotationAmount, _surface.pickPointNormal) : Quaternion.AngleAxis(rotationAmount, calcNoAlignmentRotationAxis());
            foreach (var targetParent in _surfaceSnapTargetParents)
            {
                targetParent.surfaceAnchorDirection = rotation * targetParent.surfaceAnchorDirection;
                targetParent.transform.rotateAround(rotation, _surface.pickPoint);
            }

            snap();
        }

        private void offsetFromSurface()
        {
            if (!Mouse.instance.hasMoved || !FixedShortcuts.checkMouseOffsetFromSurfaceDelay()) return;

            float offsetAmount          = Mouse.instance.delta.x * InputPrefs.instance.mouseOffsetSensitivity;
            _appliedOffsetFromSurface   += offsetAmount;

            foreach (var targetParent in _surfaceSnapTargetParents)
            {
                if (!targetParent.projectionResult.wasProjected) continue;
                targetParent.transform.position += targetParent.projectionResult.projectionPlane.normal * offsetAmount;
            }

            updateSurfaceAnchorDirections();
        }

        private void offsetFromAnchor()
        {
            if (!Mouse.instance.hasMoved || !FixedShortcuts.checkMouseOffsetFromPointDelay()) return;

            float scaleFactor = 1.0f + Mouse.instance.deltaFromCapture(_mouseDeltaCaptureId).x * InputPrefs.instance.mouseOffsetSensitivity;
            foreach (var targetParent in _surfaceSnapTargetParents)
            {
                if (!targetParent.projectionResult.wasProjected) continue;
                targetParent.transform.position = (_surface.pickPoint + targetParent.surfaceAnchorDirectionSnapshot * scaleFactor);
            }

            updateSurfaceAnchorDirections();
            snap();
        }

        private Vector3 calcNoAlignmentRotationAxis()
        {
            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Object)
            {
                if (_surface.objectType == GameObjectType.Mesh && _surface.surfaceObject.isTerrainMesh())
                    return ObjectPrefs.instance.getTerrainMeshUp(_surface.surfaceObject);
                else if (_surface.objectType == GameObjectType.Terrain) return _surface.surfaceObject.transform.up;
                else return PluginScene.instance.grid.up;
            }
            else
            if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Grid) return PluginScene.instance.grid.up;
            else return Vector3.up;
        }

        private bool updateSurface()
        {
            if (_isSurfaceLocked && _surface.surfaceType != ObjectTransformSessionSurface.Type.Invalid)
                return _surface.pickFromSelf();

            _surfacePickRaycastFilter.raycastGrid       = sharedSettings.allowsGridSurface;
            _surfacePickRaycastFilter.raycastObjects    = sharedSettings.allowsObjectSurface;
            _surfacePickRaycastFilter.objectTypes       = GameObjectType.None;
            if (sharedSettings.allowsMeshSurface)       _surfacePickRaycastFilter.objectTypes |= GameObjectType.Mesh;
            if (sharedSettings.allowsTerrainSurface)    _surfacePickRaycastFilter.objectTypes |= GameObjectType.Terrain;
            if (sharedSettings.allowsSpriteSurface)     _surfacePickRaycastFilter.objectTypes |= GameObjectType.Sprite;
            _surfacePickRaycastFilter.setIgnoredObjects(_allTargetObjects);
            if (_objectsIgnoredAsSurface.Count != 0)    _surfacePickRaycastFilter.addIgnoredObjects(_objectsIgnoredAsSurface);

            _surfacePickRaycastFilter.clearPrimeFocusObjects();
            _surfacePickRaycastFilter.usePrimeFocusObjects = false;

            if (_transformMode != TransformMode.Snap)
            {
                if (_surface.surfaceObject != null && _surface.surfaceType != ObjectTransformSessionSurface.Type.Grid)
                {
                    _surfacePickRaycastFilter.usePrimeFocusObjects  = true;
                    _surfacePickRaycastFilter.raycastGrid           = false;
                    _surfacePickRaycastFilter.setPrimeFocusObject(_surface.surfaceObject);
                }
                else
                if (_surface.surfaceType == ObjectTransformSessionSurface.Type.Grid)
                {
                    _surfacePickRaycastFilter.raycastObjects        = false;
                }
            }

            _surface.pickFromScene(_surfacePickRaycastFilter);
            return _surface.surfaceType != ObjectTransformSessionSurface.Type.Invalid;
        }

        private void updateSurfaceAnchorDirections()
        {
            if (_surfaceSnapTargetParents.Count > 1 || !sharedSettings.snapSingleTargetToCursor)
                ObjectSurfaceSnapTargetParent.updateSurfaceAnchorDirections(_surfaceSnapTargetParents, _surface.pickPoint);
        }

        private void updateTotalOffsetFromSurface()
        {
            _totalOffsetFromSurface = _appliedOffsetFromSurface + sharedSettings.implicitOffsetFromSurface;
        }
    }
}
#endif