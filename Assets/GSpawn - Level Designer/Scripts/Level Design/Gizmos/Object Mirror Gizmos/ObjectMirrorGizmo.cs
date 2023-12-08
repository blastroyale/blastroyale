#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class MirroredObjectList
    {
        public List<GameObject> objects = new List<GameObject>();
    }

    public enum MirrorGizmoMidSnapMode
    {
        Default = 0,
        TileRuleGrid
    }

    public class ObjectMirrorGizmo : PluginGizmo
    {
        private enum PlanePatchId
        {
            TopLeft = 0,
            TopRight,
            BottomRight,
            BottomLeft
        }

        private class MirrorPlanePatch
        {
            public XYQuad3D     quad = new XYQuad3D();
            public Color        fillColor;
        }

        private class MirrorPlane
        {
            public PlaneId              id;
            public MirrorPlanePatch[]   patches         = new MirrorPlanePatch[4]
            { new MirrorPlanePatch(), new MirrorPlanePatch(), new MirrorPlanePatch(), new MirrorPlanePatch() };

            public Quaternion           modelRotation   = Quaternion.identity;
            public Vector3              modelNormal     = Vector3.zero;
        }

        private Color[]                 _moveAxisColors         = new Color[] { DefaultSystemValues.xAxisColor, DefaultSystemValues.yAxisColor, DefaultSystemValues.zAxisColor };
        private Vector3[]               _modelMoveDirs          = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward };
        private Color[]                 _planeFillColors        = new Color[3];
        private bool[]                  _planeMask              = new bool[3];

        private Color[]                 _rotationHandleColors   = new Color[] { DefaultSystemValues.xAxisColor, DefaultSystemValues.yAxisColor, DefaultSystemValues.zAxisColor };
        private Vector3[]               _rotationAxes           = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward };

        // 1-to-1 mapping with PlanePatchId (in pairs of 2)
        private Vector3[]               _patchCalcAxes          = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.up, Vector3.right, Vector3.down, Vector3.left, Vector3.down };

        // 1-to-1 mapping with PlaneId
        private MirrorPlane[]           _mirrorPlanes           = new MirrorPlane[]
        {
            new MirrorPlane(){ id = PlaneId.XY, modelRotation = Quaternion.identity, modelNormal = Vector3.forward },
            new MirrorPlane(){ id = PlaneId.YZ, modelRotation = Quaternion.AngleAxis(90.0f, Vector3.up), modelNormal = Vector3.right },
            new MirrorPlane(){ id = PlaneId.ZX, modelRotation = Quaternion.AngleAxis(90.0f, Vector3.right), modelNormal = Vector3.up }
        };
        private List<MirrorPlanePatch> _sortedPatches           = new List<MirrorPlanePatch>();

        private SceneRaycastFilter          _snapToObjectsRaycastFilter = new SceneRaycastFilter()
        { objectTypes = GameObjectType.Mesh | GameObjectType.Terrain | GameObjectType.Sprite };
        private ObjectRaycastConfig         _snapToObjectsRaycastConfig = new ObjectRaycastConfig()
        { raycastPrecision = ObjectRaycastPrecision.BestFit };
        private ObjectBounds.QueryConfig    _mirroredBoundsQConfig      = ObjectBounds.QueryConfig.defaultConfig;

        private int                         _snapHandleId;
        private int                         _xAxisMoveHandleId;
        private int                         _yAxisMoveHandleId;
        private int                         _zAxisMoveHandleId;
        private int                         _xAxisRotationHandleId;
        private int                         _yAxisRotationHandleId;
        private int                         _zAxisRotationHandleId;

        [SerializeField]
        private MirrorGizmoMidSnapMode      _midSnapMode        = MirrorGizmoMidSnapMode.Default;

        [NonSerialized]
        private List<Vector3>               _vector3Buffer      = new List<Vector3>(5);
        [NonSerialized]
        private List<Plane>                 _mirrorPlaneBuffer      = new List<Plane>(3);
        [NonSerialized]
        private List<PlaneId>               _mirrorPlaneIdBuffer    = new List<PlaneId>(3);
        [NonSerialized]
        private List<OBB>                   _obbBuffer          = new List<OBB>(8);
        [NonSerialized]
        private List<Vector3Int>            _vec3IntBuffer      = new List<Vector3Int>();
        [NonSerialized]
        private List<TileRuleGridCellRange>     _tileRuleGridCellRangeBuffer    = new List<TileRuleGridCellRange>();
        [NonSerialized]
        private List<TileRuleConnectionPath>    _tileRuleConnectionPathBuffer   = new List<TileRuleConnectionPath>();
        [NonSerialized]
        private List<Symmetry.MirroredOBB>  _mirroredOBBBuffer  = new List<Symmetry.MirroredOBB>(8);
        [NonSerialized]
        private IEnumerable<GameObject>     _targetObjects      = null;
        [NonSerialized]
        private List<GameObject>            _parentBuffer       = new List<GameObject>();
        [NonSerialized]
        private ObjectMirrorGizmoSettings   _sharedSettings;

        [NonSerialized]
        private ObjectOverlapFilter         _symPairsOverlapFilter          = new ObjectOverlapFilter();
        [NonSerialized]
        private List<GameObject>            _symPairsObjectOverlapBuffer    = new List<GameObject>();
        [NonSerialized]
        private ObjectOutline               _symPairsOutline                = new ObjectOutline();
        

        public ObjectMirrorGizmoSettings    sharedSettings          { get { return _sharedSettings; } set { _sharedSettings = value; } }
        public bool                         isDraggingHandles 
        {
            get 
            { 
                int hotControl = GUIUtility.hotControl;
                if (hotControl == 0) return false;

                return hotControl == _snapHandleId || hotControl == _xAxisMoveHandleId || hotControl == _yAxisMoveHandleId || hotControl == _zAxisMoveHandleId ||
                    hotControl == _xAxisRotationHandleId || hotControl == _yAxisRotationHandleId || hotControl == _zAxisRotationHandleId;
            } 
        }
        public MirrorGizmoMidSnapMode       midSnapMode             { get { return _midSnapMode; } set { _midSnapMode = value; } }
        public TileRuleGrid                 tileRuleGrid            { get; set; }
        public int                          tileRuleGridYOffset     { get; set; }

        public void snapToView()
        {
            Ray ray = new Ray(PluginCamera.camera.transform.position, PluginCamera.camera.transform.forward);
            var hit = PluginScene.instance.raycastGrid(ray);
            if (hit != null)
            {
                UndoEx.record(this);
                position = PluginScene.instance.grid.snapAllAxes(hit.hitPoint);
            }
            else
            {
                UndoEx.record(this);
                Sphere sphere = new Sphere(position, getRotationHandleSize());
                position = PluginCamera.camera.calcSphereCenterInFrontOfCamera(sphere);
            }
        }

        public void bindTargetObjects(IEnumerable<GameObject> targetObjects)
        {
            _targetObjects = targetObjects;
        }

        public void mirrorTargets()
        {
            mirrorObjects(_targetObjects);
        }

        public void mirrorObjectsOrganized_NoDuplicateCommand(IEnumerable<GameObject> gameObjects, List<MirroredObjectList> mirroredObjectLists)
        {
            mirroredObjectLists.Clear();
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer)) return;
            if (_mirrorPlaneBuffer.Count == 0) return;

            if (_mirrorPlaneBuffer.Count == 1)
            {
                mirroredObjectLists.Add(new MirroredObjectList());
            }
            else
            if (_mirrorPlaneBuffer.Count == 2)
            {
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
            }
            else
            if (_mirrorPlaneBuffer.Count == 3)
            {
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
            }

            GameObjectEx.getParents(gameObjects, _parentBuffer);
            bool mirrorRotation = sharedSettings.mirrorRotation;
            bool mirrorSpanning = sharedSettings.mirrorSpanning;
            foreach (var parent in _parentBuffer)
            {
                _mirroredOBBBuffer.Clear();
                OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(parent, _mirroredBoundsQConfig);
                if (!hierarchyWorldOBB.isValid) continue;

                _mirroredOBBBuffer.Add(new Symmetry.MirroredOBB() { obb = hierarchyWorldOBB, mirroredRotation = new Symmetry.MirroredRotation() { rotation = parent.transform.rotation, axesScaleSign = Vector3.one } });
                for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
                {
                    if (!mirrorSpanning)
                    {
                        var location = Box3D.classifyAgainstPlane(hierarchyWorldOBB.center, hierarchyWorldOBB.size, hierarchyWorldOBB.rotation, _mirrorPlaneBuffer[planeIndex]);
                        if (location == PlaneClassifyResult.Spanning)
                        {
                            _mirroredOBBBuffer.Add(new Symmetry.MirroredOBB(OBB.getInvalid(), new Symmetry.MirroredRotation()));
                            continue;
                        }
                    }

                    int numOBBs = _mirroredOBBBuffer.Count;
                    for (int obbIndex = 0; obbIndex < numOBBs; ++obbIndex)
                    {
                        if (_mirroredOBBBuffer[obbIndex].obb.isValid)
                        {
                            Symmetry.MirroredOBB mirroredOBB = Symmetry.mirrorOBB(_mirroredOBBBuffer[obbIndex], mirrorRotation, _mirrorPlaneBuffer[planeIndex]);
                            _mirroredOBBBuffer.Add(mirroredOBB);
                        }
                        else _mirroredOBBBuffer.Add(_mirroredOBBBuffer[obbIndex]);
                    }
                }

                if (_mirroredOBBBuffer.Count != 0)
                {
                    int listIndex   = 0;
                    int startOBB    = 1;
                    for (int obbIndex = startOBB; obbIndex < _mirroredOBBBuffer.Count; ++obbIndex)
                    {
                        Symmetry.MirroredOBB mirroredOBB = _mirroredOBBBuffer[obbIndex];
                        if (!mirroredOBB.obb.isValid)
                        {
                            ++listIndex;
                            continue;
                        }

                        GameObject prefabAsset = parent.getOutermostPrefabAsset();
                        if (prefabAsset == null)
                        {
                            ++listIndex;
                            continue;
                        }

                        GameObject mirroredObject = prefabAsset.instantiatePrefab(parent.transform.position, parent.transform.rotation, parent.transform.lossyScale);
                        mirroredObject.transform.parent = parent.transform.parent;

                        UndoEx.registerCreatedObject<GameObject>(mirroredObject);
                        mirroredObjectLists[listIndex++].objects.Add(mirroredObject);

                        Transform transform = mirroredObject.transform;
                        UndoEx.recordTransform(transform);
                        if (mirrorRotation)
                        {
                            transform.rotation = mirroredOBB.mirroredRotation.rotation;
                            transform.setWorldScale(Vector3.Scale(transform.lossyScale, mirroredOBB.mirroredRotation.axesScaleSign));
                        }
                        transform.position = ObjectPositionCalculator.calcRootPosition(mirroredObject, mirroredOBB.obb.center, transform.lossyScale, transform.rotation);
                    }
                }
            }
        }

        public void mirrorObjectsOrganized(IEnumerable<GameObject> gameObjects, List<MirroredObjectList> mirroredObjectLists)
        {
            mirroredObjectLists.Clear();
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer)) return;
            if (_mirrorPlaneBuffer.Count == 0) return;

            if (_mirrorPlaneBuffer.Count == 1)
            {
                mirroredObjectLists.Add(new MirroredObjectList());
            }
            else
            if (_mirrorPlaneBuffer.Count == 2)
            {
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
            }
            else
            if (_mirrorPlaneBuffer.Count == 3)
            {
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
                mirroredObjectLists.Add(new MirroredObjectList());
            }

            GameObjectEx.getParents(gameObjects, _parentBuffer);
            bool mirrorRotation = sharedSettings.mirrorRotation;
            bool mirrorSpanning = sharedSettings.mirrorSpanning;
            foreach (var parent in _parentBuffer)
            {
                _mirroredOBBBuffer.Clear();
                OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(parent, _mirroredBoundsQConfig);
                if (!hierarchyWorldOBB.isValid) continue;

                _mirroredOBBBuffer.Add(new Symmetry.MirroredOBB() { obb = hierarchyWorldOBB, mirroredRotation = new Symmetry.MirroredRotation() { rotation = parent.transform.rotation, axesScaleSign = Vector3.one } });
                for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
                {
                    if (!mirrorSpanning)
                    {
                        var location = Box3D.classifyAgainstPlane(hierarchyWorldOBB.center, hierarchyWorldOBB.size, hierarchyWorldOBB.rotation, _mirrorPlaneBuffer[planeIndex]);
                        if (location == PlaneClassifyResult.Spanning)
                        {
                            _mirroredOBBBuffer.Add(new Symmetry.MirroredOBB(OBB.getInvalid(), new Symmetry.MirroredRotation()));
                            continue;
                        }
                    }

                    int numOBBs = _mirroredOBBBuffer.Count;
                    for (int obbIndex = 0; obbIndex < numOBBs; ++obbIndex)
                    {
                        if (_mirroredOBBBuffer[obbIndex].obb.isValid)
                        {
                            Symmetry.MirroredOBB mirroredOBB = Symmetry.mirrorOBB(_mirroredOBBBuffer[obbIndex], mirrorRotation, _mirrorPlaneBuffer[planeIndex]);
                            _mirroredOBBBuffer.Add(mirroredOBB);
                        }
                        else _mirroredOBBBuffer.Add(_mirroredOBBBuffer[obbIndex]);
                    }
                }

                if (_mirroredOBBBuffer.Count != 0)
                {
                    int listIndex   = 0;
                    int startOBB    = 1;
                    for (int obbIndex = startOBB; obbIndex < _mirroredOBBBuffer.Count; ++obbIndex)
                    {
                        Symmetry.MirroredOBB mirroredOBB = _mirroredOBBBuffer[obbIndex];
                        if (!mirroredOBB.obb.isValid)
                        {
                            ++listIndex;
                            continue;
                        }

                        GameObject prefabAsset = parent.getOutermostPrefabAsset();
                        if (prefabAsset == null)
                        {
                            ++listIndex;
                            continue;
                        }

                        GameObject mirroredObject = UnityEditorCommands.duplicate(parent);
                        mirroredObjectLists[listIndex++].objects.Add(mirroredObject);
                    
                        Transform transform = mirroredObject.transform;
                        UndoEx.recordTransform(transform);
                        if (mirrorRotation)
                        {
                            transform.rotation = mirroredOBB.mirroredRotation.rotation;
                            transform.setWorldScale(Vector3.Scale(transform.lossyScale, mirroredOBB.mirroredRotation.axesScaleSign));
                        }
                        transform.position = ObjectPositionCalculator.calcRootPosition(mirroredObject, mirroredOBB.obb.center, transform.lossyScale, transform.rotation);
                    }
                }
            }
        }

        public void mirrorObjects(IEnumerable<GameObject> gameObjects)
        {
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer)) return;

            GameObjectEx.getParents(gameObjects, _parentBuffer);
            bool mirrorRotation = sharedSettings.mirrorRotation;
            bool mirrorSpanning = sharedSettings.mirrorSpanning;
            foreach (var parent in _parentBuffer)
            {
                _mirroredOBBBuffer.Clear();
                OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(parent, _mirroredBoundsQConfig);
                if (!hierarchyWorldOBB.isValid) continue;

                _mirroredOBBBuffer.Add(new Symmetry.MirroredOBB() { obb = hierarchyWorldOBB, mirroredRotation = new Symmetry.MirroredRotation() { rotation = parent.transform.rotation, axesScaleSign = Vector3.one } });
                for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
                {
                    if (!mirrorSpanning)
                    {
                        var location = Box3D.classifyAgainstPlane(hierarchyWorldOBB.center, hierarchyWorldOBB.size, hierarchyWorldOBB.rotation, _mirrorPlaneBuffer[planeIndex]);
                        if (location == PlaneClassifyResult.Spanning) continue;
                    }

                    int numOBBs = _mirroredOBBBuffer.Count;
                    for (int obbIndex = 0; obbIndex < numOBBs; ++obbIndex)
                    {
                        Symmetry.MirroredOBB mirroredOBB = Symmetry.mirrorOBB(_mirroredOBBBuffer[obbIndex], mirrorRotation, _mirrorPlaneBuffer[planeIndex]);
                        _mirroredOBBBuffer.Add(mirroredOBB);
                    }
                }
             
                if (_mirroredOBBBuffer.Count != 0)
                {
                    int startOBB = 1;
                    for (int obbIndex = startOBB; obbIndex < _mirroredOBBBuffer.Count; ++obbIndex)
                    {
                        Symmetry.MirroredOBB mirroredOBB = _mirroredOBBBuffer[obbIndex];
                        GameObject mirroredObject = UnityEditorCommands.duplicate(parent);

                        Transform transform = mirroredObject.transform;
                        UndoEx.recordTransform(transform);
                        if (mirrorRotation)
                        {
                            transform.rotation = mirroredOBB.mirroredRotation.rotation;
                            transform.setWorldScale(Vector3.Scale(transform.lossyScale, mirroredOBB.mirroredRotation.axesScaleSign));
                        }
                        transform.position = ObjectPositionCalculator.calcRootPosition(mirroredObject, mirroredOBB.obb.center, transform.lossyScale, transform.rotation);
                    }
                }
            }
        }

        public void mirrorObjects(IEnumerable<GameObject> gameObjects, MirroredObjectList mirroredObjectList, bool append)
        {
            if (!append) mirroredObjectList.objects.Clear();
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer)) return;

            GameObjectEx.getParents(gameObjects, _parentBuffer);
            bool mirrorRotation = sharedSettings.mirrorRotation;
            bool mirrorSpanning = sharedSettings.mirrorSpanning;
            foreach (var parent in _parentBuffer)
            {
                _mirroredOBBBuffer.Clear();
                OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(parent, _mirroredBoundsQConfig);
                if (!hierarchyWorldOBB.isValid) continue;

                _mirroredOBBBuffer.Add(new Symmetry.MirroredOBB() { obb = hierarchyWorldOBB, mirroredRotation = new Symmetry.MirroredRotation() { rotation = parent.transform.rotation, axesScaleSign = Vector3.one } });
                for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
                {
                    if (!mirrorSpanning)
                    {
                        var location = Box3D.classifyAgainstPlane(hierarchyWorldOBB.center, hierarchyWorldOBB.size, hierarchyWorldOBB.rotation, _mirrorPlaneBuffer[planeIndex]);
                        if (location == PlaneClassifyResult.Spanning) continue;
                    }

                    int numOBBs = _mirroredOBBBuffer.Count;
                    for (int obbIndex = 0; obbIndex < numOBBs; ++obbIndex)
                    {
                        Symmetry.MirroredOBB mirroredOBB = Symmetry.mirrorOBB(_mirroredOBBBuffer[obbIndex], mirrorRotation, _mirrorPlaneBuffer[planeIndex]);
                        _mirroredOBBBuffer.Add(mirroredOBB);
                    }
                }

                if (_mirroredOBBBuffer.Count != 0)
                {
                    int startOBB = 1;
                    for (int obbIndex = startOBB; obbIndex < _mirroredOBBBuffer.Count; ++obbIndex)
                    {
                        Symmetry.MirroredOBB mirroredOBB = _mirroredOBBBuffer[obbIndex];
                        GameObject mirroredObject = UnityEditorCommands.duplicate(parent);
                        mirroredObjectList.objects.Add(mirroredObject);
         
                        Transform transform = mirroredObject.transform;
                        UndoEx.recordTransform(transform);
                        if (mirrorRotation)
                        {
                            transform.rotation = mirroredOBB.mirroredRotation.rotation;
                            transform.setWorldScale(Vector3.Scale(transform.lossyScale, mirroredOBB.mirroredRotation.axesScaleSign));
                        }
                        transform.position = ObjectPositionCalculator.calcRootPosition(mirroredObject, mirroredOBB.obb.center, transform.lossyScale, transform.rotation);
                    }
                }
            }
        }

        public void mirrorObjects_NoDuplicateCommand(IEnumerable<GameObject> gameObjects, MirroredObjectList mirroredObjectList, bool append)
        {
            if (!append) mirroredObjectList.objects.Clear();
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer)) return;

            GameObjectEx.getParents(gameObjects, _parentBuffer);
            bool mirrorRotation = sharedSettings.mirrorRotation;
            bool mirrorSpanning = sharedSettings.mirrorSpanning;
            foreach (var parent in _parentBuffer)
            {
                _mirroredOBBBuffer.Clear();
                OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(parent, _mirroredBoundsQConfig);
                if (!hierarchyWorldOBB.isValid) continue;

                _mirroredOBBBuffer.Add(new Symmetry.MirroredOBB() { obb = hierarchyWorldOBB, mirroredRotation = new Symmetry.MirroredRotation() { rotation = parent.transform.rotation, axesScaleSign = Vector3.one } });
                for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
                {
                    if (!mirrorSpanning)
                    {
                        var location = Box3D.classifyAgainstPlane(hierarchyWorldOBB.center, hierarchyWorldOBB.size, hierarchyWorldOBB.rotation, _mirrorPlaneBuffer[planeIndex]);
                        if (location == PlaneClassifyResult.Spanning) continue;
                    }

                    int numOBBs = _mirroredOBBBuffer.Count;
                    for (int obbIndex = 0; obbIndex < numOBBs; ++obbIndex)
                    {
                        Symmetry.MirroredOBB mirroredOBB = Symmetry.mirrorOBB(_mirroredOBBBuffer[obbIndex], mirrorRotation, _mirrorPlaneBuffer[planeIndex]);
                        _mirroredOBBBuffer.Add(mirroredOBB);
                    }
                }

                if (_mirroredOBBBuffer.Count != 0)
                {
                    int startOBB = 1;
                    for (int obbIndex = startOBB; obbIndex < _mirroredOBBBuffer.Count; ++obbIndex)
                    {
                        GameObject prefabAsset = parent.getOutermostPrefabAsset();
                        if (prefabAsset == null) continue;

                        Symmetry.MirroredOBB mirroredOBB    = _mirroredOBBBuffer[obbIndex];
                        GameObject mirroredObject           = prefabAsset.instantiatePrefab(parent.transform.position, parent.transform.rotation, parent.transform.lossyScale);
                        mirroredObject.transform.parent     = parent.transform.parent;

                         UndoEx.registerCreatedObject<GameObject>(mirroredObject);
                        mirroredObjectList.objects.Add(mirroredObject);

                        Transform transform = mirroredObject.transform;
                        UndoEx.recordTransform(transform);
                        if (mirrorRotation)
                        {
                            transform.rotation = mirroredOBB.mirroredRotation.rotation;
                            transform.setWorldScale(Vector3.Scale(transform.lossyScale, mirroredOBB.mirroredRotation.axesScaleSign));
                        }
                        transform.position = ObjectPositionCalculator.calcRootPosition(mirroredObject, mirroredOBB.obb.center, transform.lossyScale, transform.rotation);
                    }
                }
            }
        }

        public void mirrorObject_NoDuplicateCommand(GameObject gameObject, GameObject prefabAsset)
        {
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer)) return;

            bool mirrorRotation = sharedSettings.mirrorRotation;
            bool mirrorSpanning = sharedSettings.mirrorSpanning;

            _mirroredOBBBuffer.Clear();
            OBB hierarchyWorldOBB = ObjectBounds.calcHierarchyWorldOBB(gameObject, _mirroredBoundsQConfig);
            if (!hierarchyWorldOBB.isValid) return;

            _mirroredOBBBuffer.Add(new Symmetry.MirroredOBB() { obb = hierarchyWorldOBB, mirroredRotation = new Symmetry.MirroredRotation() { rotation = gameObject.transform.rotation, axesScaleSign = Vector3.one } });
            for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
            {
                if (!mirrorSpanning)
                {
                    var location = Box3D.classifyAgainstPlane(hierarchyWorldOBB.center, hierarchyWorldOBB.size, hierarchyWorldOBB.rotation, _mirrorPlaneBuffer[planeIndex]);
                    if (location == PlaneClassifyResult.Spanning) continue;
                }

                int numOBBs = _mirroredOBBBuffer.Count;
                for (int obbIndex = 0; obbIndex < numOBBs; ++obbIndex)
                {
                    Symmetry.MirroredOBB mirroredOBB = Symmetry.mirrorOBB(_mirroredOBBBuffer[obbIndex], mirrorRotation, _mirrorPlaneBuffer[planeIndex]);
                    _mirroredOBBBuffer.Add(mirroredOBB);
                }
            }

            if (_mirroredOBBBuffer.Count != 0)
            {
                int startOBB = 1;
                Transform sourceTransform = gameObject.transform;
                for (int obbIndex = startOBB; obbIndex < _mirroredOBBBuffer.Count; ++obbIndex)
                {
                    Symmetry.MirroredOBB mirroredOBB = _mirroredOBBBuffer[obbIndex];

                    GameObject mirroredObject = prefabAsset.instantiatePrefab(sourceTransform.position, sourceTransform.rotation, sourceTransform.lossyScale);
                    mirroredObject.transform.parent = sourceTransform.parent;

                    UndoEx.registerCreatedObject<GameObject>(mirroredObject);

                    // Note: Need to record transform for Undo/Redo. If we don't do this,
                    //       the transform will not be restored correctly on Redo (e.g. mirroring
                    //       while modular snap spawn mode is enabled).
                    Transform transform = mirroredObject.transform;
                    UndoEx.recordTransform(transform);
                    if (mirrorRotation)
                    {
                        transform.rotation = mirroredOBB.mirroredRotation.rotation;
                        transform.setWorldScale(Vector3.Scale(transform.lossyScale, mirroredOBB.mirroredRotation.axesScaleSign));
                    }
                    transform.position = ObjectPositionCalculator.calcRootPosition(mirroredObject, mirroredOBB.obb.center, transform.lossyScale, transform.rotation);
                }
            }
        }

        public void mirrorOBBs(List<OBB> obbs, List<OBB> mirroredOBBs)
        {
            mirroredOBBs.Clear();
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer)) return;

            bool mirrorRotation = sharedSettings.mirrorRotation;
            bool mirrorSpanning = sharedSettings.mirrorSpanning;
            foreach (var obb in obbs)
            {
                _obbBuffer.Clear();
                if (!obb.isValid) continue;

                _obbBuffer.Add(obb);
                for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
                {
                    if (!mirrorSpanning)
                    {
                        var location = Box3D.classifyAgainstPlane(obb.center, obb.size, obb.rotation, _mirrorPlaneBuffer[planeIndex]);
                        if (location == PlaneClassifyResult.Spanning) continue;
                    }

                    int numOBBs = _obbBuffer.Count;
                    for (int obbIndex = 0; obbIndex < numOBBs; ++obbIndex)
                    {
                        OBB mirroredOBB = Symmetry.mirrorOBB(_obbBuffer[obbIndex], mirrorRotation, _mirrorPlaneBuffer[planeIndex]);
                        _obbBuffer.Add(mirroredOBB);
                    }
                }

                if (_obbBuffer.Count != 0)
                {
                    int startOBB = 1;
                    for (int obbIndex = startOBB; obbIndex < _obbBuffer.Count; ++obbIndex)
                    {
                        mirroredOBBs.Add(_obbBuffer[obbIndex]);
                    }
                }
            }
        }

        public void mirrorOBB(OBB obb, List<OBB> mirroredOBBs)
        {
            mirroredOBBs.Clear();
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer)) return;

            bool mirrorRotation = sharedSettings.mirrorRotation;
            bool mirrorSpanning = sharedSettings.mirrorSpanning;
                 
            _obbBuffer.Clear();
            if (!obb.isValid) return;
         
            _obbBuffer.Add(obb);
            for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
            {
                if (!mirrorSpanning)
                {
                    var location = Box3D.classifyAgainstPlane(obb.center, obb.size, obb.rotation, _mirrorPlaneBuffer[planeIndex]);
                    if (location == PlaneClassifyResult.Spanning) continue;
                }

                int numOBBs = _obbBuffer.Count;
                for (int obbIndex = 0; obbIndex < numOBBs; ++obbIndex)
                {
                    OBB mirroredOBB = Symmetry.mirrorOBB(_obbBuffer[obbIndex], mirrorRotation, _mirrorPlaneBuffer[planeIndex]);
                    _obbBuffer.Add(mirroredOBB);
                }
            }

            if (_obbBuffer.Count != 0)
            {
                int startOBB = 1;
                for (int obbIndex = startOBB; obbIndex < _obbBuffer.Count; ++obbIndex)
                {
                    mirroredOBBs.Add(_obbBuffer[obbIndex]);
                }
            }
        }

        public void mirrorTileRuleGridCellRange(TileRuleGridCellRange cellRange, List<TileRuleGridCellRange> mirroredRanges)
        {
            mirroredRanges.Clear();
            if (tileRuleGrid == null) return;
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer, _mirrorPlaneIdBuffer)) return;

            Vector3Int mirrorCellCoords = tileRuleGrid.mirrorGizmoCellCoords;

            _tileRuleGridCellRangeBuffer.Clear();
            _tileRuleGridCellRangeBuffer.Add(cellRange);

            for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
            {
                int numRanges = _tileRuleGridCellRangeBuffer.Count;
                for (int rangeIndex = 0; rangeIndex < numRanges; ++rangeIndex)
                {
                    var range = _tileRuleGridCellRangeBuffer[rangeIndex];
                    var mirroredRange = new TileRuleGridCellRange(Symmetry.mirror3DGridCell(range.min, mirrorCellCoords, _mirrorPlaneIdBuffer[planeIndex]),
                        Symmetry.mirror3DGridCell(range.max, mirrorCellCoords, _mirrorPlaneIdBuffer[planeIndex]));

                    _tileRuleGridCellRangeBuffer.Add(mirroredRange);
                }
            }

            if (_tileRuleGridCellRangeBuffer.Count != 0)
            {
                int startRange = 1;
                for (int rangeIndex = startRange; rangeIndex < _tileRuleGridCellRangeBuffer.Count; ++rangeIndex)
                {
                    mirroredRanges.Add(_tileRuleGridCellRangeBuffer[rangeIndex]);
                }
            }
        }

        public void mirrorTileRuleGridCellCoords(Vector3Int cellCoords, List<Vector3Int> mirroredCoords)
        {
            mirroredCoords.Clear();
            if (tileRuleGrid == null) return;
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer, _mirrorPlaneIdBuffer)) return;
          
            Vector3Int mirrorCellCoords = tileRuleGrid.mirrorGizmoCellCoords;
            if (cellCoords == mirrorCellCoords) return;
           
            _vec3IntBuffer.Clear();
            _vec3IntBuffer.Add(cellCoords);
            for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
            {
                int numCoords = _vec3IntBuffer.Count;
                for (int coordsIndex = 0; coordsIndex < numCoords; ++coordsIndex)
                {
                    _vec3IntBuffer.Add(Symmetry.mirror3DGridCell(_vec3IntBuffer[coordsIndex], mirrorCellCoords, _mirrorPlaneIdBuffer[planeIndex]));
                }
            }

            if (_vec3IntBuffer.Count != 0)
            {
                int startCoords = 1;
                for (int coordsIndex = startCoords; coordsIndex < _vec3IntBuffer.Count; ++coordsIndex)
                {
                    mirroredCoords.Add(_vec3IntBuffer[coordsIndex]);
                }
            }
        }

        public void mirrorTileRuleConnectionPath(TileRuleConnectionPath connectionPath, List<TileRuleConnectionPath> connectionPaths) 
        {
            if (connectionPath.cells.Count == 0) return;

            connectionPaths.Clear();
            if (tileRuleGrid == null) return;
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer, _mirrorPlaneIdBuffer)) return;

            Vector3Int mirrorCellCoords = tileRuleGrid.mirrorGizmoCellCoords;
            
            _tileRuleConnectionPathBuffer.Clear();
            _tileRuleConnectionPathBuffer.Add(connectionPath);

            for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
            {
                int numPaths = _tileRuleConnectionPathBuffer.Count;
                for (int pathIndex = 0; pathIndex < numPaths; ++pathIndex)
                {
                    var path            = _tileRuleConnectionPathBuffer[pathIndex];
                    var mirroredPath    = new TileRuleConnectionPath();

                    foreach (var cell in path.cells)
                    {
                        mirroredPath.cells.Add(Symmetry.mirror3DGridCell(cell, mirrorCellCoords, _mirrorPlaneIdBuffer[planeIndex]));
                    }

                    _tileRuleConnectionPathBuffer.Add(mirroredPath);
                }
            }

            if (_tileRuleConnectionPathBuffer.Count != 0)
            {
                int startPath = 1;
                for (int pathIndex = startPath; pathIndex < _tileRuleConnectionPathBuffer.Count; ++pathIndex)
                {
                    connectionPaths.Add(_tileRuleConnectionPathBuffer[pathIndex]);
                }
            }
        }

        public void drawMirroredOBBs(List<OBB> obbs)
        {
            Event e = Event.current;
            if (e.type != EventType.Repaint) return;

            HandlesEx.saveColor();
            HandlesEx.saveMatrix();
            HandlesEx.saveLit();

            Handles.lighting = false;
            foreach (var obb in obbs)
            {
                Handles.matrix = Matrix4x4.TRS(obb.center, obb.rotation, obb.size);

                Handles.color = GizmoPrefs.instance.mirrorIndicatorFillColor;
                Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1.0f, e.type);

                Handles.color = GizmoPrefs.instance.mirrorIndicatorWireColor;
                //Handles.DrawWireCube(Vector3.zero, Vector3.one);
                HandlesEx.drawUnitWireCube();
            }

            HandlesEx.restoreColor();
            HandlesEx.restoreMatrix();
            HandlesEx.restoreLit();
        }

        public void drawMirroredOBB(OBB obb)
        {
            Event e = Event.current;
            if (e.type != EventType.Repaint) return;

            HandlesEx.saveColor();
            HandlesEx.saveMatrix();
            HandlesEx.saveLit();

            Handles.lighting    = false;
            Handles.matrix      = Matrix4x4.TRS(obb.center, obb.rotation, obb.size);

            Handles.color = GizmoPrefs.instance.mirrorIndicatorFillColor;
            Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1.0f, e.type);

            Handles.color = GizmoPrefs.instance.mirrorIndicatorWireColor;
            //Handles.DrawWireCube(Vector3.zero, Vector3.one);
            HandlesEx.drawUnitWireCube();

            HandlesEx.restoreColor();
            HandlesEx.restoreMatrix();
            HandlesEx.restoreLit();
        }

        protected override void doOnSceneGUI()
        {
            updateHandleIds();

            drawMirrorPlanes();
            drawMidSnapHandle();
            drawMoveHandles();
            drawRotationHandles();
            drawMirroredBoxes();
            if (isDraggingHandles) drawSymmetricPairHighlights();
            drawUIHandles();
        }

        protected override void onEnable()
        {
            _snapToObjectsRaycastFilter.customFilter = (GameObject go) => { return go.GetComponent<ObjectSpawnGuideMono>() == null; };
        }

        private void updateHandleIds()
        {
            _snapHandleId           = GUIUtility.GetControlID(FocusType.Passive);
            _xAxisMoveHandleId      = GUIUtility.GetControlID(FocusType.Passive);
            _yAxisMoveHandleId      = GUIUtility.GetControlID(FocusType.Passive);
            _zAxisMoveHandleId      = GUIUtility.GetControlID(FocusType.Passive);
            _xAxisRotationHandleId  = GUIUtility.GetControlID(FocusType.Passive);
            _yAxisRotationHandleId  = GUIUtility.GetControlID(FocusType.Passive);
            _zAxisRotationHandleId  = GUIUtility.GetControlID(FocusType.Passive);
        }

        private void drawUIHandles()
        {
            if (GizmoPrefs.instance.mirrorShowInfoText)
            {
                Camera camera       = PluginCamera.camera;
                float handleSize    = HandleUtility.GetHandleSize(position);

                Handles.BeginGUI();
                Handles.Label(position - camera.transform.up * 1.0f * handleSize - camera.transform.right * 1.0f * handleSize,
                    "Position: " + position.ToString("F3"), GUIStyleDb.instance.sceneViewInfoLabel);
                Handles.EndGUI();
            }
        }

        private void drawMirrorPlanes()
        {
            _sortedPatches.Clear();
            float quarterPlaneSize  = GizmoPrefs.instance.mirrorPlaneSize * 0.25f * HandleUtility.GetHandleSize(position);
            Vector2 patchSize       = Vector2Ex.create(GizmoPrefs.instance.mirrorPlaneSize * 0.5f * HandleUtility.GetHandleSize(position));

            _planeFillColors[0]     = GizmoPrefs.instance.mirrorXYPlaneColor;
            _planeFillColors[1]     = GizmoPrefs.instance.mirrorYZPlaneColor;
            _planeFillColors[2]     = GizmoPrefs.instance.mirrorZXPlaneColor;

            _planeMask[0]           = sharedSettings.useXYPlane;
            _planeMask[1]           = sharedSettings.useYZPlane;
            _planeMask[2]           = sharedSettings.useZXPlane;

            // Note: We need to create the plane patches first so that we
            //       can sort. This is necessary because we have to disable
            //       the depth test while drawing to allow for alpha blending.
            for (int planeIndex = 0; planeIndex < _mirrorPlanes.Length; ++planeIndex)
            {
                if (!_planeMask[planeIndex]) continue;

                var planeInfo = _mirrorPlanes[planeIndex];
                Quaternion patchRotation = rotation * planeInfo.modelRotation;

                // Note: Assume 1-to-1 mapping with 'PlanePatchId'
                for (int patchIndex = 0; patchIndex < 4; ++patchIndex)
                {
                    planeInfo.patches[patchIndex].fillColor = _planeFillColors[planeIndex];

                    XYQuad3D quad = planeInfo.patches[patchIndex].quad;
                    quad.rotation = patchRotation;
                    quad.size = patchSize;

                    Vector3 xAxis = patchRotation * _patchCalcAxes[patchIndex * 2];
                    Vector3 yAxis = patchRotation * _patchCalcAxes[patchIndex * 2 + 1];
                    quad.center = position + xAxis * quarterPlaneSize + yAxis * quarterPlaneSize;

                    // Store this patch to sort it later
                    _sortedPatches.Add(planeInfo.patches[patchIndex]);
                }
            }

            var camPos = PluginCamera.camera.transform.position;
            _sortedPatches.Sort(delegate (MirrorPlanePatch p0, MirrorPlanePatch p1)
            {
                float d0 = (camPos - p0.quad.center).magnitude;
                float d1 = (camPos - p1.quad.center).magnitude;
                return d1.CompareTo(d0);
            });

            Material material = MaterialPool.instance.simpleDiffuse;
            material.setZTestEnabled(false);
            material.setZWriteEnabled(false);
            material.setCullModeOff();
            for (int patchIndex = 0; patchIndex < _sortedPatches.Count; ++patchIndex)
            {
                var patch = _sortedPatches[patchIndex];
                material.SetColor("_Color", patch.fillColor); 
                material.SetPass(0);
                patch.quad.drawFilled();

                material.SetColor("_Color", GizmoPrefs.instance.mirrorPlaneBorderColor);
                material.SetPass(0);
                patch.quad.drawWire();
            }
        }

        private void drawMidSnapHandle()
        {
            EditorGUI.BeginChangeCheck();
            Handles.Slider(_snapHandleId, position, rotation * Vector3.right, getSnapHandleSize(), Handles.CubeHandleCap, 0.0f);
            if (EditorGUI.EndChangeCheck())
            {
                if (_midSnapMode == MirrorGizmoMidSnapMode.TileRuleGrid)
                {
                    if (tileRuleGrid != null)
                    {
                        Vector3Int cellCoords;
                        if (tileRuleGrid.pickCellCoords(PluginCamera.camera.getCursorRay(), tileRuleGridYOffset, out cellCoords))
                        {
                            tileRuleGrid.mirrorGizmoCellCoords = cellCoords;
                        }
                    }
                }
                else
                {
                    Ray ray = PluginCamera.camera.getCursorRay();
                    SceneRayHit rayHit = PluginScene.instance.raycastClosest(ray, _snapToObjectsRaycastFilter, _snapToObjectsRaycastConfig);
                    if (rayHit.anyHit)
                    {
                        if (rayHit.wasGridHit && (!rayHit.wasObjectHit || rayHit.gridHit.hitEnter < rayHit.objectHit.hitEnter))
                        {
                            var hitCell = rayHit.gridHit.hitCell;
                            PluginScene.instance.grid.calcCellCenterAndCorners(hitCell, true, _vector3Buffer);
                            var closestPtIndex = Vector3Ex.findIndexOfPointClosestToPoint(_vector3Buffer, rayHit.gridHit.hitPoint);
                            if (closestPtIndex >= 0)
                            {
                                UndoEx.record(this);
                                position = _vector3Buffer[closestPtIndex];
                            }
                        }
                        else
                        if (rayHit.wasObjectHit && (!rayHit.wasGridHit || rayHit.objectHit.hitEnter < rayHit.gridHit.hitEnter))
                        {
                            GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(rayHit.objectHit.hitObject);
                            position = PluginScene.instance.grid.snapAllAxes(rayHit.objectHit.hitPoint);
                            /*if (objectType == GameObjectType.Terrain)
                            {
                                UndoEx.record(this);
                                position = rayHit.objectHit.hitPoint;
                            }
                            else
                            {
                                OBB worldOBB = ObjectBounds.calcWorldOBB(rayHit.objectHit.hitObject, _snapToObjectsBoundsQConfig);
                                if (worldOBB.isValid)
                                {
                                    Box3DFace hitFace = Box3D.findFaceClosestToPoint(rayHit.objectHit.hitPoint, worldOBB.center, worldOBB.size, worldOBB.rotation);
                                    Box3D.calcFaceCenterAndCorners(worldOBB.center, worldOBB.size, worldOBB.rotation, hitFace, _faceCenterAndCorners);
                                    var closestPtIndex = Vector3Ex.findIndexOfPointClosestToPoint(_faceCenterAndCorners, rayHit.objectHit.hitPoint);
                                    if (closestPtIndex >= 0)
                                    {
                                        UndoEx.record(this);
                                        position = _faceCenterAndCorners[closestPtIndex];
                                    }
                                }
                            }*/
                        }
                    }
                }
            }
        }

        private void drawMoveHandles()
        {
            float offsetAlongDirection  = getRotationHandleSize() + 0.2f * HandleUtility.GetHandleSize(position);
            var moveSnapStep            = sharedSettings.moveSnapStep;

            HandlesEx.saveColor();
            for (int axisIndex = 0; axisIndex < 3; ++axisIndex)
            {
                Handles.color = _moveAxisColors[axisIndex];
                EditorGUI.BeginChangeCheck();
                Vector3 direction = rotation *_modelMoveDirs[axisIndex];
                Vector3 currentPosition = position + direction * offsetAlongDirection;
                Vector3 newPosition = Handles.Slider(getMoveHandleId(axisIndex), currentPosition, direction, getMoveHandleSize(), Handles.ConeHandleCap, moveSnapStep[axisIndex]);    
                if (EditorGUI.EndChangeCheck())
                {
                    UndoEx.record(this);
                    position += (newPosition - currentPosition);
                }
            }
            HandlesEx.restoreColor();
        }

        private void drawRotationHandles()
        {
            if (!sharedSettings.hasRotationHandles) return;

            var rotationSnapStep = sharedSettings.rotationSnapStep;
            for (int axisIndex = 0; axisIndex < 3; ++axisIndex)
            {
                Handles.color = _rotationHandleColors[axisIndex];
                EditorGUI.BeginChangeCheck();
                Quaternion rotQuat = Handles.Disc(getRotationHandleId(axisIndex), rotation, position, rotation * _rotationAxes[axisIndex], getRotationHandleSize(), true, rotationSnapStep[axisIndex]);
                if (EditorGUI.EndChangeCheck())
                {
                    UndoEx.record(this);
                    rotation = rotQuat;
                }
            }
        }

        private int getMoveHandleId(int axisIndex)
        {
            if (axisIndex == 0) return _xAxisMoveHandleId;
            else if (axisIndex == 1) return _yAxisMoveHandleId;
            return _zAxisMoveHandleId;
        }

        private int getRotationHandleId(int axisIndex)
        {
            if (axisIndex == 0) return _xAxisRotationHandleId;
            else if (axisIndex == 1) return _yAxisRotationHandleId;
            return _zAxisRotationHandleId;
        }

        private void drawMirroredBoxes()
        {
            if (_targetObjects == null) return;

            Event e = Event.current;
            if (e.type != EventType.Repaint) return;
            if (!gatherMirrorPlanes(_mirrorPlaneBuffer)) return;

            HandlesEx.saveColor();
            HandlesEx.saveMatrix();
            HandlesEx.saveLit();

            Handles.lighting = false;

            GameObjectEx.getParents(_targetObjects, _parentBuffer);
            bool mirrorRotation = sharedSettings.mirrorRotation;
            bool mirrorSpanning = sharedSettings.mirrorSpanning;
            foreach(var parent in _parentBuffer)
            {
                _obbBuffer.Clear();
                OBB obb = ObjectBounds.calcHierarchyWorldOBB(parent, _mirroredBoundsQConfig);
                if (!obb.isValid) continue;

                _obbBuffer.Add(obb);
                for (int planeIndex = 0; planeIndex < _mirrorPlaneBuffer.Count; ++planeIndex)
                {
                    if (!mirrorSpanning)
                    {
                        var location = Box3D.classifyAgainstPlane(obb.center, obb.size, obb.rotation, _mirrorPlaneBuffer[planeIndex]);
                        if (location == PlaneClassifyResult.Spanning) continue;
                    }

                    int numOBBs = _obbBuffer.Count;
                    for (int obbIndex = 0; obbIndex < numOBBs; ++obbIndex)
                    {
                        OBB mirroredOBB = Symmetry.mirrorOBB(_obbBuffer[obbIndex], mirrorRotation, _mirrorPlaneBuffer[planeIndex]);
                        _obbBuffer.Add(mirroredOBB);
                    }
                }

                int startOBB = 1;
                for (int obbIndex = startOBB; obbIndex < _obbBuffer.Count; ++obbIndex)
                {
                    Handles.matrix = Matrix4x4.TRS(_obbBuffer[obbIndex].center, _obbBuffer[obbIndex].rotation, _obbBuffer[obbIndex].size);

                    Handles.color = GizmoPrefs.instance.mirrorIndicatorFillColor;
                    Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1.0f, e.type);

                    Handles.color = GizmoPrefs.instance.mirrorIndicatorWireColor;
                    //Handles.DrawWireCube(Vector3.zero, Vector3.one);
                    HandlesEx.drawUnitWireCube();
                }
            }

            HandlesEx.restoreColor();
            HandlesEx.restoreMatrix();
            HandlesEx.restoreLit();
        }

        private void drawSymmetricPairHighlights()
        {
            if (sharedSettings.useXYPlane)
                drawSymmetricPairHighlights(GizmoPrefs.instance.mirrorXYPlaneColor, PlaneId.XY);

            if (sharedSettings.useYZPlane)
                drawSymmetricPairHighlights(GizmoPrefs.instance.mirrorYZPlaneColor, PlaneId.YZ);

            if (sharedSettings.useZXPlane)
                drawSymmetricPairHighlights(GizmoPrefs.instance.mirrorZXPlaneColor, PlaneId.ZX);
        }

        private void drawSymmetricPairHighlights(Color highlightColor, PlaneId planeId)
        {
            highlightColor = highlightColor.createNewAlpha(1.0f);

            Vector3 obbSize     = Vector3.one;
            obbSize.y           = GizmoPrefs.instance.mirrorSymmetricPairHighlightRadius * 2.0f;
            obbSize[(2 + (int)planeId) % 3] = GizmoPrefs.instance.mirrorSymmetricPairHighlightRadius * 2.0f;
            OBB overlapOBB      = new OBB(position, obbSize, rotation);
            Plane mirrorPlane   = new Plane(rotation * _mirrorPlanes[(int)planeId].modelNormal, position);

            _symPairsOverlapFilter.objectTypes = GameObjectType.Mesh | GameObjectType.Sprite;

            var overlapConfig = ObjectOverlapConfig.defaultConfig;
            overlapConfig.prefabMode = ObjectOverlapPrefabMode.OnlyPrefabInstanceRoot;

            _symPairsOutline.objectGather.Clear();
            if (PluginScene.instance.overlapBox(overlapOBB, _symPairsOverlapFilter, overlapConfig, _symPairsObjectOverlapBuffer))
            {
                // Identify pairs of objects that are equally spaced from the mirror plane
                int numObjects = _symPairsObjectOverlapBuffer.Count;
                for (int i = 0; i < numObjects; ++i)
                {
                    GameObject firstObject = _symPairsObjectOverlapBuffer[i];
                    GameObject firstPrefab = firstObject.getOutermostPrefabAsset();
                    if (firstPrefab == null) continue;

                    float d0 = mirrorPlane.GetDistanceToPoint(firstObject.transform.position);
                    float absD0 = Mathf.Abs(d0);

                    for (int j = i + 1; j < numObjects; ++j)
                    {
                        GameObject secondObject = _symPairsObjectOverlapBuffer[j];

                        //GameObject secondPrefab = secondObject.getOutermostPrefabAsset();
                        //if (secondPrefab != firstPrefab) continue;

                        // Only proceed if they reside on different sides of the plane
                        float d1 = mirrorPlane.GetDistanceToPoint(secondObject.transform.position);
                        if (Mathf.Sign(d0) != Mathf.Sign(d1))
                        {
                            // Ensure equal distance from plane
                            if (Mathf.Abs(absD0 - Mathf.Abs(d1)) < 1e-5f)
                            {
                                _symPairsOutline.objectGather.Add(firstObject);     
                                _symPairsOutline.objectGather.Add(secondObject);
                            }
                        }
                    }
                }

                _symPairsOutline.drawHandles(highlightColor);
            }
        }

        private bool gatherMirrorPlanes(List<Plane> planes)
        {
            planes.Clear();
            if (sharedSettings.useXYPlane)
                planes.Add(new Plane(rotation * _mirrorPlanes[(int)PlaneId.XY].modelNormal, position));
            if (sharedSettings.useYZPlane)
                planes.Add(new Plane(rotation * _mirrorPlanes[(int)PlaneId.YZ].modelNormal, position));
            if (sharedSettings.useZXPlane)
                planes.Add(new Plane(rotation * _mirrorPlanes[(int)PlaneId.ZX].modelNormal, position));

            return planes.Count != 0;
        }

        private bool gatherMirrorPlanes(List<Plane> planes, List<PlaneId> planeIds)
        {
            planes.Clear();
            planeIds.Clear();

            if (sharedSettings.useXYPlane)
            {
                planes.Add(new Plane(rotation * _mirrorPlanes[(int)PlaneId.XY].modelNormal, position));
                planeIds.Add(PlaneId.XY);
            }
            if (sharedSettings.useYZPlane)
            {
                planes.Add(new Plane(rotation * _mirrorPlanes[(int)PlaneId.YZ].modelNormal, position));
                planeIds.Add(PlaneId.YZ);
            }

            if (sharedSettings.useZXPlane)
            {
                planes.Add(new Plane(rotation * _mirrorPlanes[(int)PlaneId.ZX].modelNormal, position));
                planeIds.Add(PlaneId.ZX);
            }

            return planes.Count != 0;
        }

        private float getMoveHandleSize()
        {
            return HandleUtility.GetHandleSize(position) * 0.20f;
        }

        private float getSnapHandleSize()
        {
            return HandleUtility.GetHandleSize(position) * 0.112f;
        }

        private float getRotationHandleSize()
        {
            return HandleUtility.GetHandleSize(position) * 1.3f;
        }
    }
}
#endif