#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class TileRuleProfileDbUI : PluginUI
    {
        private static Color        _bitColor_Mid               = ColorEx.create(255, 152, 73, 255); 
        private static Color        _bitColor_Unimportant       = ColorEx.createNewAlpha(Color.black, 0.0f);
        private static Color        _bitColor_RequiredOn        = new Color32(0, 127, 14, 255);
        private static Color        _bitColor_RequiredOff       = new Color32(190, 0, 0, 255);

        [SerializeField]
        private float               _tileRuleUIContainerWidth   = 300.0f;
        [SerializeField]
        private float               _prefabPreviewScale         = UIValues.minPrefabPreviewScale;
        [SerializeField]
        private Vector2             _ruleUIScrollOffset         = Vector3.zero;
        [NonSerialized]
        private Slider              _previewScaleSlider;
        [SerializeField]
        private Vector2             _prefabSettingsScrollPos;
        [SerializeField]
        private TileRuleFilter          _tileRuleFilter             = TileRuleFilter.All;
        [SerializeField]
        private TileRuleNeighborRadius  _neighborRadius             = TileRuleNeighborRadius.One;
        [SerializeField]
        private bool                    _autoRefreshTileGrids       = true;

        [NonSerialized]
        private TwoPaneSplitView    _splitView;
        [NonSerialized]
        private ScrollView          _tileRuleUIContainer;
        [NonSerialized]
        private VisualElement       _prefabSettingsContainer;
        [NonSerialized]
        private Button              _useDefaultsBtn;
        [NonSerialized]
        private ProfileSelectionUI<TileRuleProfileDb, TileRuleProfile>          _profileSelectionUI;
        [NonSerialized]
        private List<PluginPrefab>              _pluginPrefabBuffer             = new List<PluginPrefab>();
        [NonSerialized]
        private List<TileRulePrefab>            _tileRulePrefabBuffer           = new List<TileRulePrefab>();
        [NonSerialized]
        private List<UITileRulePrefabItemData>  _tileRulePrefabItemDataBuffer   = new List<UITileRulePrefabItemData>();
        [NonSerialized]
        private List<TileRule>                  _tileRuleBuffer                 = new List<TileRule>();

        public float                            prefabPreviewScale              { get { return _prefabPreviewScale; } set { UndoEx.record(this); _prefabPreviewScale = Mathf.Clamp(value, UIValues.minPrefabPreviewScale, UIValues.maxPrefabPreviewScale); EditorUtility.SetDirty(this); } }

        public static TileRuleProfileDbUI       instance                        { get { return TileRuleProfileDb.instance.ui; } } 

        public void getVisibleSelectedPrefabs(List<TileRulePrefab> tileRulePrefabs)
        {
            tileRulePrefabs.Clear();

            var activeProfile       = TileRuleProfileDb.instance.activeProfile;
            int numTileRules        = activeProfile.numTileRules;
            for (int i = 0; i < numTileRules; ++i)
            {
                var prefabView = activeProfile.getTileRule(i).prefabView;
                if (prefabView != null)
                {
                    prefabView.getVisibleSelectedItemData(_tileRulePrefabItemDataBuffer);
                    foreach (var itemData in _tileRulePrefabItemDataBuffer)
                        tileRulePrefabs.Add(itemData.tileRulePrefab);
                }
            }
        }

        public void getVisiblePrefabs(List<TileRulePrefab> tileRulePrefabs)
        {
            tileRulePrefabs.Clear();

            var activeProfile = TileRuleProfileDb.instance.activeProfile;
            int numTileRules = activeProfile.numTileRules;
            for (int i = 0; i < numTileRules; ++i)
            {
                var prefabView = activeProfile.getTileRule(i).prefabView;
                if (prefabView != null)
                {
                    prefabView.getVisibleItemData(_tileRulePrefabItemDataBuffer);
                    foreach (var itemData in _tileRulePrefabItemDataBuffer)
                        tileRulePrefabs.Add(itemData.tileRulePrefab);
                }
            }
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow     = 1.0f;
            _profileSelectionUI                 = new ProfileSelectionUI<TileRuleProfileDb, TileRuleProfile>();
            _profileSelectionUI.build(TileRuleProfileDb.instance, "tile rule profile", contentContainer);

            createTopToolbar();
            createSecondaryTopToolbar();

            _splitView                      = new TwoPaneSplitView();
            _splitView.orientation          = TwoPaneSplitViewOrientation.Horizontal;
            contentContainer.Add(_splitView);

            _tileRuleUIContainer = new ScrollView(ScrollViewMode.Vertical);
            _splitView.Add(_tileRuleUIContainer);
            _tileRuleUIContainer.verticalScroller.valueChanged += p =>
            {
                _ruleUIScrollOffset = _tileRuleUIContainer.scrollOffset;
            };
            _tileRuleUIContainer.scrollOffset = _ruleUIScrollOffset;

            _prefabSettingsContainer = new VisualElement();
            _prefabSettingsContainer.style.flexGrow = 1.0f;
            _splitView.Add(_prefabSettingsContainer);
            _splitView.fixedPaneIndex = _splitView.IndexOf(_tileRuleUIContainer);
            _splitView.fixedPaneInitialDimension = _tileRuleUIContainerWidth;
            
            createRuleUIs();
            createBottomToolbar();
            populatePrefabViews();
            createPrefabSettingsControls(contentContainer);
        }

        protected override void onEnabled()
        {
            EditorApplication.update += onEditorUpdate;
            TileRuleProfileDb.instance.activeProfileChanged += onActiveProfileChanged;
        }

        protected override void onDisabled()
        {
            EditorApplication.update -= onEditorUpdate;
            TileRuleProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
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

            _tileRuleUIContainer.scrollOffset = _ruleUIScrollOffset;
        }

        private void onEditorUpdate()
        {
            if (uiVisibleAndReady)
            {
                float w = _tileRuleUIContainer.style.width.value.value;
                if (w != _tileRuleUIContainerWidth)
                {
                    _tileRuleUIContainerWidth = w;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        private void populatePrefabViews()
        {
            var activeProfile = TileRuleProfileDb.instance.activeProfile;
            for (int i = 0; i < activeProfile.numTileRules; ++i)
            {
                TileRule tileRule   = activeProfile.getTileRule(i);
                populatePrefabView(tileRule);
            }
        }

        private void populatePrefabView(TileRule tileRule)
        {
            var activeProfile = TileRuleProfileDb.instance.activeProfile;
            var prefabView = tileRule.prefabView;
            if (prefabView == null) return;

            prefabView.onBeginBuild();
            tileRule.getPrefabs(_tileRulePrefabBuffer);
            foreach (var rulePrefab in _tileRulePrefabBuffer)
            {
                prefabView.addItem(new UITileRulePrefabItemData() { tileRulePrefab = rulePrefab, tileRuleProfile = activeProfile }, true);
            }

            prefabView.onEndBuild();
            prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
        }

        private void onActiveProfileChanged(TileRuleProfile newActiveProfile)
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            createRuleUIs();
            populatePrefabViews();
        }

        private void createTopToolbar()
        {
            Toolbar toolbar = new Toolbar();
            toolbar.style.flexShrink = 0.0f;
            contentContainer.Add(toolbar);

            var btn             = UI.createSmallCreateNewToolbarButton(toolbar);
            btn.clicked         += () => { createNewTileRule(); };
            btn.tooltip         = "Create a new tile rule.";

            btn                 = UI.createSmallResetPrefabPreviewsToolbarButton(toolbar);
            btn.clicked         += () => { TileRuleProfileDb.instance.activeProfile.resetPrefabPreviews(); };

            btn                 = UI.createToolbarButton(TexturePool.instance.delete, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            btn.tooltip         = "Delete all rules.";
            btn.clicked         += () => { TileRuleProfileDb.instance.activeProfile.deleteAllTileRules(); autoRefreshTileGrids(true); };
            UI.useDefaultMargins(btn);
        }

        private void createSecondaryTopToolbar()
        {
            Toolbar toolbar             = new Toolbar();
            toolbar.style.flexShrink    = 0.0f;
            contentContainer.Add(toolbar);

            var btn                     = UI.createSmallToolbarFilterPrefixButton("Tile rule filter.", true, toolbar);
            btn.RegisterCallback<MouseUpEvent>(p => 
            {
                if (p.button == (int)MouseButton.LeftMouse)
                {
                    UndoEx.record(this);
                    if (FixedShortcuts.ui_EnableClearAllOnMouseUp(p)) _tileRuleFilter = TileRuleFilter.None;
                    else _tileRuleFilter = TileRuleFilter.All;
                }
            });
            var ruleTypeFilerField  = UI.createEnumFlagsField(typeof(TileRuleFilter), "_tileRuleFilter", serializedObject, "", "Tile rule filter.", toolbar);
            ruleTypeFilerField.RegisterValueChangedCallback(p => { applyTileRuleFilters(); });
            ruleTypeFilerField.style.maxWidth = 100.0f;

            UI.createToolbarSpacer(toolbar);

            var icon                = UI.createIcon(TexturePool.instance.tileNeighborRadius, 16.0f, toolbar);
            UI.useDefaultMargins(icon);
            var neighborRadiusField = UI.createEnumField(typeof(TileRuleNeighborRadius), "_neighborRadius", serializedObject, "", 
                "Allows you to select the neighbor radius. A larger radius offers more flexibility and can be useful for more advanced use cases.", toolbar);
            neighborRadiusField.style.width = 70.0f;
            neighborRadiusField.RegisterValueChangedCallback(p => 
            {
                var activeProfile = TileRuleProfileDb.instance.activeProfile;
                for (int i = 0; i < activeProfile.numTileRules; ++i)
                {
                    TileRule tileRule = activeProfile.getTileRule(i);
                    createBitButtonGrid(tileRule, null);
                    tileRule.ui.style.height            = getTileRuleUIHeight();
                    tileRule.prefabView.style.height    = tileRule.ui.style.height;
                }
            });
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
                    var activeProfile   = TileRuleProfileDb.instance.activeProfile;
                    int numTileRules    = activeProfile.numTileRules;
                    for (int i = 0; i < numTileRules; ++i)
                    {
                        var prefabView  = activeProfile.getTileRule(i).prefabView;
                        if (prefabView != null)
                        {
                            prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
                        }
                    }

                    _previewScaleSlider.tooltip = "Prefab preview scale [" + prefabPreviewScale + "]";
                });

            UI.createHorizontalSpacer(toolbar);
            var icon            = UI.createIcon(TexturePool.instance.autoRefresh, toolbar);
            UI.useDefaultMargins(icon);
            var autoUpdateGrids = UI.createToggle("_autoRefreshTileGrids", serializedObject, "", "If checked, any tile grids that " + 
                "use the active rule profile will be automatically refreshed when making changes to the tile rules. You should uncheck " + 
                "this when working with grids that contain a large number of tiles. In that case, each refresh might take quite a bit of time to finish.", toolbar);

            var refreshGridsBtn         = new Button();
            toolbar.Add(refreshGridsBtn);
            refreshGridsBtn.text        = "Refresh grids";
            refreshGridsBtn.tooltip     = "Refreshes all tiles inside the grids that use this tile rule profile.";
            refreshGridsBtn.clicked     += () => { autoRefreshTileGrids(true); };
        }

        private void createPrefabSettingsControls(VisualElement parent)
        {
            const float labelWidth          = 130.0f;
            IMGUIContainer imGUIContainer   = UI.createIMGUIContainer(_prefabSettingsContainer);
            imGUIContainer.style.flexGrow   = 1.0f;
            imGUIContainer.style.marginTop  = 3.0f;
            imGUIContainer.style.marginLeft = 3.0f;
            imGUIContainer.onGUIHandler = () =>
            {
                getVisibleSelectedPrefabs(_tileRulePrefabBuffer);
                _useDefaultsBtn.setDisplayVisible(_tileRulePrefabBuffer.Count != 0);

                if (_tileRulePrefabBuffer.Count == 0)
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
                    var diff = TileRulePrefab.checkDiff(_tileRulePrefabBuffer);

                    #pragma warning disable 0612
                    // Used 
                    // Note: Not needed. It can't be used to temporarily disable prefabs because as soon as
                    //       the used property is enabled, the entire grid is refreshed.
                    /*bool used           = _tileRulePrefabBuffer[0].used;
                    EditorGUI.showMixedValue = diff.used;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text     = "Used";
                    guiContent.tooltip  = "If checked, the prefab will be used when painting. Otherwise, it will be ignored.";
                    bool newUsed        = EditorGUILayout.Toggle(guiContent, used, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var trPrefab in _tileRulePrefabBuffer)
                            trPrefab.used = newUsed;

                        int numRules = TileRuleProfileDb.instance.activeProfile.numTileRules;
                        for (int i = 0; i < numRules; ++i)
                        {
                            var rule = TileRuleProfileDb.instance.activeProfile.getTileRule(i);
                            rule.onPrefabsUsedStateChanged();
                            if (rule.prefabView != null) rule.prefabView.refreshSelectedItemsUI();
                        }
                    }*/

                    // Spawn chance
                    float spawnChance           = _tileRulePrefabBuffer[0].spawnChance;
                    EditorGUI.showMixedValue    = diff.spawnChance;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text             = "Spawn chance";
                    guiContent.tooltip          = "The prefab's chance to be spawned while painting.";
                    float newSpawnChance        = EditorGUILayout.FloatField(guiContent, spawnChance, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var trPrefab in _tileRulePrefabBuffer)
                            trPrefab.spawnChance = newSpawnChance;

                        int numRules = TileRuleProfileDb.instance.activeProfile.numTileRules;
                        for (int i = 0; i < numRules; ++i)
                        {
                            var rule = TileRuleProfileDb.instance.activeProfile.getTileRule(i);
                            rule.onPrefabsSpawnChanceChanged();
                        }

                        autoRefreshTileGrids(false);
                    }

                    // X condition
                    EditorGUILayout.Separator();
                    bool cellCond               = _tileRulePrefabBuffer[0].cellXCondition;
                    EditorGUI.showMixedValue    = diff.cellXCondition;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text             = "Cell X condition";
                    guiContent.tooltip          = "If checked, the prefab will only be picked if it satisfies the cell condition along the X axis.";
                    bool newCellCond            = EditorGUILayout.Toggle(guiContent, cellCond, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var trPrefab in _tileRulePrefabBuffer)
                            trPrefab.cellXCondition = newCellCond;

                        int numRules = TileRuleProfileDb.instance.activeProfile.numTileRules;
                        for (int i = 0; i < numRules; ++i)
                        {
                            var rule = TileRuleProfileDb.instance.activeProfile.getTileRule(i);
                            rule.onPrefabsConditionsChanged();
                        }

                        autoRefreshTileGrids(false);
                    }

                    int minCell, maxCell, newMinCell, newMaxCell;
                    if (newCellCond)
                    {
                        // Min cell X
                        minCell                     = _tileRulePrefabBuffer[0].minCellX;
                        EditorGUI.showMixedValue    = diff.minCellX;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text     = "Min X";
                        guiContent.tooltip  = "The minimum X coordinate.";
                        newMinCell          = EditorGUILayout.IntField(guiContent, minCell, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var trPrefab in _tileRulePrefabBuffer)
                                trPrefab.minCellX = newMinCell;

                            autoRefreshTileGrids(false);
                        }

                        // Max cell X
                        maxCell                     = _tileRulePrefabBuffer[0].maxCellX;
                        EditorGUI.showMixedValue    = diff.maxCellX;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text     = "Max X";
                        guiContent.tooltip  = "The maximum X coordinate.";
                        newMaxCell          = EditorGUILayout.IntField(guiContent, maxCell, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var trPrefab in _tileRulePrefabBuffer)
                                trPrefab.maxCellX = newMaxCell;

                            autoRefreshTileGrids(false);
                        }
                    }

                    // Y condition
                    if (newCellCond) EditorGUILayout.Separator();
                    cellCond                    = _tileRulePrefabBuffer[0].cellYCondition;
                    EditorGUI.showMixedValue    = diff.cellYCondition;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text             = "Cell Y condition";
                    guiContent.tooltip          = "If checked, the prefab will only be picked if it satisfies the cell condition along the Y axis.";
                    newCellCond = EditorGUILayout.Toggle(guiContent, cellCond, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var trPrefab in _tileRulePrefabBuffer)
                            trPrefab.cellYCondition = newCellCond;

                        int numRules = TileRuleProfileDb.instance.activeProfile.numTileRules;
                        for (int i = 0; i < numRules; ++i)
                        {
                            var rule = TileRuleProfileDb.instance.activeProfile.getTileRule(i);
                            rule.onPrefabsConditionsChanged();
                        }

                        autoRefreshTileGrids(false);
                    }

                    if (newCellCond)
                    {
                        // Min cell Y
                        minCell                     = _tileRulePrefabBuffer[0].minCellY;
                        EditorGUI.showMixedValue    = diff.minCellY;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text             = "Min Y";
                        guiContent.tooltip          = "The minimum Y coordinate.";
                        newMinCell = EditorGUILayout.IntField(guiContent, minCell, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var trPrefab in _tileRulePrefabBuffer)
                                trPrefab.minCellY = newMinCell;

                            autoRefreshTileGrids(false);
                        }

                        // Max cell Y
                        maxCell                     = _tileRulePrefabBuffer[0].maxCellY;
                        EditorGUI.showMixedValue    = diff.maxCellY;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text             = "Max Y";
                        guiContent.tooltip          = "The maximum Y coordinate.";
                        newMaxCell = EditorGUILayout.IntField(guiContent, maxCell, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var trPrefab in _tileRulePrefabBuffer)
                                trPrefab.maxCellY = newMaxCell;

                            autoRefreshTileGrids(false);
                        }
                    }

                    // Z condition
                    if (newCellCond) EditorGUILayout.Separator();
                    cellCond                    = _tileRulePrefabBuffer[0].cellZCondition;
                    EditorGUI.showMixedValue    = diff.cellZCondition;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text             = "Cell Z condition";
                    guiContent.tooltip          = "If checked, the prefab will only be picked if it satisfies the cell condition along the Z axis.";
                    newCellCond = EditorGUILayout.Toggle(guiContent, cellCond, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var trPrefab in _tileRulePrefabBuffer)
                            trPrefab.cellZCondition = newCellCond;

                        int numRules = TileRuleProfileDb.instance.activeProfile.numTileRules;
                        for (int i = 0; i < numRules; ++i)
                        {
                            var rule = TileRuleProfileDb.instance.activeProfile.getTileRule(i);
                            rule.onPrefabsConditionsChanged();
                        }

                        autoRefreshTileGrids(false);
                    }

                    if (newCellCond)
                    {
                        // Min cell Z
                        minCell                     = _tileRulePrefabBuffer[0].minCellZ;
                        EditorGUI.showMixedValue    = diff.minCellZ;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text             = "Min Z";
                        guiContent.tooltip          = "The minimum Z coordinate.";
                        newMinCell = EditorGUILayout.IntField(guiContent, minCell, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var trPrefab in _tileRulePrefabBuffer)
                                trPrefab.minCellZ = newMinCell;

                            autoRefreshTileGrids(false);
                        }

                        // Max cell Z
                        maxCell                     = _tileRulePrefabBuffer[0].maxCellZ;
                        EditorGUI.showMixedValue    = diff.maxCellZ;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text             = "Max Z";
                        guiContent.tooltip          = "The maximum Z coordinate.";
                        newMaxCell = EditorGUILayout.IntField(guiContent, maxCell, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var trPrefab in _tileRulePrefabBuffer)
                                trPrefab.maxCellZ = newMaxCell;

                            autoRefreshTileGrids(false);
                        }
                    }

                    #pragma warning restore 0612
                    EditorUIEx.restoreShowMixedValue();
                    EditorUIEx.restoreLabelWidth();

                    EditorGUILayout.EndScrollView();
                }
            };

            _useDefaultsBtn = UI.createUseDefaultsButton(() =>
            {
                getVisibleSelectedPrefabs(_tileRulePrefabBuffer);
                foreach (var trPrefab in _tileRulePrefabBuffer)
                    trPrefab.useDefaults();

                int numRules = TileRuleProfileDb.instance.activeProfile.numTileRules;
                for (int i = 0; i < numRules; ++i)
                {
                    var rule = TileRuleProfileDb.instance.activeProfile.getTileRule(i);
                    rule.onPrefabsSettingsChanged();
                }

                autoRefreshTileGrids(false);

            }, _prefabSettingsContainer);
        }

        private void createNewTileRule()
        {
            var tileRule = TileRuleProfileDb.instance.activeProfile.createTileRule();
            createRuleUI(tileRule);
            scrollToTileRuleUI(tileRule);

            if (!filterTileRule(tileRule))
                tileRule.ui.setDisplayVisible(false);
        }

        private void createRuleUIs()
        {
            // Note: Can happen during Undo/Redo if the UI is not visible (i.e. window is not open).
            if (_tileRuleUIContainer == null) return;

            // Note: Always start with a clean slate (e.g. when calling from onUndoRedo).
            _tileRuleUIContainer.Clear();

            var activeProfile = TileRuleProfileDb.instance.activeProfile;
            for (int i = 0; i < activeProfile.numTileRules; ++i)
            {
                TileRule tileRule   = activeProfile.getTileRule(i);
                createRuleUI(tileRule);
            }

            applyTileRuleFilters();
        }

        private void createRuleUI(TileRule tileRule)
        {
            var activeProfile = TileRuleProfileDb.instance.activeProfile;

            tileRule.ui = new VisualElement();
            _tileRuleUIContainer.Add(tileRule.ui);

            tileRule.ui.style.flexDirection     = FlexDirection.Row;
            tileRule.ui.style.setMargins(0.0f);
            tileRule.ui.style.marginLeft        = 2.0f;
            tileRule.ui.style.marginTop         = 2.0f;
            tileRule.ui.style.setBorderColor(Color.black);
            tileRule.ui.style.setBorderWidth(1.0f);
            tileRule.ui.style.borderRightWidth  = 0.0f;
            tileRule.ui.style.height            = getTileRuleUIHeight();

            // Create button grid
            var ruleMaskControls                    = new VisualElement();
            ruleMaskControls.style.flexDirection    = FlexDirection.Column;
            ruleMaskControls.style.marginTop        = 2.0f;
            ruleMaskControls.style.marginLeft       = 2.0f;
            ruleMaskControls.style.flexGrow         = 1.0f;
            tileRule.ui.Add(ruleMaskControls);

            createBitButtonGrid(tileRule, ruleMaskControls);

            // Create bottom toolbar(s)
            // Rule type toolbar
            var bottomToolbar = new Toolbar();
            ruleMaskControls.Add(bottomToolbar);

            var ruleTypeField                   = UI.createEnumField(typeof(TileRuleType), "_ruleType", tileRule.serializedObject, "", "The rule type.", bottomToolbar);
            ruleTypeField.style.marginLeft      = 0.0f;
            ruleTypeField.style.marginRight     = 0.0f;
            ruleTypeField.style.flexGrow        = 1.0f;
            ruleTypeField.RegisterValueChangedCallback(p => 
            {
                // Note: For some reason, when prefabs are deleted from the prefab manager, this callback
                //       is fired even if the value hasn't changed. So we need to only take action if the
                //       tile rule profile window is focused.
                if (EditorWindow.focusedWindow.GetType() == typeof(TileRuleProfileDbWindow))
                    autoRefreshTileGrids(false); 
            });

            // Rule rotation mode toolbar
            bottomToolbar = new Toolbar();
            ruleMaskControls.Add(bottomToolbar);

            var ruleRotationModeField = UI.createEnumField(typeof(TileRuleRotationMode), "_ruleRotationMode", tileRule.serializedObject, "",
                "The rule rotation mode. Fixed - use the rule as is. Rotated - generate rotated versions and use them during detection.", bottomToolbar);
            ruleRotationModeField.style.marginLeft  = 0.0f;
            ruleRotationModeField.style.marginRight = 0.0f;
            ruleRotationModeField.style.flexGrow    = 1.0f;
            ruleRotationModeField.RegisterValueChangedCallback(p => 
            {
                // Note: For some reason, when prefabs are deleted from the prefab manager, this callback
                //       is fired even if the value hasn't changed. So we need to only take action if the
                //       tile rule profile window is focused.
                if (EditorWindow.focusedWindow.GetType() == typeof(TileRuleProfileDbWindow))
                    autoRefreshTileGrids(false); 
            });

            // Move up/down & top/bottom toolbar
            bottomToolbar = new Toolbar();
            ruleMaskControls.Add(bottomToolbar);

            var btn         = UI.createToolbarButton(TexturePool.instance.itemArrowDown, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, bottomToolbar);
            btn.tooltip     = "Move rule down.";
            UI.useDefaultMargins(btn);
            btn.clicked     += () => 
            { 
                TileRuleProfileDb.instance.activeProfile.moveRuleDown(tileRule); 
                refresh();
                scrollToTileRuleUI(tileRule);
            };

            btn         = UI.createToolbarButton(TexturePool.instance.itemArrowUp, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, bottomToolbar);
            btn.tooltip = "Move rule up.";
            UI.useDefaultMargins(btn);
            btn.clicked += () =>
            {
                TileRuleProfileDb.instance.activeProfile.moveRuleUp(tileRule);
                refresh();
                scrollToTileRuleUI(tileRule);
            };

            btn         = UI.createToolbarButton(TexturePool.instance.itemArrowBottom, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, bottomToolbar);
            btn.tooltip = "Move rule to bottom.";
            UI.useDefaultMargins(btn);
            btn.clicked += () => 
            { 
                TileRuleProfileDb.instance.activeProfile.moveRuleToBottom(tileRule); 
                refresh();
                scrollToTileRuleUI(tileRule);
            };

            btn         = UI.createToolbarButton(TexturePool.instance.itemArrowTop, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, bottomToolbar);
            btn.tooltip = "Move rule to top.";
            UI.useDefaultMargins(btn);
            btn.clicked += () =>
            {
                TileRuleProfileDb.instance.activeProfile.moveRuleToTop(tileRule);
                refresh();
                scrollToTileRuleUI(tileRule);
            };

            // Delete actions toolbar
            bottomToolbar   = new Toolbar();
            ruleMaskControls.Add(bottomToolbar);

            btn             = UI.createToolbarButton(TexturePool.instance.delete, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, bottomToolbar);
            btn.tooltip     = "Delete tile rule.";
            UI.useDefaultMargins(btn);
            btn.clicked     += () => { activeProfile.deleteTileRule(tileRule); autoRefreshTileGrids(false); };

            btn             = UI.createSmallLoadPrefabsButton(bottomToolbar);
            btn.clicked     += () => { createTileRulePrefabsFromPrefabsInManager(tileRule); };

            UI.createHorizontalSpacer(bottomToolbar);
            btn             = UI.createToolbarButton(TexturePool.instance.clear, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, bottomToolbar);
            btn.tooltip     = "Clear tile rule (i.e. delete all prefabs associated with the rule).";
            UI.useDefaultMargins(btn);
            btn.clicked     += () => { tileRule.deleteAllPrefabs(); populatePrefabView(tileRule); autoRefreshTileGrids(false); };

            // Create prefab view
            tileRule.prefabView     = new GridView<UITileRulePrefabItem, UITileRulePrefabItemData>(tileRule.prefabViewState, tileRule.ui);
            var prefabView          = tileRule.prefabView;

            prefabView.canDelete                = true;
            prefabView.style.marginLeft         = 2.0f;
            prefabView.style.marginTop          = -1.0f;
            prefabView.style.borderTopWidth     = 1.0f;
            prefabView.style.flexGrow           = 1.0f;
            prefabView.style.borderLeftColor    = Color.black;
            prefabView.style.borderLeftWidth    = 1.0f;
            prefabView.style.height             = tileRule.ui.style.height;
            // Note: The following line of code causes issues when using a TwoPaneSplitView.
            //prefabView.style.maxWidth           = _tileRuleUIContainer.style.width.value.value - 10.0f - 3.0f * _ruleMaskButtonSize;
            prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));

            prefabView.selectedItemsWillBeDeleted   += onSelectedPrefabItemsWillBeDeleted;
            prefabView.selectionChanged             += onPrefabSelectionChanged;

            prefabView.RegisterCallback<DragPerformEvent>(p =>
            {
                if (PluginDragAndDrop.initiatedByPlugin)
                {
                    if (PluginPrefabManagerUI.instance.dragAndDropInitiatedByPrefabView())
                    {
                        if (activeProfile != null)
                        {
                            createTileRulePrefabsFromPrefabsInManager(tileRule);
                        }
                        PluginDragAndDrop.endDrag();
                    }
                }
            });

            prefabView.RegisterCallback<MouseDownEvent>(p => 
            {
                if (p.button == (int)MouseButton.RightMouse)
                {
                    var visSelectedPrefabs  = new List<TileRulePrefab>();
                    var visiblePrefabs      = new List<TileRulePrefab>();
                    getVisibleSelectedPrefabs(visSelectedPrefabs);
                    getVisiblePrefabs(visiblePrefabs);

                    PluginGenericMenu menu = new PluginGenericMenu();
                    menu.addItem(GenericMenuItemCategory.VisiblePrefabs, GenericMenuItemId.SelectAll, visiblePrefabs.Count != 0,
                        () =>
                        {
                            var activeProfile   = TileRuleProfileDb.instance.activeProfile;
                            int numRules        = activeProfile.numTileRules;
                            for (int i = 0; i < numRules; i++)
                            {
                                var tileRule = activeProfile.getTileRule(i);
                                if (filterTileRule(tileRule))
                                {
                                    tileRule.prefabView.setAllItemsSelected(true, true, false);
                                }
                            }
                        });
                    menu.addItem(GenericMenuItemCategory.VisiblePrefabs, GenericMenuItemId.DeselectAll, visSelectedPrefabs.Count != 0,
                        () =>
                        {
                            var activeProfile   = TileRuleProfileDb.instance.activeProfile;
                            int numRules        = activeProfile.numTileRules;
                            for (int i = 0; i < numRules; i++)
                            {
                                var tileRule = activeProfile.getTileRule(i);
                                if (filterTileRule(tileRule))
                                {
                                    tileRule.prefabView.setAllItemsSelected(false, true, false);
                                }
                            }
                        });
                    menu.addItem(GenericMenuItemCategory.VisiblePrefabs, GenericMenuItemId.HighlightSelectedInManager, visSelectedPrefabs.Count != 0,
                        () =>
                        {
                            TileRulePrefab.getPluginPrefabs(visSelectedPrefabs, _pluginPrefabBuffer);
                            PluginPrefabManagerUI.instance.selectPluginPrefabsAndMakeVisible(_pluginPrefabBuffer, true);
                        });

                    menu.showAsContext();
                }
            });
        }

        private void createTileRulePrefabsFromPrefabsInManager(TileRule tileRule)
        {
            PluginPrefabManagerUI.instance.getVisibleSelectedPrefabs(_pluginPrefabBuffer);
            tileRule.createPrefabs(_pluginPrefabBuffer, _tileRulePrefabBuffer, false, "Creating Tile Rule Prefabs");
            foreach (var rulePrefab in _tileRulePrefabBuffer)
                tileRule.prefabView.addItem(new UITileRulePrefabItemData()
                { tileRulePrefab = rulePrefab, tileRuleProfile = TileRuleProfileDb.instance.activeProfile }, true);

            // If this is the first prefab added to the tile rule, refresh
            if (tileRule.numPrefabs == 1) autoRefreshTileGrids(false);
        }

        private void applyTileRuleFilters()
        {
            var activeProfile = TileRuleProfileDb.instance.activeProfile;
            activeProfile.getTileRules(_tileRuleBuffer);

            foreach(var tileRule in _tileRuleBuffer) 
            {
                if (tileRule.ui != null)
                    tileRule.ui.setDisplayVisible(filterTileRule(tileRule));
            }
        }

        private bool filterTileRule(TileRule tileRule)
        {
            switch (tileRule.ruleType)
            {
                case TileRuleType.Standard:

                    return (_tileRuleFilter & TileRuleFilter.Standard) != 0;

                case TileRuleType.Platform:

                    return (_tileRuleFilter & TileRuleFilter.Platform) != 0;

                case TileRuleType.Ramp:

                    return (_tileRuleFilter & TileRuleFilter.Ramp) != 0;
            }

            return true;
        }

        private void scrollToTileRuleUI(TileRule tileRule)
        {
            UndoEx.record(this);
            var scrollToAction = new ScrollToItem<VisualElement>(_tileRuleUIContainer, tileRule.ui, Undo.GetCurrentGroup(), this);
            GSpawn.active.registerEditorUpdateAction(scrollToAction);
        }

        private VisualElement createBitButtonGrid(TileRule tileRule, VisualElement parent)
        {
            if (tileRule.bitButtonGrid != null)
            {
                parent = tileRule.bitButtonGrid.parent;
                parent.Remove(tileRule.bitButtonGrid);
            }

            var bitButtonGrid                   = new VisualElement();
            tileRule.bitButtonGrid              = bitButtonGrid;
            bitButtonGrid.style.flexDirection   = FlexDirection.Column;
            bitButtonGrid.style.flexGrow        = 1.0f;
            bitButtonGrid.style.flexShrink      = 0.0f;
            parent.Insert(0, bitButtonGrid);
            for (int row = 0; row < TileRuleMask.numBitRows; ++row)
            {
                var bitRow = new VisualElement();
                bitRow.style.flexShrink     = 0.0f;
                bitRow.style.flexDirection  = FlexDirection.Row;
                bitButtonGrid.Add(bitRow);
                for (int col = 0; col < TileRuleMask.bitRowSize; ++col)
                {
                    var bitBtn = createRuleMaskBitButton(tileRule, row, col, bitRow);
                    bitRow.Add(bitBtn);
                }
            }

            parent.style.minWidth = getBitMaskWidth();
            parent.style.maxWidth = parent.style.minWidth;

            tileRule.bitButtonGrid = bitButtonGrid;
            return bitButtonGrid;
        }

        private bool _paintingBitButtons = false;
        private Button createRuleMaskBitButton(TileRule tileRule, int row, int col, VisualElement parent)
        {
            var buttonSize              = getRuleMaskBitButtonSize();

            var bitBtn                  = UI.createIconButton(TexturePool.instance.white, parent);
            bitBtn.style.marginRight    = 0.0f;
            bitBtn.style.marginLeft     = 0.0f;
            bitBtn.style.marginTop      = 0.0f;
            bitBtn.style.marginBottom   = 0.0f;
            bitBtn.style.width          = buttonSize;
            bitBtn.style.height         = buttonSize;
            bitBtn.style.maxHeight      = buttonSize;
            bitBtn.style.maxWidth       = buttonSize;
            bitBtn.name                 = TileRuleMask.rowColToBitIndex(row, col).ToString();

            if (TileRuleMask.isMiddleBit(row, col))
            {
                bitBtn.tooltip = "Left/Right click to enable all neighbors. \nShift + left/right click to disable all neighbors.";
            }           

            bitBtn.RegisterCallback<MouseMoveEvent>(p => 
            {
                if (TileRuleMask.isMiddleBit(row, col)) return;

                if (Event.current.type == EventType.MouseDrag)
                {
                    if (Event.current.button == (int)MouseButton.LeftMouse)
                    {
                        if (FixedShortcuts.ui_EnableClearAll()) tileRule.clearMaskBit(row, col, TileRuleBitMaskId.ReqOn);
                        else tileRule.setMaskBit(row, col, TileRuleBitMaskId.ReqOn);

                        refreshTileRuleBitButton(tileRule, bitBtn);

                        Event.current.disable();
                        _paintingBitButtons = true;
                    }
                    else
                    if (Event.current.button == (int)MouseButton.RightMouse)
                    {
                        if (FixedShortcuts.ui_EnableClearAll()) tileRule.clearMaskBit(row, col, TileRuleBitMaskId.ReqOff);
                        else tileRule.setMaskBit(row, col, TileRuleBitMaskId.ReqOff);

                        refreshTileRuleBitButton(tileRule, bitBtn);

                        Event.current.disable();
                        _paintingBitButtons = true;
                    }
                }
            });
      
            // Note: Capturing the MouseDownEvent doesn't work for the left mouse button, except when
            //       the SHIFT key is pressed. 
            bitBtn.RegisterCallback<MouseUpEvent>((p) => 
            {
                if (_paintingBitButtons)
                {
                    // Note: Refresh the grid here, where we know painting ends. This speeds up
                    //       the painting process.
                    autoRefreshTileGrids(false);        
                    _paintingBitButtons = false;
                    return;
                }

                if (p.button == (int)MouseButton.LeftMouse)
                {
                    if (TileRuleMask.isMiddleBit(row, col))
                    {
                        if (FixedShortcuts.ui_EnableClearAllOnMouseUp(p)) tileRule.useDefaultMask();
                        else tileRule.setAllMaskBits(TileRuleBitMaskId.ReqOn);

                        refreshAllTileRuleBitButtons(tileRule);
                        autoRefreshTileGrids(false);
                    }
                    else
                    {
                        tileRule.toggleMaskBit(row, col, TileRuleBitMaskId.ReqOn);
                        refreshTileRuleBitButton(tileRule, bitBtn);
                        autoRefreshTileGrids(false);
                    }
                }
                if (p.button == (int)MouseButton.RightMouse)
                {
                    if (TileRuleMask.isMiddleBit(row, col))
                    {
                        if (FixedShortcuts.ui_EnableClearAllOnMouseUp(p)) tileRule.useDefaultMask();
                        else tileRule.setAllMaskBits(TileRuleBitMaskId.ReqOff);

                        refreshAllTileRuleBitButtons(tileRule);
                        autoRefreshTileGrids(false);
                    }
                    else
                    {
                        tileRule.toggleMaskBit(row, col, TileRuleBitMaskId.ReqOff);
                        refreshTileRuleBitButton(tileRule, bitBtn);
                        autoRefreshTileGrids(false);
                    }
                }
            });

            updateBitButtonBorder(bitBtn);
            updateBitButtonBackground(bitBtn, row, col, tileRule);
            bitBtn.setDisplayVisible(TileRuleMask.isBitInRadius(_neighborRadius, row, col));

            return bitBtn;
        }

        private void refreshTileRuleBitButton(TileRule tileRule, Button bitButton)
        {
            int row, col;
            TileRuleMask.bitIndexToRowCol(int.Parse(bitButton.name), out row, out col);
            updateBitButtonBackground(bitButton, row, col, tileRule);
            bitButton.setDisplayVisible(TileRuleMask.isBitInRadius(_neighborRadius, row, col));
        }

        private void refreshAllTileRuleBitButtons(TileRule tileRule)
        {
            var bitButtons = tileRule.bitButtonGrid.Query<Button>().ToList();
            foreach (var bitButton in bitButtons)
            {
                // Note: Filter any toolbar buttons.
                if (!(bitButton is ToolbarButton))
                    refreshTileRuleBitButton(tileRule, bitButton);
            }
        }

        private void updateBitButtonBorder(Button bitButton)
        {
            int row, col;
            TileRuleMask.bitIndexToRowCol(int.Parse(bitButton.name), out row, out col);

            const float borderWidth         = 1.0f;
            Color borderColor               = Color.black;

            bitButton.style.borderRightColor   = borderColor;
            bitButton.style.borderRightWidth   = borderWidth;
            bitButton.style.borderTopColor     = borderColor;
            bitButton.style.borderTopWidth     = borderWidth;

            switch (_neighborRadius)
            {
                case TileRuleNeighborRadius.One:

                    if (col == 2)
                    {
                        bitButton.style.borderLeftColor = borderColor;
                        bitButton.style.borderLeftWidth = borderWidth;
                    }
                    if (row == 4)
                    {
                        bitButton.style.borderBottomColor = borderColor;
                        bitButton.style.borderBottomWidth = borderWidth;
                    }
                    break;

                case TileRuleNeighborRadius.Two:

                    if (col == 1)
                    {
                        bitButton.style.borderLeftColor = borderColor;
                        bitButton.style.borderLeftWidth = borderWidth;
                    }
                    if (row == 5)
                    {
                        bitButton.style.borderBottomColor = borderColor;
                        bitButton.style.borderBottomWidth = borderWidth;
                    }
                    break;

                    #pragma warning disable 0612
                case TileRuleNeighborRadius.Three:

                    if (col == 0)
                    {
                        bitButton.style.borderLeftColor = borderColor;
                        bitButton.style.borderLeftWidth = borderWidth;
                    }
                    if (row == 6)
                    {
                        bitButton.style.borderBottomColor = borderColor;
                        bitButton.style.borderBottomWidth = borderWidth;
                    }
                    break;
                    #pragma warning restore 0612
            }
        }

        private void updateBitButtonBackground(Button bitButton, int row, int col, TileRule tileRule)
        {
            if (TileRuleMask.isMiddleBit(row, col))
            {
                bitButton.style.unityBackgroundImageTintColor = _bitColor_Mid;
            }
            else
            {
                if (tileRule.checkMaskBit(row, col, TileRuleBitMaskId.ReqOff))
                    bitButton.style.unityBackgroundImageTintColor = _bitColor_RequiredOff;
                else if (tileRule.checkMaskBit(row, col, TileRuleBitMaskId.ReqOn))
                    bitButton.style.unityBackgroundImageTintColor = _bitColor_RequiredOn;
                else
                    bitButton.style.unityBackgroundImageTintColor = _bitColor_Unimportant;
            }
        }

        #pragma warning disable 0612
        private float getTileRuleUIHeight()
        {
            if (_neighborRadius == TileRuleNeighborRadius.Three) return 210.0f;
            return 180.0f;
        }

        private float getBitMaskWidth()
        {
            // Note: For 'Three' we need to use a larger width. It seems that
            //       the bit buttons can't be scaled down to the required size
            //       otherwise as if they had some kind of internal min size
            //       requirement.
            if (_neighborRadius == TileRuleNeighborRadius.Three) return 120.0f;
            return 90.0f;
        }

        private float getRuleMaskBitButtonSize()
        {
            switch (_neighborRadius)
            {
                case TileRuleNeighborRadius.One:

                    return getBitMaskWidth() / 3.0f;

                case TileRuleNeighborRadius.Two:

                    return getBitMaskWidth() / 5.0f;

                case TileRuleNeighborRadius.Three:

                    return getBitMaskWidth() / 7.0f;

                default:

                    return 0.0f;
            }
        }
        #pragma warning restore 0612

        private void onSelectedPrefabItemsWillBeDeleted(GridView<UITileRulePrefabItem, UITileRulePrefabItemData> gridView, List<PluginGuid> itemIds)
        {
            _tileRulePrefabBuffer.Clear();
            foreach (var itemId in itemIds)
                _tileRulePrefabBuffer.Add(gridView.getItemData(itemId).tileRulePrefab);

            TileRuleProfileDb.instance.activeProfile.deletePrefabs(_tileRulePrefabBuffer);
            autoRefreshTileGrids(false);
        }

        private void onPrefabSelectionChanged(GridView<UITileRulePrefabItem, UITileRulePrefabItemData> gridView)
        {
            if (!Event.current.control)
            {
                var activeProfile = TileRuleProfileDb.instance.activeProfile;
                for (int i = 0; i < activeProfile.numTileRules; ++i)
                {
                    TileRule tileRule = activeProfile.getTileRule(i);
                    if (tileRule.prefabView != gridView && tileRule.prefabView != null)
                        tileRule.prefabView.setAllItemsSelected(false, false, false);
                }
            }
        }

        private void autoRefreshTileGrids(bool forceRefresh)
        {
            if (_autoRefreshTileGrids || forceRefresh)
                TileRuleGridDb.instance.refreshTileRuleGridTiles(TileRuleProfileDb.instance.activeProfile);
        }
    }
}
#endif