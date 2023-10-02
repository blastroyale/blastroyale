#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GSpawn
{
    public abstract class PluginGizmo : ScriptableObject
    {
        private SerializedObject    _serializedObject;

        [SerializeField]
        private Vector3             _position               = Vector3.zero;
        [SerializeField]
        private Vector3             _rotation               = Vector3.zero;
        [SerializeField]    
        private Vector3             _scale                  = Vector3.one;
        [SerializeField]
        protected bool              _enabled                = true;

        public Vector3              position                { get { return _position; } set { _position = value; } }
        public Quaternion           rotation                { get { return Quaternion.Euler(_rotation); } set { _rotation = value.eulerAngles; } }
        public Vector3              scale                   { get { return _scale; } set { _scale = value; } }
        public bool                 enabled                 { get { return _enabled; } set { _enabled = value; } }
        public SerializedObject     serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }

        public static string        positionPropertyName    { get { return "_position"; } }
        public static string        rotationPropertyName    { get { return "_rotation"; } }

        public void onSceneGUI()
        {
            if (_enabled) doOnSceneGUI();
        }

        protected abstract void doOnSceneGUI();

        protected virtual void onDestroy() { }
        protected virtual void onEnable() { }

        private void OnDestroy()
        {
            onDestroy();
        }

        private void OnEnable()
        {
            onEnable();
        }
    }
}
#endif