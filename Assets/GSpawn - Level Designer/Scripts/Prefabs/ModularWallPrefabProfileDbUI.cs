#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ModularWallRuleUI
    {
        [NonSerialized]
        public VisualElement    ui;
        [NonSerialized]
        public GridView<UIModularWallPrefabItem, UIModularWallPrefabItemData> prefabView;
    }

    public class ModularWallPrefabProfileDbUI : PluginUI
    {
        [NonSerialized]
        private ModularWallRuleUI[] _ruleUIs            = new ModularWallRuleUI[Enum.GetValues(typeof(ModularWallRuleId)).Length];
        [SerializeField]
        private GridViewState[]     _prefabViewStates   = new GridViewState[Enum.GetValues(typeof(ModularWallRuleId)).Length];

        [SerializeField]
        private float               _ruleUIContainerWidth       = 300.0f;
        [NonSerialized]
        private float               _prefabPreviewHeight        = 100.0f;
        [SerializeField]
        private float               _prefabPreviewScale         = UIValues.minPrefabPreviewScale;
        [SerializeField]
        private Vector2             _ruleUIScrollOffset         = Vector3.zero;
        [NonSerialized]
        private Slider              _previewScaleSlider;
        [SerializeField]
        private Vector2             _prefabSettingsScrollPos;

        [NonSerialized]
        private TwoPaneSplitView            _splitView;
        [NonSerialized]
        private ScrollView                  _ruleUIContainer;
        [NonSerialized]
        private VisualElement               _prefabSettingsContainer;
        [NonSerialized]
        private Button                      _useDefaultsBtn;
        [NonSerialized]
        private ProfileSelectionUI<ModularWallPrefabProfileDb, ModularWallPrefabProfile> _profileSelectionUI;
        [NonSerialized]
        private List<PluginPrefab>                  _pluginPrefabBuffer             = new List<PluginPrefab>();
        [NonSerialized]
        private List<ModularWallPrefab>             _mdWallPrefabBuffer             = new List<ModularWallPrefab>();
        [NonSerialized]
        private List<UIModularWallPrefabItemData>   _mdWallPrefabItemDataBuffer     = new List<UIModularWallPrefabItemData>();

        public float                                prefabPreviewScale  { get { return _prefabPreviewScale; } set { UndoEx.record(this); _prefabPreviewScale = Mathf.Clamp(value, UIValues.minPrefabPreviewScale, UIValues.maxPrefabPreviewScale); EditorUtility.SetDirty(this); } }
        public static ModularWallPrefabProfileDbUI  instance            { get { return ModularWallPrefabProfileDb.instance.ui; } }

        public void getVisibleSelectedPrefabs(List<ModularWallPrefab> mdWallPrefabs)
        {
            mdWallPrefabs.Clear();
            foreach (var ruleId in ModularWallRuleIdEx.ruleIdArray)
            {
                var ruleUI      = getRuleUI(ruleId);
                if (ruleUI == null) continue;

                var prefabView  = ruleUI.prefabView;
                if (prefabView != null)
                {
                    prefabView.getVisibleSelectedItemData(_mdWallPrefabItemDataBuffer);
                    foreach (var itemData in _mdWallPrefabItemDataBuffer)
                        mdWallPrefabs.Add(itemData.mdWallPrefab);
                }
            }
        }

        private void onActiveProfileChanged(ModularWallPrefabProfile newActiveProfile)
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            createRuleUIs();
            populatePrefabViews();
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow = 1.0f;
            _profileSelectionUI = new ProfileSelectionUI<ModularWallPrefabProfileDb, ModularWallPrefabProfile>();
            _profileSelectionUI.build(ModularWallPrefabProfileDb.instance, "modular wall prefab profile", contentContainer);

            createTopToolbar();

            _splitView                  = new TwoPaneSplitView();
            _splitView.orientation      = TwoPaneSplitViewOrientation.Horizontal;
            contentContainer.Add(_splitView);

            _ruleUIContainer = new ScrollView(ScrollViewMode.Vertical);
            _splitView.Add(_ruleUIContainer);
            _ruleUIContainer.style.flexGrow = 1.0f;
            _ruleUIContainer.verticalScroller.valueChanged += p =>
            {
                _ruleUIScrollOffset = _ruleUIContainer.scrollOffset;
            };
            _ruleUIContainer.scrollOffset = _ruleUIScrollOffset;

            _prefabSettingsContainer = new VisualElement();
            _prefabSettingsContainer.style.flexGrow = 1.0f;
            _splitView.Add(_prefabSettingsContainer);
            _splitView.fixedPaneIndex = _splitView.IndexOf(_ruleUIContainer);
            _splitView.fixedPaneInitialDimension = _ruleUIContainerWidth;

            createRuleUIs();
            populatePrefabViews();
            createPrefabSettingsControls();
            createProfileSettingsControls(contentContainer);
            createBottomToolbar();
        }

        private void createTopToolbar()
        {
            Toolbar toolbar = new Toolbar();
            toolbar.style.flexShrink = 0.0f;
            contentContainer.Add(toolbar);

            var btn         = UI.createSmallResetPrefabPreviewsToolbarButton(toolbar);
            btn.clicked     += () => { ModularWallPrefabProfileDb.instance.activeProfile.resetPrefabPreviews(); };
            UI.useDefaultMargins(btn);

            btn = UI.createToolbarButton(TexturePool.instance.delete, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            btn.tooltip     = "Delete all wall prefabs.";
            btn.clicked     += () => { ModularWallPrefabProfileDb.instance.activeProfile.deleteAllPrefabs(); populatePrefabViews(); };
            UI.useDefaultMargins(btn);
        }

        private void createProfileSettingsControls(VisualElement parent)
        {
            var imGUIParent = new VisualElement();
            parent.Add(imGUIParent);
            imGUIParent.style.flexDirection = FlexDirection.Row;
            imGUIParent.style.minHeight = 130.0f;

            const float labelWidth  = 130.0f;
            var imGUIContainer      = UI.createIMGUIContainer(imGUIParent);
            imGUIContainer.style.flexGrow       = 1.0f;
            imGUIContainer.style.marginTop      = 1.0f;
            imGUIContainer.style.marginLeft     = 0.0f;
            imGUIContainer.style.borderTopColor = Color.black;
            imGUIContainer.style.borderTopWidth = 1.0f;
            imGUIContainer.style.overflow       = Overflow.Hidden;
            imGUIContainer.onGUIHandler         = () =>
            {
                var activeProfile       = ModularWallPrefabProfileDb.instance.activeProfile;

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Profile Settings", GUIStyleDb.instance.uiInfoLabel);

                EditorUIEx.saveLabelWidth();
                EditorGUIUtility.labelWidth = labelWidth;

                var guiContent      = new GUIContent();
                guiContent.text     = "Example prefab";
                guiContent.tooltip  = "An example prefab that contains the necessary wall pieces (straight, inner corner & outer corner) to allow the plugin to detect how the pieces fit together.";

                EditorGUI.BeginChangeCheck();
                var newPrefab       = EditorGUILayout.ObjectField(guiContent, activeProfile.examplePrefab, typeof(GameObject), false) as GameObject;
                if (EditorGUI.EndChangeCheck()) activeProfile.examplePrefab = newPrefab;

                EditorGUILayout.BeginHorizontal();
                guiContent.text             = "Up axis";
                guiContent.tooltip          = "The wall prefab up axis. Must be the same for all prefabs.";
                EditorGUI.BeginChangeCheck();
                var newAxis                 = (ModularWallAxis)EditorGUILayout.EnumPopup(guiContent, activeProfile.wallUpAxis);
                if (EditorGUI.EndChangeCheck()) activeProfile.wallUpAxis = newAxis;
                EditorGUILayout.LabelField("[Size: " + activeProfile.wallHeight.ToString("F3") + "]");
                EditorGUILayout.EndHorizontal();

                guiContent.text             = "Truncate forward size";
                guiContent.tooltip          = "Check this if the wall pieces contain small bumps at the end points where they connect to other pieces. If you spawn walls " + 
                "and see gaps between the wall pieces, this is an indication that you should check this toggle.";
                EditorGUI.BeginChangeCheck();
                var newTruncate             = EditorGUILayout.Toggle(guiContent, activeProfile.truncateForwardSize);
                if (EditorGUI.EndChangeCheck()) activeProfile.truncateForwardSize = newTruncate;

                guiContent.text             = "Spawn pillars";
                guiContent.tooltip          = "If checked, pillars will be spawned by picking prefabs from the specified random prefab pool.";
                EditorGUI.BeginChangeCheck();
                var newSpawnPillars         = EditorGUILayout.Toggle(guiContent, activeProfile.spawnPillars);
                if (EditorGUI.EndChangeCheck()) activeProfile.spawnPillars = newSpawnPillars;

                if (activeProfile.spawnPillars)
                {
                    string newProfileName = EditorUIEx.profileNameSelectionField<RandomPrefabProfileDb, RandomPrefabProfile>(RandomPrefabProfileDb.instance, "Pillar prefab profile", labelWidth, activeProfile.pillarProfileName);
                    if (newProfileName != activeProfile.pillarProfileName)
                    {
                        activeProfile.pillarProfileName = newProfileName;
                    }
                }

                EditorUIEx.restoreLabelWidth();

                guiContent.text = "Refresh";
                guiContent.tooltip = "Refresh prefab data. Useful when you make changes to the example prefab.";
                if (GUILayout.Button(guiContent, GUILayout.Width(100.0f)))
                {
                    UndoEx.saveEnabledState();
                    UndoEx.enabled = false;
                    activeProfile.refreshPrefabData();
                    UndoEx.restoreEnabledState();
                }
            };
        }

        private void createBottomToolbar()
        {
            Toolbar toolbar             = new Toolbar();
            toolbar.style.flexShrink    = 0.0f;
            contentContainer.Add(toolbar);

            _previewScaleSlider = UI.createSlider("_prefabPreviewScale", serializedObject, string.Empty, "Prefab preview scale [" + prefabPreviewScale + "]", UIValues.minPrefabPreviewScale, UIValues.maxPrefabPreviewScale, toolbar);
            _previewScaleSlider.style.width = 80.0f;
            _previewScaleSlider.RegisterValueChangedCallback
                ((p) =>
                {
                    var activeProfile = ModularWallPrefabProfileDb.instance.activeProfile;
                    foreach (var ruleUI in _ruleUIs)
                    {
                        if (ruleUI != null && ruleUI.prefabView != null)
                        {
                            ruleUI.prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
                        }
                    }

                    _previewScaleSlider.tooltip = "Prefab preview scale [" + prefabPreviewScale + "]";
                });
        }

        private void createRuleUIs()
        {
            // Note: Can happen during Undo/ Redo if the UI is not visible (i.e.window is not open).
            if (_ruleUIContainer == null) return;

            // Note: Always start with a clean slate (e.g. when calling from onUndoRedo).
            _ruleUIContainer.Clear();

            var ruleIds = Enum.GetValues(typeof(ModularWallRuleId));
            foreach (var item in ruleIds) 
            {
                var ruleId = (ModularWallRuleId)item;
                createRuleUI(ruleId);
            }
        }

        private void createRuleUI(ModularWallRuleId ruleId)
        {
            float ruleTextureSize = TexturePool.getModularWallRuleTextureSize();

            _ruleUIs[(int)ruleId]       = new ModularWallRuleUI();
            VisualElement ui            = new VisualElement();
            _ruleUIs[(int)ruleId].ui    = ui;
            _ruleUIContainer.Add(ui);

            ui.style.marginLeft             = 2.0f;
            ui.style.marginTop              = 2.0f;
            ui.style.setBorderWidth(1.0f);
            ui.style.setBorderColor(Color.black);
            ui.style.borderRightWidth       = 0.0f;
            ui.style.flexDirection          = FlexDirection.Row;
       
            VisualElement leftColumn        = new VisualElement();
            ui.Add(leftColumn);
            leftColumn.style.flexDirection      = FlexDirection.Column;
            leftColumn.style.flexShrink         = 0.0f;
            leftColumn.style.marginLeft         = 2.0f;
            leftColumn.style.marginTop          = 2.0f;
            leftColumn.style.marginBottom       = 2.0f;
            leftColumn.style.maxWidth           = ruleTextureSize + 20.0f;
            leftColumn.style.minWidth           = leftColumn.style.maxWidth;
            leftColumn.style.maxHeight          = _prefabPreviewHeight + 25.0f;
            leftColumn.style.minHeight          = _prefabPreviewHeight;

            VisualElement ruleIcon          = new VisualElement();
            leftColumn.Add(ruleIcon);
            ruleIcon.style.backgroundImage  = TexturePool.instance.getModularWallRuleTexture(ruleId);
            ruleIcon.style.width            = ruleTextureSize;
            ruleIcon.style.height           = ruleTextureSize;
            ruleIcon.style.alignSelf        = Align.Center;

            UI.createRowSeparator(leftColumn);

            Label ruleName                  = new Label();
            leftColumn.Add(ruleName);
            ruleName.style.unityFontStyleAndWeight  = FontStyle.Bold;
            ruleName.text                           = ruleId.ToString();
            ruleName.style.alignSelf                = Align.Center;
            ruleName.style.overflow                 = Overflow.Hidden;

            var ruleToolbar         = new Toolbar();
            leftColumn.Add(ruleToolbar);
            var loadPrefabsBtn      = UI.createSmallLoadPrefabsButton(ruleToolbar);

            VisualElement rightColumn           = new VisualElement();
            ui.Add(rightColumn);
            rightColumn.style.flexDirection     = FlexDirection.Column;
            rightColumn.style.flexGrow          = 1.0f;

            // Create prefab view
            _ruleUIs[(int)ruleId].prefabView = new GridView<UIModularWallPrefabItem, UIModularWallPrefabItemData>(getRulePrefabViewState(ruleId), rightColumn);
            var prefabView = _ruleUIs[(int)ruleId].prefabView;
            loadPrefabsBtn.clicked += () => { createWallPrefabsFromPrefabsInManager(ruleId); };

            prefabView.canDelete                = true;
            prefabView.style.marginLeft         = 2.0f;
            prefabView.style.marginTop          = -1.0f;
            prefabView.style.borderTopWidth     = 1.0f;
            prefabView.style.flexGrow           = 1.0f;
            prefabView.style.borderLeftColor    = Color.black;
            prefabView.style.borderLeftWidth    = 1.0f;
            prefabView.style.height             = _prefabPreviewHeight;
            prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));

            prefabView.selectedItemsWillBeDeleted   += onSelectedPrefabItemsWillBeDeleted;
            prefabView.selectionChanged             += onPrefabSelectionChanged;

            prefabView.RegisterCallback<DragPerformEvent>(p =>
            {
                if (PluginDragAndDrop.initiatedByPlugin)
                {
                    if (PluginPrefabManagerUI.instance.dragAndDropInitiatedByPrefabView())
                    {
                        createWallPrefabsFromPrefabsInManager(ruleId);
                        PluginDragAndDrop.endDrag();
                    }
                }
            });

            prefabView.RegisterCallback<MouseDownEvent>(p =>
            {
                if (p.button == (int)MouseButton.RightMouse)
                {
                    var visSelectedPrefabs  = new List<ModularWallPrefab>();
                    getVisibleSelectedPrefabs(visSelectedPrefabs);

                    PluginGenericMenu menu  = new PluginGenericMenu();
                    menu.addItem(GenericMenuItemCategory.VisiblePrefabs, GenericMenuItemId.HighlightSelectedInManager, visSelectedPrefabs.Count != 0,
                        () =>
                        {
                            ModularWallPrefab.getPluginPrefabs(visSelectedPrefabs, _pluginPrefabBuffer);
                            PluginPrefabManagerUI.instance.selectPluginPrefabsAndMakeVisible(_pluginPrefabBuffer, true);
                        });
                    menu.showAsContext();
                }
            });
        }

        private void createWallPrefabsFromPrefabsInManager(ModularWallRuleId ruleId)
        {
            var prefabView      = _ruleUIs[(int)ruleId].prefabView;
            var activeProfile   = ModularWallPrefabProfileDb.instance.activeProfile;
            PluginPrefabManagerUI.instance.getVisibleSelectedPrefabs(_pluginPrefabBuffer);
            activeProfile.createPrefabs(_pluginPrefabBuffer, ruleId, _mdWallPrefabBuffer, false, "Creating Modular Wall Prefabs");
            foreach (var mdWallPrefab in _mdWallPrefabBuffer)
                prefabView.addItem(new UIModularWallPrefabItemData()
                { mdWallPrefab = mdWallPrefab, mdWallPrefabProfile = activeProfile }, true);
        }

        private void populatePrefabViews()
        {
            var ruleIds = ModularWallRuleIdEx.ruleIdArray;
            foreach (var ruleId in ruleIds) populatePrefabView(ruleId);
        }

        private void createPrefabSettingsControls()
        {
            const float labelWidth          = 130.0f;
            IMGUIContainer imGUIContainer   = UI.createIMGUIContainer(_prefabSettingsContainer);
            imGUIContainer.style.flexGrow   = 1.0f;
            imGUIContainer.style.marginTop  = 3.0f;
            imGUIContainer.style.marginLeft = 3.0f;
            imGUIContainer.onGUIHandler = () =>
            {
                getVisibleSelectedPrefabs(_mdWallPrefabBuffer);
                _useDefaultsBtn.setDisplayVisible(_mdWallPrefabBuffer.Count != 0);

                if (_mdWallPrefabBuffer.Count == 0)
                {
                    EditorGUILayout.HelpBox("No prefabs selected. Select prefabs in order to change their settings.", MessageType.Info);
                    return;
                }
                else
                {
                    _prefabSettingsScrollPos = EditorGUILayout.BeginScrollView(_prefabSettingsScrollPos);
                    var guiContent = new GUIContent();

                    EditorUIEx.saveLabelWidth();
                    EditorUIEx.saveShowMixedValue();
                    EditorGUIUtility.labelWidth = labelWidth;
                    var diff = ModularWallPrefab.checkDiff(_mdWallPrefabBuffer);

                    #pragma warning disable 0612
                    // Used
                    bool used                   = _mdWallPrefabBuffer[0].used;
                    EditorGUI.showMixedValue    = diff.used;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text             = "Used";
                    guiContent.tooltip          = "If checked, the prefab will be used when creating walls. Otherwise, it will be ignored.";
                    bool newUsed                = EditorGUILayout.Toggle(guiContent, used, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var mdWallPrefab in _mdWallPrefabBuffer)
                            mdWallPrefab.used = newUsed;

                        ModularWallPrefabProfileDb.instance.activeProfile.onPrefabsUsedStateChanged();

                        foreach (var ruleUI in _ruleUIs)
                            ruleUI.prefabView.refreshUI();
                    }

                    // Spawn chance
                    float spawnChance           = _mdWallPrefabBuffer[0].spawnChance;
                    EditorGUI.showMixedValue    = diff.spawnChance;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text             = "Spawn chance";
                    guiContent.tooltip          = "The prefab's chance to be spawned while creating walls.";
                    float newSpawnChance        = EditorGUILayout.FloatField(guiContent, spawnChance, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var mdWallPrefab in _mdWallPrefabBuffer)
                            mdWallPrefab.spawnChance = newSpawnChance;

                        ModularWallPrefabProfileDb.instance.activeProfile.onPrefabsSpawnChanceChanged();
                    }

                    #pragma warning restore 0612
                    EditorUIEx.restoreShowMixedValue();
                    EditorUIEx.restoreLabelWidth();

                    EditorGUILayout.EndScrollView();
                }
            };

            _useDefaultsBtn = UI.createUseDefaultsButton(() =>
            {
                getVisibleSelectedPrefabs(_mdWallPrefabBuffer);
                foreach (var prefab in _mdWallPrefabBuffer)
                    prefab.useDefaults();

                ModularWallPrefabProfileDb.instance.activeProfile.onPrefabsUseDefaults();

                foreach (var ruleUI in _ruleUIs)
                    ruleUI.prefabView.refreshUI();

            }, _prefabSettingsContainer);
        }

        private void populatePrefabView(ModularWallRuleId ruleId)
        {
            var activeProfile   = ModularWallPrefabProfileDb.instance.activeProfile;
            var prefabView      = getRuleUI(ruleId).prefabView;
            if (prefabView == null) return;

            prefabView.onBeginBuild();
            activeProfile.getPrefabs(ruleId, _mdWallPrefabBuffer);
            foreach (var mdWallPrefab in _mdWallPrefabBuffer)
            {
                prefabView.addItem(new UIModularWallPrefabItemData() { mdWallPrefab = mdWallPrefab, mdWallPrefabProfile = activeProfile }, true);
            }

            prefabView.onEndBuild();
            prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
        }

        private void onEditorUpdate()
        {
            if (uiVisibleAndReady)
            {
                float w = _ruleUIContainer.style.width.value.value;
                if (w != _ruleUIContainerWidth)
                {
                    _ruleUIContainerWidth = w;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        private ModularWallRuleUI getRuleUI(ModularWallRuleId ruleId)
        {
            return _ruleUIs[(int)ruleId];
        }

        private GridViewState getRulePrefabViewState(ModularWallRuleId ruleId)
        {
            return _prefabViewStates[(int)ruleId];
        }

        private void onSelectedPrefabItemsWillBeDeleted(GridView<UIModularWallPrefabItem, UIModularWallPrefabItemData> gridView, List<PluginGuid> itemIds)
        {
            _mdWallPrefabBuffer.Clear();
            foreach (var itemId in itemIds)
                _mdWallPrefabBuffer.Add(gridView.getItemData(itemId).mdWallPrefab);

            ModularWallPrefabProfileDb.instance.activeProfile.deletePrefabs(_mdWallPrefabBuffer);
        }

        private void onPrefabSelectionChanged(GridView<UIModularWallPrefabItem, UIModularWallPrefabItemData> gridView)
        {
            if (!Event.current.control)
            {
                foreach (var ruleUI in _ruleUIs)
                {
                    if (ruleUI.prefabView != null && ruleUI.prefabView != gridView)
                    {
                        ruleUI.prefabView.setAllItemsSelected(false, false, false);
                    }
                }
            }
        }

        protected override void onRefresh()
        {
            createRuleUIs();
            populatePrefabViews();
        }

        protected override void onUndoRedo()
        {
            // Note: The UI may not be visible (i.e. window is not open).
            if (!uiVisibleAndReady) return;

            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            createRuleUIs();
            populatePrefabViews();

            _ruleUIContainer.scrollOffset = _ruleUIScrollOffset;
        }

        protected override void onEnabled()
        {
            EditorApplication.update += onEditorUpdate;
            ModularWallPrefabProfileDb.instance.activeProfileChanged += onActiveProfileChanged;

            if (_prefabViewStates[0] == null)
            {
                var ruleIds = Enum.GetValues(typeof(ModularWallRuleId));
                foreach (var item in ruleIds )
                {
                    var ruleId = (ModularWallRuleId)item;
                    _prefabViewStates[(int)ruleId] = ScriptableObject.CreateInstance<GridViewState>();
                    _prefabViewStates[(int)ruleId].name = GetType().Name + ruleId.ToString() + "_PrefabViewState";
                    AssetDbEx.addObjectToAsset(_prefabViewStates[(int)ruleId], ModularWallPrefabProfileDb.instance);
                }
            }
        }

        protected override void onDisabled()
        {
            EditorApplication.update -= onEditorUpdate;
            ModularWallPrefabProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
        }
    }
}
#endif