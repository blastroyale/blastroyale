#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class ObjectTransformSessionSurface
    {
        public enum Type
        {
            Invalid = 0,
            Grid,
            Object
        }

        private Type            _type;
        private GameObject      _surfaceObject;
        private GameObjectType  _objectType;
        private Vector3         _pickPoint;
        private Plane           _plane;

        public Type             surfaceType         { get { return _type; } }
        public GameObject       surfaceObject       { get { return _surfaceObject; } }
        public GameObjectType   objectType          { get { return _objectType; } }
        public Vector3          pickPoint           { get { return _pickPoint; } }
        public Vector3          pickPointNormal     { get { return _plane.normal; } }
        public Plane            plane               { get { return _plane; } }

        public void makeInvalid()
        {
            _type           = Type.Invalid;
            _surfaceObject  = null;
        }

        public void pickFromScene(SceneRaycastFilter raycastFilter)
        {
            makeInvalid();

            GameObjectType invalidObjectTypes = GameObjectType.Camera | GameObjectType.Empty | GameObjectType.Light | GameObjectType.ParticleSystem;
            if ((raycastFilter.objectTypes & invalidObjectTypes) != 0) return;

            var sceneRayHit = PluginScene.instance.raycastClosest(PluginCamera.camera.getCursorRay(), raycastFilter, ObjectRaycastConfig.defaultConfig);
            if (!sceneRayHit.anyHit) return;

            if (sceneRayHit.wasGridHit && sceneRayHit.wasObjectHit)
            {
                if (sceneRayHit.gridHit.hitEnter < sceneRayHit.objectHit.hitEnter &&
                    Mathf.Abs(sceneRayHit.gridHit.hitEnter - sceneRayHit.objectHit.hitEnter) > 1e-4f) fromGridHit(sceneRayHit);
                else fromObjectHit(sceneRayHit);
            }
            else
            if (sceneRayHit.wasGridHit) fromGridHit(sceneRayHit);
            else
            if (sceneRayHit.wasObjectHit) fromObjectHit(sceneRayHit);
        }

        public bool pickFromSelf()
        {
            if (_type == Type.Invalid) return false;

            Ray ray = PluginCamera.camera.getCursorRay();
            if (_type == Type.Grid)
            {
                float t;
                if (_plane.Raycast(ray, out t))
                {
                    _pickPoint = ray.GetPoint(t);
                    return true;
                }
            }
            else
            if (_type == Type.Object)
            {
                if (_objectType == GameObjectType.Terrain)
                {
                    var objectRayHit    = _surfaceObject.raycastUnityTerrain(ray, ObjectRaycastConfig.defaultConfig.terrainConfig);
                    if (objectRayHit != null)
                    {
                        _pickPoint      = objectRayHit.hitPoint;
                        _plane          = new Plane(objectRayHit.hitNormal, _pickPoint);
                        return true;
                    }
                }
                else
                if (_objectType == GameObjectType.Mesh)
                {
                    var objectRayHit    = _surfaceObject.raycastMesh(ray, ObjectRaycastConfig.defaultConfig.meshConfig);
                    if (objectRayHit != null)
                    {
                        _pickPoint      = objectRayHit.hitPoint;
                        _plane          = new Plane(objectRayHit.hitNormal, _pickPoint);
                        return true;
                    }
                }
                else 
                if (_objectType == GameObjectType.Sprite)
                {
                    var objectRayHit    = _surfaceObject.raycastSprite(ray);
                    if (objectRayHit != null)
                    {
                        _pickPoint      = objectRayHit.hitPoint;
                        _plane          = new Plane(objectRayHit.hitNormal, _pickPoint);
                        return true;
                    }
                }
            }

            return true;
        }

        private void fromObjectHit(SceneRayHit sceneRayHit)
        {
            _plane          = sceneRayHit.objectHit.hitPlane;
            _pickPoint      = sceneRayHit.objectHit.hitPoint;
            _type           = Type.Object;
            _objectType     = GameObjectDataDb.instance.getGameObjectType(sceneRayHit.objectHit.hitObject);
            _surfaceObject  = sceneRayHit.objectHit.hitObject;
        }

        private void fromGridHit(SceneRayHit sceneRayHit)
        {
            _plane          = sceneRayHit.gridHit.hitPlane;
            _pickPoint      = sceneRayHit.gridHit.hitPoint;
            _type           = Type.Grid;
            _surfaceObject  = null;
        }
    }
}
#endif