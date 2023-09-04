#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class ObjectEraseBrush3D : ObjectEraseTool
    {
        private enum SurfaceType
        {
            Invalid = 0,
            Mesh,
            TerrainMesh,
            SphericalMesh,
            UnityTerrain,
            Grid
        }

        private struct Surface
        {
            public GameObject   gameObject;
            public SurfaceType  surfaceType;
            public Vector3      genericNormal;
            public Vector3      pickNormal;
            public Vector3      pickPoint;
            public bool         isLocked;

            public bool requiresCircleProjection()
            {
                return surfaceType == SurfaceType.UnityTerrain || 
                    surfaceType == SurfaceType.TerrainMesh || 
                    surfaceType == SurfaceType.SphericalMesh;
            }
            
            public bool isTerrain()
            {
                return surfaceType == SurfaceType.UnityTerrain || surfaceType == SurfaceType.TerrainMesh;
            }

            public void reset()
            {
                gameObject      = null;
                surfaceType     = SurfaceType.Invalid;
                genericNormal   = Vector3.zero;
                pickNormal      = Vector3.zero;
                pickPoint       = Vector3.zero;
                isLocked        = false;
            }
        }

        private static readonly int         _numCirclePoints            = 150;
        [NonSerialized]
        private List<Vector3>               _circlePoints               = new List<Vector3>(_numCirclePoints);

        [NonSerialized]
        private List<Vector3>               _vector3Buffer              = new List<Vector3>();
        [NonSerialized]
        private List<GameObject>            _boxObjectOverlapBuffer     = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>            _overlappedObjects          = new List<GameObject>();

        [NonSerialized]
        private ObjectRaycastConfig         _surfacePickRaycastConfig   = ObjectRaycastConfig.defaultConfig;
        [NonSerialized]
        private SceneRaycastFilter          _surfacePickRaycastFiler    = new SceneRaycastFilter();
        [NonSerialized]
        private ObjectOverlapFilter         _objectOveralpFilter        = new ObjectOverlapFilter();
        [NonSerialized]
        private ObjectOverlapConfig         _objectOverlapConfig        = ObjectOverlapConfig.defaultConfig;
        [NonSerialized]
        private ObjectBounds.QueryConfig    _overlappedBoundsQConfig    = new ObjectBounds.QueryConfig();
        [NonSerialized]
        private Surface                     _surface                    = new Surface();

        public override ObjectEraseToolId   toolId                      { get { return ObjectEraseToolId.Brush3D; } }

        public ObjectEraseBrush3D()
        {
            _objectOveralpFilter.objectTypes        = GameObjectType.Mesh | GameObjectType.Sprite;
            _objectOveralpFilter.customFilter       = (GameObject gameObject) => { return ObjectErase.instance.canEraseObject(gameObject); };
            _overlappedBoundsQConfig.objectTypes    = GameObjectType.Mesh | GameObjectType.Sprite;
        }

        protected override void doOnSceneGUI()
        {
            Event e = Event.current;

            updateSurface();
            updateCircleDrawPoints();
            if (_surface.surfaceType == SurfaceType.Invalid) return;

            if (e.type == EventType.MouseDown && e.button == 0) _surface.isLocked = true;
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                _surface.isLocked = false;
                detectOverlappedObjects();
                eraseGameObjects(_overlappedObjects);
            }
            else if (e.type == EventType.MouseDrag && e.button == 0)
            {
                detectOverlappedObjects();
                eraseGameObjects(_overlappedObjects);
            }
            else
            if (FixedShortcuts.changeHeightByScrollWheel(e))
            {
                e.disable();
                ObjectErase.instance.eraseBrush3DSettings.eraseHeight -= 0.1f * e.delta.y;
                EditorUtility.SetDirty(ObjectErase.instance.eraseBrush3DSettings);
            }
            else
            if (FixedShortcuts.changeRadiusByScrollWheel(e))
            {
                e.disable();
                ObjectErase.instance.eraseBrush3DSettings.radius -= 0.1f * e.delta.y;
                EditorUtility.SetDirty(ObjectErase.instance.eraseBrush3DSettings);
            }
        }

        protected override void draw()
        {
            if (_surface.surfaceType == SurfaceType.Invalid) return;

            if (_surface.requiresCircleProjection())
            {
                Material material = MaterialPool.instance.simpleDiffuse;
                material.setCullModeBack();
                material.setZTestEnabled(false);

                material.SetColor("_Color", ObjectErasePrefs.instance.brush3DBorderColor);
                material.SetPass(0);
                GLEx.drawLineLoop3D(_circlePoints);
            }
            else
            {
                Matrix4x4 circleTransform = calcCircleDrawTransform();
                Material material = MaterialPool.instance.simpleDiffuse;
                material.setCullModeOff();
                material.setZTestEnabled(true);

                material.SetColor("_Color", ObjectErasePrefs.instance.brush3DBorderColor);
                material.SetPass(0);
                Graphics.DrawMeshNow(MeshPool.instance.unitWireCircleXY, circleTransform);
            }

            HandlesEx.saveColor();
            Handles.color = ObjectErasePrefs.instance.brush3DHeightIndicatorColor;
            Handles.DrawLine(_surface.pickPoint, _surface.pickPoint + _surface.genericNormal * ObjectErase.instance.eraseBrush3DSettings.eraseHeight);
            HandlesEx.restoreColor();
        }

        private Matrix4x4 calcCircleDrawTransform()
        {
            float circleRadius      = ObjectErase.instance.eraseBrush3DSettings.radius;
            Vector3 offsetNormal    = _surface.isTerrain() ? _surface.genericNormal : _surface.pickNormal;
            return Matrix4x4.TRS(_surface.pickPoint + offsetNormal * 0.005f * HandleUtility.GetHandleSize(_surface.pickPoint), calcCircleRotation(), new Vector3(circleRadius, circleRadius, 1.0f));
        }

        private Quaternion calcCircleRotation()
        {
            return QuaternionEx.create(Vector3.forward, _surface.genericNormal, Vector3.right);
        }

        private void updateSurface()
        {
            if (_surface.isLocked)
            {
                Ray pickRay = PluginCamera.camera.getCursorRay();
                if (_surface.surfaceType == SurfaceType.Grid || _surface.surfaceType == SurfaceType.Mesh)
                {
                    float t;
                    Plane surfacePlane = new Plane(_surface.pickNormal, _surface.pickPoint);
                    if (surfacePlane.Raycast(pickRay, out t)) _surface.pickPoint = pickRay.GetPoint(t);
                }
                else
                if (_surface.surfaceType == SurfaceType.UnityTerrain)
                {
                    TerrainCollider terrainCollider = _surface.gameObject.getTerrainCollider();
                    if (terrainCollider != null)
                    {
                        RaycastHit rayHit;
                        if (terrainCollider.Raycast(pickRay, out rayHit, float.MaxValue))
                            _surface.pickPoint = pickRay.GetPoint(rayHit.distance);
                    }
                }
                else
                if (_surface.surfaceType == SurfaceType.TerrainMesh || _surface.surfaceType == SurfaceType.SphericalMesh)
                {
                    PluginMesh pluginMesh = PluginMeshDb.instance.getPluginMesh(_surface.gameObject.getMesh());
                    if (pluginMesh != null)
                    {
                        MeshRayHit rayHit;
                        MeshRaycastConfig meshRaycastConfig = MeshRaycastConfig.defaultConfig;
                        if (pluginMesh.raycastClosest(pickRay, _surface.gameObject.transform, meshRaycastConfig, out rayHit))
                            _surface.pickPoint = pickRay.GetPoint(rayHit.hitEnter);
                    }
                }
            }
            else
            {
                _surface.reset();

                var eraseSettings                       = ObjectErase.instance.eraseBrush3DSettings;
                _surfacePickRaycastFiler.layerMask      = eraseSettings.surfaceLayers;
                _surfacePickRaycastFiler.raycastGrid    = eraseSettings.allowsGridSurface;
                _surfacePickRaycastFiler.raycastObjects = eraseSettings.allowsMeshSurface | eraseSettings.allowsTerrainSurface;

                _surfacePickRaycastFiler.objectTypes    = GameObjectType.None;
                if (eraseSettings.allowsMeshSurface)    _surfacePickRaycastFiler.objectTypes |= GameObjectType.Mesh;
                if (eraseSettings.allowsTerrainSurface) _surfacePickRaycastFiler.objectTypes |= GameObjectType.Terrain;

                var rayHit = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), _surfacePickRaycastFiler, _surfacePickRaycastConfig);
                if (rayHit.anyHit)
                {
                    if (rayHit.wasObjectHit && !rayHit.wasGridHit) surfaceFromObjectHit(rayHit.objectHit);
                    else if (rayHit.wasGridHit && !rayHit.wasObjectHit) surfaceFromGridHit(rayHit.gridHit);
                    else
                    {
                        if (rayHit.gridHit.hitEnter < rayHit.objectHit.hitEnter &&
                            Mathf.Abs(rayHit.gridHit.hitEnter - rayHit.objectHit.hitEnter) > 1e-4f) surfaceFromGridHit(rayHit.gridHit);
                        else surfaceFromObjectHit(rayHit.objectHit);
                    }
                }
            }
        }

        private void updateCircleDrawPoints()
        {
            if (_surface.surfaceType == SurfaceType.UnityTerrain)
            {
                Terrain terrain             = _surface.gameObject.getTerrain();
                Matrix4x4 circleTransform   = calcCircleDrawTransform();
                Vector3 circleU             = circleTransform.getRight();
                Vector3 circleV             = circleTransform.getUp();
                CircleMesh.generateXYCirclePointsCW(_surface.pickPoint, ObjectErase.instance.eraseBrush3DSettings.radius, circleU, circleV, _numCirclePoints, _circlePoints);
                terrain.projectPoints(terrain.transform.position.y, _circlePoints);
            }
            else
            if (_surface.surfaceType == SurfaceType.TerrainMesh)
            {
                PluginMesh terrainMesh      = PluginMeshDb.instance.getPluginMesh(_surface.gameObject.getMesh());
                Matrix4x4 circleTransform   = calcCircleDrawTransform();
                Vector3 circleU             = circleTransform.getRight();
                Vector3 circleV             = circleTransform.getUp();
                CircleMesh.generateXYCirclePointsCW(_surface.pickPoint, ObjectErase.instance.eraseBrush3DSettings.radius, circleU, circleV, _numCirclePoints, _circlePoints);
                TerrainMeshUtil.projectPoints(_surface.gameObject, terrainMesh, _circlePoints);
            }
            else
            if (_surface.surfaceType == SurfaceType.SphericalMesh)
            {
                Matrix4x4 circleTransform   = calcCircleDrawTransform();
                Vector3 circleU             = circleTransform.getRight();
                Vector3 circleV             = circleTransform.getUp();
                CircleMesh.generateXYCirclePointsCW(_surface.pickPoint, ObjectErase.instance.eraseBrush3DSettings.radius, circleU, circleV, _numCirclePoints, _circlePoints);
                SphericalMeshUtil.projectPoints(_surface.gameObject, _circlePoints);
            }
        }

        private void surfaceFromObjectHit(ObjectRayHit objectHit)
        {
            _surface.gameObject             = objectHit.hitObject;
            GameObjectType gameObjectType   = GameObjectDataDb.instance.getGameObjectType(_surface.gameObject);
            _surface.surfaceType            = gameObjectType == GameObjectType.Terrain ? SurfaceType.UnityTerrain : SurfaceType.Mesh;
            _surface.genericNormal          = gameObjectType == GameObjectType.Terrain ? _surface.gameObject.transform.up : objectHit.hitNormal;
            _surface.pickNormal             = objectHit.hitNormal;
            _surface.pickPoint              = objectHit.hitPoint;

            if (_surface.gameObject.isTerrainMesh())
            {
                _surface.surfaceType        = SurfaceType.TerrainMesh;
                _surface.genericNormal      = ObjectPrefs.instance.getTerrainMeshUp(_surface.gameObject);
            }
            else
            if (_surface.gameObject.isSphericalMesh())
            {
                _surface.surfaceType        = SurfaceType.SphericalMesh;
            }
        }

        private void surfaceFromGridHit(GridRayHit gridHit)
        {
            _surface.gameObject             = null;
            _surface.surfaceType            = SurfaceType.Grid;
            _surface.genericNormal          = gridHit.hitNormal;
            _surface.pickNormal             = gridHit.hitNormal;
            _surface.pickPoint              = gridHit.hitPoint;
        }

        private void detectOverlappedObjects()
        {
            _overlappedObjects.Clear();

            OBB overlapBox = calcObjectOverlapBox();
            if (_surface.gameObject != null) _objectOveralpFilter.setIgnoredObject(_surface.gameObject);
            if (PluginScene.instance.overlapBox(overlapBox, _objectOveralpFilter, _objectOverlapConfig, _boxObjectOverlapBuffer))
            {
                Quaternion circleRotation   = calcCircleRotation();
                Vector3 circleU             = circleRotation * Vector3.right;
                Vector3 circleV             = circleRotation * Vector3.up;
                float eraseHeight           = ObjectErase.instance.eraseBrush3DSettings.eraseHeight;
                float circleRadius          = ObjectErase.instance.eraseBrush3DSettings.radius;
                bool allowPartialOverlap    = ObjectErase.instance.eraseBrush3DSettings.allowPartialOverlap;

                if (_surface.surfaceType == SurfaceType.UnityTerrain)
                {
                    const float onTerrainEps = 1e-6f;
                    Terrain terrain = _surface.gameObject.getTerrain();
                    foreach (var go in _boxObjectOverlapBuffer)
                    {
                        OBB obb = ObjectBounds.calcWorldOBB(go, _overlappedBoundsQConfig);
                        if (!Box3D.isSpanningOrOnOrInFrontOfUnityTerrain(obb.center, obb.size, obb.rotation, terrain, onTerrainEps, eraseHeight)) continue;
     
                        if (allowPartialOverlap)
                        {
                            if (Circle3D.intersectsOBBAsInfiniteCylinder(_surface.pickPoint, circleRadius, circleU, circleV, obb)) _overlappedObjects.Add(go);
                        }
                        else
                        {
                            Box3D.calcCorners(obb.center, obb.size, obb.rotation, _vector3Buffer, false);
                            if (Circle3D.containsPointsAsInfiniteCylinder(_surface.pickPoint, circleRadius, circleU, circleV, _vector3Buffer)) _overlappedObjects.Add(go);
                        }
                    }
                }
                else
                if (_surface.surfaceType == SurfaceType.TerrainMesh)
                {
                    const float onTerrainEps    = 1e-6f;
                    PluginMesh terrainMesh      = PluginMeshDb.instance.getPluginMesh(_surface.gameObject.getMesh());            
                    foreach (var go in _boxObjectOverlapBuffer)
                    {
                        OBB obb = ObjectBounds.calcWorldOBB(go, _overlappedBoundsQConfig);
                        if (!Box3D.isSpanningOrOnOrInFrontOfTerrainMesh(obb.center, obb.size, obb.rotation, _surface.gameObject, terrainMesh, onTerrainEps, eraseHeight)) continue;

                        if (allowPartialOverlap)
                        {
                            if (Circle3D.intersectsOBBAsInfiniteCylinder(_surface.pickPoint, circleRadius, circleU, circleV, obb)) _overlappedObjects.Add(go);
                        }
                        else
                        {
                            Box3D.calcCorners(obb.center, obb.size, obb.rotation, _vector3Buffer, false);
                            if (Circle3D.containsPointsAsInfiniteCylinder(_surface.pickPoint, circleRadius, circleU, circleV, _vector3Buffer)) _overlappedObjects.Add(go);
                        }
                    }
                }
                else
                if (_surface.surfaceType == SurfaceType.Grid || _surface.surfaceType == SurfaceType.Mesh)
                {
                    // Note: Lift the plane up a bit to avoid deleting objects below or sitting exactly on the surface plane.
                    const float onPlaneEps  = 1e-6f;
                    Plane surfacePlane      = new Plane(_surface.pickNormal, _surface.pickPoint + _surface.pickNormal * 1e-3f);
                    foreach(var go in _boxObjectOverlapBuffer)
                    {
                        OBB obb = ObjectBounds.calcWorldOBB(go, _overlappedBoundsQConfig);
                        if (!Box3D.isSpanningOrOnOrInFrontOfPlane(obb.center, obb.size, obb.rotation, surfacePlane, onPlaneEps, eraseHeight)) continue;

                        if (allowPartialOverlap)
                        {
                            if (Circle3D.intersectsOBBAsInfiniteCylinder(_surface.pickPoint, circleRadius, circleU, circleV, obb)) _overlappedObjects.Add(go);
                        }
                        else
                        {
                            Box3D.calcCorners(obb.center, obb.size, obb.rotation, _vector3Buffer, false);
                            if (Circle3D.containsPointsAsInfiniteCylinder(_surface.pickPoint, circleRadius, circleU, circleV, _vector3Buffer)) _overlappedObjects.Add(go);
                        }
                    }
                }
                else
                if (_surface.surfaceType == SurfaceType.SphericalMesh)
                {
                    const float onSphereEps = 1e-6f;
                    foreach (var go in _boxObjectOverlapBuffer)
                    {
                        OBB obb = ObjectBounds.calcWorldOBB(go, _overlappedBoundsQConfig);
                        if (!Box3D.isSpanningOrOnOrInFrontOfSphericalMesh(obb.center, obb.size, obb.rotation, _surface.gameObject, onSphereEps, eraseHeight)) continue;

                        if (allowPartialOverlap)
                        {
                            if (Circle3D.intersectsOBBAsInfiniteCylinder(_surface.pickPoint, circleRadius, circleU, circleV, obb)) _overlappedObjects.Add(go);
                        }
                        else
                        {
                            Box3D.calcCorners(obb.center, obb.size, obb.rotation, _vector3Buffer, false);
                            if (Circle3D.containsPointsAsInfiniteCylinder(_surface.pickPoint, circleRadius, circleU, circleV, _vector3Buffer)) _overlappedObjects.Add(go);
                        }
                    }
                }
            }
        }

        private OBB calcObjectOverlapBox()
        {
            float circleRadius          = ObjectErase.instance.eraseBrush3DSettings.radius;
            float eraseHeight           = ObjectErase.instance.eraseBrush3DSettings.eraseHeight;
            if (_surface.surfaceType == SurfaceType.UnityTerrain)
            {
                AABB circlePointsAABB   = new AABB(_circlePoints);
                OBB overlapBox          = new OBB();
                overlapBox.rotation     = calcCircleRotation();
                overlapBox.center       = circlePointsAABB.center;
                overlapBox.size         = new Vector3(circleRadius * 2.0f, circleRadius * 2.0f, circlePointsAABB.size.y + eraseHeight);
                return overlapBox;
            }
            else
            if (_surface.surfaceType == SurfaceType.TerrainMesh)
            {
                AABB circlePointsAABB   = new AABB(_circlePoints);
                OBB overlapBox          = new OBB();
                overlapBox.rotation     = calcCircleRotation();
                overlapBox.center       = circlePointsAABB.center;
                overlapBox.size         = new Vector3(circleRadius * 2.0f, circleRadius * 2.0f, circlePointsAABB.size.magnitude + eraseHeight);
                return overlapBox;
            }
            else
            if (_surface.surfaceType == SurfaceType.Grid || _surface.surfaceType == SurfaceType.Mesh)
            {
                float boxHeight         = eraseHeight;
                OBB overlapBox          = new OBB();
                overlapBox.rotation     = calcCircleRotation();
                overlapBox.center       = _surface.pickPoint + _surface.pickNormal * (boxHeight * 0.5f);
                overlapBox.size         = new Vector3(circleRadius * 2.0f, circleRadius * 2.0f, boxHeight);
                return overlapBox;
            }
            else
            if (_surface.surfaceType == SurfaceType.SphericalMesh)
            {
                AABB circlePointsAABB   = new AABB(_circlePoints);
                OBB overlapBox          = new OBB();
                overlapBox.rotation     = calcCircleRotation();
                overlapBox.center       = circlePointsAABB.center;
                overlapBox.size         = new Vector3(circleRadius * 2.0f, circleRadius * 2.0f, circlePointsAABB.size.magnitude + eraseHeight);
                return overlapBox;
            }
            else return OBB.getInvalid();
        }
    }
}
#endif