#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectBoxSnapSession : ObjectTransformSession
    {
        private class SnapPivot
        {
            public bool     isAvailable;
            public Vector3  position;
            public bool     isBoxCenter;
        }

        private SnapPivot                           _snapPivot                      = new SnapPivot();
        private SceneRaycastFilter                  _pickSnapPivotRaycastFilter     = new SceneRaycastFilter() { objectTypes = GameObjectType.Mesh | GameObjectType.Sprite, usePrimeFocusObjects = true };
        private SceneRaycastFilter                  _pickSnapDestRaycastFilter      = new SceneRaycastFilter();
        private ObjectBounds.QueryConfig            _objectOBBQConfig               = new ObjectBounds.QueryConfig() { objectTypes = GameObjectType.Mesh | GameObjectType.Sprite };

        private List<ObjectRayHit>                  _objectRayHitBuffer             = new List<ObjectRayHit>();
        private List<Vector3>                       _vector3Buffer                  = new List<Vector3>();
        private List<Vector2>                       _vector2Buffer                  = new List<Vector2>();
        private Vector3[]                           _quadVertBuffer                 = new Vector3[5];

        public ObjectBoxSnapSettings                sharedSettings                  { get; set; }
        public override string                      sessionName                     { get { return "Box Snap"; } }
        public override ObjectTransformSessionType  sessionType                     { get { return ObjectTransformSessionType.BoxSnap; } }

        protected override void update()
        {
            if (Mouse.instance.isButtonDown(0)) snap();
            else pickSnapPivot();
        }

        protected override bool onCanBegin()
        {
            return sharedSettings != null;
        }

        protected override bool onBegin()
        {
            _snapPivot.isAvailable = false;
            _pickSnapPivotRaycastFilter.setPrimeFocusObjects(_allTargetObjects);

            return true;
        }

        protected override void onEnd()
        {
            _snapPivot.isAvailable = false;
        }

        protected override void draw()
        {
            HandlesEx.saveColor();

            var sessionPrefs = ObjectTransformSessionPrefs.instance;
            if (sessionPrefs.boxSnapObjectBoxWireColor.a != 0.0f)
            {
                HandlesEx.saveMatrix();
                foreach (var targetObject in _allTargetObjects)
                {
                    OBB worldOBB = ObjectBounds.calcWorldOBB(targetObject, _objectOBBQConfig);
                    if (worldOBB.isValid)
                    {
                        Handles.color = sessionPrefs.boxSnapObjectBoxWireColor;
                        Handles.matrix = worldOBB.transformMatrix;
                        //Handles.DrawWireCube(Vector3.zero, Vector3.one);
                        HandlesEx.drawUnitWireCube();
                    }
                }
                HandlesEx.restoreMatrix();
            }

            if (_snapPivot.isAvailable)
            {
                Handles.color = _snapPivot.isBoxCenter ? sessionPrefs.boxSnapCenterTickColor : sessionPrefs.boxSnapTickColor;
                Handles.DotHandleCap(0, _snapPivot.position, Quaternion.identity, HandleUtility.GetHandleSize(_snapPivot.position) * sessionPrefs.boxSnapTickSize, EventType.Repaint);
            }

            HandlesEx.restoreColor();
        }

        private void pickSnapPivot()
        {
            _snapPivot.isAvailable = false;
            _snapPivot.isBoxCenter = false;

            var raycastConfig = ObjectRaycastConfig.defaultConfig;
            raycastConfig.raycastPrecision = ObjectRaycastPrecision.Box;
            PluginScene.instance.raycastAll(PluginCamera.camera.getCursorRay(), _pickSnapPivotRaycastFilter, raycastConfig, true, _objectRayHitBuffer);

            if (_objectRayHitBuffer.Count != 0)
            {
                var closestHit  = _objectRayHitBuffer[0];
                var worldOBB    = ObjectBounds.calcWorldOBB(closestHit.hitObject, _objectOBBQConfig);
                if (worldOBB.isValid)
                {
                    worldOBB.calcCenterAndCorners(_vector3Buffer);
                    PluginCamera.camera.worldToScreenPoints(_vector3Buffer, _vector2Buffer);
                    int closestPt = Vector2Ex.findIndexOfPointClosestToPoint(_vector2Buffer, Mouse.instance.positionYUp);
                    if (closestPt >= 0)
                    {
                        _snapPivot.isAvailable  = true;
                        _snapPivot.position     = _vector3Buffer[closestPt];
                        _snapPivot.isBoxCenter  = (worldOBB.center - _vector3Buffer[closestPt]).magnitude < 1e-6f;
                    }
                }
            }
        }

        private void snap()
        {
            if (!_snapPivot.isAvailable || !Mouse.instance.hasMoved) return;

            _pickSnapDestRaycastFilter.setIgnoredObjects(_targetObjects);
            _pickSnapDestRaycastFilter.layerMask        = sharedSettings.destinationLayers;
            _pickSnapDestRaycastFilter.raycastGrid      = sharedSettings.allowsGridDestination;
            _pickSnapDestRaycastFilter.raycastObjects   = sharedSettings.allowsObjectDestination;
            _pickSnapDestRaycastFilter.objectTypes      = GameObjectType.None;
            if (sharedSettings.allowsObjectDestination)
            {
                if (sharedSettings.allowsMeshDestination)       _pickSnapDestRaycastFilter.objectTypes |= GameObjectType.Mesh;
                if (sharedSettings.allowsSpriteDestination)     _pickSnapDestRaycastFilter.objectTypes |= GameObjectType.Sprite;
                if (sharedSettings.allowsTerrainDestination)    _pickSnapDestRaycastFilter.objectTypes |= GameObjectType.Terrain;
            }

            var sceneRayHit = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), _pickSnapDestRaycastFilter, ObjectRaycastConfig.defaultConfig);
            if (sceneRayHit.anyHit)
            {
                if (sceneRayHit.wasGridHit && !sceneRayHit.wasObjectHit) snapToGrid(sceneRayHit.gridHit);
                else
                if (sceneRayHit.wasObjectHit && !sceneRayHit.wasGridHit)
                {
                    GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(sceneRayHit.objectHit.hitObject);
                    if (objectType == GameObjectType.Mesh) snapToMeshObject(sceneRayHit.objectHit);
                    else if (objectType == GameObjectType.Sprite) snapToSpriteObject(sceneRayHit.objectHit);
                }
                else
                if (sceneRayHit.wasGridHit && sceneRayHit.wasObjectHit)
                {
                    GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(sceneRayHit.objectHit.hitObject);
                    if (sceneRayHit.gridHit.hitEnter < sceneRayHit.objectHit.hitEnter &&
                        Mathf.Abs(sceneRayHit.gridHit.hitEnter - sceneRayHit.objectHit.hitEnter) > 1e-4f) snapToGrid(sceneRayHit.gridHit);
                    else if (objectType == GameObjectType.Mesh) snapToMeshObject(sceneRayHit.objectHit);
                    else if (objectType == GameObjectType.Terrain) snapToTerrainObject(sceneRayHit.objectHit);
                    else if (objectType == GameObjectType.Sprite) snapToSpriteObject(sceneRayHit.objectHit);
                }
            }
        }

        private void snapToGrid(GridRayHit gridRayHit)
        {
            gridRayHit.hitGrid.calcCellCenterAndCorners(gridRayHit.hitCell, true, _vector3Buffer);
            var closestPt = Vector3Ex.findIndexOfPointClosestToPoint(_vector3Buffer, gridRayHit.hitPoint);
            if (closestPt >= 0) applySnapVector(_vector3Buffer[closestPt] - _snapPivot.position);
        }

        private void snapToMeshObject(ObjectRayHit objectHit)
        {
            var worldOBB = ObjectBounds.calcMeshWorldOBB(objectHit.hitObject);
            if (worldOBB.isValid)
            {
                worldOBB.calcCenterAndCorners(_vector3Buffer);
                PluginCamera.camera.worldToScreenPoints(_vector3Buffer, _vector2Buffer);
                int closestPt = Vector2Ex.findIndexOfPointClosestToPoint(_vector2Buffer, Mouse.instance.positionYUp);
                if (closestPt >= 0) applySnapVector(_vector3Buffer[closestPt] - _snapPivot.position);
            }
        }

        private void snapToTerrainObject(ObjectRayHit objectHit)
        {
            Terrain terrain = objectHit.hitObject.getTerrain();
            terrain.calcQuadCorners(objectHit.hitPoint, _quadVertBuffer);
            var closestPt = Vector3Ex.findIndexOfPointClosestToPoint(_quadVertBuffer, objectHit.hitPoint);
            if (closestPt >= 0) applySnapVector(_quadVertBuffer[closestPt] - _snapPivot.position);
        }

        private void snapToSpriteObject(ObjectRayHit objectHit)
        {
            var spriteOBB = ObjectBounds.calcSpriteWorldOBB(objectHit.hitObject);
            if (spriteOBB.isValid)
            {
                spriteOBB.calcCenterAndCorners(_vector3Buffer);
                PluginCamera.camera.worldToScreenPoints(_vector3Buffer, _vector2Buffer);
                int closestPt = Vector2Ex.findIndexOfPointClosestToPoint(_vector2Buffer, Mouse.instance.positionYUp);
                if (closestPt >= 0) applySnapVector(_vector3Buffer[closestPt] - _snapPivot.position);
            }
        }

        private void applySnapVector(Vector3 snapVector)
        {
            UndoEx.recordGameObjectTransforms(_targetParents);
            foreach (var parent in _targetParents)
                parent.transform.position += snapVector;
            ObjectEvents.onObjectsTransformed();

            _snapPivot.position += snapVector;
        }
    }
}
#endif