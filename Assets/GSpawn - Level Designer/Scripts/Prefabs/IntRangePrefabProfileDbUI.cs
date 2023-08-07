#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class IntRangePrefabProfileDbUI : PluginUI
    {
        [SerializeField]
        private float                           _prefabPreviewScale = UIValues.defaultPrefabPreviewScale;
        private Slider                          _previewScaleSlider;
        private Button                          _useDefaultsBtn;

        private EntitySearchField                                                   _prefabSearchField;
        private ProfileSelectionUI<IntRangePrefabProfileDb, IntRangePrefabProfile>  _profileSelectionUI;

        [SerializeField]
        private GridViewState                                                   _prefabViewState;
        [NonSerialized]
        private GridView<UIIntRangePrefabItem, UIIntRangePrefabItemData>        _prefabView;

        [NonSerialized]
        private List<IntRangePrefab>            _intRangePrefabBuffer           = new List<IntRangePrefab>();
        [NonSerialized]
        private List<PluginPrefab>              _pluginPrefabBuffer             = new List<PluginPrefab>();
        [NonSerialized]
        private List<UIIntRangePrefabItemData>  _intRangePrefabItemDataBuffer   = new List<UIIntRangePrefabItemData>();

        public float                            prefabPreviewScale              { get { return _prefabPreviewScale; } set { UndoEx.record(this); _prefabPreviewScale = Mathf.Clamp(value, UIValues.minPrefabPreviewScale, UIValues.maxPrefabPreviewScale); EditorUtility.SetDirty(this); } }

        public static IntRangePrefabProfileDbUI instance                        { get { return IntRangePrefabProfileDb.instance.ui; } }

        public void getVisibleSelectedPrefabs(List<IntRangePrefab> intRangePrefabs)
        {
            intRangePrefabs.Clear();
            if (_prefabView != null)
            {
                _prefabView.getVisibleSelectedItemData(_intRangePrefabItemDataBuffer);
                foreach (var itemData in _intRangePrefabItemDataBuffer)
                    intRangePrefabs.Add(itemData.intRangePrefab);
            }
        }

        public void onIntRangePrefabNeedsUIRefresh(IntRangePrefab irPrefab)
        {
            if (_prefabView != null)
                _prefabView.refreshItemUI(UIIntRangePrefabItem.getItemId(irPrefab));
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            if (_prefabView != null) _prefabView.deleteItems(itemData => itemData.intRangePrefab.prefabAsset == prefabAsset);
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow = 1.0f;

            _profileSelectionUI = new ProfileSelectionUI<IntRangePrefabProfileDb, IntRangePrefabProfile>();
            _profileSelectionUI.build(IntRangePrefabProfileDb.instance, "integer range prefab profile", contentContainer);

            contentContainer.RegisterCallback<MouseDownEvent>(p =>
            {
                if (p.button == (int)MouseButton.RightMouse)
                {
                    getVisibleSelectedPrefabs(_intRangePrefabBuffer);

                    PluginGenericMenu menu = new PluginGenericMenu();
                    menu.addItem(GenericMenuItemCategory.VisiblePrefabs, GenericMenuItemId.HighlightSelectedInManager, _intRangePrefabBuffer.Count != 0,
                    () =>
                    {
                        IntRangePrefab.getPluginPrefabs(_intRangePrefabBuffer, _pluginPrefabBuffer);
                        PluginPrefabManagerUI.instance.selectPluginPrefabsAndMakeVisible(_pluginPrefabBuffer, true);
                    });

                    menu.showAsContext();
                }
            });

            createTopToolbar();
            createSearchToolbar();
            createPrefabView();
            createPrefabSettingsControls();
            createBottomToolbar();
            populatePrefabView();
        }

        protected override void onRefresh()
        {
            populatePrefabView();
        }

        protected override void onEnabled()
        {
            IntRangePrefabProfileDb.instance.activeProfileChanged += onActiveProfileChanged;;

            if (_prefabViewState == null)
            {
                _prefabViewState = ScriptableObject.CreateInstance<GridViewState>();
                _prefabViewState.name = GetType().Name + "_PrefabViewState";
                AssetDbEx.addObjectToAsset(_prefabViewState, IntRangePrefabProfileDb.instance);
            }
        }

        protected override void onDisabled()
        {
            IntRangePrefabProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_prefabViewState);
        }

        protected override void onUndoRedo()
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            populatePrefabView();
        }

        private void createTopToolbar()
        {
            Toolbar toolbar             = new Toolbar();
            toolbar.style.flexShrink    = 0.0f;
            contentContainer.Add(toolbar);

            var resetPreviewsBtn        = UI.createSmallResetPrefabPreviewsToolbarButton(toolbar);
            resetPreviewsBtn.clicked    += () => { IntRangePrefabProfileDb.instance.activeProfile.resetPrefabPreviews(); };

            var loadPrefabsBtn          = UI.createSmallLoadPrefabsButton(toolbar);
            loadPrefabsBtn.clicked      += () => { createIntRangePrefabsFromPrefabsInManager(); };
        }

        private void createSearchToolbar()
        {
            var searchToolbar = new Toolbar();
            searchToolbar.style.flexShrink = 0.0f;
            contentContainer.Add(searchToolbar);

            _prefabSearchField = new EntitySearchField(searchToolbar,
                (nameList) => { IntRangePrefabProfileDb.instance.activeProfile.getPrefabNames(nameList); },
                (name) => { _prefabView.filterItems(filterPrefabViewItem); });
        }

        private bool filterPrefabViewItem(UIIntRangePrefabItemData itemData)
        {
            return filterPrefab(itemData.intRangePrefabProfile, itemData.intRangePrefab);
        }

        private bool filterPrefab(IntRangePrefabProfile irPrefabProfile, IntRangePrefab irPrefab)
        {
            if (!_prefabSearchField.matchName(irPrefab.prefabAsset.name)) return false;
            return true;
        }

        private void onActiveProfileChanged(IntRangePrefabProfile newActiveProfile)
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            populatePrefabView();
        }

        private void createPrefabView()
        {
            _prefabView             = new GridView<UIIntRangePrefabItem, UIIntRangePrefabItemData>(_prefabViewState, contentContainer);
            _prefabView.selectedItemsWillBeDeleted += onSelectedPrefabItemsWillBeDeleted;
            _prefabView.canDelete   = true;

            _prefabView.style.setBorderWidth(1.0f);
            _prefabView.style.setBorderColor(Color.black);
            _prefabView.style.setMargins(UIValues.wndMargin);
            _prefabView.style.marginTop = 3.0f;

            _prefabView.RegisterCallback<DragPerformEvent>(p =>
            {
                if (PluginDragAndDrop.initiatedByPlugin)
                {
                    if (PluginPrefabManagerUI.instance.dragAndDropInitiatedByPrefabView())
                    {
                        createIntRangePrefabsFromPrefabsInManager();
                        PluginDragAndDrop.endDrag();
                    }
                }
            });
        }

        private void createIntRangePrefabsFromPrefabsInManager()
        {
            var activeProfile = IntRangePrefabProfileDb.instance.activeProfile;
            if (activeProfile != null)
            {
                PluginPrefabManagerUI.instance.getVisibleSelectedPrefabs(_pluginPrefabBuffer);
                activeProfile.createPrefabs(_pluginPrefabBuffer, _intRangePrefabBuffer, false, "Creating Integer Range Prefabs");
                foreach (var irPrefab in _intRangePrefabBuffer)
                    _prefabView.addItem(new UIIntRangePrefabItemData() { intRangePrefab = irPrefab, intRangePrefabProfile = activeProfile }, true);
            }
        }

        private void createPrefabSettingsControls()
        {
            const float labelWidth          = 130.0f;
            IMGUIContainer imGUIContainer   = UI.createIMGUIContainer(contentContainer);
            imGUIContainer.style.flexShrink = 0.0f;
            imGUIContainer.style.marginLeft = 3.0f;
            imGUIContainer.onGUIHandler     = () =>
            {
                getVisibleSelectedPrefabs(_intRangePrefabBuffer);
                _useDefaultsBtn.setDisplayVisible(_intRangePrefabBuffer.Count != 0);

                if (_intRangePrefabBuffer.Count == 0)
                {
                    EditorGUILayout.HelpBox("No prefabs selected. Select prefabs in order to change their settings.", MessageType.Info);
                    return;
                }
                else
                {
                    var guiContent = new GUIContent();

                    EditorUIEx.saveLabelWidth();
                    EditorUIEx.saveShowMixedValue();

                    EditorGUIUtility.labelWidth = labelWidth;
                    var diff = IntRangePrefab.checkDiff(_intRangePrefabBuffer);

                    // Used
                    bool used = _intRangePrefabBuffer[0].used;
                    EditorGUI.showMixedValue = diff.used;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Used";
                    guiContent.tooltip = "If checked, the prefab will be taken into account when picking prefabs.";
                    bool newUsed = EditorGUILayout.Toggle(guiContent, used, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var irPrefab in _intRangePrefabBuffer)
                            irPrefab.used = newUsed;

                        foreach (var selectedItemId in _prefabViewState.selectedItems)
                            _prefabView.refreshItemUI(selectedItemId);
                    }

                    // Min
                    int min = _intRangePrefabBuffer[0].min;
                    EditorGUI.showMixedValue = diff.min;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Min";
                    guiContent.tooltip = "The minimum value.";
                    int newMin = EditorGUILayout.IntField(guiContent, min, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var intRangePrefab in _intRangePrefabBuffer)
                            intRangePrefab.min = newMin;
                    }

                    // Max
                    int max = _intRangePrefabBuffer[0].max;
                    EditorGUI.showMixedValue = diff.max;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Max";
                    guiContent.tooltip = "The maximum value.";
                    int newMax = EditorGUILayout.IntField(guiContent, max, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var intRangePrefab in _intRangePrefabBuffer)
                            intRangePrefab.max = newMax;
                    }

                    EditorUIEx.restoreShowMixedValue();
                    EditorUIEx.restoreLabelWidth();

                    // Note: Leave some space between the settings control the and use defaults button.
                    EditorGUILayout.Separator();
                }
            };

            _useDefaultsBtn = UI.createUseDefaultsButton(() =>
            {
                getVisibleSelectedPrefabs(_intRangePrefabBuffer);
                foreach (var intRangePrefab in _intRangePrefabBuffer)
                    intRangePrefab.useDefaults();

                _prefabView.refreshUI();

            }, contentContainer);
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

        private void populatePrefabView()
        {
            if (_prefabView == null) return;

            _prefabSearchField.refreshMatchNames();

            _prefabView.onBeginBuild();
            var activeProfile = IntRangePrefabProfileDb.instance.activeProfile;
            if (activeProfile != null)
            {
                activeProfile.getPrefabs(_intRangePrefabBuffer);
                foreach (var irPrefab in _intRangePrefabBuffer)
                {
                    _prefabView.addItem(new UIIntRangePrefabItemData() { intRangePrefab = irPrefab, intRangePrefabProfile = activeProfile }, true);
                }
            }
            _prefabView.onEndBuild();
            _prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
        }

        private void onSelectedPrefabItemsWillBeDeleted(GridView<UIIntRangePrefabItem, UIIntRangePrefabItemData> gridView, List<PluginGuid> itemIds)
        {
            _intRangePrefabBuffer.Clear();
            foreach (var itemId in itemIds)
                _intRangePrefabBuffer.Add(_prefabView.getItemData(itemId).intRangePrefab);

            IntRangePrefabProfileDb.instance.activeProfile.deletePrefabs(_intRangePrefabBuffer);
        }
    }
}
#endif