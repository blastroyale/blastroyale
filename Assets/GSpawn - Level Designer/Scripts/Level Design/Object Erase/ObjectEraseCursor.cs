#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class ObjectEraseCursor : ObjectEraseTool
    {
        private Plane                       _cullPlane;
        private bool                        _updateCullPlane    = true;
        private SceneRaycastFilter          _raycastFilter      = new SceneRaycastFilter();
        private ObjectBounds.QueryConfig    _boundsQConfig      = new ObjectBounds.QueryConfig();

        public override ObjectEraseToolId   toolId              { get { return ObjectEraseToolId.Cursor; } }

        public ObjectEraseCursor()
        {
            _raycastFilter.objectTypes      = GameObjectType.Mesh | GameObjectType.Sprite;
            _raycastFilter.raycastGrid      = false;
            _raycastFilter.customFilter     = (GameObject gameObject) => { return ObjectErase.instance.canEraseObject(gameObject); };
            _boundsQConfig.objectTypes      = GameObjectType.Mesh | GameObjectType.Sprite;
        }

        protected override void doOnSceneGUI()
        {
            Event e = Event.current;
            if (e.button == 0)
            {
                GameObject pickedObject = null;
                switch (e.type)
                {
                    case EventType.MouseDrag:

                        pickedObject = pickGameObject();
                        if (pickedObject != null) eraseGameObject(pickedObject);
                        break;

                    case EventType.MouseUp:

                        pickedObject = pickGameObject();
                        if (pickedObject != null) eraseGameObject(pickedObject);
                        _updateCullPlane = true;
                        break;
                }
            }
        }

        protected override void draw()
        {
        }

        private GameObject pickGameObject()
        {
            GameObject pickedObject = null;
            Ray pickRay = PluginCamera.camera.getCursorRay();
            var rayHit  = PluginScene.instance.raycastClosest(pickRay, _raycastFilter, ObjectRaycastConfig.defaultConfig);
            if (rayHit.wasObjectHit)
            {
                if (_updateCullPlane)
                {
                    pickedObject = rayHit.objectHit.hitObject;
                    _cullPlane = new Plane(rayHit.objectHit.hitNormal, rayHit.objectHit.hitPoint - rayHit.objectHit.hitNormal * 1e-3f);
                    _updateCullPlane = false;
                }
                else
                {
                    bool eraseCullPlaneToggle = FixedShortcuts.eraseCullPlaneToggle(Event.current);
                    bool useCullPlane = (eraseCullPlaneToggle && !ObjectErase.instance.eraseCursorSettings.enableCullPlaneByDefault) ||
                                        (!eraseCullPlaneToggle && ObjectErase.instance.eraseCursorSettings.enableCullPlaneByDefault);
                    if (useCullPlane)
                    {
                        OBB worldOBB = ObjectBounds.calcWorldOBB(rayHit.objectHit.hitObject, _boundsQConfig);
                        if (worldOBB.isValid && Box3D.classifyAgainstPlane(worldOBB.center, worldOBB.size, worldOBB.rotation, _cullPlane) == PlaneClassifyResult.Spanning)
                            pickedObject = rayHit.objectHit.hitObject;
                    }
                    else pickedObject = rayHit.objectHit.hitObject;
                }
            }

            return pickedObject;
        }
    }
}
#endif