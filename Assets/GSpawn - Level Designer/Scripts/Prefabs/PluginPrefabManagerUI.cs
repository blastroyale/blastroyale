#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class PluginPrefabManagerUI : PluginUI
    {
        [SerializeField]
        private GridViewState                                           _prefabViewState;
        [NonSerialized]
        private GridView<UIPluginPrefabItem, UIPluginPrefabItemData>    _prefabView;

        private EntitySearchField       _prefabSearchField;
        private Slider                  _previewScaleSlider;
        [SerializeField]
        private float                   _prefabPreviewScale     = 1.0f;
        [SerializeField]
        private PluginPrefabFilter      _prefabFilter           = PluginPrefabFilter.All;
        [NonSerialized]
        private EnumFlagsField          _prefabFilterField;

        [SerializeField]
        private List<PrefabLib>         _targetLibs             = new List<PrefabLib>();
        [SerializeField]
        private PrefabLibHashSet        _targetLibSet           = new PrefabLibHashSet();
        [SerializeField]
        private int                     _numPrefabs             = 0;

        [NonSerialized]
        private List<PluginGuid>        _prefabIdBuffer         = new List<PluginGuid>();
        [NonSerialized]
        private List<PluginPrefab>      _pluginPrefabBuffer     = new List<PluginPrefab>();
        [NonSerialized]
        private List<GameObject>        _prefabAssetBuffer      = new List<GameObject>();
        [NonSerialized]
        private List<PrefabLib>         _prefabLibBuffer        = new List<PrefabLib>();
        [NonSerialized]
        private List<PluginPrefab>      _copyPastePrefabsBuffer = new List<PluginPrefab>();
        [NonSerialized]
        private List<PluginPrefab>      _createdPrefabsBuffer   = new List<PluginPrefab>();
        [NonSerialized]
        private List<string>            _stringBuffer           = new List<string>();

        public int                      numTargetLibs           { get { return _targetLibs.Count; } }
        public int                      numPrefabs              { get { return _numPrefabs; } }
        public float                    prefabPreviewScale      { get { return _prefabPreviewScale; } set { UndoEx.record(this); _prefabPreviewScale = Mathf.Clamp(value, UIValues.minPrefabPreviewScale, UIValues.maxPrefabPreviewScale); EditorUtility.SetDirty(this); } }
        public bool                     anyPrefabsSelected      { get { return _prefabViewState != null && _prefabViewState.selectedItems.Count != 0; } }
        public int                      copyPasteInitiatorId    { get { return GetHashCode(); } }

        public static PluginPrefabManagerUI instance            { get { return PrefabLibProfileDb.instance.prefabManagerUI; } }

        public PluginPrefab getFirstDragAndDropPrefab()
        {
            if (!dragAndDropInitiatedByPrefabView()) return null;

            var dropData = _prefabView.dragAndDropData as GridView<UIPluginPrefabItem, UIPluginPrefabItemData>.DragAndDropData;
            return dropData.getItem(0).data.prefab;
        }

        public void selectPluginPrefabsAndMakeVisible(List<PluginPrefab> pluginPrefabs, bool updatePrefabFilter)
        {
            if (uiVisibleAndReady)
            {
                PrefabLibProfileDb.instance.activeProfile.findOwnerLibsOfPrefabs(pluginPrefabs, _prefabLibBuffer);
                PrefabLibProfileDbUI.instance.selectPrefabLibsAndMakeVisible(_prefabLibBuffer);

                UIPluginPrefabItem.getItemIds(pluginPrefabs, _prefabIdBuffer);
                _prefabView.setAllItemsSelected(false, false, false);
                _prefabView.setItemsSelected(_prefabIdBuffer, true, false, true);
                _prefabView.scheduleScrollToItems(_prefabIdBuffer);

                _prefabSearchField.clearSearchName(false);
                if (updatePrefabFilter)
                {
                    // Note: Call 'SetValueWithoutNotify' first to clear the filter. This will
                    //       make sure that the value changed callback will be triggered when the
                    //       final value is set. Otherwise, the value changed callback might not
                    //       be called if the filters are already set to PluginPrefabFilter.All & ~PluginPrefabFilter.Unselected.
                    _prefabFilterField.SetValueWithoutNotify(PluginPrefabFilter.None);
                    _prefabFilterField.value = PluginPrefabFilter.All & ~PluginPrefabFilter.Unselected;
                }
            }
        }

        public void assignObjectGroupToSelectedVisiblePrefabs(ObjectGroup objectGroup)
        {
            if (uiVisibleAndReady)
            {
                getVisibleSelectedPrefabs(_pluginPrefabBuffer);
                foreach (var prefab in _pluginPrefabBuffer)
                    prefab.objectGroup = objectGroup;

                UIPluginPrefabItem.getItemIds(_pluginPrefabBuffer, _prefabIdBuffer);
                _prefabView.refreshItemsUI(_prefabIdBuffer);
            }
        }

        public bool dragAndDropInitiatedByPrefabView()
        {
            if (!uiVisibleAndReady) return false;

            return PluginDragAndDrop.initiatorId == _prefabView.dragAndDropInitiatorId;
        }

        public void selectAndScrollToPrefab(PluginPrefab prefab)
        {
            if (uiVisibleAndReady)
            {
                PluginGuid itemId = UIPluginPrefabItem.getItemId(prefab);
                _prefabView.setAllItemsSelected(false, false, false);
                _prefabView.setItemSelected(itemId, true, true);
                _prefabView.scheduleScrollToItem(itemId);
            }
        }

        public void selectAndScrollToPrefabs(List<PluginPrefab> prefabs)
        {
            if (uiVisibleAndReady)
            {
                UIPluginPrefabItem.getItemIds(prefabs, _prefabIdBuffer);
                _prefabView.setAllItemsSelected(false, false, false);
                _prefabView.setItemsSelected(_prefabIdBuffer, true, true, true);
                _prefabView.scheduleScrollToItems(_prefabIdBuffer);
            }
        }

        public void selectAndScrollToPrefabs(List<GameObject> prefabAssets)
        {
            if (uiVisibleAndReady)
            {
                _pluginPrefabBuffer.Clear();
                foreach (var lib in _targetLibs)
                {
                    foreach(var prefabAsset in prefabAssets)
                    {
                        var prefab = lib.getPrefab(prefabAsset);
                        if (prefab != null) _pluginPrefabBuffer.Add(prefab);
                    }
                }

                UIPluginPrefabItem.getItemIds(_pluginPrefabBuffer, _prefabIdBuffer);
                _prefabView.setAllItemsSelected(false, false, false);
                _prefabView.setItemsSelected(_prefabIdBuffer, true, true, true);
                _prefabView.scheduleScrollToItems(_prefabIdBuffer);
            }
        }

        public void getSelectedPrefabs(List<PluginPrefab> prefabs, bool onlyVisible)
        {
            prefabs.Clear();
            if (!uiVisibleAndReady) return;

            var selectedItemData = new List<UIPluginPrefabItemData>();
            _prefabView.getSelectedItemData(selectedItemData, onlyVisible);

            foreach (var itemData in selectedItemData)
                prefabs.Add(itemData.prefab);
        }

        public void getVisibleSelectedPrefabs(List<PluginPrefab> prefabs)
        {
            prefabs.Clear();
            if (!uiVisibleAndReady) return;

            var selectedPrefabItemData = new List<UIPluginPrefabItemData>();
            _prefabView.getVisibleSelectedItemData(selectedPrefabItemData);

            foreach (var itemData in selectedPrefabItemData)
                prefabs.Add(itemData.prefab);
        }

        public void getVisibleSelectedPrefabAssets(List<GameObject> prefabs)
        {
            prefabs.Clear();
            if (!uiVisibleAndReady) return;

            var selectedPrefabItemData = new List<UIPluginPrefabItemData>();
            _prefabView.getVisibleSelectedItemData(selectedPrefabItemData);

            foreach (var itemData in selectedPrefabItemData)
                prefabs.Add(itemData.prefab.prefabAsset);
        }

        public void setAllPrefabsSelected(bool selected, bool onlyVisible)
        {
            if (uiVisibleAndReady)
            {
                _prefabView.setAllItemsSelected(selected, onlyVisible, true);
            }
        }

        public void setPrefabsSelected(List<PluginPrefab> prefabs, bool selected, bool scrollToSelected, bool onlyVisible)
        {
            if (uiVisibleAndReady)
            {
                UIPluginPrefabItem.getItemIds(prefabs, _prefabIdBuffer);
                _prefabView.setItemsSelected(_prefabIdBuffer, selected, onlyVisible, true);
                if (scrollToSelected) _prefabView.scheduleScrollToItems(_prefabIdBuffer);
            }
        }

        public void onPrefabObjectGroupLinksChanged()
        {
            if (uiVisibleAndReady) _prefabView.refreshUI();
        }

        public void libsChangedPrefabVisibility(List<PrefabLib> libs, bool visible)
        {
            if (!uiVisibleAndReady) return;

            foreach (var lib in libs)
            {
                if (_targetLibSet.Contains(lib))
                {
                    lib.getPrefabs(_pluginPrefabBuffer);
                    foreach (var prefab in _pluginPrefabBuffer)
                    {
                        // Note: Some prefabs may have been previosuly filtered and may not be present in the grid view.
                        var prefabId = UIPluginPrefabItem.getItemId(prefab);
                        if (_prefabView.containsItem(prefabId))
                            _prefabView.setItemVisible(prefabId, filterPrefab(lib, prefab));
                    }
                }
            }
        }

        public void onTargetLibsChangedContent()
        {
            _numPrefabs = PrefabLib.calcNumPrefabsInLibs(_targetLibs);
            populatePrefabView();
        }

        public void setTargetLibs(List<PrefabLib> newTargetLibs)
        {
            if (_prefabView == null) refreshTargetLibCollections(newTargetLibs);
            else
            {
                // Note: We need to supply a new set of names for the search field
                //       to work with and those are the names of the prefabs residing
                //       in the new target libs.
                PrefabLib.getPrefabNames(newTargetLibs, _stringBuffer);
                _prefabSearchField.refreshMatchNames(_stringBuffer);

                var newTargetLibSet = new HashSet<PrefabLib>(newTargetLibs);
                _prefabView.deleteItems(itemData => !newTargetLibSet.Contains(itemData.prefabLib));

                foreach (var lib in newTargetLibs)
                {
                    if (_targetLibSet.Contains(lib)) continue;

                    lib.getPrefabs(_pluginPrefabBuffer);
                    foreach (var prefab in _pluginPrefabBuffer)
                    {
                        _prefabView.addItem(new UIPluginPrefabItemData() 
                        { prefab = prefab, prefabLib = lib }, filterPrefab(lib, prefab));
                    }                      
                }

                refreshTargetLibCollections(newTargetLibs);
            }
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            if (_prefabView != null) _prefabView.deleteItems(itemData => itemData.prefab.prefabAsset == prefabAsset);
            onTargetLibsChangedContent();
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow = 1.0f;
            createTopToolbar();
            creteSecondaryTopToolbar();
            createPrefabView();
            createBottomToolbar();
            populatePrefabView();

            contentContainer.RegisterCallback<KeyDownEvent>(onKeyDown);

            // Note: Needed the first time the window is created in order to make sure 
            //       that the correct prefabs show up.
            if (_targetLibs.Count == 0)
            {
                PrefabLibProfileDbUI.instance.getVisibleSelectedLibs(_prefabLibBuffer);
                if (_prefabLibBuffer.Count != 0) setTargetLibs(_prefabLibBuffer);
            }
        }

        protected override void onRefresh()
        {
            if (_prefabView != null) _prefabView.refreshUI();
        }

        protected override void onEnabled()
        {
            if (_prefabViewState == null)
            {
                _prefabViewState        = ScriptableObject.CreateInstance<GridViewState>();
                _prefabViewState.name   = GetType().Name + "_PrefabViewState";

                // Note: Attach to PrefabLibProfileDb.instance since that is the actual parent asset.
                AssetDbEx.addObjectToAsset(_prefabViewState, PrefabLibProfileDb.instance);
            }
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_prefabViewState);
        }

        protected override void onUndoRedo()
        {
            if (_prefabView != null && GSpawn.active != null) populatePrefabView();
        }

        private void onKeyDown(KeyDownEvent e)
        {
            if (FixedShortcuts.ui_Copy(e))
            {
                _prefabView.setAllItemsCopyPasteMode(CopyPasteMode.None);
                UICopyPaste.cancel();

                getVisibleSelectedPrefabs(_copyPastePrefabsBuffer);
                UIPluginPrefabItem.getItemIds(_copyPastePrefabsBuffer, _prefabIdBuffer);
                _prefabView.setItemsCopyPasteMode(_prefabIdBuffer, CopyPasteMode.Copy);
                UICopyPaste.begin(CopyPasteMode.Copy, copyPasteInitiatorId, copyPasteSelectedPrefabsInSelectedLibs, cancelPrefabCopyPaste);
            }
            // Note: Cut pasting works, but because multiple libs can be selected as destination libs,
            //       the source prefabs need to be deleted and new ones created instead. This can cause
            //       confusion because curve prefabs, props prefabs etc which are linked to the source
            //       prefabs will also be deleted.
            /*
            else if (FixedShortcuts.ui_Cut(e))
            {
                _prefabView.setAllItemsCopyPasteMode(CopyPasteMode.None);
                UICopyPaste.cancel();

                getVisibleSelectedPrefabs(_copyPastePrefabsBuffer);
                UIPluginPrefabItem.getItemIds(_copyPastePrefabsBuffer, _prefabIdBuffer);
                _prefabView.setItemsCopyPasteMode(_prefabIdBuffer, CopyPasteMode.Cut);
                UICopyPaste.begin(CopyPasteMode.Cut, copyPasteInitiatorId, cutPasteSelectedPrefabsInSelectedLibs, cancelPrefabCopyPaste);
            }*/
            else if (FixedShortcuts.cancelAction(Event.current))
            {
                UICopyPaste.cancel();
            }
        }

        private void cancelPrefabCopyPaste()
        {
            _copyPastePrefabsBuffer.Clear();
            _prefabView.setAllItemsCopyPasteMode(CopyPasteMode.None);

            // Note: We need to make sure all prefabs have their copy paste mode
            //       set to none. This is because the lib selection might have changed
            //       and the prefab previews might not have and a chance to update 
            //       their copy paste mode.
            int numLibs = PrefabLibProfileDb.instance.activeProfile.numLibs;
            for (int libIndex = 0; libIndex < numLibs; ++libIndex)
            {
                PrefabLib lib = PrefabLibProfileDb.instance.activeProfile.getLib(libIndex);
                int numPrefabs = lib.numPrefabs;
                for (int prefabIndex = 0; prefabIndex < numPrefabs; ++prefabIndex)
                    lib.getPrefab(prefabIndex).uiCopyPasteMode = CopyPasteMode.None;
            }
        }

        private void copyPasteSelectedPrefabsInSelectedLibs()
        {
            PrefabLibProfileDbUI.instance.getVisibleSelectedLibs(_prefabLibBuffer);

            _createdPrefabsBuffer.Clear();
            foreach(var lib in _prefabLibBuffer)
                lib.createPrefabs(_copyPastePrefabsBuffer, _createdPrefabsBuffer, true, "Pasting Prefabs");

            // Note: Source prefabs are no longer copy paste source.
            foreach (var prefab in _copyPastePrefabsBuffer)
                prefab.uiCopyPasteMode = CopyPasteMode.None;

            if (_createdPrefabsBuffer.Count == 0) return;
            onTargetLibsChangedContent();

            UIPluginPrefabItem.getItemIds(_createdPrefabsBuffer, _prefabIdBuffer);
            _prefabView.setAllItemsSelected(false, false, false);
            _prefabView.setItemsSelected(_prefabIdBuffer, true, true, false);
            _prefabView.scheduleScrollToItems(_prefabIdBuffer);

            PrefabLibProfileDbUI.instance.refresh();
        }

        private void cutPasteSelectedPrefabsInSelectedLibs()
        {
            PrefabLibProfileDbUI.instance.getVisibleSelectedLibs(_prefabLibBuffer);

            _createdPrefabsBuffer.Clear();
            foreach (var lib in _prefabLibBuffer)
                lib.createPrefabs(_copyPastePrefabsBuffer, _createdPrefabsBuffer, true, "Pasting Prefabs");

            PluginPrefabEvents.onPrefabsWillBeRemoved(_copyPastePrefabsBuffer);

            if (_createdPrefabsBuffer.Count == 0) return;
            PrefabLib.deletePrefabsFromLibs(_copyPastePrefabsBuffer, "Cutting Prefabs");
            onTargetLibsChangedContent();

            UIPluginPrefabItem.getItemIds(_createdPrefabsBuffer, _prefabIdBuffer);
            _prefabView.setAllItemsSelected(false, false, false);
            _prefabView.setItemsSelected(_prefabIdBuffer, true, true, false);
            _prefabView.scheduleScrollToItems(_prefabIdBuffer);

            PrefabLibProfileDbUI.instance.refresh();
        }

        private void refreshTargetLibCollections(IEnumerable<PrefabLib> newTargetLibs)
        {
            _targetLibs.Clear();
            _targetLibs.AddRange(newTargetLibs);

            _numPrefabs = 0;
            _targetLibSet.Clear();
            foreach (var lib in newTargetLibs)
            {
                _numPrefabs += lib.numPrefabs;
                _targetLibSet.Add(lib);
            }
        }

        private void createPrefabView()
        {
            _prefabView                             = new GridView<UIPluginPrefabItem, UIPluginPrefabItemData>(_prefabViewState, contentContainer);
            _prefabView.canDelete                   = true;
            _prefabView.selectedItemsWillBeDeleted  += onSelectedPrefabItemsWillBeDeleted;
            _prefabView.RegisterCallback<DragPerformEvent>((p) =>
            {
                if (numTargetLibs != 0 && !PluginDragAndDrop.initiatedByPlugin)
                {
                    var dragAndDrop = new PrefabDragAndDrop();
                    dragAndDrop.dropPrefabsInLibs(_targetLibs);
                    if (!dragAndDrop.anythingDropped) return;

                    populatePrefabView();

                    _numPrefabs = PrefabLib.calcNumPrefabsInLibs(_targetLibs);
                    PrefabLibProfileDbUI.instance.refresh();

                    dragAndDrop.getDroppedPrefabs(_pluginPrefabBuffer);
                    PluginPrefab.getPrefabIds(_pluginPrefabBuffer, _prefabIdBuffer);

                    _prefabView.setAllItemsSelected(false, false, false);
                    _prefabView.setItemsSelected(_prefabIdBuffer, true, false, true);
                    _prefabView.scheduleScrollToItems(_prefabIdBuffer);
                }
                else
                if (PluginDragAndDrop.initiatedByPlugin)
                {
                    if (ObjectGroupDbUI.instance.dragAndDropInitiatedByObjectGroupView())
                    {
                        var dragData = PluginDragAndDrop.pluginData as TreeView<UIObjectGroupItem, ObjectGroup>.DragAndDropData;
                        var objectGroup = dragData.getItem(0).data;

                        assignObjectGroupToSelectedVisiblePrefabs(objectGroup);
                        PluginDragAndDrop.endDrag();

                        refresh();
                    }
                }
            });
        }

        private void createTopToolbar()
        {
            Toolbar toolbar             = new Toolbar();
            toolbar.style.flexShrink    = 0.0f;
            contentContainer.Add(toolbar);

            var resetPreviewsBtn        = UI.createSmallResetPrefabPreviewsToolbarButton(toolbar);
            resetPreviewsBtn.clicked    += () => 
            {
                foreach (var lib in _targetLibs)
                {
                    for (int i = 0; i < lib.numPrefabs; ++i)
                        lib.getPrefab(i).resetPreview();
                }    
            };

            var selectPrefabsInScene        = UI.createToolbarButton(TexturePool.instance.hand, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            selectPrefabsInScene.tooltip    = "Select prefabs in scene. Note: Hold down Ctrl to append.";
            selectPrefabsInScene.RegisterCallback<MouseUpEvent>((p) =>
            {
                GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectSelection;
                getVisibleSelectedPrefabAssets(_prefabAssetBuffer);
                ObjectSelection.instance.selectPrefabInstances(_prefabAssetBuffer);
                GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectSelection;

            });
            UI.useDefaultMargins(selectPrefabsInScene);

            var deselectPrefabsInScene      = UI.createToolbarButton(TexturePool.instance.handNo, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            deselectPrefabsInScene.tooltip  = "Deselect prefabs in scene.";
            deselectPrefabsInScene.RegisterCallback<MouseUpEvent>((p) =>
            {
                GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectSelection;
                getVisibleSelectedPrefabAssets(_prefabAssetBuffer);
                ObjectSelection.instance.deselectPrefabInstances(_prefabAssetBuffer);
                GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectSelection;
            });
            UI.useDefaultMargins(deselectPrefabsInScene);

            var activatePrefabInstances         = UI.createToolbarButton(TexturePool.instance.lightBulb, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            activatePrefabInstances.tooltip     = "Activate prefabs in scene.";
            activatePrefabInstances.RegisterCallback<MouseUpEvent>((p) =>
            {
                getVisibleSelectedPrefabAssets(_prefabAssetBuffer);
                PluginScene.instance.setPrefabInstancesActive(_prefabAssetBuffer, true, true);
            });
            UI.useDefaultMargins(activatePrefabInstances);

            var deactivatePrefabInstances       = UI.createToolbarButton(TexturePool.instance.lightBulbGray, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            deactivatePrefabInstances.tooltip   = "Deactivate prefabs in scene.";
            deactivatePrefabInstances.RegisterCallback<MouseUpEvent>((p) =>
            {
                getVisibleSelectedPrefabAssets(_prefabAssetBuffer);
                PluginScene.instance.setPrefabInstancesActive(_prefabAssetBuffer, false, true);
            });
            UI.useDefaultMargins(deactivatePrefabInstances);

            var deletePrefabInstances           = UI.createToolbarButton(TexturePool.instance.delete, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            deletePrefabInstances.tooltip       = "Delete prefabs in scene. Note: Tile rule prefab instances will not be deleted.";
            deletePrefabInstances.RegisterCallback<MouseUpEvent>((p) =>
            {
                getVisibleSelectedPrefabs(_pluginPrefabBuffer);
                PluginScene.instance.deletePrefabInstances(_pluginPrefabBuffer);
            });
            UI.useDefaultMargins(deletePrefabInstances);

            var breakObjectGroupLinks         = UI.createToolbarButton(TexturePool.instance.objectGroupDelete, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            breakObjectGroupLinks.tooltip     = "Break object group links for selected prefabs.";
            breakObjectGroupLinks.RegisterCallback<MouseUpEvent>((p) =>
            {
                getVisibleSelectedPrefabs(_pluginPrefabBuffer);
                foreach (var prefab in _pluginPrefabBuffer)
                    prefab.objectGroup = null;

                UIPluginPrefabItem.getItemIds(_pluginPrefabBuffer, _prefabIdBuffer);
                _prefabView.refreshItemsUI(_prefabIdBuffer);
            });
            UI.useDefaultMargins(breakObjectGroupLinks);

            var applyObjectGroupLinks = UI.createToolbarButton(TexturePool.instance.objectGroupRotated, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            applyObjectGroupLinks.tooltip = "Apply object group links for selected prefabs. Pressing this button will ensure that the instances of the selected prefabs are attached as " + 
                "children of the associated object groups. Prefabs that are not associated with an object group, will be attached to the default group if one is available. Otherwise, " + 
                "they will be made to reside at the scene root.";
            applyObjectGroupLinks.RegisterCallback<MouseUpEvent>((p) =>
            {
                getVisibleSelectedPrefabs(_pluginPrefabBuffer);
                PluginPrefab.applyObjectGroupLinks(_pluginPrefabBuffer);
            });
            UI.useDefaultMargins(applyObjectGroupLinks);

            var generateDecorRulesBtn = UI.createToolbarButton(TexturePool.instance.decor, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(generateDecorRulesBtn);
            generateDecorRulesBtn.tooltip = "Generate decor rules for the currently selected prefabs.";
            generateDecorRulesBtn.clicked += () => 
            {
                getVisibleSelectedPrefabAssets(_prefabAssetBuffer);
                PrefabDecorRuleDb.instance.generateDecorRules(_prefabAssetBuffer, PrefabLibProfileDb.instance.activeProfile);
            };
        }

        private void creteSecondaryTopToolbar()
        {
            Toolbar toolbar             = new Toolbar();
            toolbar.style.flexShrink    = 0.0f;
            contentContainer.Add(toolbar);

            var btn = UI.createSmallToolbarFilterPrefixButton("Prefab filter.", true, toolbar);
            btn.RegisterCallback<MouseUpEvent>(p =>
            {
                if (p.button == (int)MouseButton.LeftMouse)
                {
                    UndoEx.record(this);
                    if (FixedShortcuts.ui_EnableClearAllOnMouseUp(p)) _prefabFilter = PluginPrefabFilter.None;
                    else _prefabFilter = PluginPrefabFilter.All;
                }
            });
            _prefabFilterField = UI.createEnumFlagsField(typeof(TileRuleFilter), "_prefabFilter", serializedObject, "", "Prefab filter.", toolbar);
            _prefabFilterField.RegisterValueChangedCallback(p => { UndoEx.record(this); _prefabFilter = (PluginPrefabFilter)p.newValue; _prefabView.filterItems(filterPrefabViewItem); });

            _prefabSearchField = new EntitySearchField(toolbar,
                (nameList) => { PrefabLib.getPrefabNames(_targetLibs, nameList); },
                (name) => {_prefabView.filterItems(filterPrefabViewItem); });
        }

        private void createBottomToolbar()
        {
            Toolbar toolbar                 = new Toolbar();
            toolbar.style.flexShrink        = 0.0f;
            contentContainer.Add(toolbar);

            _previewScaleSlider             = UI.createSlider("_prefabPreviewScale", serializedObject, string.Empty, "Prefab preview scale [" + prefabPreviewScale + "]", UIValues.minPrefabPreviewScale, UIValues.maxPrefabPreviewScale, toolbar);   
            _previewScaleSlider.style.width = 80.0f;
            _previewScaleSlider.RegisterValueChangedCallback
                ((p) =>
                {
                    _prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
                    _previewScaleSlider.tooltip = "Prefab preview scale [" + prefabPreviewScale + "]";
                });
        }

        private bool filterPrefabViewItem(UIPluginPrefabItemData itemData)
        {
            return filterPrefab(itemData.prefabLib, itemData.prefab);
        }

        private bool filterPrefab(PrefabLib lib, PluginPrefab prefab)
        {
            if (!lib.prefabsVisibleInManagerLocal || !lib.prefabsVisibleInManagerGlobal()) return false;
            if (!_prefabSearchField.matchName(prefab.prefabAsset.name)) return false;

            if (_prefabFilter == PluginPrefabFilter.All) return true;
  
            if (prefab.uiSelected)
            {
                if ((_prefabFilter & PluginPrefabFilter.Selected) == 0) return false;
            }
            else
            {
                if ((_prefabFilter & PluginPrefabFilter.Unselected) == 0) return false;
            }
              
            if (prefab.hasObjectGroup)
            { 
                if ((_prefabFilter & PluginPrefabFilter.ObjectGroup) == 0) return false;
            }
            else
            {
                if ((_prefabFilter & PluginPrefabFilter.NoObjectGroup) == 0) return false;
            }

            return true;
        }

        private void populatePrefabView()
        {
            if (_prefabView == null) return;

            _prefabSearchField.refreshMatchNames();

            _prefabView.onBeginBuild();
            foreach (var lib in _targetLibs)
            {
                lib.getPrefabs(_pluginPrefabBuffer);
                foreach (var prefab in _pluginPrefabBuffer)
                {
                    _prefabView.addItem(new UIPluginPrefabItemData() 
                    { prefab = prefab, prefabLib = lib }, filterPrefab(lib, prefab));
                }                   
            }

            _prefabView.onEndBuild();
            _prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
        }

        private void onSelectedPrefabItemsWillBeDeleted(GridView<UIPluginPrefabItem, UIPluginPrefabItemData> gridView, List<PluginGuid> itemIds)
        {
            UICopyPaste.cancel();

            _pluginPrefabBuffer.Clear();
            foreach (var itemId in itemIds)
                _pluginPrefabBuffer.Add(_prefabView.getItemData(itemId).prefab);

            PluginPrefabEvents.onPrefabsWillBeRemoved(_pluginPrefabBuffer);

            foreach (var lib in _targetLibs)
                lib.deletePrefabs(_pluginPrefabBuffer);

            _numPrefabs = PrefabLib.calcNumPrefabsInLibs(_targetLibs);
            PrefabLibProfileDbUI.instance.refresh();
        }
    }
}
#endif