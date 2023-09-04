#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectVertexSnapSession : ObjectTransformSession
    {
        private class SnapPivot
        {
            public enum Owner
            {
                Mesh,
                Other
            }

            public bool         isAvailable;
            public Vector3      position;
            public Vector3      normal;
            public Vector3[]    triangleVerts   = new Vector3[3];
            public Owner        owner           = Owner.Other;
        }

        private SnapPivot                           _snapPivot                  = new SnapPivot();
        private SceneRaycastFilter                  _pickSnapPivotRaycastFilter = new SceneRaycastFilter() { objectTypes = GameObjectType.Mesh | GameObjectType.Sprite, usePrimeFocusObjects = true };
        private SceneRaycastFilter                  _pickSnapDestRaycastFilter  = new SceneRaycastFilter();

        private List<GameObject>                    _gameObjectBuffer           = new List<GameObject>();
        private List<ObjectRayHit>                  _objectRayHitBuffer         = new List<ObjectRayHit>();
        private List<Vector3>                       _vector3Buffer              = new List<Vector3>();
        private Vector3[]                           _triangleVertBuffer         = new Vector3[3];
        private Vector3[]                           _quadVertBuffer             = new Vector3[5];

        public ObjectVertexSnapSettings             sharedSettings              { get; set; }
        public override string                      sessionName                 { get { return "Vertex Snap"; } }
        public override ObjectTransformSessionType  sessionType                 { get { return ObjectTransformSessionType.VertexSnap; } }

        protected override void update()
        {
            if (Mouse.instance.isButtonDown(0)) snap();
            else pickSnapVertex();
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
            if (_snapPivot.isAvailable)
            {
                if (_snapPivot.owner == SnapPivot.Owner.Mesh)
                {
                    Handles.color = sessionPrefs.vertSnapTriangleWireColor;
                    Handles.DrawLine(_snapPivot.triangleVerts[0], _snapPivot.triangleVerts[1]);
                    Handles.DrawLine(_snapPivot.triangleVerts[1], _snapPivot.triangleVerts[2]);
                    Handles.DrawLine(_snapPivot.triangleVerts[2], _snapPivot.triangleVerts[0]);
                }

                Handles.color = sessionPrefs.vertSnapTickColor;
                Handles.DotHandleCap(0, _snapPivot.position, Quaternion.identity, HandleUtility.GetHandleSize(_snapPivot.position) * sessionPrefs.vertSnapTickSize, EventType.Repaint);
            }

            HandlesEx.restoreColor();
        }

        private void pickSnapVertex()
        {
            _snapPivot.isAvailable = false;
            PluginScene.instance.raycastAll(PluginCamera.camera.getCursorRay(), _pickSnapPivotRaycastFilter, ObjectRaycastConfig.defaultConfig, true, _objectRayHitBuffer);

            if (_objectRayHitBuffer.Count != 0)
            {
                var closestHit = _objectRayHitBuffer[0];
                GameObjectType objectType = GameObjectDataDb.instance.getGameObjectType(closestHit.hitObject);
                if (objectType == GameObjectType.Mesh)
                {
                    PluginMesh pluginMesh = PluginMeshDb.instance.getPluginMesh(closestHit.hitObject.getMesh());
                    if (pluginMesh != null)
                    {
                        pluginMesh.getTriangleVerts(closestHit.meshRayHit.triangleIndex, _snapPivot.triangleVerts, closestHit.hitObject.transform);
                        int closestVert             = Vector3Ex.findIndexOfPointClosestToPoint(_snapPivot.triangleVerts, closestHit.hitPoint);
                        if (closestVert >= 0)
                        {
                            _snapPivot.isAvailable  = true;
                            _snapPivot.position     = _snapPivot.triangleVerts[closestVert];
                            _snapPivot.normal       = closestHit.hitNormal;
                            _snapPivot.owner        = SnapPivot.Owner.Mesh;
                        }
                    }
                }
                if (objectType == GameObjectType.Sprite)
                {
                    OBB worldOBB = ObjectBounds.calcSpriteWorldOBB(closestHit.hitObject);
                    if (worldOBB.isValid)
                    {
                        worldOBB.calcCenterAndCorners(_vector3Buffer);
                        int closestCorner           = Vector3Ex.findIndexOfPointClosestToPoint(_vector3Buffer, closestHit.hitPoint);
                        if (closestCorner >= 0)
                        {
                            _snapPivot.isAvailable  = true;
                            _snapPivot.position     = _vector3Buffer[closestCorner];
                            _snapPivot.normal       = closestHit.hitNormal;
                            _snapPivot.owner        = SnapPivot.Owner.Other;
                        }
                    }
                }
            }
        }

        private void snap()
        {
            // Note: We need to check if the mouse has moved. It seems that if the
            //       mouse stays in the same position, due to (maybe) floating point
            //       rounding errors, the objects can snap to one plane and the
            //       vertex to another.
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
                    else if (objectType == GameObjectType.Terrain) snapToTerrainObject(sceneRayHit.objectHit);
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
            var pluginMesh      = PluginMeshDb.instance.getPluginMesh(objectHit.hitObject.getMesh());
            if (pluginMesh != null)
            {
                pluginMesh.getTriangleVerts(objectHit.meshRayHit.triangleIndex, _triangleVertBuffer, objectHit.hitObject.transform);
                var closestPt   = Vector3Ex.findIndexOfPointClosestToPoint(_triangleVertBuffer, objectHit.hitPoint);
                if (closestPt >= 0) applySnapVector(_triangleVertBuffer[closestPt] - _snapPivot.position);
            }
        }

        private void snapToTerrainObject(ObjectRayHit objectHit)
        {
            Terrain terrain     = objectHit.hitObject.getTerrain();
            terrain.calcQuadCorners(objectHit.hitPoint, _quadVertBuffer);
            var closestPt       = Vector3Ex.findIndexOfPointClosestToPoint(_quadVertBuffer, objectHit.hitPoint);
            if (closestPt >= 0) applySnapVector(_quadVertBuffer[closestPt] - _snapPivot.position);
        }

        private void snapToSpriteObject(ObjectRayHit objectHit)
        {
            var spriteOBB       = ObjectBounds.calcSpriteWorldOBB(objectHit.hitObject);
            if (spriteOBB.isValid)
            {
                spriteOBB.calcCenterAndCorners(_vector3Buffer);
                var closestPt   = Vector3Ex.findIndexOfPointClosestToPoint(_vector3Buffer, objectHit.hitPoint);
                if (closestPt >= 0) applySnapVector(_vector3Buffer[closestPt] - _snapPivot.position);
            }
        }

        private void applySnapVector(Vector3 snapVector)
        {
            GameObjectEx.getParents(_targetObjects, _gameObjectBuffer);
            UndoEx.recordGameObjectTransforms(_gameObjectBuffer);
            foreach (var parent in _gameObjectBuffer)
                parent.transform.position += snapVector;
            ObjectEvents.onObjectsTransformed();

            _snapPivot.position += snapVector;
            if (_snapPivot.owner == SnapPivot.Owner.Mesh)
            {
                _snapPivot.triangleVerts[0] += snapVector;
                _snapPivot.triangleVerts[1] += snapVector;
                _snapPivot.triangleVerts[2] += snapVector;
            }
        }
    }
}
#endif