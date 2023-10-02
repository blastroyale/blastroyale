#if UNITY_EDITOR
using UnityEngine;
using System;

namespace GSpawn
{
    [Serializable]
    public class PrefabMeshObject
    {
        [SerializeField]
        private GameObject              _gameObject;
        [SerializeField]
        private MeshFilter              _meshFilter;
        [SerializeField]
        private MeshRenderer            _meshRenderer;
        [SerializeField]
        private SkinnedMeshRenderer     _skinnedMeshRenderer;
        [SerializeField]
        private Mesh                    _mesh;
        [SerializeField]
        private Material[]              _sharedMaterials;
        [SerializeField]
        private Vector3                 _rootRelativePosition;
        [SerializeField]
        private Quaternion              _rootRelativeRotation;
        [SerializeField]
        private Vector3                 _rootRelativeScale;

        public int                      numSharedMaterials      { get { return _sharedMaterials.Length; } }
        public Vector3                  rootRelativePosition    { get { return _rootRelativePosition; } }
        public Quaternion               rootRelativeRotation    { get { return _rootRelativeRotation; } }
        public Vector3                  rootRelativeScale       { get { return _rootRelativeScale; } }
        public Mesh                     mesh                    { get { return _mesh; } }
        public GameObject               gameObject              { get { return _gameObject; } }

        public PrefabMeshObject(GameObject gameObject, GameObject prefabRoot)
        {
            _gameObject                 = gameObject;
            _meshFilter                 = gameObject.getMeshFilter();
            _skinnedMeshRenderer        = gameObject.getSkinnedMeshRenderer();

            if (_meshFilter != null)
            {
                _mesh                   = _meshFilter.sharedMesh;
                _meshRenderer           = gameObject.getMeshRenderer();
                if (_meshRenderer != null) _sharedMaterials = _meshRenderer.sharedMaterials;
            }
            else if (_skinnedMeshRenderer != null)
            {
                _mesh                   = _skinnedMeshRenderer.sharedMesh;
                _sharedMaterials        = _skinnedMeshRenderer.sharedMaterials;
            }

            Transform objectTransform   = gameObject.transform;
            Transform rootTransform     = prefabRoot.transform;
            _rootRelativePosition       = rootTransform.InverseTransformPoint(objectTransform.position);
            _rootRelativeRotation       = Quaternion.Inverse(objectTransform.rotation) * rootTransform.rotation;
            _rootRelativeScale          = Vector3.Scale(rootTransform.lossyScale, objectTransform.lossyScale); 
        }

        public Material getSharedMaterial(int index)
        {
            return _sharedMaterials[index];
        }
    }
}
#endif