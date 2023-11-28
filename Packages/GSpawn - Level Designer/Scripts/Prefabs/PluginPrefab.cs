#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Flags]
    public enum PluginPrefabFilter
    {
        None            = 0,
        Selected        = 1,
        Unselected      = 2,
        ObjectGroup     = 4,
        NoObjectGroup   = 8,
        All             = ~0
    }

    public class PluginPrefab : ScriptableObject, IUIItemStateProvider
    {
        [SerializeField]
        private PluginGuid              _guid                                   = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private GameObject              _prefabAsset;
        [SerializeField]
        private PrefabPreview           _preview                                = new PrefabPreview();
        [SerializeField]
        private SceneGUIDObjectGroupMap _sceneToObjectGroup                     = new SceneGUIDObjectGroupMap();

        [SerializeField]
        private TransformTRS            _spawnGuideTransformTRS                 = new TransformTRS();
        [SerializeField]
        private int                     _modularSnapVerticalStep                = 0;
        [SerializeField]
        private float                   _surfaceSnapAppliedOffsetFromSurface    = 0.0f;

        [SerializeField]
        private Vector3                 _modelSize                              = Vector3.zero;

        [SerializeField]
        private bool                    _uiSelected                             = false;
        [NonSerialized]
        private CopyPasteMode           _uiCopyPasteMode                        = CopyPasteMode.None;

        public PluginGuid               guid                                    { get { return _guid; } }
        public Texture2D                previewTexture                          { get { return _preview.texture; } }
        public GameObject               prefabAsset
        {
            get { return _prefabAsset; }
            set
            {
                _prefabAsset = value;
                _preview.setPrefab(_prefabAsset);
                _modelSize = PrefabDataDb.instance.getData(prefabAsset).modelSize;
                _spawnGuideTransformTRS.extract(_prefabAsset.transform);
                name = _prefabAsset.name;
            }
        }
        public bool                     hasObjectGroup 
        { 
            get 
            {
                if (_sceneToObjectGroup.Count == 0) return false;

                string activeSceneGUID = SceneEx.getActiveSceneGUIDString();
                ObjectGroup objectGroup = null;

                // Note: The object group can be null if it has been deleted.
                if (_sceneToObjectGroup.TryGetValue(activeSceneGUID, out objectGroup)) return objectGroup != null;
                return false;
            } 
        }
        public ObjectGroup              objectGroup 
        { 
            get 
            {
                if (_sceneToObjectGroup.Count == 0) return null;

                string activeSceneGUID = SceneEx.getActiveSceneGUIDString();
                ObjectGroup objectGroup = null;

                // Note: The object group can be null if it has been deleted.
                if (_sceneToObjectGroup.TryGetValue(activeSceneGUID, out objectGroup)) return objectGroup;
                return null;
            } 
            set 
            {
                string activeSceneGUID = SceneEx.getActiveSceneGUIDString();

                UndoEx.record(this);
                if (!_sceneToObjectGroup.ContainsKey(activeSceneGUID)) _sceneToObjectGroup.Add(activeSceneGUID, value);
                else _sceneToObjectGroup[activeSceneGUID] = value;
                EditorUtility.SetDirty(this); 
            }
        }
        public TransformTRS             spawnGuideTransformTRS                  { get { return _spawnGuideTransformTRS; } set { _spawnGuideTransformTRS = value; EditorUtility.SetDirty(this); } }
        public int                      modularSnapVerticalStep                 { get { return _modularSnapVerticalStep; } set { _modularSnapVerticalStep = value; EditorUtility.SetDirty(this); } }
        public float                    surfaceSnapAppliedOffsetFromSurface     { get { return _surfaceSnapAppliedOffsetFromSurface; } set { _surfaceSnapAppliedOffsetFromSurface = value; EditorUtility.SetDirty(this); } }
        public Vector3                  modelSize                               { get { return _modelSize; } }
        public bool                     uiSelected                              { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode            uiCopyPasteMode                         { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }

        public static void applyObjectGroupLinks(List<PluginPrefab> prefabs)
        {
            var prefabInstances = new List<GameObject>();
            foreach (var prefab in prefabs)
            {
                PluginScene.instance.findPrefabInstances(prefab.prefabAsset, prefabInstances);

                ObjectGroup objectGroup = prefab.objectGroup;
                if (objectGroup == null) objectGroup = ObjectGroupDb.instance.defaultObjectGroup;

                if (objectGroup != null) objectGroup.addChildren(prefabInstances);
                else UndoEx.setObjectsTransformParent(prefabInstances, null);
            }
        }

        public static void getPrefabIds(List<PluginPrefab> prefabs, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var prefab in prefabs)
                ids.Add(prefab.guid);
        }

        public static void getPrefabAssets(List<PluginPrefab> prefabs, List<GameObject> prefabAssets)
        {
            prefabAssets.Clear();
            foreach (var prefab in prefabs)
                prefabAssets.Add(prefab.prefabAsset);
        }

        public bool isAssociatedWithObjectGroup(string groupName, bool ignoreCurrentScene)
        {
            if (string.IsNullOrEmpty(groupName)) return false;

            string activeSceneGUID = SceneEx.getActiveSceneGUIDString();
            foreach(var pair in _sceneToObjectGroup)
            {
                if (pair.Key == activeSceneGUID && ignoreCurrentScene) continue;

                if (pair.Value != null && pair.Value.objectGroupName == groupName) return true;
            }

            return false;
        }

        public void resetPreview()
        {
            _preview.reset();
        }

        public void regeneratePreview()
        {
            _preview.regenerate();
        }

        public void rotatePreview(Vector2 yawPitch)
        {
            _preview.rotate(yawPitch);
        }

        public void onSceneAssetWillBeDeleted(SceneAsset sceneAsset, string assetPath)
        {
            string sceneGUIDString = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            if (_sceneToObjectGroup.ContainsKey(sceneGUIDString))
            {
                _sceneToObjectGroup.Remove(sceneGUIDString);
                EditorUtility.SetDirty(this);
            }
        }

        public void attachInstanceToObjectGroup(GameObject gameObject)
        {
            if (hasObjectGroup)
            {
                UndoEx.recordTransform(gameObject.transform);
                gameObject.transform.parent = objectGroup.gameObject.transform;
            }
            else
            {
                ObjectGroup defaultGroup = ObjectGroupDb.instance.defaultObjectGroup;
                if (defaultGroup != null)
                {
                    UndoEx.recordTransform(gameObject.transform);
                    gameObject.transform.parent = defaultGroup.gameObject.transform;
                }
            }
        }

        public GameObject spawn()
        {
            GameObject gameObject = _prefabAsset.instantiatePrefab();
            UndoEx.registerCreatedObject(gameObject);
            attachInstanceToObjectGroup(gameObject);

            return gameObject;
        }

        public GameObject spawnDisconnected()
        {
            GameObject gameObject = GameObject.Instantiate(_prefabAsset);
            UndoEx.registerCreatedObject(gameObject);

            if (hasObjectGroup)
            {
                UndoEx.recordTransform(gameObject.transform);
                gameObject.transform.parent = objectGroup.gameObject.transform;
            }
            else
            {
                ObjectGroup defaultGroup = ObjectGroupDb.instance.defaultObjectGroup;
                if (defaultGroup != null)
                {
                    UndoEx.recordTransform(gameObject.transform);
                    gameObject.transform.parent = defaultGroup.gameObject.transform;
                }
            }

            return gameObject;
        }

        public GameObject spawn(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject gameObject               = spawn();
            gameObject.transform.position       = position;
            gameObject.transform.rotation       = rotation;
            gameObject.transform.localScale     = scale;
            return gameObject;
        }

        private void onProjectChanged()
        {
            // Note: Can happen when deleting the data folder.
            if (this == null) return;

            if (name != _prefabAsset.name)
            {
                // Note: Undo doesn't seem to work here but it's not necessary.
                //       If the change is undone/redone, the UI's will be refreshed automatically.
                name = _prefabAsset.name;
                PluginPrefabEvents.onPrefabChangedName(this);
            }
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += onProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= onProjectChanged;
        }
    }
}
#endif