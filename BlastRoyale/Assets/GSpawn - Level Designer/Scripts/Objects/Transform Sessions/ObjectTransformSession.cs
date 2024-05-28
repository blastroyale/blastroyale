#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public enum ObjectTransformSessionType
    {
        Projection = 0,
        VertexSnap,
        BoxSnap,
        SurfaceSnap,
        ModularSnap
    }

    public abstract class ObjectTransformSession : ScriptableObject
    {
        [NonSerialized]
        private bool                                _isActive;
        [NonSerialized]
        protected IEnumerable<GameObject>           _targetObjects;
        [NonSerialized]
        protected List<GameObject>                  _targetParents                      = new List<GameObject>();
        [NonSerialized]
        protected List<GameObject>                  _allTargetObjects                   = new List<GameObject>();

        public bool                                 isActive                            { get { return _isActive; } }
        public abstract string                      sessionName                         { get; }
        public abstract ObjectTransformSessionType  sessionType                         { get; }
        public virtual bool                         clientCanUpdateTargetTransforms     { get { return true; } }

        public static ObjectTransformSession create(ObjectTransformSessionType sessionType)
        {
            if (sessionType == ObjectTransformSessionType.BoxSnap)          return ScriptableObject.CreateInstance<ObjectBoxSnapSession>();
            else if (sessionType == ObjectTransformSessionType.SurfaceSnap) return ScriptableObject.CreateInstance<ObjectSurfaceSnapSession>();
            else if (sessionType == ObjectTransformSessionType.ModularSnap) return ScriptableObject.CreateInstance<ObjectModularSnapSession>();
            else if (sessionType == ObjectTransformSessionType.Projection)  return ScriptableObject.CreateInstance<ObjectProjectionSession>();
            else if (sessionType == ObjectTransformSessionType.VertexSnap)  return ScriptableObject.CreateInstance<ObjectVertexSnapSession>();

            return null;
        }

        public virtual void onTargetTransformsChanged() { }
        public virtual void onUndoRedo() { }

        public void bindTargetObjects(IEnumerable<GameObject> targetObjects)
        {
            if (!isActive) _targetObjects = targetObjects;
        }

        public bool begin()
        {
            if (_isActive) return false;
            if (!onCanBegin()) return false;
        
            GameObjectEx.getParents(_targetObjects, _targetParents);
            if (_targetParents.Count == 0) return false;
            GameObjectEx.getAllObjectsInHierarchies(_targetParents, false, false, _allTargetObjects);

            _isActive = onBegin();
            return _isActive;
        }

        public void end()
        {
            if (_isActive)
            {
                onEnd();
                _isActive = false;
            }
        }

        public void onSceneGUI()
        {
            drawUIHandles();

            if (isActive)
            {
                update();
                draw();
            }
        }

        protected virtual void draw() { }
        protected virtual void drawUIHandles() { }
        protected abstract bool onCanBegin();
        protected abstract bool onBegin();
        protected abstract void onEnd();
        protected abstract void update();
        protected virtual void onDisabled() { }
        protected virtual void onEnabled() { }
        protected virtual void onDestroy() { }

        protected void removeNullObjects()
        {
            _targetParents.RemoveAll(item => item == null);
            _allTargetObjects.RemoveAll(item => item == null);
            if (_targetParents.Count == 0) end();
        }

        private void OnDisable()
        {
            onDisabled();
        }

        private void OnEnable()
        {
            onEnabled();
        }

        private void OnDestroy()
        {
            onDestroy();
        }
    }
}
#endif