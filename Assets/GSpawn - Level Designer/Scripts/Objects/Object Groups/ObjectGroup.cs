#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectGroup : ScriptableObject, IUIItemStateProvider
    {
        [SerializeField]
        private ObjectGroup         _parentGroup;
        [SerializeField]
        private PluginGuid          _guid                       = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private string              _objectGroupName            = string.Empty;
        [NonSerialized]
        private GameObject          _gameObject;                // Note: Can't serialize game objects. Loosing ref between Unity sessions because object groups are assets.
        [SerializeField]
        private string              _globalObjectIdString       = string.Empty;   // Our link to the game object
        [SerializeField]
        private bool                _uiSelected                 = false;
        [NonSerialized]
        private CopyPasteMode       _uiCopyPasteMode            = CopyPasteMode.None;
        [NonSerialized]
        private List<GameObject>    _gameObjectBuffer           = new List<GameObject>();

        public PluginGuid           guid                        { get { return _guid; } }
        public ObjectGroup          parentGroup                 { get { return _parentGroup; } }
        public string               objectGroupName             { get { return _objectGroupName; } }
        public GameObject           gameObject 
        {
            get 
            {
                if (_gameObject == null)
                {
                    _gameObject = GameObjectEx.globalIdStringToGameObject(_globalObjectIdString);

                    // Note: A second check is necessary because unfortunately, the global ID
                    //       is SOMETIMES regenerated when deleting the group object from the 
                    //       scene (e.g. delete and then Undo to restore). This is why object groups
                    //       must have unique names.
                    if (_gameObject == null) _gameObject = GameObject.Find(_objectGroupName);
                    if (_gameObject != null)
                    {
                        if (parentGroup != null) _gameObject.transform.parent = parentGroup.gameObject.transform;
                    }
                }

                return _gameObject; 
            }
            set 
            {
                _gameObject             = value;
                _objectGroupName        = _gameObject.name;
                _globalObjectIdString   = _gameObject.getGlobalIdSlowString();
                if (parentGroup != null) _gameObject.transform.parent = parentGroup.gameObject.transform;
                EditorUtility.SetDirty(this); 
            } 
        }
        public bool                 uiSelected                  { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode        uiCopyPasteMode             { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }

        public void syncName()
        {
            if (_gameObject.name != _objectGroupName)
            {
                UndoEx.record(this);
                _objectGroupName    = _gameObject.name;
                this.name           = _objectGroupName;
            }
        }

        [NonSerialized] List<GameObject> _childrenBuffer = new List<GameObject>();
        public void getAllNonGroupChildren(List<GameObject> allChildren)
        {
            allChildren.Clear();
            gameObject.getAllChildren(true, true, _childrenBuffer);

            int numChildren = _childrenBuffer.Count;
            for (int i = 0; i < numChildren; ++i)
            {
                var child = _childrenBuffer[i];
                if (ObjectGroupDb.instance.isObjectGroup(child)) continue;

                allChildren.Add(child);
            }
        }

        public void destroyImmediateNonGroupChildren()
        {
            _gameObjectBuffer.Clear();
            int numChildren = gameObject.transform.childCount;
            for (int i = 0; i < numChildren; ++i)
            {
                Transform child = gameObject.transform.GetChild(i);
                if (ObjectGroupDb.instance.isObjectGroup(child.gameObject)) continue;
                _gameObjectBuffer.Add(child.gameObject);
            }

            // Note: The objects which are about to be deleted could be selected etc
            ObjectEvents.onObjectsWillBeDestroyed(_gameObjectBuffer);

            foreach (var go in _gameObjectBuffer)
                UndoEx.destroyGameObjectImmediate(go);
        }

        public void setDirectObjectGroupChildIndex(ObjectGroup childGroup, int newIndex)
        {
            if (childGroup.parentGroup != this) return;

            Transform transform     = gameObject.transform;
            int numChildren         = transform.childCount;
            int currentGroupIndex   = -1;

            for (int i = 0; i < numChildren; ++i)
            {
                var go = transform.GetChild(i).gameObject;
                if (ObjectGroupDb.instance.isObjectGroup(go)) ++currentGroupIndex;
                
                if (currentGroupIndex == newIndex)
                {
                    UndoEx.registerChildrenOrderUndo(transform);
                    childGroup.gameObject.transform.SetSiblingIndex(i);
                    break;
                }
            }
        }

        public void setParentObjectGroup(ObjectGroup parentGroup)
        {
            if (parentGroup)
            {
                UndoEx.setTransformParent(gameObject.transform, parentGroup.gameObject.transform);
                UndoEx.record(this);
                _parentGroup = parentGroup;
                EditorUtility.SetDirty(this);
            }
            else
            {
                UndoEx.setTransformParent(gameObject.transform, null);
                UndoEx.record(this);
                _parentGroup = null;
                EditorUtility.SetDirty(this);
            }
        }

        public void addChildren(IEnumerable<GameObject> gameObjects)
        {
            Transform groupTransform = gameObject.transform;
            foreach (var gameObject in gameObjects)
                UndoEx.setTransformParent(gameObject.transform, groupTransform);
        }

        public ObjectGroup findParentGroup()
        {
            Transform currentParent = gameObject.transform.parent;
            while (currentParent != null)
            {
                var objectGroup = ObjectGroupDb.instance.findObjectGroup(currentParent.gameObject);
                if (objectGroup != null) return objectGroup;

                currentParent = currentParent.parent;
            }

            return null;
        }

        public bool isChildOf(ObjectGroup parentObjectGroup)
        {
            if (parentGroup == null) return false;
            if (parentGroup == parentObjectGroup) return true;

            Transform currentParent = gameObject.transform.parent;
            while (currentParent != null)
            {
                var parent = ObjectGroupDb.instance.findObjectGroup(currentParent.gameObject);
                if (parent == parentObjectGroup) return true;

                currentParent = currentParent.parent;
            }

            return false;
        }

        public void getDirectChildren(List<ObjectGroup> children)
        {
            children.Clear();

            Transform transform = gameObject.transform;
            int numChildren     = transform.childCount;

            for (int childIndex = 0; childIndex < numChildren; ++childIndex)
            {
                GameObject childObject = transform.GetChild(childIndex).gameObject;
                ObjectGroup childObjectGroup = ObjectGroupDb.instance.findObjectGroup(childObject);
                if (childObjectGroup != null) children.Add(childObjectGroup);
            }
        }

        public void getAllChildGroups(List<ObjectGroup> allChildren)
        {
            allChildren.Clear();

            // Note: It can happen when calling 'ObjectGroupDb.instance.deleteObjectGroup(this)'
            //       from 'onHierarchyChanged'.
            if (_gameObject == null) return;
        
            gameObject.getAllChildren(true, true, _gameObjectBuffer);
            foreach(var childObject in _gameObjectBuffer)
            {
                ObjectGroup group = ObjectGroupDb.instance.findObjectGroup(childObject);
                if (group != null) allChildren.Add(group);
            }
        }

        [NonSerialized]
        private List<string> _nameBuffer = new List<string>();
        private void onHierarchyChanged()
        {
            if (GSpawn.active == null) return;

            if (gameObject == null)
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled = false;
                ObjectGroupDbUI.instance.onObjectGroupWillBeDeleted(this);
                ObjectGroupDb.instance.deleteObjectGroup(this);
                PluginPrefabManagerUI.instance.onPrefabObjectGroupLinksChanged();
                UndoEx.restoreEnabledState();
            }
            else
            if (gameObject.name != _objectGroupName)
            {
                // Note: No duplicate names allowed.
                ObjectGroupDb.instance.getObjectGroupNames_IgnoreGroup(_nameBuffer, this);
                if (_nameBuffer.Contains(gameObject.name))
                    gameObject.name = UniqueNameGen.generate(gameObject.name, _nameBuffer);
             
                syncName();
                ObjectGroupDbUI.instance.onObjectGroupNeedsUIRefresh(this);
            }
            else
            {
                // Maybe its parent changed
                if (gameObject.transform.hasChanged)
                {
                    gameObject.transform.hasChanged = false;
                    ObjectGroupDbUI.instance.refresh();
                }
            }
        }

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += onHierarchyChanged;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= onHierarchyChanged;
        }
    }
}
#endif