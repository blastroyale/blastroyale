#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class RandomPrefabProfileDbUI : PluginUI
    {
        [SerializeField]
        private float                           _prefabPreviewScale             = UIValues.defaultPrefabPreviewScale;
        private Slider                          _previewScaleSlider;
        private Button                          _useDefaultsBtn;

        private EntitySearchField                                               _prefabSearchField;
        private ProfileSelectionUI<RandomPrefabProfileDb, RandomPrefabProfile>  _profileSelectionUI;

        [SerializeField]
        private GridViewState                   _prefabViewState;

        [NonSerialized]
        private GridView<UIRandomPrefabItem, UIRandomPrefabItemData>    _prefabView;
        [NonSerialized]
        private List<RandomPrefab>                                      _randomPrefabBuffer             = new List<RandomPrefab>();
        [NonSerialized]
        private List<PluginPrefab>                                      _pluginPrefabBuffer             = new List<PluginPrefab>();
        [NonSerialized]
        private List<UIRandomPrefabItemData>                            _randomPrefabItemDataBuffer     = new List<UIRandomPrefabItemData>();

        public float                            prefabPreviewScale  { get { return _prefabPreviewScale; } set { UndoEx.record(this); _prefabPreviewScale = Mathf.Clamp(value, UIValues.minPrefabPreviewScale, UIValues.maxPrefabPreviewScale); EditorUtility.SetDirty(this); } }

        public static RandomPrefabProfileDbUI   instance            { get { return RandomPrefabProfileDb.instance.ui; } }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            if (_prefabView != null) _prefabView.deleteItems(itemData => itemData.randomPrefab.prefabAsset == prefabAsset);
        }

        public void getVisibleSelectedPrefabs(List<RandomPrefab> randomPrefabs)
        {
            randomPrefabs.Clear();
            if (_prefabView != null)
            {
                _prefabView.getVisibleSelectedItemData(_randomPrefabItemDataBuffer);
                foreach (var itemData in _randomPrefabItemDataBuffer)
                    randomPrefabs.Add(itemData.randomPrefab);
            }
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow             = 1.0f;

            _profileSelectionUI = new ProfileSelectionUI<RandomPrefabProfileDb, RandomPrefabProfile>();
            _profileSelectionUI.build(RandomPrefabProfileDb.instance, "random prefab profile", contentContainer);

            contentContainer.RegisterCallback<MouseDownEvent>(p => 
            {
                if (p.button == (int)MouseButton.RightMouse)
                {
                    getVisibleSelectedPrefabs(_randomPrefabBuffer);

                    PluginGenericMenu menu = new PluginGenericMenu();
                    menu.addItem(GenericMenuItemCategory.VisiblePrefabs, GenericMenuItemId.HighlightSelectedInManager, _randomPrefabBuffer.Count != 0,
                    () =>
                    {
                        RandomPrefab.getPluginPrefabs(_randomPrefabBuffer, _pluginPrefabBuffer);
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
            RandomPrefabProfileDb.instance.activeProfileChanged += onActiveProfileChanged;

            if (_prefabViewState == null)
            {
                _prefabViewState = ScriptableObject.CreateInstance<GridViewState>();
                _prefabViewState.name = GetType().Name + "_PrefabViewState";
                AssetDbEx.addObjectToAsset(_prefabViewState, RandomPrefabProfileDb.instance);
            }
        }

        protected override void onDisabled()
        {
            RandomPrefabProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
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
            resetPreviewsBtn.clicked    += () => { RandomPrefabProfileDb.instance.activeProfile.resetPrefabPreviews(); };

            var loadPrefabsBtn          = UI.createSmallLoadPrefabsButton(toolbar);
            loadPrefabsBtn.clicked      += () => { createRandomPrefabsFromPrefabsInManager(); };
        }

        private void createSearchToolbar()
        {
            var searchToolbar               = new Toolbar();
            searchToolbar.style.flexShrink  = 0.0f;
            contentContainer.Add(searchToolbar);

            _prefabSearchField = new EntitySearchField(searchToolbar,
                (nameList) => { RandomPrefabProfileDb.instance.activeProfile.getPrefabNames(nameList); },
                (name) => { _prefabView.filterItems(filterPrefabViewItem); });
        }

        private bool filterPrefabViewItem(UIRandomPrefabItemData itemData)
        {
            return filterPrefab(itemData.randomPrefabProfile, itemData.randomPrefab);
        }

        private bool filterPrefab(RandomPrefabProfile randomPrefabProfile, RandomPrefab randomPrefab)
        {
            if (!_prefabSearchField.matchName(randomPrefab.prefabAsset.name)) return false;
            return true;
        }

        private void onActiveProfileChanged(RandomPrefabProfile newActiveProfile)
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            populatePrefabView();
        }

        private void createPrefabView()
        {
            _prefabView                             = new GridView<UIRandomPrefabItem, UIRandomPrefabItemData>(_prefabViewState, contentContainer);
            _prefabView.selectedItemsWillBeDeleted  += onSelectedPrefabItemsWillBeDeleted;
            _prefabView.canDelete                   = true;

            // _prefabView.style.height = 300.0f;
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
                        createRandomPrefabsFromPrefabsInManager();
                        PluginDragAndDrop.endDrag();
                    }
                }
            });
        }

        private void createRandomPrefabsFromPrefabsInManager()
        {
            var activeProfile = RandomPrefabProfileDb.instance.activeProfile;
            if (activeProfile != null)
            {
                PluginPrefabManagerUI.instance.getVisibleSelectedPrefabs(_pluginPrefabBuffer);
                activeProfile.createPrefabs(_pluginPrefabBuffer, _randomPrefabBuffer, false, "Creating Random Prefabs");
                foreach (var randomPrefab in _randomPrefabBuffer)
                    _prefabView.addItem(new UIRandomPrefabItemData() { randomPrefab = randomPrefab, randomPrefabProfile = activeProfile }, true);
            }
        }

        private void createPrefabSettingsControls()
        {
            const float labelWidth              = 130.0f;
            IMGUIContainer imGUIContainer       = UI.createIMGUIContainer(contentContainer);
            imGUIContainer.style.flexShrink     = 0.0f;
            imGUIContainer.style.marginLeft     = 3.0f;
            imGUIContainer.onGUIHandler         = () =>
            {
                getVisibleSelectedPrefabs(_randomPrefabBuffer);
                _useDefaultsBtn.setDisplayVisible(_randomPrefabBuffer.Count != 0);

                if (_randomPrefabBuffer.Count == 0)
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
                    var diff = RandomPrefab.checkDiff(_randomPrefabBuffer);

                    // Used
                    bool used = _randomPrefabBuffer[0].used;
                    EditorGUI.showMixedValue = diff.used;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Used";
                    guiContent.tooltip = "If checked, the prefab will be taken into account when randomly picking prefabs.";
                    bool newUsed = EditorGUILayout.Toggle(guiContent, used, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var randomPrefab in _randomPrefabBuffer)
                            randomPrefab.used = newUsed;

                        RandomPrefabProfileDb.instance.activeProfile.onPrefabsUsedStateChanged();

                        foreach (var selectedItemId in _prefabViewState.selectedItems)
                            _prefabView.refreshItemUI(selectedItemId);
                    }

                    // Probability
                    float probability = _randomPrefabBuffer[0].probability;
                    EditorGUI.showMixedValue = diff.probability;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Probability";
                    guiContent.tooltip = "The prefab's chance to be picked.";
                    float newProbability = EditorGUILayout.FloatField(guiContent, probability, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var randomPrefab in _randomPrefabBuffer)
                            randomPrefab.probability = newProbability;

                        RandomPrefabProfileDb.instance.activeProfile.onPrefabsProbabilityChanged();
                    }

                    EditorUIEx.restoreShowMixedValue();
                    EditorUIEx.restoreLabelWidth();

                    // Note: Leave some space between the settings control the and use defaults button.
                    EditorGUILayout.Separator();
                }
            };

            _useDefaultsBtn = UI.createUseDefaultsButton(() =>
            {
                getVisibleSelectedPrefabs(_randomPrefabBuffer);
                foreach (var randomPrefab in _randomPrefabBuffer)
                    randomPrefab.useDefaults();

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
            var activeProfile = RandomPrefabProfileDb.instance.activeProfile;
            if (activeProfile != null)
            {
                activeProfile.getPrefabs(_randomPrefabBuffer);
                foreach(var randomPrefab in _randomPrefabBuffer)
                {
                    _prefabView.addItem(new UIRandomPrefabItemData() { randomPrefab = randomPrefab, randomPrefabProfile = activeProfile }, true);
                }
            }
            _prefabView.onEndBuild();
            _prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
        }

        private void onSelectedPrefabItemsWillBeDeleted(GridView<UIRandomPrefabItem, UIRandomPrefabItemData> gridView, List<PluginGuid> itemIds)
        {
            _randomPrefabBuffer.Clear();
            foreach (var itemId in itemIds)
                _randomPrefabBuffer.Add(_prefabView.getItemData(itemId).randomPrefab);

            RandomPrefabProfileDb.instance.activeProfile.deletePrefabs(_randomPrefabBuffer);
        }
    }
}
#endif