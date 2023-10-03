#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public enum ObjectGizmoTransformPivot
    {
        Mesh = 0,
        Center
    }

    public enum ObjectGizmoTransformSpace
    {
        Local = 0,
        Global
    }

    [Serializable]
    public abstract class ObjectTransformGizmo : PluginGizmo
    {
        [Flags]
        protected enum Channels
        { 
            None = 0,
            Position = 1,
            Rotation = 2,
            Scale = 4,
        }

        protected Vector3                   _newPosition;
        protected Quaternion                _newRotation;
        protected Vector3                   _newScale;

        private Vector3                     _centerPivot            = Vector3.zero;
        private ObjectScaleStartDataMap     _scaleStartDataMap      = new ObjectScaleStartDataMap();
        private ObjectScale.ScaleConfig     _scaleConfig            = new ObjectScale.ScaleConfig();

        [SerializeField]
        private ObjectGizmoTransformPivot   _transformPivot         = ObjectGizmoTransformPivot.Center;
        [SerializeField]
        private ObjectGizmoTransformSpace   _transformSpace         = ObjectGizmoTransformSpace.Global;

        [SerializeField]
        protected GameObject                _pivotObject;
        [NonSerialized]
        private List<GameObject>            _targetObjects;
        [NonSerialized]
        private List<GameObject>            _targetParents          = new List<GameObject>();

        public ObjectGizmoTransformPivot    transformPivot          { get { return _transformPivot; } set { _transformPivot = value; refreshPositionAndRotation(); } }
        public ObjectGizmoTransformSpace    transformSpace          { get { return _transformSpace; } set { _transformSpace = value; refreshPositionAndRotation(); } }

        public int calcNumTargetParents()
        {
            GameObjectEx.getParents(_targetObjects, _targetParents);
            int numParents = _targetParents.Count;
            _targetParents.Clear();

            return numParents;
        }

        public void bindTargetObjects(List<GameObject> targetObjects)
        {
            _targetObjects = targetObjects;
            refreshPositionAndRotation();
        }

        public void onTargetObjectTransformsChanged()
        {
            refreshPosition();
            refreshRotation();
        }

        public void refreshPositionAndRotation()
        {
            refreshPosition();
            refreshRotation();
        }

        public void refreshPosition()
        {
            if (_targetObjects == null) return;
           
            if (_transformPivot == ObjectGizmoTransformPivot.Center ||
                _pivotObject == null) position = calcTargetObjectsCenter();
            else
            if (_transformPivot == ObjectGizmoTransformPivot.Mesh)
                position = _pivotObject.transform.position;
        }

        public virtual void refreshRotation()
        {
            if (_targetObjects == null) return;

            if (_transformSpace == ObjectGizmoTransformSpace.Global ||
                _pivotObject == null) rotation = Quaternion.identity;
            else
            if (_transformSpace == ObjectGizmoTransformSpace.Local)
                rotation = _pivotObject.transform.rotation;
        }

        public void onTargetObjectsUpdated(GameObject pivotObject)
        {
            _pivotObject = pivotObject;
            refreshPositionAndRotation();
        }

        public Vector3 calcTargetObjectsCenter()
        {
            if (_targetObjects == null) return Vector3.zero;

            ObjectBounds.QueryConfig boundsQConfig  = new ObjectBounds.QueryConfig();
            boundsQConfig.volumelessSize            = Vector3.zero;
            boundsQConfig.objectTypes               = GameObjectType.All;

            Vector3 center = Vector3.zero;
            int numObjects = 0;
            foreach (var go in _targetObjects)
            {
                ++numObjects;

                OBB obb = ObjectBounds.calcWorldOBB(go, boundsQConfig);
                if (obb.isValid) center += obb.center;
            }

            return numObjects != 0 ? center * (1.0f / (float)numObjects) : Vector3.zero;
        }

        protected override void doOnSceneGUI()
        {
            if (_targetObjects != null)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    scale = Vector3.one;
                    _scaleStartDataMap.clear();
                    _centerPivot = position;
                }
  
                GameObjectEx.getParents(_targetObjects, _targetParents);
                if (_targetObjects.Count != 0)
                {
                    Channels channels = draw();
                    if ((channels & Channels.Position) != 0) moveObjects();
                    if ((channels & Channels.Rotation) != 0) rotateObjects();
                    if ((channels & Channels.Scale) != 0) scaleObjects();

                    if (channels != Channels.None) ObjectEvents.onObjectsTransformedByGizmo(this);
                }
            }
        }

        protected abstract Channels draw();

        private void moveObjects()
        {
            Vector3 moveVector = _newPosition - position;
            UndoEx.recordGameObjectTransforms(_targetParents);

            foreach (var go in _targetParents)
                go.transform.position += moveVector;

            UndoEx.record(this);
            position = _newPosition;
        }

        private void rotateObjects()
        {
            Quaternion rotQuat = QuaternionEx.createRelativeRotation(rotation, _newRotation);
            if (transformPivot == ObjectGizmoTransformPivot.Center)
            {
                UndoEx.recordGameObjectTransforms(_targetParents);
                foreach (var go in _targetParents)
                    go.transform.rotateAround(rotQuat, _centerPivot);
            }
            else
            if (transformPivot == ObjectGizmoTransformPivot.Mesh)
            {
                UndoEx.recordGameObjectTransforms(_targetParents);
                foreach (var go in _targetParents)
                    go.transform.rotation *= rotQuat;
            }

            UndoEx.record(this);
            rotation = _newRotation;
        }

        private void scaleObjects()
        {
            if (_scaleStartDataMap.empty) _scaleStartDataMap.get(_targetParents, _centerPivot);

            _scaleConfig.scaleFactor                    = Vector3.Scale(_newScale, scale.replaceZero(1.0f).getInverse());
            _scaleConfig.scaleAxesRotation              = rotation;
            _scaleConfig.alignScaleFactorToScaleAxes    = true;

            if (transformPivot == ObjectGizmoTransformPivot.Center)
            {
                _scaleConfig.pivot = _centerPivot;
                UndoEx.recordGameObjectTransforms(_targetParents);

                foreach (var go in _targetParents)
                {
                    _scaleConfig.scaleStartData = _scaleStartDataMap.getData(go);
                    ObjectScale.scale(go, _scaleConfig);
                }
            }
            else
            if (transformPivot == ObjectGizmoTransformPivot.Mesh)
            {
                UndoEx.recordGameObjectTransforms(_targetParents);
                foreach (var go in _targetParents)
                {
                    _scaleConfig.scaleStartData.localScale      = _scaleStartDataMap.getLocalScale(go);
                    _scaleConfig.scaleStartData.pivotToPosition = Vector3.zero;
                    _scaleConfig.pivot                          = go.transform.position;
                    ObjectScale.scale(go, _scaleConfig);
                }
            }

            UndoEx.record(this);
            scale = _newScale;
        }
    }
}
#endif