#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectSpawnGuide : ScriptableObject
    {
        [SerializeField]
        private PluginPrefab            _sourcePrefab;
        [SerializeField]
        private GameObject              _guide;
        [NonSerialized]
        private ObjectTransformSession  _transformSession;
        [NonSerialized]
        private List<GameObject>        _transformSessionTargets    = new List<GameObject>();
        [NonSerialized]
        private ObjectBounds.QueryConfig _boundsQConfig;

        public float                    volumeRadius                { get { return _guide != null ? calcWorldOBB().extents.magnitude : 0.0f; } }
        public bool                     isPresentInScene            { get { return _guide != null && _guide.activeSelf; } }
        public bool                     isTransformSessionActive    { get { return _transformSession != null && _transformSession.isActive; } }
        public ObjectTransformSession   transformSession            { get { return _transformSession; } }
        public TransformTRS             transformTRS                { get { return isPresentInScene ? _guide.transform.createTransformTRS() : new TransformTRS(); } }
        public GameObject               gameObject                  { get { return _guide; } }
        public Transform                transform                   { get { return _guide.transform; } }
        public Vector3                  position                    { get { return _guide.transform.position; } }
        public Quaternion               rotation                    { get { return _guide.transform.rotation; } }
        public Vector3                  lossyScale                  { get { return _guide.transform.lossyScale; } }
        public PluginPrefab             sourcePrefab                { get { return _sourcePrefab; } }
        public ObjectBounds.QueryConfig worldOBBQConfig             { get { return _boundsQConfig; } }

        public ObjectSpawnGuide()
        {
            _boundsQConfig              = ObjectBounds.QueryConfig.defaultConfig;
            _boundsQConfig.objectTypes  = GameObjectType.Mesh | GameObjectType.Sprite | GameObjectType.Terrain;
        }

        public void syncGridCellSizeToPrefabSize()
        {
            if (_sourcePrefab == null) return;

            OBB prefabOBB = ObjectBounds.calcHierarchyWorldOBB(_sourcePrefab.prefabAsset, _boundsQConfig);
            if (prefabOBB.isValid)
            {
                const float eps = 1e-4f;
                var grid = PluginScene.instance.grid;

                Vector3 prefabSize  = prefabOBB.size;
                grid.activeSettings.cellSizeX = MathEx.roundCorrectError(prefabSize.x, eps);
                grid.activeSettings.cellSizeZ = MathEx.roundCorrectError(prefabSize.z, eps);
            }
        }

        public bool isObjectPartOfGuideHierarchy(GameObject gameObject)
        {
            return isPresentInScene && (gameObject == _guide || gameObject.transform.IsChildOf(_guide.transform));
        }

        public void setRotationAndScale(Quaternion rotation, Vector3 scale)
        {
            if (isPresentInScene)
            {
                _guide.transform.rotation = rotation;
                _guide.transform.setWorldScale(scale);
            }
        }

        public void setRotation(Quaternion rotation)
        {
            if (isPresentInScene)
                _guide.transform.rotation = rotation;
        }

        public OBB calcWorldOBB()
        {
            if (_guide == null) return OBB.getInvalid();
            return ObjectBounds.calcHierarchyWorldOBB(_guide, _boundsQConfig);
        }

        public void onSceneGUI()
        {
            if (_guide == null && transformSession != null)
            {
                transformSession.end();
            }

            if (isPresentInScene && (_sourcePrefab == null || _sourcePrefab.prefabAsset == null))
            {
                destroyGuide();
                return;
            }

            if (FixedShortcuts.cancelAction(Event.current))
            {
                destroyGuide();
                return;
            }

            setGuideObjectActive(!PluginScene.instance.snapGridToPickedObjectEnabled);

            // Note: We can't begin the session immediately after double clicking on UI elements
            //       such as prefab previews. So we will use this delay strategy.
            if (!isTransformSessionActive && isPresentInScene)
            {
                transformSession.begin();
                if (transformSession.sessionType == ObjectTransformSessionType.ModularSnap)
                {
                    ObjectModularSnapSession modularSnapSession = transformSession as ObjectModularSnapSession;
                    modularSnapSession.setVerticalStep(_guide, _sourcePrefab.modularSnapVerticalStep);
                }
                else
                if (transformSession.sessionType == ObjectTransformSessionType.SurfaceSnap)
                {
                    ObjectSurfaceSnapSession surfaceSnapSession = transformSession as ObjectSurfaceSnapSession;
                    surfaceSnapSession.setAppliedOffsetFromSurface(_sourcePrefab.surfaceSnapAppliedOffsetFromSurface);
                }
            }

            if (isTransformSessionActive)
            {
                // Note: Check if the transform session has modified the scale or rotation of the spawn guide.
                TransformTRS spawnGuideTRS = transformTRS;
                transformSession.onSceneGUI();
                if (spawnGuideTRS.rotationOrScaleDiffers(transformTRS)) storeTRSInSourcePrefab();
                if (transformSession.sessionType == ObjectTransformSessionType.ModularSnap)
                {
                    ObjectModularSnapSession modularSnapSession = transformSession as ObjectModularSnapSession;
                    _sourcePrefab.modularSnapVerticalStep = modularSnapSession.getVerticalStep(_guide);
                }
                else
                if (transformSession.sessionType == ObjectTransformSessionType.SurfaceSnap)
                {
                    ObjectSurfaceSnapSession surfaceSnapSession = transformSession as ObjectSurfaceSnapSession;
                    _sourcePrefab.surfaceSnapAppliedOffsetFromSurface = surfaceSnapSession.appliedOffsetFromSurface;
                }
            }
        }

        public void usePrefab(PluginPrefab prefab, ObjectTransformSession transformSession)
        {
            _sourcePrefab   = prefab;

            UndoEx.saveEnabledState();
            UndoEx.enabled  = false;
            if (_guide != null)
            {
                ObjectEvents.onObjectWillBeDestroyed(_guide);
                DestroyImmediate(_guide);
            }
            _guide = _sourcePrefab.spawnDisconnected();
            _guide.makeEditorOnly();

            string removeStr        = "(Clone)";
            int substrIndex         = _guide.name.IndexOf(removeStr);
            _guide.name             = _guide.name.Remove(substrIndex, removeStr.Length);
            _guide.transform.parent = null; // Note: Don't attach to object group.
            prefab.spawnGuideTransformTRS.applyRotationAndScale(_guide.transform);
            _guide.AddComponent<ObjectSpawnGuideMono>();
            UndoEx.restoreEnabledState();

            _transformSessionTargets.Clear();
            _transformSessionTargets.Add(_guide);

            if (_transformSession != null) _transformSession.end();
            _transformSession = transformSession;
            _transformSession.end();
            _transformSession.bindTargetObjects(_transformSessionTargets);
        }

        public void resetRotationToOriginal()
        {
            if (!isPresentInScene) return;
            if (!_transformSession.clientCanUpdateTargetTransforms) return;

            UndoEx.recordTransform(_guide.transform);
            _guide.resetRotationToOriginal();
            storeTRSInSourcePrefab();
        }

        public void resetScaleToOriginal()
        {
            if (!isPresentInScene) return;
            if (!_transformSession.clientCanUpdateTargetTransforms) return;

            UndoEx.recordTransform(_guide.transform);
            _guide.transform.localScale = _sourcePrefab.prefabAsset.transform.localScale;
            storeTRSInSourcePrefab();
        }

        public void rotate(Vector3 axis, float degrees)
        {
            if (!isPresentInScene) return;
            if (!_transformSession.clientCanUpdateTargetTransforms) return;

            UndoEx.recordTransform(_guide.transform);
            _guide.transform.Rotate(axis, degrees, Space.World);

            _transformSession.onTargetTransformsChanged();
            storeTRSInSourcePrefab();
            ObjectEvents.onObjectsTransformed();
        }

        public void rotate(Vector3 point, Vector3 axis, float degrees)
        {
            if (!isPresentInScene) return;
            if (!_transformSession.clientCanUpdateTargetTransforms) return;

            UndoEx.recordTransform(_guide.transform);
            _guide.transform.RotateAround(point, axis, degrees);

            _transformSession.onTargetTransformsChanged();
            storeTRSInSourcePrefab();
            ObjectEvents.onObjectsTransformed();
        }

        public void setGuideObjectActive(bool active)
        {
            if (_guide != null && _guide.activeSelf != active)
            {
                _guide.SetActive(active);
                if (!active) _transformSession.end();
                else _transformSession.begin();
            }
        }

        public GameObject spawn()
        {
            if (_guide != null && _sourcePrefab != null && _sourcePrefab.prefabAsset != null)
                return _sourcePrefab.spawn(_guide.transform.position, _guide.transform.rotation, _guide.transform.lossyScale);

            return null;
        }

        public GameObject spawn(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (_guide != null && _sourcePrefab != null && _sourcePrefab.prefabAsset != null)
                return _sourcePrefab.spawn(position, rotation, scale);

            return null;
        }

        public void randomizeTransformIfNecessary(TransformRandomizationSettings randomizationSettings, Vector3 surfaceNormal)
        {
            if (isPresentInScene) randomizationSettings.randomizeTransform(_guide.transform, surfaceNormal);
        }

        public void destroyGuide()
        {
            if (_guide == null) return;

            if (_transformSession != null) _transformSession.end();
            _transformSessionTargets.Clear();

            if (_guide != null)
            {
                ObjectEvents.onObjectWillBeDestroyed(_guide);
                DestroyImmediate(_guide);
            }
        }

        public void storeTRSInSourcePrefab()
        {
            if (isPresentInScene)
            {
                _sourcePrefab.spawnGuideTransformTRS = _guide.transform.createTransformTRS();
            }
        }

        private void onPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            destroyGuide();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged  += onPlayModeStateChanged;
            Selection.selectionChanged              += onEditorSelectionChanged;
            Undo.undoRedoPerformed                  += onUndoRedo;
        }

        private void OnDisable()
        {
            destroyGuide();

            EditorApplication.playModeStateChanged  -= onPlayModeStateChanged;
            Selection.selectionChanged              -= onEditorSelectionChanged;
            Undo.undoRedoPerformed                  -= onUndoRedo;
        }

        private void onEditorSelectionChanged()
        {
            destroyGuide();
        }

        private void onUndoRedo()
        {
            if (_guide == null && _transformSession != null) _transformSession.end();
        }
    }
}
#endif