#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public class PrefabData
    {
        [SerializeField]
        private bool                    _isDirty            = true;
        [SerializeField]
        private GameObject              _prefabAsset;
        [SerializeField]
        private bool                    _hasMeshes;
        [SerializeField]
        private bool                    _hasSprites;
        [SerializeField]
        private bool                    _hasTerrains;
        [SerializeField]
        private bool                    _hasLights;
        [SerializeField]
        private bool                    _hasParticleSystems;
        [SerializeField]
        private bool                    _hasCameras;
        [SerializeField]
        private List<PrefabMeshObject>  _meshObjects        = new List<PrefabMeshObject>();
        [SerializeField]
        private Vector3                 _modelSize          = Vector3.zero;

        public GameObject               prefabAsset         { get { return _prefabAsset; } }
        public bool                     hasMeshes           { get { if (_isDirty) refresh(); return _hasMeshes; } }
        public bool                     hasSprites          { get { if (_isDirty) refresh(); return _hasSprites; } }
        public bool                     hasTerrains         { get { if (_isDirty) refresh(); return _hasTerrains; } }
        public bool                     hasLights           { get { if (_isDirty) refresh(); return _hasLights; } }
        public bool                     hasParticleSystems  { get { if (_isDirty) refresh(); return _hasParticleSystems; } }
        public bool                     hasCameras          { get { if (_isDirty) refresh(); return _hasCameras; } }
        public bool                     hasVolume           { get { return hasMeshes || hasSprites || hasTerrains; } }
        public Vector3                  modelSize           { get { return _modelSize; } }
        public int                      numMeshObjects      { get { if (_isDirty) refresh(); return _meshObjects.Count; } }

        public PrefabData(GameObject prefab)
        {
            _prefabAsset = prefab;
            _isDirty = true;
            calcModelSize();
        }

        public PrefabMeshObject getPrefabMeshObject(int index)
        {
            if (_isDirty) refresh();
            return _meshObjects[index];
        }

        private void refresh()
        {
            _hasMeshes          = _prefabAsset.hierarchyHasMesh(false, false);
            _hasSprites         = _prefabAsset.hierarchyHasSprite(false, false);
            _hasTerrains        = _prefabAsset.hierarchyHasTerrain(false, false);
            _hasLights          = _prefabAsset.hierarchyHasObjectsOfType(GameObjectType.Light, false, false);
            _hasParticleSystems = _prefabAsset.hierarchyHasObjectsOfType(GameObjectType.ParticleSystem, false, false);
            _hasCameras         = _prefabAsset.hierarchyHasObjectsOfType(GameObjectType.Camera, false, false);
            calcModelSize();

            _meshObjects.Clear();
            var childrenAndSelf = new List<GameObject>();
            _prefabAsset.getAllChildrenAndSelf(false, false, childrenAndSelf);
            foreach (var prefabObject in childrenAndSelf)
            {
                var mesh = prefabObject.getMesh();
                if (mesh != null) _meshObjects.Add(new PrefabMeshObject(prefabObject, _prefabAsset));
            }

            _isDirty = false;
        }

        private void calcModelSize()
        {
            var boundsQConfig = ObjectBounds.QueryConfig.defaultConfig;
            OBB obb = ObjectBounds.calcHierarchyWorldOBB(_prefabAsset, boundsQConfig);
            if (obb.isValid) _modelSize = obb.size;
            else _modelSize = Vector3.zero;
        }
    }
}
#endif