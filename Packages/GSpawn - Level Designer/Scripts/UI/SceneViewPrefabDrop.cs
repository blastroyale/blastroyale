#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class SceneViewPrefabDrop : ScriptableObject
    {
        public delegate void                DragPerformedHandler    (List<GameObject> spawnedInstances);
        public event                        DragPerformedHandler    dragPerformed;

        [NonSerialized]
        private PluginPrefab                _prefab;
        [NonSerialized]
        private List<GameObject>            _objectBuffer               = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _spawnedInstancesBuffer     = new List<GameObject>();

        [SerializeField]
        private ObjectProjectionSettings    _projectionSettings;
        [SerializeField]
        private GameObject                  _dragAndDropGuide;

        private SceneRaycastFilter          _raycastFilter              = new SceneRaycastFilter()
        {
            objectTypes = GameObjectType.Mesh | GameObjectType.Terrain,
            raycastGrid = true,
            raycastObjects = true
        };
        [NonSerialized]
        private bool                        _isActive           = false;

        public bool                         isActive            { get { return _isActive; } }

        public void begin(PluginPrefab prefab)
        {
            if (_isActive) return;

            _isActive = true;
            _prefab = prefab;
            //_undoGroupIndex = Undo.GetCurrentGroup();
        }

        public void cancel()
        {
            onDragEnded();
        }

        public void onSceneGUI()
        {
            // Note: When a prefab is dragged and dropped into the prefab manager, when the mouse enters
            //       the scene view window later on, a prefab will be instantiated because _isActive doesn't
            //       get set to false.
            if (Event.current.type == EventType.MouseMove)
                _isActive = false;

            if (_isActive)
            {
                // Note: Create it here because we want the guide to become visible
                //       only when the mouse cursor enters the scene view.
                if (_dragAndDropGuide == null)
                {
                    if (SceneViewEx.sceneViewHasFocus())
                    {
                        // Note: Collapse the spawn operation to the same group as when the drag started.
                        //       Otherwise we can't undo/redo the spawn.
                        _dragAndDropGuide = _prefab.prefabAsset.instantiatePrefab();
                        //Undo.CollapseUndoOperations(_undoGroupIndex);
                    }
                }

                if (_dragAndDropGuide == null) return;

                Event e = Event.current;
                if (e.type == EventType.DragUpdated)
                {
                    PluginDragAndDrop.defaultUpdateVisualMode(Event.current.type);
                    GameObjectEx.getAllChildrenAndSelf(_dragAndDropGuide, false, false, _objectBuffer);
                    _raycastFilter.setIgnoredObjects(_objectBuffer);

                    var raycastHit = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), _raycastFilter, ObjectRaycastConfig.defaultConfig);
                    if (!raycastHit.anyHit) return;

                    RayHit closestHit = raycastHit.getClosestRayHit();
                    //UndoEx.recordTransform(_dragAndDropGuide.transform);        // Note: We need this here to allow Undo/Redo to work correctly after drag and drop ends.
                    _dragAndDropGuide.transform.position = closestHit.hitPoint;
                    ObjectProjection.projectHierarchyOnPlane(_dragAndDropGuide, closestHit.hitPlane, _projectionSettings);

                    if (FixedShortcuts.enableSnapAllAxes(e)) PluginScene.instance.grid.snapObjectAllAxes(_dragAndDropGuide);
                    //Undo.CollapseUndoOperations(_undoGroupIndex);               // Note: We need this here to allow Undo/Redo to work correctly after drag and drop ends.
                }
                else
                if (e.type == EventType.DragPerform) dragPerform();
                else if (e.type == EventType.DragExited || FixedShortcuts.cancelAction(e)) onDragEnded();
            }
        }

        private void dragPerform()
        {
            GameObject finalObject = _prefab.spawn(_dragAndDropGuide.transform.position,
                    _dragAndDropGuide.transform.rotation, _dragAndDropGuide.transform.localScale);

            GameObject.DestroyImmediate(_dragAndDropGuide);
            _dragAndDropGuide = null;

            if (dragPerformed != null)
            {
                _spawnedInstancesBuffer.Clear();
                _spawnedInstancesBuffer.Add(finalObject);
                dragPerformed(_spawnedInstancesBuffer);
                _spawnedInstancesBuffer.Clear();
            }

            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
            {
                ObjectSelection.instance.setSelectedObject(finalObject);
                if (ObjectSelectionPrefs.instance.prefabSpawnTransformSession == ObjectSelectionPrefabTransformSession.ModularSnap)
                    ObjectSelection.instance.beginTransformSession(ObjectTransformSessionType.ModularSnap);
                else if (ObjectSelectionPrefs.instance.prefabSpawnTransformSession == ObjectSelectionPrefabTransformSession.SurfaceSnap)
                    ObjectSelection.instance.beginTransformSession(ObjectTransformSessionType.SurfaceSnap);

                //Undo.CollapseUndoOperations(_undoGroupIndex);
            }

            _isActive   = false;
            _prefab     = null;
            PluginDragAndDrop.endDrag();
        }

        private void onDragEnded()
        {          
            _isActive   = false;
            _prefab     = null;
            if (_dragAndDropGuide != null) GameObject.DestroyImmediate(_dragAndDropGuide);
        }

        private void OnEnable()
        {
            if (_projectionSettings == null)
            {
                _projectionSettings                 = ScriptableObject.CreateInstance<ObjectProjectionSettings>();
                _projectionSettings.alignAxis       = false;
                _projectionSettings.embedInSurface  = true;
                _projectionSettings.halfSpace       = ObjectProjectionHalfSpace.InFront;
            }
            PluginDragAndDrop.ended += onDragEnded;
        }

        private void OnDisable()
        {
            _isActive   = false;
            _prefab     = null;
            ScriptableObjectEx.destroyImmediate(_projectionSettings);
            if (_dragAndDropGuide != null) DestroyImmediate(_dragAndDropGuide);
            PluginDragAndDrop.ended -= onDragEnded;
        }
    }
}
#endif