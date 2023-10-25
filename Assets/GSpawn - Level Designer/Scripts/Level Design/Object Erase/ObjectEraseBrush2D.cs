#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class ObjectEraseBrush2D : ObjectEraseTool
    {
        [NonSerialized]
        private List<GameObject>            _overlappedObjects  = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _gameObjectBuffer   = new List<GameObject>();
        [NonSerialized]
        private List<Vector2>               _vector2Buffer      = new List<Vector2>();
        [NonSerialized]
        private Plane                       _cullPlane;
        [NonSerialized]
        private bool                        _updateCullPlane    = true;
        [NonSerialized]
        private Rect                        _rect               = new Rect();
        [NonSerialized]
        private SceneRaycastFilter          _raycastFilter      = new SceneRaycastFilter();
        [NonSerialized]
        private ObjectBounds.QueryConfig    _boundsQConfig      = new ObjectBounds.QueryConfig();

        public override ObjectEraseToolId   toolId            { get { return ObjectEraseToolId.Brush2D; } }

        public ObjectEraseBrush2D()
        {
            _raycastFilter.objectTypes = GameObjectType.Mesh | GameObjectType.Sprite;
            _raycastFilter.raycastGrid = false;
            _boundsQConfig.objectTypes = GameObjectType.Mesh | GameObjectType.Sprite;
        }

        protected override void doOnSceneGUI()
        {
            Event e = Event.current;
            updateOverlapRect();

            if (e.button == 0)
            {
                switch (e.type)
                {
                    case EventType.MouseDrag:

                        if (_updateCullPlane) updateCullPlane();
                        detectOverlappedObjects();
                        eraseGameObjects(_overlappedObjects);
                        break;

                    case EventType.MouseUp:

                        if (_updateCullPlane) updateCullPlane();
                        detectOverlappedObjects();
                        eraseGameObjects(_overlappedObjects);
                        _updateCullPlane = true;
                        break;

                    default:

                        if (FixedShortcuts.changeRadiusByScrollWheel(e))
                        {
                            e.disable();
                            ObjectErase.instance.eraseBrush2DSettings.radius -= 2.0f * e.delta.y;
                            EditorUtility.SetDirty(ObjectErase.instance.eraseBrush2DSettings);
                        }
                        break;
                }
            }
        }

        protected override void draw()
        {
            Matrix4x4 circleTransform   = PluginCamera.camera.calcXYCircleOnNearPlaneTransform(_rect.center, _rect.width * 0.5f, 1e-4f);
            Material material           = MaterialPool.instance.simpleDiffuse;
            material.setCullModeOff();

            material.SetColor("_Color", ObjectErasePrefs.instance.brush2DFillColor);
            material.SetPass(0);
            Graphics.DrawMeshNow(MeshPool.instance.unitCircleXY, circleTransform);

            material.SetColor("_Color", ObjectErasePrefs.instance.brush2DBorderColor);
            material.SetPass(0);
            Graphics.DrawMeshNow(MeshPool.instance.unitWireCircleXY, circleTransform);
        }

        private void detectOverlappedObjects()
        {
            _overlappedObjects.Clear();
            bool allowPartialOverlap = ObjectErase.instance.eraseBrush2DSettings.allowPartialOverlap;
            Rect pickRect = allowPartialOverlap ? PluginCamera.camera.pixelRect : _rect.createInvertedYCoords(PluginCamera.camera);

            var pickedObjects = HandleUtility.PickRectObjects(pickRect);
            if (pickedObjects.Length == 0) return;

            Vector2 circleCenter = _rect.center;
            float circleRadius = ObjectErase.instance.eraseBrush2DSettings.radius;

            bool eraseCullPlaneToggle = FixedShortcuts.eraseCullPlaneToggle(Event.current);
            bool useCullPlane = !_updateCullPlane && ((eraseCullPlaneToggle && !ObjectErase.instance.eraseBrush2DSettings.enableCullPlaneByDefault) ||
                                (!eraseCullPlaneToggle && ObjectErase.instance.eraseBrush2DSettings.enableCullPlaneByDefault));
     
            // Note: HandleUtility.PickRectObjects seems to pick only parents. We need all objects intersected by the rectangle.
            GameObjectEx.getAllObjectsInHierarchies(pickedObjects, false, false, _gameObjectBuffer);
            foreach (GameObject go in _gameObjectBuffer)
            {
                if (ObjectErase.instance.canEraseObject(go))
                {
                    if (useCullPlane)
                    {
                        OBB worldOBB = ObjectBounds.calcWorldOBB(go, _boundsQConfig);
                        if (!worldOBB.isValid || Box3D.classifyAgainstPlane(worldOBB.center, worldOBB.size, worldOBB.rotation, _cullPlane) != PlaneClassifyResult.Spanning)
                            continue;
                    }

                    Rect objectScreenRect = ObjectBounds.calcScreenRect(go, PluginCamera.camera, _boundsQConfig);
                    if (allowPartialOverlap)
                    {
                        if (Circle2D.intersectsRect(circleCenter, circleRadius, objectScreenRect)) _overlappedObjects.Add(go);
                    }
                    else
                    {
                        objectScreenRect.calcCorners(_vector2Buffer);
                        if (Circle2D.containsPoints(circleCenter, circleRadius, _vector2Buffer)) _overlappedObjects.Add(go);
                    }
                }
            }
        }

        private void updateOverlapRect()
        {
            Vector2 mousePos    = Event.current.mousePosition;
            mousePos.y          = PluginCamera.camera.pixelHeight - mousePos.y;
            _rect               = RectEx.create(mousePos, Vector2.one * ObjectErase.instance.eraseBrush2DSettings.radius * 2.0f);
        }

        private void updateCullPlane()
        {
            Ray pickRay = PluginCamera.camera.getCursorRay();
            var rayHit  = PluginScene.instance.raycastClosest(pickRay, _raycastFilter, ObjectRaycastConfig.defaultConfig);
            if (rayHit.wasObjectHit)
            {
                _cullPlane = new Plane(rayHit.objectHit.hitNormal, rayHit.objectHit.hitPoint - rayHit.objectHit.hitNormal * 1e-3f);
                _updateCullPlane = false;
            }
        }
    }
}
#endif