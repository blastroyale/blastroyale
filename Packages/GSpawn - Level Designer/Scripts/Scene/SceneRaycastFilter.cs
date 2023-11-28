#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class SceneRaycastFilter
    {
        private int                     _layerMask              = ~0;
        private GameObjectType          _objectTypes            = GameObjectType.Mesh | GameObjectType.Sprite | GameObjectType.Terrain;
        private HashSet<GameObject>     _ignoredObjects         = new HashSet<GameObject>();
        private bool                    _raycastGrid            = true;
        private bool                    _raycastObjects         = true;
        private bool                    _usePrimeFocusObjects   = false;
        private HashSet<GameObject>     _primeFocusObjects      = new HashSet<GameObject>();

        public Func<GameObject, bool>   customFilter            { get; set; }
        public int                      layerMask               { get { return _layerMask; } set { _layerMask = value; } }
        public GameObjectType           objectTypes             { get { return _objectTypes; } set { _objectTypes = value; } }
        public bool                     raycastGrid             { get { return _raycastGrid; } set { _raycastGrid = value; } }
        public bool                     raycastObjects          { get { return _raycastObjects; } set { _raycastObjects = value; } }
        public bool                     usePrimeFocusObjects    { get { return _usePrimeFocusObjects; } set { _usePrimeFocusObjects = value; } }

        public void clearIgnoredObjects()
        {
            _ignoredObjects.Clear();
        }

        public void setIgnoredObjects(IEnumerable<GameObject> ignoredObjects)
        {
            _ignoredObjects.Clear();
            foreach (var go in ignoredObjects)
                _ignoredObjects.Add(go);
        }

        public void setIgnoredObject(GameObject gameObject)
        {
            _ignoredObjects.Clear();
            _ignoredObjects.Add(gameObject);
        }

        public void addIgnoredObject(GameObject gameObject)
        {
            _ignoredObjects.Add(gameObject);
        }

        public void addIgnoredObjects(IEnumerable<GameObject> ignoredObjects)
        {
            foreach (var go in ignoredObjects)
                _ignoredObjects.Add(go);
        }

        public void clearPrimeFocusObjects()
        {
            _primeFocusObjects.Clear();
        }

        public void setPrimeFocusObject(GameObject gameObject)
        {
            _primeFocusObjects.Clear();
            _primeFocusObjects.Add(gameObject);
        }

        public void setPrimeFocusObjects(IEnumerable<GameObject> gameObjects)
        {
            _primeFocusObjects.Clear();
            foreach (var go in gameObjects)
                _primeFocusObjects.Add(go);
        }

        public bool filterObject(GameObject gameObject)
        {
            if (customFilter != null && !customFilter(gameObject)) return false;
            if (_usePrimeFocusObjects && !_primeFocusObjects.Contains(gameObject)) return false;

            return LayerEx.isBitSet(_layerMask, gameObject.layer) && !_ignoredObjects.Contains(gameObject) &&
                    (GameObjectDataDb.instance.getGameObjectType(gameObject) & _objectTypes) != 0;
        }
    }
}
#endif