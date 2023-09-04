#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class PrefabLibProfileDbUI : PluginUI
    {
        private ProfileSelectionUI<PrefabLibProfileDb, PrefabLibProfile> _profileSelectionUI;

        [NonSerialized]
        private List<PluginGuid>        _libIdBuffer            = new List<PluginGuid>();
        [NonSerialized]
        private List<PrefabLib>         _libBuffer              = new List<PrefabLib>();
        [NonSerialized]
        private List<PluginPrefab>      _prefabBuffer           = new List<PluginPrefab>();
        [NonSerialized]
        private List<PluginPrefab>      _selectedPrefabBuffer   = new List<PluginPrefab>();
        [NonSerialized]
        private List<ObjectGroup>       _objectGroupBuffer      = new List<ObjectGroup>();

        [SerializeField]
        private TreeViewState                           _libViewState;
        [NonSerialized]
        private TreeView<UIPrefabLibItem, PrefabLib>    _libView;

        private EntitySearchField       _libSearchField;
        private TextField               _newLibNameField;
        private ToolbarButton           _createPrefabsFromSelectionBtn;

        public static PrefabLibProfileDbUI instance             { get { return PrefabLibProfileDb.instance.ui; } }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            UICopyPaste.cancel();
            refresh();
        }

        public void getVisibleSelectedLibs(List<PrefabLib> libs)
        {
            libs.Clear();
            if (uiVisibleAndReady) _libView.getVisibleSelectedItemData(libs);
        }

        public void getSelectedLibs(List<PrefabLib> libs)
        {
            libs.Clear();
            if (uiVisibleAndReady) _libView.getSelectedItemData(libs);
        }

        public void selectOwnerLibsOfPrefabAssets(List<GameObject> prefabAssets)
        {
            if (uiVisibleAndReady)
            {
                _libView.setAllItemsSelected(false, false, false);
                PrefabLibProfileDb.instance.activeProfile.findOwnerLibsWithAnyPrefabAsset(prefabAssets, _libBuffer);
                UIPrefabLibItem.getItemIds(_libBuffer, _libIdBuffer);
                _libView.setItemsSelected(_libIdBuffer, true, true, true);
            }
        }

        public void selectPrefabLibsAndMakeVisible(List<PrefabLib> libs)
        {
            if (uiVisibleAndReady)
            {
                _libSearchField.clearSearchName(true);
                _libView.setAllItemsSelected(false, false, false);
                UIPrefabLibItem.getItemIds(libs, _libIdBuffer);
                _libView.expandUpwards(_libIdBuffer);
                _libView.setItemsSelected(_libIdBuffer, true, true, true);

                PrefabLibProfileDb.instance.activeProfile.getLibs(_libBuffer);
                foreach(var lib in _libBuffer)
                {
                    lib.setPrefabsVisibleInManagerLocal(true);
                    _libView.refreshItemUI(lib.guid);
                }
            }
        }

        public void addPrefabToSelectedLibraries(GameObject prefabAsset)
        {
            if (uiVisibleAndReady && _libView.numSelectedItems != 0)
            {
                _prefabBuffer.Clear();
                List<PrefabLib> selectedLibs = new List<PrefabLib>();
                _libView.getSelectedItemData(selectedLibs);
                foreach (var lib in selectedLibs)
                    _prefabBuffer.Add(lib.createPrefab(prefabAsset));

                _libView.refreshUI();
                PluginPrefabManagerUI.instance.onTargetLibsChangedContent();
                PluginPrefabManagerUI.instance.selectAndScrollToPrefabs(_prefabBuffer);
                _prefabBuffer.Clear();
            }
        }

        protected override void onBuild()
        {
            _profileSelectionUI = new ProfileSelectionUI<PrefabLibProfileDb, PrefabLibProfile>();
            _profileSelectionUI.build(PrefabLibProfileDb.instance, "prefab library", contentContainer);

            contentContainer.style.flexGrow = 1.0f;
            createTopToolbar();
            createSearchToolbar();
            createLibView();
            createBottomToolbar();
            populateLibView();

            contentContainer.RegisterCallback<KeyDownEvent>(onKeyDown);
        }

        protected override void onRefresh()
        {
            if (_libView != null) _libView.refreshUI();
        }

        private void onKeyDown(KeyDownEvent e)
        {
            if (FixedShortcuts.ui_Paste(e) && UICopyPaste.initiatorId == PluginPrefabManagerUI.instance.copyPasteInitiatorId) UICopyPaste.paste();
        }

        private void populateLibView()
        {
            if (_libView == null) return;

            _libView.onBeginBuild();

            _libSearchField.refreshMatchNames();
            var rootLibs = new List<PrefabLib>();
            PrefabLibProfileDb.instance.activeProfile.getRootLibs(rootLibs);

            foreach (var rootLib in rootLibs)
            {
                _libView.addItem(rootLib, filterLib(rootLib));
                createLibItemsRecurse(rootLib);
            }

            _libView.onEndBuild();
        }

        private void createLibItemsRecurse(PrefabLib parentLib)
        {
            var childLibs = new List<PrefabLib>();
            parentLib.getDirectChildren(childLibs);

            foreach (var childLib in childLibs)
            {
                _libView.addItem(childLib, UIPrefabLibItem.getItemId(parentLib), filterLib(childLib));
            }

            foreach (var childLib in childLibs)
                createLibItemsRecurse(childLib);
        }

        private void createTopToolbar()
        {
            Toolbar toolbar = new Toolbar();
            toolbar.style.flexShrink = 0.0f;
            contentContainer.Add(toolbar);

            _createPrefabsFromSelectionBtn = UI.createToolbarButton("Create prefab...", toolbar);
            _createPrefabsFromSelectionBtn.tooltip = "Opens up a new window which will allow you to create a prefab from the currently selected objects.";
            _createPrefabsFromSelectionBtn.RegisterCallback<MouseUpEvent>((p) =>
            {
                PluginWindow.showUtility<PrefabFromSelectedObjectsCreationSettingsWindow>("Prefab Creation");
            });

            /*_createObjectGroupsFromLibsBtn = UI.createToolbarButton("Create object groups...", toolbar);
            _createObjectGroupsFromLibsBtn.tooltip = "Opens up a new window which will present you with a few options for creating object groups for all prefabs in all libraries.";
            _createObjectGroupsFromLibsBtn.clicked += () =>
            {
                var window = PluginWindow.showUtility<ObjectGroupsFromPrefabLibsCreationSettingsWindow>("Object Group Creation");
                Vector2 size = new Vector2(380.0f, 140.0f);
                window.setMinMaxSize(size);
                window.setSize(size);
                window.centerOnScreen();
            };*/

            var autoLinkObjectGroupsBtn = UI.createToolbarButton("Auto-link object groups", toolbar);
            autoLinkObjectGroupsBtn.tooltip = "Automatically link object groups to prefabs based on prefab-group links that already exist in other scenes.";
            autoLinkObjectGroupsBtn.clicked += () =>
            {
                int numLibs = PrefabLibProfileDb.instance.activeProfile.numLibs;
                ObjectGroupDb.instance.getObjectGroups(_objectGroupBuffer);

                PluginProgressDialog.begin("Linking Object Groups");

                // Loop through each object group
                int numObjectGroups = _objectGroupBuffer.Count;
                for (int groupIndex = 0; groupIndex < numObjectGroups; ++groupIndex)
                {
                    ObjectGroup objectGroup = _objectGroupBuffer[groupIndex];
                    PluginProgressDialog.updateProgress(objectGroup.objectGroupName, (groupIndex + 1) / (float)numObjectGroups);

                    // Loop through each prefab in each prefab lib and associate the prefab
                    // with this group if it's already associated with a group that has the same
                    // name in a different scene.
                    for (int libIndex = 0; libIndex < numLibs; ++libIndex)
                    {
                        PrefabLib prefabLib = PrefabLibProfileDb.instance.activeProfile.getLib(libIndex);
                        int numPrefabs = prefabLib.numPrefabs;
                        for (int prefabIndex = 0; prefabIndex < numPrefabs; ++prefabIndex)
                        {
                            PluginPrefab pluginPrefab = prefabLib.getPrefab(prefabIndex);
                            if (pluginPrefab.isAssociatedWithObjectGroup(objectGroup.objectGroupName, true))
                                pluginPrefab.objectGroup = objectGroup;
                        }
                    }
                }
                PluginProgressDialog.end();

                PluginPrefabManagerUI.instance.refresh();

                EditorUtility.DisplayDialog("Success", "Object groups assigned successfully.", "Ok");
            };

            var generateDecorRulesBtn     = UI.createToolbarButton("Generate decor rules", toolbar);
            generateDecorRulesBtn.tooltip = "Pressing this button will instruct the plugin to parse the currently loaded scene and detect the way in which " +
                "prefabs decorate each other. This can be used for example when spawning props with the Props Spawn tool in order to 'snap' the spawn guide " +
                "to the correct position in relation to the object being hovered by the mouse cursor. Only the prefabs in the currently active profile are affected. " + 
                "Note: If any prefabs in the active profile are present in other profiles, those will be affected too.";
            generateDecorRulesBtn.clicked += () => 
            {
                PrefabDecorRuleDb.instance.generateDecorRules(PrefabLibProfileDb.instance.activeProfile);
            };

            UI.createColumnSeparator(toolbar).style.flexGrow = 1.0f;
            var refreshLibsBtn      = UI.createToolbarButton(TexturePool.instance.refresh, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(refreshLibsBtn);
            refreshLibsBtn.tooltip  = "Refresh libraries. Pressing this button will check if new prefab assets have been added to any folders " + 
                "associated with existing libraries and add those prefabs to the libraries. Only the libraries in the currently active profile are affected.";
            refreshLibsBtn.clicked += () => 
            {
                PluginProgressDialog.begin("Refresh Libraries");
                var activeProfile   = PrefabLibProfileDb.instance.activeProfile;
                int numLibs         = activeProfile.numLibs;
                bool anythingAdded  = false;
                for (int i = 0; i < numLibs; ++i)
                {
                    var lib         = activeProfile.getLib(i);
                    PluginProgressDialog.updateProgress(lib.libName, (i + 1) / numLibs);
                    if (string.IsNullOrEmpty(lib.folderPath) || !FileSystem.folderExists(lib.folderPath)) continue;

                    var loadedPrefabAssets  = AssetDbEx.loadPrefabs(lib.folderPath, null);
                    int numPrefabs          = loadedPrefabAssets.Count;
                    for (int j = 0; j < numPrefabs; ++j)
                    {
                        if (!lib.containsPrefab(loadedPrefabAssets[j]))
                        {
                            lib.createPrefab(loadedPrefabAssets[j]);
                            anythingAdded = true;
                        }
                    }
                    _libView.refreshItemUI(lib.guid);
                }
                if (anythingAdded) PluginPrefabManagerUI.instance.onTargetLibsChangedContent();
                PluginProgressDialog.end();
            };
        }

        private void createSearchToolbar()
        {
            var searchToolbar               = new Toolbar();
            searchToolbar.style.flexShrink  = 0.0f;
            contentContainer.Add(searchToolbar);

            _libSearchField = new EntitySearchField(searchToolbar, (entityNames) =>
            { PrefabLibProfileDb.instance.activeProfile.getLibDisplayNames(entityNames); },
            (name) => { _libView.filterItems(filterLib); });
        }

        private void createLibView()
        {
            _libView                    = new TreeView<UIPrefabLibItem, PrefabLib>(_libViewState, contentContainer);
            _libView.createItemDataFunc = () => { return PrefabLibProfileDb.instance.activeProfile.createLib("Prefab Library"); };
            _libView.canCopyPaste       = true;
            _libView.canCutPaste        = false;   // Note: Don't allow cut-paste to avoid deleting and creating prefabs. Can break links with curve/prop etc prefabs.
            _libView.canDuplicate       = true;

            _libView.RegisterCallback<DragPerformEvent>((p) =>
            {
                if (PluginDragAndDrop.initiatedByPlugin)
                {
                    if (PluginPrefabManagerUI.instance.dragAndDropInitiatedByPrefabView())
                    {
                        var dropDestLib = _libView.dropDestinationData;                                              
                        if (dropDestLib != null)
                        {
                            bool moved = false;
                            PluginPrefabManagerUI.instance.getVisibleSelectedPrefabs(_selectedPrefabBuffer);
                            if (FixedShortcuts.ui_DropAsCopy(Event.current))
                            {
                                dropDestLib.createPrefabs(_selectedPrefabBuffer, _prefabBuffer, false, "Dropping Prefabs");
                                moved = _prefabBuffer.Count != 0;
                            }
                            else
                            {
                                // Note: Don't use getVisibleSelectedLibs. Some libs may belong to a lib hierarchy that
                                //       has been collapsed and are not visible.
                                getSelectedLibs(_libBuffer); 
                                int numMoved = PrefabLib.movePrefabs(_selectedPrefabBuffer, _libBuffer, dropDestLib, "Moving Prefabs");
                                moved = (numMoved != 0);

                                // Store in _prefabBuffer so that we can call 'PluginPrefabManagerUI.instance.selectAndScrollToPrefabs'
                                // using the same list (i.e. _prefabBuffer).
                                _prefabBuffer.Clear();
                                if (moved) _prefabBuffer.AddRange(_selectedPrefabBuffer);
                            }

                            // Note: The number of created prefabs can be 0 if we are dragging the prefabs into the same library
                            //       (i.e. the source and destination libraries are the same).
                            if (moved)
                            {
                                _libView.setAllItemsSelected(false, false, false);
                                _libView.setItemSelected(UIPrefabLibItem.getItemId(dropDestLib), true, true);
                                _libView.refreshUI();

                                PluginPrefabManagerUI.instance.onTargetLibsChangedContent();
                                PluginPrefabManagerUI.instance.selectAndScrollToPrefabs(_prefabBuffer);
                            }
                        }
                        PluginDragAndDrop.endDrag();
                    }
                }
                else
                {
                    var dragAndDrop     = new PrefabDragAndDrop();
                    dragAndDrop.dropFoldersInLibDb();
                    if (!dragAndDrop.anythingDropped) return;

                    var parentLib       = _libView.dropDestinationData;
                    var droppedRootLibs = new List<PrefabLib>();
                    dragAndDrop.getDroppedRootLibs(droppedRootLibs);

                    if (parentLib != null)
                    {
                        foreach (var droppedLib in droppedRootLibs)
                            droppedLib.parentLib = parentLib;
                    }

                    populateLibView();

                    UIPrefabLibItem.getItemIds(droppedRootLibs, _libIdBuffer);
                    _libView.setAllItemsSelected(false, false, false);
                    _libView.setItemsSelected(_libIdBuffer, true, true, true);
                    _libView.scheduleScrollToItems(_libIdBuffer);
                }
            });

            _libView.selectedItemsWillBeDeleted     += onSelectedLibItemsWillBeDeleted;
            _libView.itemsDetachedFromParent        += onLibItemsDetachedFromParent;
            _libView.selectionChanged               += onLibSelectionChanged;
            _libView.paste                          += onPasteLibs;
        }

        private void createBottomToolbar()
        {
            var bottomToolbar               = new Toolbar();
            bottomToolbar.style.flexShrink  = 0.0f;
            contentContainer.Add(bottomToolbar);

            var createLibButton             = UI.createSmallCreateNewToolbarButton(bottomToolbar);
            createLibButton.clicked         += () => { createNewLib(); };
            createLibButton.tooltip         = "Create a new prefab library with the specified name.";

            _newLibNameField                = UI.createToolbarTextField(bottomToolbar);
            _newLibNameField.style.flexGrow = 1.0f;
        }

        private void createNewLib()
        {
            var newLib = PrefabLibProfileDb.instance.activeProfile.createLib(_newLibNameField.text);
            if (newLib != null)
            {
                _libView.setAllItemsSelected(false, false, false);
                PluginGuid newLibId = _libView.addItem(newLib, true);
                _libView.setItemSelected(newLibId, true, false);
                _libView.scheduleScrollToItem(newLibId);
                onLibSelectionChanged(_libView);
            }
        }

        private bool filterLib(PrefabLib lib)
        {
            if (!_libSearchField.matchName(PrefabLibProfileDb.instance.activeProfile.getLibDisplayName(lib))) return false;
            return true;
        }

        private void onLibSelectionChanged(TreeView<UIPrefabLibItem, PrefabLib> treeView)
        {
            var libs = new List<PrefabLib>();
            _libView.getSelectedItemData(libs);
            PluginPrefabManagerUI.instance.setTargetLibs(libs);
        }

        private void onSelectedLibItemsWillBeDeleted(TreeView<UIPrefabLibItem, PrefabLib> treeView, List<PluginGuid> parentLibIds, List<PluginGuid> allSelectedLibIds, List<PluginGuid> allItemIds)
        {
            UICopyPaste.cancel();

            var libs = new List<PrefabLib>();
            _libView.getItemData(allItemIds, libs);

            PrefabLib.getPrefabs(libs, _prefabBuffer);

            PluginPrefabEvents.onPrefabsWillBeRemoved(_prefabBuffer);
            PrefabLibProfileDb.instance.activeProfile.deleteLibs(libs);

            var targetLibs = new List<PrefabLib>();
            _libView.getSelectedItemData(targetLibs);
            targetLibs.RemoveAll(item => allSelectedLibIds.Contains(UIPrefabLibItem.getItemId(item)));
            PluginPrefabManagerUI.instance.setTargetLibs(targetLibs);
        }

        private void onLibItemsDetachedFromParent(TreeView<UIPrefabLibItem, PrefabLib> treeView, List<PluginGuid> itemIds)
        {
            var libs = new List<PrefabLib>();
            _libView.getItemData(itemIds, libs);
            foreach (var lib in libs)
                lib.parentLib = null;
        }

        private void onPasteLibs(TreeView<UIPrefabLibItem, PrefabLib> treeView, List<PluginGuid> srcLibIds, List<PluginGuid> destLibIds, CopyPasteMode copyPasteMode)
        {
            var srcLibs = new List<PrefabLib>();
            _libView.getItemData(srcLibIds, srcLibs);

            var destLibs = new List<PrefabLib>();
            _libView.getItemData(destLibIds, destLibs);

            if (copyPasteMode == CopyPasteMode.Cut)
            {
                PrefabLib.getPrefabs(srcLibs, _prefabBuffer);
                PluginPrefabEvents.onPrefabsWillBeRemoved(_prefabBuffer);
            }

            PrefabLib.paste(srcLibs, destLibs, copyPasteMode);
            PluginPrefabManagerUI.instance.onTargetLibsChangedContent();
        }

        protected override void onEnabled()
        {           
            EditorApplication.update += onEditorUpdate;
            if (_libViewState == null)
            {
                _libViewState = ScriptableObject.CreateInstance<TreeViewState>();
                _libViewState.name = GetType().Name + "_LibViewState";

                // Note: Attach to PrefabLibProfileDb.instance since that is the actual parent asset.
                AssetDbEx.addObjectToAsset(_libViewState, PrefabLibProfileDb.instance);
            }

            PrefabLibProfileDb.instance.activeProfileChanged += onActiveProfileChanged;

            var prefabManagerUI = PluginPrefabManagerUI.instance;
            if (prefabManagerUI != null)
            {
                var activeLibProfile = PrefabLibProfileDb.instance.activeProfile;
                activeLibProfile.getSelectedLibs(_libBuffer);
                PluginPrefabManagerUI.instance.setTargetLibs(_libBuffer);
            }
        }

        protected override void onDisabled()
        {
            EditorApplication.update -= onEditorUpdate;
            PrefabLibProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_libViewState);
        }

        protected override void onUndoRedo()
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            if (_libView != null)
            {
                populateLibView();
                var libs = new List<PrefabLib>();
                _libView.getSelectedItemData(libs);
                PluginPrefabManagerUI.instance.setTargetLibs(libs);
            }
        }

        private void onActiveProfileChanged(PrefabLibProfile newActiveProfile)
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            if (_libView != null && 
                (UICopyPaste.initiatorId == _libView.copyPasteInitiatorId || 
                UICopyPaste.initiatorId == PluginPrefabManagerUI.instance.copyPasteInitiatorId)) UICopyPaste.cancel();

            populateLibView();
            onLibSelectionChanged(_libView);
        }

        private void onEditorUpdate()
        {
            // Note: This line seems to throw errors when deleting the Plugin folder. It's harmless.
            //if (!Plugin.isActiveSelected) return;

            /*if (_createPrefabsFromSelectionBtn != null)
            {
                if (Plugin.active.levelDesignToolType != LevelDesignToolType.ObjectSelection ||
                    !ObjectSelection.instance.canReplace() ||
                    (_libViewState != null && _libViewState.selectedItems.Count == 0)) _createPrefabsFromSelectionBtn.SetEnabled(false);
                else _createPrefabsFromSelectionBtn.SetEnabled(true);
            }

            if (_createObjectGroupsFromLibsBtn != null)
            {
                _createObjectGroupsFromLibsBtn.SetEnabled(PrefabLibProfileDb.instance.activeProfile.numLibs != 0 && Plugin.isActiveSelected);
            }*/
        }
    }
}
#endif