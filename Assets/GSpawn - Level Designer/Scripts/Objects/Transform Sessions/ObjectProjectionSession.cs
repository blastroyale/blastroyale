#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectProjectionSession : ObjectTransformSession
    {
        public delegate void                        PerformedProjectionHandler  ();
        public event                                PerformedProjectionHandler  projected;

        private GameObject                          _surfaceObject;
        private GameObjectType                      _surfaceObjectType;
        private ObjectRayHit                        _projectionRayHit;

        private List<ObjectRayHit>                  _objectRayHits      = new List<ObjectRayHit>();
        private SceneRaycastFilter                  _raycastFilter      = new SceneRaycastFilter()
        {
            objectTypes = GameObjectType.Mesh | GameObjectType.Sprite | GameObjectType.Terrain
        };

        public ObjectProjectionSettings             sharedSettings      { get; set; }
        public override string                      sessionName         { get { return "Projection"; } }
        public override ObjectTransformSessionType  sessionType         { get { return ObjectTransformSessionType.Projection; } }

        protected override void update()
        {
            Event e = Event.current;
            if (e.type == EventType.MouseUp &&
                e.button == (int)MouseButton.LeftMouse)
            {
                pickSurfaceObject();
                projectOnSurfaceObject();
                e.disable();
            }
        }

        protected override bool onCanBegin()
        {
            return sharedSettings != null;
        }

        protected override bool onBegin()
        {
            _raycastFilter.setIgnoredObjects(_targetObjects);
            return true;
        }

        protected override void onEnd()
        {
            _surfaceObject      = null;
            _projectionRayHit   = null;
        }

        private void pickSurfaceObject()
        {
            Ray pickRay         = PluginCamera.camera.getCursorRay();
            PluginScene.instance.raycastAll(pickRay, _raycastFilter, ObjectRaycastConfig.defaultConfig,  true, _objectRayHits);

            _projectionRayHit   = _objectRayHits.Count != 0 ? _objectRayHits[0] : null;
            _surfaceObject      = _projectionRayHit != null ? _projectionRayHit.hitObject : null;
            if (_surfaceObject != null) _surfaceObjectType = GameObjectDataDb.instance.getGameObjectType(_surfaceObject);
        }

        private void projectOnSurfaceObject()
        {
            if (_surfaceObject == null) return;
         
            UndoEx.recordGameObjectTransforms(_targetParents);
            ObjectProjection.projectHierarchiesOnObject(_targetParents, _surfaceObject, _surfaceObjectType, _projectionRayHit.hitPlane, sharedSettings, null);

            ObjectEvents.onObjectsTransformed();
            if (projected != null) projected();
        }
    }
}
#endif