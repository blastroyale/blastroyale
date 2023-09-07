#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public class ObjectGroupHashSet             : SerializableHashSet<ObjectGroup> {}
    [Serializable]
    public class SceneGUIDObjectGroupSetMap     : SerializableDictionary<string, ObjectGroupHashSet> {}
    [Serializable]
    public class SceneGUIDObjectGroupMap        : SerializableDictionary<string, ObjectGroup> {}

    public class ObjectGroupDb : ScriptableObject
    {
        private static ObjectGroupDb                        _instance;

        [SerializeField] ObjectGroupActionFilters           _actionFilters;

        [NonSerialized]
        private ObjectGroupDbUI                             _ui;
        [NonSerialized]
        private PrefabsFromObjectGroupsCreationSettingsUI   _prefabCreationSettingsUI;
        [NonSerialized]
        private PrefabsFromObjectGroupsCreationSettings     _prefabCreationSettings;
        [SerializeField]
        private SceneGUIDObjectGroupSetMap                  _sceneToObjectGroups        = new SceneGUIDObjectGroupSetMap();
        [SerializeField]
        private SceneGUIDObjectGroupMap                     _sceneToDefaultObjectGroup  = new SceneGUIDObjectGroupMap();

        [NonSerialized]
        private List<string>                                _stringBuffer               = new List<string>();
        [NonSerialized]
        private List<ObjectGroup>                           _objectGroupBuffer          = new List<ObjectGroup>();
        [NonSerialized]
        private List<ObjectGroup>                           _parentGroupBuffer          = new List<ObjectGroup>();
        [NonSerialized]
        private List<ObjectGroup>                           _childGroupBuffer           = new List<ObjectGroup>();
        [NonSerialized]
        private List<ObjectGroup>                           _sglGroupBuffer             = new List<ObjectGroup>();
        [NonSerialized]
        private List<GameObject>                            _gameObjectBuffer           = new List<GameObject>();

        public ObjectGroupDbUI                              ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<ObjectGroupDbUI>(PluginFolders.objectGroups);

                return _ui;
            }
        }
        public PrefabsFromObjectGroupsCreationSettingsUI    prefabCreationSettingsUI
        {
            get
            {
                if (_prefabCreationSettingsUI == null)
                    _prefabCreationSettingsUI = AssetDbEx.loadScriptableObject<PrefabsFromObjectGroupsCreationSettingsUI>(PluginFolders.objectGroups);

                return _prefabCreationSettingsUI;
            }
        }
        public PrefabsFromObjectGroupsCreationSettings      prefabCreationSettings
        {
            get
            {
                if (_prefabCreationSettings == null)
                    _prefabCreationSettings = AssetDbEx.loadScriptableObject<PrefabsFromObjectGroupsCreationSettings>(PluginFolders.objectGroups);

                return _prefabCreationSettings;
            }
        }
        public int                                          numObjectGroups 
        { 
            get 
            {
                var objectGroups = getSceneObjectGroups(SceneEx.getCurrent());
                return objectGroups != null ? objectGroups.Count : 0;
            } 
        }
        public int                                          globalNumObjectGroups
        {
            get
            {
                int numObjectGroups = 0;
                foreach (var pair in _sceneToObjectGroups)
                    numObjectGroups += pair.Value.Count;

                return numObjectGroups;
            }
        }
        public ObjectGroup                                  defaultObjectGroup
        {
            get
            {
                ObjectGroup defaultObjectGroup;
                if (_sceneToDefaultObjectGroup.TryGetValue(SceneEx.getActiveSceneGUIDString(), out defaultObjectGroup)) return defaultObjectGroup;
                return null;
            }
        }
        public ObjectGroupActionFilters                     actionFilters
        {
            get
            {
                if (_actionFilters == null)
                {
                    _actionFilters = ScriptableObject.CreateInstance<ObjectGroupActionFilters>();
                    AssetDbEx.addObjectToAsset(_actionFilters, this);
                }
                return _actionFilters;
            }
        }

        public static ObjectGroupDb                         instance
        {
            get
            {
                if (_instance == null) _instance = AssetDbEx.loadScriptableObject<ObjectGroupDb>(PluginFolders.objectGroups);
                return _instance;
            }
        }
        public static bool                                  exists                  { get { return _instance != null; } }

        public static void getObjectGroupIds(List<ObjectGroup> objectGroups, List<PluginGuid> objectGroupIds)
        {
            objectGroupIds.Clear();
            foreach (var group in objectGroups)
                objectGroupIds.Add(group.guid);
        }

        public static void getParentGroups(List<ObjectGroup> groups, List<ObjectGroup> parentGroups)
        {
            parentGroups.Clear();

            foreach (var group in groups)
            {
                bool foundParent = false;
                if (group.parentGroup == null)
                {
                    parentGroups.Add(group);
                    continue;
                }

                foreach (var otherGroup in groups)
                {
                    if (otherGroup == group) continue;

                    if (group.isChildOf(otherGroup))
                    {
                        foundParent = true;
                        break;
                    }
                }

                if (!foundParent) parentGroups.Add(group);
            }
        }

        public bool createPrefabsFromObjectGroups(List<GameObject> createdPrefabAssets)
        {
            getRootObjectGroups(_objectGroupBuffer);
            return PrefabFactory.create(_objectGroupBuffer, prefabCreationSettings, createdPrefabAssets);
        }

        public void onSceneAssetWillBeDeleted(SceneAsset sceneAsset, string assetPath)
        {
            string sceneGUIDString  = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            if (_sceneToObjectGroups.ContainsKey(sceneGUIDString))
            {
                var objectGroups    = _sceneToObjectGroups[sceneGUIDString];
                foreach (var objectGroup in objectGroups)
                {
                    AssetDbEx.removeObjectFromAsset(objectGroup, this);
                    ScriptableObjectEx.destroyImmediate(objectGroup);
                }
                _sceneToObjectGroups.Remove(sceneGUIDString);
                EditorUtility.SetDirty(this);
            }
            if (_sceneToDefaultObjectGroup.ContainsKey(sceneGUIDString))
            {
                _sceneToDefaultObjectGroup.Remove(sceneGUIDString);
                EditorUtility.SetDirty(this);
            }
        }

        public void scenesWithObjectGroups(List<SceneAsset> sceneAssets)
        {
            sceneAssets.Clear();
            foreach(var pair in _sceneToObjectGroups)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(pair.Key);
                sceneAssets.Add(AssetDbEx.loadAsset<SceneAsset>(scenePath));
            }
        }

        public void syncNames()
        {
            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups != null)
            {
                foreach (var group in activeSceneGroups)
                {
                    group.syncName();
                }

                EditorUtility.SetDirty(this);
            }
        }

        public void setDefaultObjectGroup(ObjectGroup objectGroup)
        {
            string sceneGUID = SceneEx.getActiveSceneGUIDString();

            if (objectGroup == null)
            {
                UndoEx.record(this);
                if (!_sceneToDefaultObjectGroup.TryAdd(sceneGUID, null))
                    _sceneToDefaultObjectGroup[sceneGUID] = null;
                EditorUtility.SetDirty(this);
            }
            else
            if (containsObjectGroup(objectGroup))
            {
                UndoEx.record(this);
                if (!_sceneToDefaultObjectGroup.TryAdd(sceneGUID, objectGroup))
                    _sceneToDefaultObjectGroup[sceneGUID] = objectGroup;
                EditorUtility.SetDirty(this);
            }
        }

        public ObjectGroup createObjectGroup(GameObject gameObject)
        {
            if (!gameObject.isSceneObject() || isObjectGroup(gameObject)) return null;
            if (PluginInstanceData.instance.isPlugin(gameObject))
            {
                Debug.Log("Plugin objects (i.e. objects with the '" + GSpawn.pluginName + "' script attached to them) can not be made into object groups.");
                return null;
            }

            UndoEx.saveEnabledState();
            UndoEx.enabled      = false;
            ObjectGroup group   = UndoEx.createScriptableObject<ObjectGroup>();

            getObjectGroupNames(_stringBuffer, null);
            if (_stringBuffer.Contains(gameObject.name))
                gameObject.name = UniqueNameGen.generate(gameObject.name, _stringBuffer);

            group.gameObject    = gameObject;
            group.name          = group.objectGroupName;

            UndoEx.record(this);
            addObjectGroup(SceneEx.getCurrent(), group);
            EditorUtility.SetDirty(this);

            UndoEx.restoreEnabledState();

            return group;
        }

        public ObjectGroup createObjectGroup(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            getObjectGroupNames(_stringBuffer, null);
            name                    = UniqueNameGen.generate(name, _stringBuffer);

            UndoEx.saveEnabledState();
            UndoEx.enabled          = false;

            GameObject gameObject   = new GameObject(name);
            UndoEx.registerCreatedObject(gameObject);
            EditorUtility.SetDirty(gameObject);         // Note: Need to mark it as dirty in order to be saved in the scene between Unity sessions.

            ObjectGroup group       = UndoEx.createScriptableObject<ObjectGroup>();
            group.gameObject        = gameObject;
            group.name              = group.objectGroupName;

            UndoEx.record(this);
            addObjectGroup(SceneEx.getCurrent(), group);
            EditorUtility.SetDirty(this);

            UndoEx.restoreEnabledState();

            return group;
        }

        public ObjectGroup cloneObjectGroup(ObjectGroup objectGroup)
        {
            if (containsObjectGroup(objectGroup))
            {
                getObjectGroupNames(_stringBuffer, null);
                string name             = UniqueNameGen.generate(objectGroup.gameObject.name, _stringBuffer);

                UndoEx.saveEnabledState();
                UndoEx.enabled          = false;

                GameObject gameObject   = new GameObject(name);
                UndoEx.registerCreatedObject(gameObject);
                EditorUtility.SetDirty(gameObject);         // Note: Need to mark it as dirty in order to be saved in the scene between Unity sessions.

                ObjectGroup group       = UndoEx.createScriptableObject<ObjectGroup>();
                group.gameObject        = gameObject;
                group.name              = group.objectGroupName;
                group.setParentObjectGroup(objectGroup.findParentGroup());

                UndoEx.record(this);
                addObjectGroup(SceneEx.getCurrent(), group);
                EditorUtility.SetDirty(this);

                UndoEx.restoreEnabledState();

                return group;
            }

            return null;
        }

        public void renameObjectGroup(ObjectGroup objectGroup, string newName)
        {
            if (!string.IsNullOrEmpty(newName) && 
                containsObjectGroup(objectGroup) && 
                objectGroup.gameObject.name != newName)
            {
                // Note: Object groups must have unique names.
                getObjectGroupNames(_stringBuffer, objectGroup.objectGroupName);
                newName = UniqueNameGen.generate(newName, _stringBuffer);

                UndoEx.record(objectGroup.gameObject);
                objectGroup.gameObject.name = newName;
                objectGroup.name            = newName;
            }
        }

        public void deleteObjectGroup(ObjectGroup objectGroup)
        {
            _sglGroupBuffer.Clear();
            _sglGroupBuffer.Add(objectGroup);
            deleteObjectGroups(_sglGroupBuffer);
        }

        public void deleteObjectGroups(List<ObjectGroup> objectGroups)
        {
            if (objectGroups.Count != 0)
            {
                UndoEx.record(this);
                getParentGroups(objectGroups, _parentGroupBuffer);
                _objectGroupBuffer.Clear();

                var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
                if (activeSceneGroups == null) return;

                foreach (var parentObjectGroup in _parentGroupBuffer)
                {
                    if (containsObjectGroup(parentObjectGroup))
                    {
                        activeSceneGroups.Remove(parentObjectGroup);
                        _objectGroupBuffer.Add(parentObjectGroup);
                        parentObjectGroup.getAllChildGroups(_childGroupBuffer);
                        foreach (var child in _childGroupBuffer)
                        {
                            activeSceneGroups.Remove(child);
                            _objectGroupBuffer.Add(child);
                        }
                    }
                }

                foreach (var objectGroup in _objectGroupBuffer)
                    UndoEx.destroyObjectImmediate(objectGroup);

                EditorUtility.SetDirty(this);
            }
        }

        public void deleteAllObjectGroups(Scene scene)
        {
            var sceneGroups = getSceneObjectGroups(scene);
            if (sceneGroups == null) return;

            if (sceneGroups.Count != 0)
            {
                UndoEx.record(this);
                _objectGroupBuffer.Clear();
                _objectGroupBuffer.AddRange(sceneGroups.hashSet);
                getParentGroups(_objectGroupBuffer, _parentGroupBuffer);
                _objectGroupBuffer.Clear();

                foreach (var parentObjectGroup in _parentGroupBuffer)
                {
                    if (containsObjectGroup(parentObjectGroup))
                    {
                        sceneGroups.Remove(parentObjectGroup);
                        _objectGroupBuffer.Add(parentObjectGroup);
                        parentObjectGroup.getAllChildGroups(_childGroupBuffer);
                        foreach (var child in _childGroupBuffer)
                        {
                            sceneGroups.Remove(child);
                            _objectGroupBuffer.Add(child);
                        }
                    }
                }

                foreach (var objectGroup in _objectGroupBuffer)
                    UndoEx.destroyObjectImmediate(objectGroup);

                EditorUtility.SetDirty(this);
            }
        }

        public bool containsObjectGroup(ObjectGroup objectGroup)
        {
            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return false;

            return activeSceneGroups.Contains(objectGroup);
        }

        public bool isObjectGroup(GameObject gameObject)
        {
            if (gameObject == null) return false;

            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return false;

            foreach (var group in activeSceneGroups)
                if (group.gameObject == gameObject) return true;

            return false;
        }

        public bool isRootObjectGroup(ObjectGroup objectGroup)
        {
            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return false;

            Transform groupTransform = objectGroup.gameObject.transform;
            foreach (var group in activeSceneGroups)
            {
                if (objectGroup != group)
                {
                    if (groupTransform.IsChildOf(group.gameObject.transform)) return false;
                }
            }

            return true;
        }

        public ObjectGroup findObjectGroup(GameObject gameObject)
        {
            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return null;

            foreach (var group in activeSceneGroups)
                if (group.gameObject == gameObject) return group;

            return null;
        }

        public void findObjectGroups(List<GameObject> gameObjects, List<ObjectGroup> objectGroups)
        {
            objectGroups.Clear();

            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return;

            foreach (var group in activeSceneGroups)
            {
                if (gameObjects.Contains(group.gameObject)) 
                    objectGroups.Add(group);
            }
        }

        public void getObjectGroups(List<ObjectGroup> objectGroups)
        {
            objectGroups.Clear();

            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return;

            objectGroups.AddRange(activeSceneGroups.hashSet);
        }

        public void getEmptyObjectGroups(List<ObjectGroup> objectGroups)
        {
            objectGroups.Clear();
            getRootObjectGroups(_objectGroupBuffer);
            foreach(var rootGroup in _objectGroupBuffer)
            {
                rootGroup.gameObject.getAllChildren(true, true, _gameObjectBuffer);
                if (_gameObjectBuffer.Count == 0) objectGroups.Add(rootGroup);
                else
                {
                    bool foundNonGroupObjects = false;
                    foreach(var child in _gameObjectBuffer)
                    {
                        if (!isObjectGroup(child))
                        {
                            foundNonGroupObjects = true;
                            break;
                        }
                    }

                    if (!foundNonGroupObjects)
                    {
                        objectGroups.Add(rootGroup);
                        foreach (var child in _gameObjectBuffer)
                            objectGroups.Add(findObjectGroup(child));
                    }
                }
            }
        }

        public void getRootObjectGroups(List<ObjectGroup> objectGroups)
        {
            objectGroups.Clear();

            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return;

            foreach (var group in activeSceneGroups)
            {
                if (isRootObjectGroup(group)) objectGroups.Add(group);
            }
        }

        public void getGameObjects(List<ObjectGroup> objectGroups, List<GameObject> gameObjects)
        {
            gameObjects.Clear();
            foreach(var objectGroup in objectGroups)
            {
                if (containsObjectGroup(objectGroup))
                {
                    gameObjects.Add(objectGroup.gameObject);
                }
            }
        }

        public void getObjectGroupNames(List<string> names, string ignoredName)
        {
            names.Clear();

            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return;

            foreach (var objectGroup in activeSceneGroups)
            {
                if (objectGroup.objectGroupName != ignoredName)
                    names.Add(objectGroup.gameObject.name);
            }
        }

        public void getObjectGroupNames_IgnoreGroup(List<string> names, ObjectGroup ignoredGroup)
        {
            names.Clear();

            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return;

            foreach (var objectGroup in activeSceneGroups)
            {
                if (objectGroup != ignoredGroup)
                    names.Add(objectGroup.gameObject.name);
            }
        }

        // Note: Necessary to detect when an object group has a null gameObject in
        //       which case it has to be removed. It can happen when the user deletes
        //       the object group from the hierarchy view.
        public void deleteNullObjectGroups()
        {
            var activeSceneGroups = getSceneObjectGroups(SceneEx.getCurrent());
            if (activeSceneGroups == null) return;

            if (activeSceneGroups.Count != 0)
            {
                _objectGroupBuffer.Clear();
                foreach (var objectGroup in activeSceneGroups)
                {
                    if (objectGroup.gameObject == null)
                        _objectGroupBuffer.Add(objectGroup);
                }

                activeSceneGroups.RemoveWhere(item => item.gameObject == null);

                foreach (var objectGroup in _objectGroupBuffer)
                {
                    // Note: When calling ScriptableObjectEx.destroyImmediate instead of
                    //       UndoEx.destroyObjectImmediate, we need to remove the sub-asset.
                    AssetDbEx.removeObjectFromAsset(objectGroup, this);
                    ScriptableObjectEx.destroyImmediate(objectGroup);
                }

                EditorUtility.SetDirty(this);
            }
        }

        private void addObjectGroup(Scene scene, ObjectGroup objectGroup)
        {
            string sceneGUID = scene.getGUIDString();
            ObjectGroupHashSet objectGroups;
            if (!_sceneToObjectGroups.TryGetValue(sceneGUID, out objectGroups))
            {
                objectGroups = new ObjectGroupHashSet();
                _sceneToObjectGroups.Add(sceneGUID, objectGroups);
            }
            objectGroups.Add(objectGroup);

            AssetDbEx.addObjectToAsset(objectGroup, this);
        }

        private ObjectGroupHashSet getSceneObjectGroups(Scene scene)
        {
            if (!scene.IsValid()) return null;

            string sceneGUID = scene.getGUIDString();
            ObjectGroupHashSet objectGroupSet;
            if (_sceneToObjectGroups.TryGetValue(sceneGUID, out objectGroupSet)) return objectGroupSet;

            return null;
        }

        private void OnDestroy()
        {
            int numScenes = SceneManager.sceneCount;
            for (int i = 0; i < numScenes; ++i)
                deleteAllObjectGroups(SceneManager.GetSceneAt(i));

            _sceneToObjectGroups.Clear();
            _sceneToDefaultObjectGroup.Clear();

            ScriptableObjectEx.destroyImmediate(_actionFilters);
        }
    }
}
#endif