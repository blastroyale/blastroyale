#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectGroupDbUI : PluginUI
    {
        [NonSerialized]
        private List<ObjectGroup>                           _objectGroupBuffer = new List<ObjectGroup>();
        [NonSerialized]
        private List<PluginGuid>                            _objectGroupIdBuffer = new List<PluginGuid>();
        [NonSerialized]
        private List<GameObject>                            _objectBuffer_0 = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>                            _objectBuffer_1 = new List<GameObject>();

        private ToolbarButton                               _assignSelectedObjectsBtn;

        [SerializeField]
        private TreeViewState                               _objectGroupViewState;
        private TreeView<UIObjectGroupItem, ObjectGroup>    _objectGroupView;

        private EntitySearchField                           _objectGroupSearchField;
        private TextField                                   _newObjectGroupNameField;

        public static ObjectGroupDbUI                       instance { get { return ObjectGroupDb.instance.ui; } }

        public bool dragAndDropInitiatedByObjectGroupView()
        {
            if (_objectGroupView == null) return false;

            return PluginDragAndDrop.initiatorId == _objectGroupView.dragAndDropInitiatorId;
        }

        public void onObjectGroupWillBeDeleted(ObjectGroup objectGroup)
        {
            // Note: _objectGroupView.containsItem(objectGroup.guid) should not be necessary
            //       but when deleting hierarchies of object groups together with their children
            //       exceptions are thrown because some items can not be found.
            if (_objectGroupView != null && _objectGroupView.containsItem(objectGroup.guid))
                _objectGroupView.deleteItem(objectGroup.guid);
        }

        public void onObjectGroupNeedsUIRefresh(ObjectGroup objectGroup)
        {
            if (_objectGroupView != null)
                _objectGroupView.refreshItemUI(UIObjectGroupItem.getItemId(objectGroup));
        }

        protected override void onRefresh()
        {
            populateObjectGroupView();
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow = 1.0f;
            createTopToolbar();
            createSearchToolbar();
            createObjectGroupView();
            populateObjectGroupView();
            createBottomToolbar();
            createActionFitlersUI();
        }

        protected override void onEnabled()
        {
            if (_objectGroupViewState == null)
            {
                _objectGroupViewState = ScriptableObject.CreateInstance<TreeViewState>();
                _objectGroupViewState.name = GetType().Name + "_ObjectGroupViewState";
                AssetDbEx.addObjectToAsset(_objectGroupViewState, ObjectGroupDb.instance);
            }
            EditorApplication.update += onEditorUpdate;
        }

        protected override void onDisabled()
        {
            EditorApplication.update -= onEditorUpdate;
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_objectGroupViewState);
        }

        protected override void onUndoRedo()
        {
            if (_objectGroupView != null)
                populateObjectGroupView();
        }

        private void createTopToolbar()
        {
            var toolbar                 = new Toolbar();
            toolbar.style.flexShrink    = 0.0f;
            contentContainer.Add(toolbar);

            var button                  = UI.createToolbarButton("Create prefabs...", toolbar);
            button.tooltip              = "Opens up a new window which will allow you to create prefabs from object groups.";
            button.clicked              += () =>
            {
                var window = PluginWindow.showUtility<PrefabsFromObjectGroupsCreationSettingsWindow>("Prefab Creation");
                Vector2 size = new Vector2(380.0f, 85.0f);
                window.setMinMaxSize(size);
                window.setSize(size);
                window.centerOnScreen();
            };

            toolbar.Add(new ToolbarSpacer());
            button                      = UI.createToolbarButton(TexturePool.instance.lightBulb, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(button);
            button.tooltip              = "Activate selected groups.";
            button.clicked              += () => { getVisibleSelectedObjectGroups(_objectGroupBuffer); PluginScene.instance.setObjectGroupsActive(_objectGroupBuffer, true, true); };

            button                      = UI.createToolbarButton(TexturePool.instance.lightBulbGray, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(button);
            button.tooltip              = "Deactivate selected groups.";
            button.clicked              += () => { getVisibleSelectedObjectGroups(_objectGroupBuffer); PluginScene.instance.setObjectGroupsActive(_objectGroupBuffer, false, true); };

            button                      = UI.createToolbarButton(TexturePool.instance.clear, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(button);
            button.tooltip              = "Delete children of selected groups.";
            button.clicked += () => 
            { 
                getVisibleSelectedObjectGroups(_objectGroupBuffer);
                foreach (var g in _objectGroupBuffer)
                    g.destroyImmediateNonGroupChildren();
            };

            _assignSelectedObjectsBtn           = UI.createToolbarButton(TexturePool.instance.objectGroupHand, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_assignSelectedObjectsBtn);
            _assignSelectedObjectsBtn.tooltip   = "Assign selected objects to selected group.";
            _assignSelectedObjectsBtn.clicked   += () => 
            {
                getVisibleSelectedObjectGroups(_objectGroupBuffer);
                if (_objectGroupBuffer.Count != 0)
                {
                    ObjectSelection.instance.getSelectedObjects(_objectBuffer_0);
                    GameObjectEx.getParents(_objectBuffer_0, _objectBuffer_1);
                    GameObjectEx.getPrefabInstancesAndNonInstances(_objectBuffer_1, _objectBuffer_0);
                    _objectGroupBuffer[0].addChildren(_objectBuffer_0);
                }
            };

            UI.createColumnSeparator(toolbar);
            button                  = UI.createToolbarButton(TexturePool.instance.delete, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(button);
            button.tooltip          = "Delete selected object groups. Note: Pressing this button will also delete the object groups from the scene.";
            button.clicked          += () =>
            {
                if (EditorUtility.DisplayDialog("Delete Object Groups", "This action will remove the selected object groups from the scene. Would you like to continue? (You can Undo this operation).", "Yes", "No"))
                {
                    getVisibleSelectedObjectGroups(_objectGroupBuffer);
                    UIObjectGroupItem.getItemIds(_objectGroupBuffer, _objectGroupIdBuffer);
                    _objectGroupView.deleteItems(_objectGroupIdBuffer);
                    ObjectGroupDb.instance.getGameObjects(_objectGroupBuffer, _objectBuffer_0);
                    PrefabLibProfileDb.instance.performPrefabAction((prefab) => { if (_objectGroupBuffer.Contains(prefab.objectGroup)) prefab.objectGroup = null; });
                    ObjectGroupDb.instance.deleteObjectGroups(_objectGroupBuffer);
                    UndoEx.destroyGameObjectsImmediate(_objectBuffer_0);
                }
            };
        }

        private void createBottomToolbar()
        {
            var toolbar                     = new Toolbar();
            toolbar.style.flexShrink        = 0.0f;
            contentContainer.Add(toolbar);

            var createObjectGroupButton     = UI.createSmallCreateNewToolbarButton(toolbar);
            createObjectGroupButton.clicked += () => { createNewObjectGroup(_newObjectGroupNameField.text); };
            createObjectGroupButton.tooltip = "Create a new object group with the specified name.";

            _newObjectGroupNameField                = UI.createToolbarTextField(toolbar);
            _newObjectGroupNameField.style.flexGrow = 1.0f;
        }

        private void createNewObjectGroup(string name)
        {
            var newObjectGroup = ObjectGroupDb.instance.createObjectGroup(name);
            if (newObjectGroup != null)
            {
                _objectGroupView.setAllItemsSelected(false, false, false);
                PluginGuid newGroupId = _objectGroupView.addItem(newObjectGroup, true);
                _objectGroupView.setItemSelected(newGroupId, true, false);
                _objectGroupView.scheduleScrollToItem(newGroupId);
            }
        }

        private void createObjectGroupView()
        {
            _objectGroupView                    = new TreeView<UIObjectGroupItem, ObjectGroup>(_objectGroupViewState, contentContainer);
            _objectGroupView.createItemDataFunc = () => { return ObjectGroupDb.instance.createObjectGroup("Object Group"); };
            _objectGroupView.canDuplicate       = true;
            _objectGroupView.selectedItemsWillBeDeleted += onObjectGroupSelectedItemsWillBeDeleted;

            _objectGroupView.RegisterCallback<DragPerformEvent>((p) =>
            {
                if (!PluginDragAndDrop.initiatedByPlugin)
                {
                    var droppedObjects = PluginDragAndDrop.unityObjects;
                    var newObjectGroups = new List<ObjectGroup>();
                    foreach (var droppedObject in droppedObjects)
                    {
                        GameObject gameObject = droppedObject as GameObject;
                        if (gameObject != null)
                        {
                            var newObjectGroup = ObjectGroupDb.instance.createObjectGroup(gameObject);
                            if (newObjectGroup == null) continue;

                            if (_objectGroupView.dropDestinationData != null) _objectGroupView.addItem(newObjectGroup, UIObjectGroupItem.getItemId(_objectGroupView.dropDestinationData), filterObjectGroup(newObjectGroup));
                            else _objectGroupView.addItem(newObjectGroup, filterObjectGroup(newObjectGroup));

                            newObjectGroups.Add(newObjectGroup);
                        }
                    }

                    if (newObjectGroups.Count != 0)
                    {
                        updateObjectGroupParentChildRelationships();
                        UIObjectGroupItem.getItemIds(newObjectGroups, _objectGroupIdBuffer);
                        _objectGroupView.setAllItemsSelected(false, false, false);
                        _objectGroupView.setItemsSelected(_objectGroupIdBuffer, true, true, true);
                        _objectGroupView.scheduleScrollToItems(_objectGroupIdBuffer);
                    }
                }
                else
                {
                    if (PluginPrefabManagerUI.instance.dragAndDropInitiatedByPrefabView())
                    {
                        var objectGroup = _objectGroupView.dropDestinationData;
                        PluginPrefabManagerUI.instance.assignObjectGroupToSelectedVisiblePrefabs(objectGroup);
                        PluginDragAndDrop.endDrag();
                    }
                }
            });
        }

        private void createActionFitlersUI()
        {
            ObjectGroupDb.instance.actionFilters.buildUI(contentContainer);
        }

        private void populateObjectGroupView()
        {
            if (_objectGroupView == null) return;
            _objectGroupSearchField.refreshMatchNames();

            _objectGroupView.onBeginBuild();
            var rootObjectGroups = new List<ObjectGroup>();
            ObjectGroupDb.instance.getRootObjectGroups(rootObjectGroups);

            foreach (var objectGroup in rootObjectGroups)
            {
                _objectGroupView.addItem(objectGroup, filterObjectGroup(objectGroup));
                createObjectGroupItemsRecurse(objectGroup);
            }

            _objectGroupView.onEndBuild();
        }

        private void createObjectGroupItemsRecurse(ObjectGroup parentGroup)
        {
            var childGroups = new List<ObjectGroup>();
            parentGroup.getDirectChildren(childGroups);

            foreach (var childGroup in childGroups)
                _objectGroupView.addItem(childGroup, UIObjectGroupItem.getItemId(parentGroup), filterObjectGroup(childGroup));

            foreach (var childGroup in childGroups)
                createObjectGroupItemsRecurse(childGroup);
        }

        private void createSearchToolbar()
        {
            var toolbar                 = new Toolbar();
            toolbar.style.flexShrink    = 0.0f;
            contentContainer.Add(toolbar);

            _objectGroupSearchField = new EntitySearchField(toolbar, (entityNames) =>
            { ObjectGroupDb.instance.getObjectGroupNames(entityNames, null); },
            (name) => { _objectGroupView.filterItems(filterObjectGroup); });
        }

        private void onObjectGroupSelectedItemsWillBeDeleted(TreeView<UIObjectGroupItem, ObjectGroup> treeView, List<PluginGuid> selectedParentIds, List<PluginGuid> allSelectedItemIds, List<PluginGuid> allItemIds)
        {
            _objectGroupView.getItemData(allItemIds, _objectGroupBuffer);

            // Note: Remove prefab links to groups that will be removed in order to allow Undo/Redo.
            PrefabLibProfileDb.instance.performPrefabAction((prefab) => { if (_objectGroupBuffer.Contains(prefab.objectGroup)) prefab.objectGroup = null; });
            ObjectGroupDb.instance.deleteObjectGroups(_objectGroupBuffer);
            PluginPrefabManagerUI.instance.onPrefabObjectGroupLinksChanged();
        }

        private bool filterObjectGroup(ObjectGroup objectGroup)
        {
            if (!_objectGroupSearchField.matchName(objectGroup.gameObject.name)) return false;
            return true;
        }

        private void getVisibleSelectedObjectGroups(List<ObjectGroup> objectsGroups)
        {
            objectsGroups.Clear();
            _objectGroupView.getVisibleSelectedItemData(objectsGroups);
        }

        private void updateObjectGroupParentChildRelationships()
        {
            if (_objectGroupView != null)
            {
                ObjectGroupDb.instance.getObjectGroups(_objectGroupBuffer);
                foreach (var objectGroup in _objectGroupBuffer)
                {
                    var parentObjectGroup = objectGroup.findParentGroup();
                    if (parentObjectGroup != null) _objectGroupView.setItemParent(UIObjectGroupItem.getItemId(objectGroup), UIObjectGroupItem.getItemId(parentObjectGroup));
                    else _objectGroupView.detachItemFromParent(UIObjectGroupItem.getItemId(objectGroup));
                }
            }

            _objectGroupBuffer.Clear();
        }

        private void onEditorUpdate()
        {
/*
            if (_assignSelectedObjectsBtn != null)
            {
                _assignSelectedObjectsBtn.SetEnabled(Plugin.isActiveSelected && Plugin.active.levelDesignToolType == LevelDesignToolType.ObjectSelection &&
                         ObjectSelection.instance.numSelectedObjects != 0 && _objectGroupView.numSelectedItems == 1);
            }*/
        }
    }
}
#endif