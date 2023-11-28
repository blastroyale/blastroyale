#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectOverlapFilter
    {
        private int                     _layerMask              = ~0;
        private GameObjectType          _objectTypes            = GameObjectType.Mesh | GameObjectType.Sprite | GameObjectType.Terrain;
        private HashSet<GameObject>     _ignoredObjects         = new HashSet<GameObject>();
        private bool                    _usePrimeFocusObjects   = false;
        private HashSet<GameObject>     _primeFocusObjects      = new HashSet<GameObject>();
        private GameObject              _ignoredHierarchy;

        public int                      layerMask               { get { return _layerMask; } set { _layerMask = value; } }
        public GameObjectType           objectTypes             { get { return _objectTypes; } set { _objectTypes = value; } }
        public Func<GameObject, bool>   customFilter            { get; set; }
        public bool                     usePrimeFocusObjects    { get { return _usePrimeFocusObjects; } set { _usePrimeFocusObjects = value; } }
        public GameObject               ignoredHierarchy        { get { return _ignoredHierarchy; } set { _ignoredHierarchy = value; } }

        public void clearIgnoredObjects()
        {
            _ignoredObjects.Clear();
        }

        public void setIgnoredObject(GameObject gameObject)
        {
            _ignoredObjects.Clear();
            _ignoredObjects.Add(gameObject);
        }

        public void setIgnoredObjects(List<GameObject> ignoredObjects)
        {
            _ignoredObjects.Clear();
            foreach (var go in ignoredObjects)
                _ignoredObjects.Add(go);
        }

        public void addIgnoredObject(GameObject ignoredObject)
        {
            _ignoredObjects.Add(ignoredObject);
        }

        public void addIgnoredObjects(IEnumerable<GameObject> ignoredObjects)
        {
            foreach (var ignoredObject in ignoredObjects)
                _ignoredObjects.Add(ignoredObject);
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
            if (_usePrimeFocusObjects && !_primeFocusObjects.Contains(gameObject)) return false;

            return LayerEx.isBitSet(_layerMask, gameObject.layer) && !_ignoredObjects.Contains(gameObject) &&
                    (GameObjectDataDb.instance.getGameObjectType(gameObject) & _objectTypes) != 0 && (customFilter == null || customFilter(gameObject)) &&
                    (_ignoredHierarchy == null || !gameObject.transform.IsChildOf(_ignoredHierarchy.transform));
        }
    }
}
#endif