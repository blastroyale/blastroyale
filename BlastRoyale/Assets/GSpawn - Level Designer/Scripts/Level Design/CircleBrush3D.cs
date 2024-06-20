#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public enum CircleBrush3DSurfaceType
    {
        UnityTerrain = 0,
        TerrainMesh,
        SphericalMesh,
        Mesh,
        Grid,
        Invalid
    }

    public struct CircleBrush3DSurface
    {
        public CircleBrush3DSurfaceType surfaceType;
        public Vector3                  normal;
        public Vector3                  pickPoint;
        public GameObject               gameObject;
        public GameObjectType           surfaceGameObjectType;
    }

    public class CircleBrush3D
    {
        private CircleBrush3DSurface        _surface                            = new CircleBrush3DSurface();
        private SceneRaycastFilter          _surfaceRaycastFilter               = new SceneRaycastFilter();
        private ObjectRaycastConfig         _objectRaycastConfig                = ObjectRaycastConfig.defaultConfig;

        private List<Vector3>               _circlePoints                       = new List<Vector3>();
        private const int                   _numCirclePoints                    = 100;
        private Vector3                     _u;
        private Vector3                     _v;
        private float                       _radius;

        private Vector3                     _strokeDirection                    = Vector3.zero;
        private Vector3                     _projectedStrokeDirection           = Vector3.zero;
        private Vector3                     _avgProjectedStrokeDirection        = Vector3.zero;
        private float                       _strokeDelta                        = 0.0f;
        private float                       _projectedStrokeDelta               = 0.0f;

        public CircleBrush3DSurface         surface                             { get { return _surface; } }
        public CircleBrush3DSurfaceType     surfaceType                         { get { return _surface.surfaceType; } }
        public bool                         isSurfaceUnityTerrain               { get { return _surface.surfaceType == CircleBrush3DSurfaceType.UnityTerrain; } }
        public bool                         isSurfaceTerrainMesh                { get { return _surface.surfaceType == CircleBrush3DSurfaceType.TerrainMesh; } }
        public bool                         isSurfaceSphericalMesh              { get { return _surface.surfaceType == CircleBrush3DSurfaceType.SphericalMesh; } }
        public bool                         isSurfaceMesh                       { get { return _surface.surfaceType == CircleBrush3DSurfaceType.Mesh; } }
        public bool                         isSurfaceObject                     { get { return !isSurfaceGrid; } }
        public bool                         isSurfaceGrid                       { get { return _surface.surfaceType == CircleBrush3DSurfaceType.Grid; } }
        public bool                         isSurfaceValid                      { get { return _surface.surfaceType != CircleBrush3DSurfaceType.Invalid; } }
        public Vector3                      surfaceNormal                       { get { return _surface.normal; } }
        public Vector3                      surfacePickPoint                    { get { return _surface.pickPoint; } }
        public Plane                        surfacePlane                        { get { return new Plane(surfaceNormal, surfacePickPoint); } }
        public GameObject                   surfaceGameObject                   { get { return _surface.gameObject; } }
        public GameObjectType               surfaceGameObjectType               { get { return _surface.surfaceGameObjectType; } }
        public Vector3                      u                                   { get { return _u; } }
        public Vector3                      v                                   { get { return _v; } }
        public Vector3                      normal                              { get { return Vector3.Cross(_u, _v).normalized; } }
        public Plane                        plane                               { get { return new Plane(normal, surfacePickPoint); } }
        public float                        radius                              { get { return _radius; } set { _radius = Mathf.Max(0.0f, value); } }
        public Vector3                      strokeDirection                     { get { return _strokeDirection; } }
        public float                        strokeDelta                         { get { return _strokeDelta; } }
        public Vector3                      projectedStrokeDirection            { get { return _projectedStrokeDirection; } }
        public Vector3                      avgProjectedStrokeDirection         { get { return _avgProjectedStrokeDirection; } }
        public float                        projectedStrokeDelta                { get { return _projectedStrokeDelta; } }
        public int                          surfaceLayers                       { get; set; }
        public GameObjectType               surfaceObjectTypes                  { get; set; }
        public bool                         allowGridSurface                    { get; set; }
        public bool                         surfaceLocked                       { get; set; }

        public void updateSurface()
        {
            Vector3 oldPickPoint = _surface.pickPoint;

            if (!surfaceLocked || !tryKeepSurface()) pickNewSurface();
            updateCircleUV();

            if (surfaceLocked)
            {
                _strokeDirection                = (_surface.pickPoint - oldPickPoint);
                _strokeDelta                    = _strokeDirection.magnitude;

                _projectedStrokeDirection       = new Plane(normal, 0.0f).projectPoint(_strokeDirection);
                _projectedStrokeDelta           = _projectedStrokeDirection.magnitude;

                _avgProjectedStrokeDirection    += _projectedStrokeDirection;
                _avgProjectedStrokeDirection.Normalize();
            }
            else
            {
                _strokeDirection                = Vector3.zero;
                _strokeDelta                    = 0.0f;
                _projectedStrokeDirection       = Vector3.zero;
                _projectedStrokeDelta           = 0.0f;
                _avgProjectedStrokeDirection    = Vector3.zero;
            }
        }

        public Vector3 calcRandomPoint()
        {
            return Circle3D.calcRandomPoint(surfacePickPoint, _radius, _u, _v);
        }

        public void draw(Color borderColor)
        {
            if (!isSurfaceValid) return;

            HandlesEx.saveColor();
            HandlesEx.saveMatrix();

            Handles.color = borderColor;
            Vector3 ptOffset = _surface.normal * 1e-3f;

            CircleMesh.generateXYCirclePointsCW(_surface.pickPoint, radius, u, _v, _numCirclePoints, _circlePoints);

            if (isSurfaceUnityTerrain)
            {
                Terrain unityTerrain    = _surface.gameObject.getTerrain();
                float terrainYPos       = _surface.gameObject.transform.position.y;

                for (int i = 0; i < _numCirclePoints; ++i)
                {
                    Vector3 firstPt     = unityTerrain.projectPoint(terrainYPos, _circlePoints[i]) + ptOffset;
                    Vector3 secondPt    = unityTerrain.projectPoint(terrainYPos, _circlePoints[(i + 1) % _numCirclePoints]) + ptOffset;
                    Handles.DrawLine(firstPt, secondPt);
                }
            }
            else
            if (isSurfaceTerrainMesh)
            {
                PluginMesh terrainMesh  = PluginMeshDb.instance.getPluginMesh(_surface.gameObject.getMesh());
                if (terrainMesh == null) return;

                for (int i = 0; i < _numCirclePoints; ++i)
                {
                    Vector3 firstPt     = TerrainMeshUtil.projectPoint(_surface.gameObject, terrainMesh, _circlePoints[i]) + ptOffset;
                    Vector3 secondPt    = TerrainMeshUtil.projectPoint(_surface.gameObject, terrainMesh, _circlePoints[(i + 1) % _numCirclePoints]) + ptOffset;
                    Handles.DrawLine(firstPt, secondPt);
                }
            }
            else
            if (isSurfaceSphericalMesh)
            {
                PluginMesh sphericalMesh    = PluginMeshDb.instance.getPluginMesh(_surface.gameObject.getMesh());
                if (sphericalMesh == null) return;

                for (int i = 0; i < _numCirclePoints; ++i)
                {
                    Vector3 firstPt         = SphericalMeshUtil.projectPoint(_surface.gameObject, sphericalMesh, _circlePoints[i]) + ptOffset;
                    Vector3 secondPt        = SphericalMeshUtil.projectPoint(_surface.gameObject, sphericalMesh, _circlePoints[(i + 1) % _numCirclePoints]) + ptOffset;
                    Handles.DrawLine(firstPt, secondPt);
                }
            }
            else
            {
                for (int i = 0; i < _numCirclePoints; ++i)
                {
                    Vector3 firstPt     = _circlePoints[i] + ptOffset;
                    Vector3 secondPt    = _circlePoints[(i + 1) % _numCirclePoints] + ptOffset;
                    Handles.DrawLine(firstPt, secondPt);
                }
            }

            HandlesEx.restoreMatrix();
            HandlesEx.restoreColor();
        }

        private bool pickNewSurface()
        {
            _surface.surfaceType                    = CircleBrush3DSurfaceType.Invalid;

            _surfaceRaycastFilter.objectTypes       = GameObjectType.None;
            if ((surfaceObjectTypes & GameObjectType.Mesh) != 0) _surfaceRaycastFilter.objectTypes |= GameObjectType.Mesh;
            if ((surfaceObjectTypes & GameObjectType.Terrain) != 0) _surfaceRaycastFilter.objectTypes |= GameObjectType.Terrain;

            _surfaceRaycastFilter.raycastGrid       = allowGridSurface;
            _surfaceRaycastFilter.raycastObjects    = _surfaceRaycastFilter.objectTypes != GameObjectType.None;
            _surfaceRaycastFilter.layerMask         = surfaceLayers;

            Ray ray = PluginCamera.camera.getCursorRay();
            SceneRayHit rayHit = PluginScene.instance.raycastClosest(ray, _surfaceRaycastFilter, _objectRaycastConfig);
            if (rayHit.wasObjectHit && !rayHit.wasGridHit) surfaceFromObjectHit(rayHit.objectHit);
            else if (rayHit.wasGridHit && !rayHit.wasObjectHit) surfaceFromGridHit(rayHit.gridHit);
            if (rayHit.wasGridHit && rayHit.wasObjectHit)
            {
                float absDelta = Mathf.Abs(rayHit.gridHit.hitEnter - rayHit.objectHit.hitEnter);
                if (rayHit.gridHit.hitEnter < rayHit.objectHit.hitEnter && absDelta > 1e-4f) surfaceFromGridHit(rayHit.gridHit);
                else surfaceFromObjectHit(rayHit.objectHit);
            }

            return _surface.surfaceType != CircleBrush3DSurfaceType.Invalid;
        }

        private bool tryKeepSurface()
        {
            if (_surface.surfaceType == CircleBrush3DSurfaceType.Invalid) return false;

            Ray ray = PluginCamera.camera.getCursorRay();
            if (_surface.surfaceType == CircleBrush3DSurfaceType.UnityTerrain)
            {
                var rayHit = _surface.gameObject.raycastUnityTerrain(ray, _objectRaycastConfig.terrainConfig);
                if (rayHit != null)
                {
                    surfaceFromObjectHit(rayHit);
                    return true;
                }
            }
            else 
            if (_surface.surfaceType == CircleBrush3DSurfaceType.TerrainMesh ||
                _surface.surfaceType == CircleBrush3DSurfaceType.SphericalMesh ||
                _surface.surfaceType == CircleBrush3DSurfaceType.Mesh)
            {
                var rayHit = _surface.gameObject.raycastMesh(ray, _objectRaycastConfig.meshConfig);
                if (rayHit != null)
                {
                    surfaceFromObjectHit(rayHit);
                    return true;
                }
            }
            else
            {
                var gridRayHit = PluginScene.instance.raycastGrid(ray);
                if (gridRayHit != null)
                {
                    surfaceFromGridHit(gridRayHit);
                    return true;
                }
            }

            _surface.surfaceType = CircleBrush3DSurfaceType.Invalid;
            return false;
        }

        private void updateCircleUV()
        {
            if (_surface.surfaceType == CircleBrush3DSurfaceType.UnityTerrain)
            {
                _u = Vector3.right;
                _v = Vector3.back;   
            }
            else
            if (_surface.surfaceType == CircleBrush3DSurfaceType.TerrainMesh)
            {
                // Note: Assume Y axis initially.
                _u = Vector3.right;
                _v = Vector3.back;

                if (ObjectPrefs.instance.terrainMeshUpAxis == Axis.X)
                {
                    _u = Vector3.forward;
                    _v = Vector3.down;
                }
                else
                if (ObjectPrefs.instance.terrainMeshUpAxis == Axis.Z)
                {
                    _u = Vector3.right;
                    _v = Vector3.up;
                }
            }
            else
            if (_surface.surfaceType == CircleBrush3DSurfaceType.SphericalMesh ||
                _surface.surfaceType == CircleBrush3DSurfaceType.Mesh)
            {
                _u = Vector3.right;
                _v = Vector3.back;

                Vector3 circleNormal    = Vector3.up;
                Vector3 rotationAxis    = Vector3.Cross(circleNormal, _surface.normal).normalized;
                float rotationAngle     = Vector3.SignedAngle(circleNormal, _surface.normal, rotationAxis);
                Quaternion rotation     = Quaternion.AngleAxis(rotationAngle, rotationAxis);

                _u = rotation * u;
                _v = rotation * _v;
            }
            else
            {
                _u = PluginScene.instance.grid.rotation * Vector3.right;
                _v = PluginScene.instance.grid.rotation * Vector3.back;
            }
        }

        private void surfaceFromObjectHit(ObjectRayHit rayHit)
        {
            _surface.gameObject     = rayHit.hitObject;
            _surface.normal         = rayHit.hitNormal;
            _surface.pickPoint      = rayHit.hitPoint;

            GameObjectType objectType       = GameObjectDataDb.instance.getGameObjectType(rayHit.hitObject);
            _surface.surfaceGameObjectType  = objectType;
            if (objectType == GameObjectType.Terrain) _surface.surfaceType = CircleBrush3DSurfaceType.UnityTerrain;
            else if (objectType == GameObjectType.Mesh)
            {
                if (rayHit.hitObject.isSphericalMesh()) _surface.surfaceType = CircleBrush3DSurfaceType.SphericalMesh;
                else if (rayHit.hitObject.isTerrainMesh()) _surface.surfaceType = CircleBrush3DSurfaceType.TerrainMesh;
                else _surface.surfaceType = CircleBrush3DSurfaceType.Mesh;
            }
        }

        private void surfaceFromGridHit(GridRayHit rayHit)
        {
            _surface.surfaceType    = CircleBrush3DSurfaceType.Grid;
            _surface.gameObject     = null;
            _surface.normal         = rayHit.hitNormal;
            _surface.pickPoint      = rayHit.hitPoint;
        }
    }
}
#endif
