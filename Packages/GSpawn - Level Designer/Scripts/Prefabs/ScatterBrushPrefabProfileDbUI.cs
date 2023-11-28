#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ScatterBrushPrefabProfileDbUI : PluginUI
    {
        [NonSerialized]
        private TwoPaneSplitView    _splitView;
        [NonSerialized]
        private VisualElement       _prefabViewContainer;
        [NonSerialized]
        private VisualElement       _prefabSettingsContainer;
        [NonSerialized]
        private Button              _useDefaultsBtn;

        [SerializeField]
        private float               _prefabViewContainerWidth                   = 300.0f;
        [SerializeField]
        private float               _prefabPreviewScale                         = UIValues.defaultPrefabPreviewScale;
        [SerializeField]
        private Vector2             _prefabSettingsScrollPos;
        private Slider              _previewScaleSlider;

        private EntitySearchField                                                           _prefabSearchField;
        private ProfileSelectionUI<ScatterBrushPrefabProfileDb, ScatterBrushPrefabProfile>  _profileSelectionUI;

        [SerializeField]
        private GridViewState                                                       _prefabViewState;
        [NonSerialized]
        private GridView<UIScatterBrushPrefabItem, UIScatterBrushPrefabItemData>    _prefabView;

        [NonSerialized]
        private List<ScatterBrushPrefab>            _brushPrefabBuffer          = new List<ScatterBrushPrefab>();
        [NonSerialized]
        private List<PluginPrefab>                  _pluginPrefabBuffer         = new List<PluginPrefab>();
        [NonSerialized]
        private List<UIScatterBrushPrefabItemData>  _brushPrefabItemDataBuffer  = new List<UIScatterBrushPrefabItemData>();

        public float                                prefabPreviewScale          { get { return _prefabPreviewScale; } set { UndoEx.record(this); _prefabPreviewScale = Mathf.Clamp(value, UIValues.minPrefabPreviewScale, UIValues.maxPrefabPreviewScale); EditorUtility.SetDirty(this); } }

        public static ScatterBrushPrefabProfileDbUI   instance                    { get { return ScatterBrushPrefabProfileDb.instance.ui; } }

        public void getVisibleSelectedPrefabs(List<ScatterBrushPrefab> brushPrefabs)
        {
            brushPrefabs.Clear();
            if (_prefabView != null)
            {
                _prefabView.getVisibleSelectedItemData(_brushPrefabItemDataBuffer);
                foreach (var itemData in _brushPrefabItemDataBuffer)
                    brushPrefabs.Add(itemData.brushPrefab);
            }
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            if (_prefabView != null) _prefabView.deleteItems(itemData => itemData.brushPrefab.prefabAsset == prefabAsset);
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow     = 1.0f;
            _profileSelectionUI                 = new ProfileSelectionUI<ScatterBrushPrefabProfileDb, ScatterBrushPrefabProfile>();
            _profileSelectionUI.build(ScatterBrushPrefabProfileDb.instance, "scatter brush", contentContainer);

            createTopToolbar();
            createSearchToolbar();

            _splitView                          = new TwoPaneSplitView();
            _splitView.orientation              = TwoPaneSplitViewOrientation.Horizontal;
            contentContainer.Add(_splitView);

            _prefabViewContainer                        = new VisualElement();
            _prefabViewContainer.style.flexShrink       = 0.0f;
            _prefabViewContainer.style.flexGrow         = 1.0f;
            _splitView.Add(_prefabViewContainer);

            _prefabSettingsContainer                = new VisualElement();
            _prefabSettingsContainer.style.flexGrow = 1.0f;
            _splitView.Add(_prefabSettingsContainer);
            _splitView.fixedPaneIndex               = _splitView.IndexOf(_prefabViewContainer);
            _splitView.fixedPaneInitialDimension    = _prefabViewContainerWidth;

            _prefabViewContainer.RegisterCallback<MouseDownEvent>(p =>
            {
                if (p.button == (int)MouseButton.RightMouse)
                {
                    getVisibleSelectedPrefabs(_brushPrefabBuffer);

                    PluginGenericMenu menu = new PluginGenericMenu();
                    menu.addItem(GenericMenuItemCategory.VisiblePrefabs, GenericMenuItemId.HighlightSelectedInManager, _brushPrefabBuffer.Count != 0,
                    () =>
                    {
                        ScatterBrushPrefab.getPluginPrefabs(_brushPrefabBuffer, _pluginPrefabBuffer);
                        PluginPrefabManagerUI.instance.selectPluginPrefabsAndMakeVisible(_pluginPrefabBuffer, true);
                    });

                    menu.showAsContext();
                }
            });

            createPrefabView();
            createBottomToolbar();
            populatePrefabView();
            createPrefabSettingsControls();
        }

        protected override void onRefresh()
        {
            populatePrefabView();
        }

        protected override void onEnabled()
        {
            EditorApplication.update += onEditorUpdate;
            ScatterBrushPrefabProfileDb.instance.activeProfileChanged += onActiveProfileChanged;

            if (_prefabViewState == null)
            {
                _prefabViewState = ScriptableObject.CreateInstance<GridViewState>();
                _prefabViewState.name = GetType().Name + "_PrefabViewState";
                AssetDbEx.addObjectToAsset(_prefabViewState, ScatterBrushPrefabProfileDb.instance);
            }
        }

        protected override void onDisabled()
        {
            EditorApplication.update -= onEditorUpdate;
            ScatterBrushPrefabProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
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

        private void onEditorUpdate()
        {
            if (uiVisibleAndReady)
            {
                float w = _prefabViewContainer.style.width.value.value;
                if (w != _prefabViewContainerWidth)
                {
                    _prefabViewContainerWidth = w;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        private void createTopToolbar()
        {
            Toolbar toolbar             = new Toolbar();
            toolbar.style.flexShrink    = 0.0f;
            contentContainer.Add(toolbar);

            var resetPreviewsBtn    = UI.createSmallResetPrefabPreviewsToolbarButton(toolbar);
            resetPreviewsBtn.clicked += () => { ScatterBrushPrefabProfileDb.instance.activeProfile.resetPrefabPreviews(); };

            var loadPrefabsBtn      = UI.createSmallLoadPrefabsButton(toolbar);
            loadPrefabsBtn.clicked += () => { createScatterBrushPrefabsFromPrefabsInManager(); };
        }

        private void createSearchToolbar()
        {
            var searchToolbar               = new Toolbar();
            searchToolbar.style.flexShrink  = 0.0f;
            contentContainer.Add(searchToolbar);

            _prefabSearchField = new EntitySearchField(searchToolbar,
                (nameList) => { ScatterBrushPrefabProfileDb.instance.activeProfile.getPrefabNames(nameList); },
                (name) => { _prefabView.filterItems(filterPrefabViewItem); });
        }

        private void createPrefabSettingsControls()
        {
            const float labelWidth              = 130.0f;
            IMGUIContainer imGUIContainer       = UI.createIMGUIContainer(_prefabSettingsContainer);
            imGUIContainer.style.flexGrow       = 1.0f;
            imGUIContainer.style.marginTop      = 3.0f;
            imGUIContainer.style.marginLeft     = 3.0f;
            imGUIContainer.onGUIHandler         = () =>
            {
                getVisibleSelectedPrefabs(_brushPrefabBuffer);
                _useDefaultsBtn.setDisplayVisible(_brushPrefabBuffer.Count != 0);

                if (_brushPrefabBuffer.Count == 0)
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
                    var diff = ScatterBrushPrefab.checkDiff(_brushPrefabBuffer);

                    #pragma warning disable 0612
                    // Used
                    bool used = _brushPrefabBuffer[0].used;
                    EditorGUI.showMixedValue = diff.used;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Used";
                    guiContent.tooltip = "If checked, the prefab will be used when painting. Otherwise, it will be ignored.";
                    bool newUsed = EditorGUILayout.Toggle(guiContent, used, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.used = newUsed;

                        ScatterBrushPrefabProfileDb.instance.activeProfile.onPrefabsUsedStateChanged();

                        foreach (var selectedItemId in _prefabViewState.selectedItems)
                            _prefabView.refreshItemUI(selectedItemId);
                    }

                    // Spawn chance
                    float spawnChance = _brushPrefabBuffer[0].spawnChance;
                    EditorGUI.showMixedValue = diff.spawnChance;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Spawn chance";
                    guiContent.tooltip = "The prefab's chance to be spawned while painting.";
                    float newSpawnChance = EditorGUILayout.FloatField(guiContent, spawnChance, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.spawnChance = newSpawnChance;

                        ScatterBrushPrefabProfileDb.instance.activeProfile.onPrefabsSpawnChanceChanged();
                    }

                    // Volume radius
                    float volumeRadius = _brushPrefabBuffer[0].volumeRadius;
                    EditorGUI.showMixedValue = diff.volumeRadius;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Volume radius";
                    guiContent.tooltip = "The volume radius used for overlap checks. Can be used to increase density or as a repel mechanism.";
                    float newVolumeRadius = EditorGUILayout.FloatField(guiContent, volumeRadius, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.volumeRadius = newVolumeRadius;
                    }

                    EditorGUILayout.BeginHorizontal();
                    guiContent.text     = "Use prefab radius";
                    guiContent.tooltip  = "Set the volume radius to the radius of the sphere that encloses the prefab volume.";
                    if (GUILayout.Button(guiContent, GUILayout.Width(120.0f)))
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.usePrefabVolumeRadius();
                    }
                    guiContent.text     = "Use flat prefab radius";
                    guiContent.tooltip  = "Same as 'Use prefab radius' but it ignores the prefab's size along the Y axis. Useful when " + 
                        "painting objects on terrains and when the objects' up axes point upwards in world space (e.g. trees, rocks etc).";
                    if (GUILayout.Button(guiContent, GUILayout.Width(140.0f)))
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.useFlatPrefabVolumeRadius();
                    }
                    EditorGUILayout.EndHorizontal();

                    // Align axis
                    EditorGUILayout.Separator();
                    bool alignAxis = _brushPrefabBuffer[0].alignAxis;
                    EditorGUI.showMixedValue = diff.alignAxis;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Align axis";
                    guiContent.tooltip = "If this is checked, the prefab will have its axis aligned to the surface normal.";
                    bool newAlignAxis = EditorGUILayout.Toggle(guiContent, alignAxis, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.alignAxis = newAlignAxis;
                    }

                    // Alignment axis
                    FlexiAxis alignmentAxis = diff.alignmentAxis ? FlexiAxis.UIMixed : _brushPrefabBuffer[0].alignmentAxis;
                    EditorGUI.showMixedValue = diff.alignmentAxis;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Alignment axis";
                    guiContent.tooltip = "If axis alignment is turned on, this is the axis which will be used for alignment.";
                    FlexiAxis newAlignmentAxis = (FlexiAxis)EditorGUILayout.EnumPopup(guiContent, alignmentAxis, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.alignmentAxis = newAlignmentAxis;
                    }

                    // Invert alignment axis
                    bool invertAlignmentAxis = _brushPrefabBuffer[0].invertAlignmentAxis;
                    EditorGUI.showMixedValue = diff.invertAlignmentAxis;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Invert axis";
                    guiContent.tooltip = "If this is checked, the alignment axis will be inverted.";
                    bool newInvertAlignmentAxis = EditorGUILayout.Toggle(guiContent, invertAlignmentAxis, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.invertAlignmentAxis = newInvertAlignmentAxis;
                    }

                    // Offset from surface
                    float offsetFromSurface = _brushPrefabBuffer[0].offsetFromSurface;
                    EditorGUI.showMixedValue = diff.offsetFromSurface;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Offset from surface";
                    guiContent.tooltip = "Allows you to specify how much the prefab will be offset from the paint surface.";
                    float newOffsetFromSurface = EditorGUILayout.FloatField(guiContent, offsetFromSurface, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.offsetFromSurface = newOffsetFromSurface;
                    }

                    // Embed in surface
                    bool embedInSurface = _brushPrefabBuffer[0].embedInSurface;
                    EditorGUI.showMixedValue = diff.embedInSurface;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Embed in surface";
                    guiContent.tooltip = "If checked, prefabs that float above the surface will be pushed inside to prevent floating.";
                    bool newEmbedInSurface = EditorGUILayout.Toggle(guiContent, embedInSurface, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.embedInSurface = newEmbedInSurface;
                    }

                    // Align to stroke
                    EditorGUILayout.Separator();
                    bool alignToStroke = _brushPrefabBuffer[0].alignToStroke;
                    EditorGUI.showMixedValue = diff.alignToStroke;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Align to stroke";
                    guiContent.tooltip = "If checked, the prefab's rotation will follow the brush stroke.";
                    bool newAlignToStroke = EditorGUILayout.Toggle(guiContent, alignToStroke, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.alignToStroke = newAlignToStroke;
                    }

                    if (newAlignToStroke)
                    {
                        // Stroke alignment axis
                        var strokeAlignAxis = diff.strokeAlignmentAxis ? FlexiAxis.UIMixed : _brushPrefabBuffer[0].strokeAlignmentAxis;
                        EditorGUI.showMixedValue = diff.strokeAlignmentAxis;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Stroke alignment axis";
                        guiContent.tooltip = "If stroke alignment is enabled, this is the prefab axis that will be aligned to the stroke direction.";
                        var newStrokeAlignAxis = (FlexiAxis)EditorGUILayout.EnumPopup(guiContent, strokeAlignAxis, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var brushPrefab in _brushPrefabBuffer)
                                brushPrefab.strokeAlignmentAxis = newStrokeAlignAxis;
                        }

                        // Invert stroke alignment axis
                        bool invertStrokeAlignmentAxis = _brushPrefabBuffer[0].invertStrokeAlignmentAxis;
                        EditorGUI.showMixedValue = diff.invertStrokeAlignmentAxis;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Invert axis";
                        guiContent.tooltip = "If this is checked, the stroke alignment axis will be inverted.";
                        bool newInvertStrokeAlignmentAxis = EditorGUILayout.Toggle(guiContent, invertStrokeAlignmentAxis, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var brushPrefab in _brushPrefabBuffer)
                                brushPrefab.invertStrokeAlignmentAxis = newInvertStrokeAlignmentAxis;
                        }
                    }

                    // Randomize rotation
                    EditorGUILayout.Separator();
                    bool randomizeRotation = _brushPrefabBuffer[0].randomizeRotation;
                    EditorGUI.showMixedValue = diff.randomizeRotation;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Randomize rotation";
                    guiContent.tooltip = "If checked, the prefab rotation will be randomized.";
                    bool newRandomizeRotation = EditorGUILayout.Toggle(guiContent, randomizeRotation, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.randomizeRotation = newRandomizeRotation;
                    }

                    if (newRandomizeRotation)
                    {
                        // Rotation randomization axis
                        ScatterBrushPrefabRotationRandomizationAxis rotationRandAxis = 
                            diff.rotationRandomizationAxis ? 
                            ScatterBrushPrefabRotationRandomizationAxis.UIMixed : _brushPrefabBuffer[0].rotationRandomizationAxis;
                        EditorGUI.showMixedValue = diff.rotationRandomizationAxis;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Randomization axis";
                        guiContent.tooltip = "If rotation randomization is on, this is the axis around which the prefab will be rotated.";
                        ScatterBrushPrefabRotationRandomizationAxis newRotationRandAxis = 
                            (ScatterBrushPrefabRotationRandomizationAxis)EditorGUILayout.EnumPopup(guiContent, rotationRandAxis, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var brushPrefab in _brushPrefabBuffer)
                                brushPrefab.rotationRandomizationAxis = newRotationRandAxis;
                        }

                        // Min random rotation
                        float minRandomRotation = _brushPrefabBuffer[0].minRandomRotation;
                        EditorGUI.showMixedValue = diff.minRandomRotation;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min rotation";
                        guiContent.tooltip = "The minimum random rotation.";
                        float newMinRandomRotation = EditorGUILayout.DelayedFloatField(guiContent, minRandomRotation, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var brushPrefab in _brushPrefabBuffer)
                                brushPrefab.minRandomRotation = newMinRandomRotation;
                        }

                        // Max random rotation
                        float maxRandomRotation = _brushPrefabBuffer[0].maxRandomRotation;
                        EditorGUI.showMixedValue = diff.minRandomRotation;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max rotation";
                        guiContent.tooltip = "The maximum random rotation.";
                        float newMaxRandomRotation = EditorGUILayout.DelayedFloatField(guiContent, maxRandomRotation, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var brushPrefab in _brushPrefabBuffer)
                                brushPrefab.maxRandomRotation = newMaxRandomRotation;
                        }
                    }

                    // Randomize scale
                    EditorGUILayout.Separator();
                    bool randomizeScale = _brushPrefabBuffer[0].randomizeScale;
                    EditorGUI.showMixedValue = diff.randomizeScale;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Randomize scale";
                    guiContent.tooltip = "If checked, the prefab scale will be randomized.";
                    bool newRandomizeScale = EditorGUILayout.Toggle(guiContent, randomizeScale, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.randomizeScale = newRandomizeScale;
                    }

                    if (newRandomizeScale)
                    {
                        // Min random scale
                        float minRandomScale = _brushPrefabBuffer[0].minRandomScale;
                        EditorGUI.showMixedValue = diff.minRandomScale;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min scale";
                        guiContent.tooltip = "The minimum random scale value.";
                        float newMinRandomScale = EditorGUILayout.DelayedFloatField(guiContent, minRandomScale, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var brushPrefab in _brushPrefabBuffer)
                                brushPrefab.minRandomScale = newMinRandomScale;
                        }

                        // Max random scale
                        float maxRandomScale = _brushPrefabBuffer[0].maxRandomScale;
                        EditorGUI.showMixedValue = diff.maxRandomScale;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max scale";
                        guiContent.tooltip = "The maximum random scale value.";
                        float newMaxRandomScale = EditorGUILayout.DelayedFloatField(guiContent, maxRandomScale, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var brushPrefab in _brushPrefabBuffer)
                                brushPrefab.maxRandomScale = newMaxRandomScale;
                        }
                    }

                    // Enable slope check
                    EditorGUILayout.Separator();
                    bool enableSlopeCheck = _brushPrefabBuffer[0].enableSlopeCheck;
                    EditorGUI.showMixedValue = diff.enableSlopeCheck;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Enable slope check";
                    guiContent.tooltip = "If checked, the surface steepness will be used to decide whether a prefab will be spawned or not. Note: This " + 
                        "field is used only with terrain surfaces and spherical meshes.";
                    bool newEnableSlopeCheck = EditorGUILayout.Toggle(guiContent, enableSlopeCheck, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var brushPrefab in _brushPrefabBuffer)
                            brushPrefab.enableSlopeCheck = newEnableSlopeCheck;
                    }

                    if (newEnableSlopeCheck)
                    {
                        // Min slope
                        float minSlope = _brushPrefabBuffer[0].minSlope;
                        EditorGUI.showMixedValue = diff.minSlope;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min slope";
                        guiContent.tooltip = "The minimum slope.";
                        float newMinSlope = EditorGUILayout.DelayedFloatField(guiContent, minSlope, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var brushPrefab in _brushPrefabBuffer)
                                brushPrefab.minSlope = newMinSlope;
                        }

                        // Max slope
                        float maxSlope = _brushPrefabBuffer[0].maxSlope;
                        EditorGUI.showMixedValue = diff.maxSlope;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max slope";
                        guiContent.tooltip = "The maximum slope.";
                        float newMaxSlope = EditorGUILayout.DelayedFloatField(guiContent, maxSlope, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var brushPrefab in _brushPrefabBuffer)
                                brushPrefab.maxSlope = newMaxSlope;
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
                getVisibleSelectedPrefabs(_brushPrefabBuffer);
                foreach (var brushPrefab in _brushPrefabBuffer)
                    brushPrefab.useDefaults();

                _prefabView.refreshUI();

            }, _prefabSettingsContainer);
        }

        private bool filterPrefabViewItem(UIScatterBrushPrefabItemData itemData)
        {
            return filterPrefab(itemData.brushPrefabProfile, itemData.brushPrefab);
        }

        private bool filterPrefab(ScatterBrushPrefabProfile brushPrefabProfile, ScatterBrushPrefab brushPrefab)
        {
            if (!_prefabSearchField.matchName(brushPrefab.prefabAsset.name)) return false;
            return true;
        }

        private void onActiveProfileChanged(ScatterBrushPrefabProfile newActiveProfile)
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            newActiveProfile.onPrefabsSpawnChanceChanged();
            populatePrefabView();
        }

        private void createPrefabView()
        {
            _prefabView                             = new GridView<UIScatterBrushPrefabItem, UIScatterBrushPrefabItemData>(_prefabViewState, _prefabViewContainer);
            _prefabView.selectedItemsWillBeDeleted += onSelectedPrefabItemsWillBeDeleted;
            _prefabView.canDelete                   = true;

            //_prefabView.style.setBorderWidth(1.0f);
            //_prefabView.style.borderRightWidth      = 1.0f;
            _prefabView.style.setBorderColor(Color.black);
            _prefabView.style.setMargins(UIValues.wndMargin);
            _prefabView.style.marginTop             = 3.0f;

            _prefabView.RegisterCallback<DragPerformEvent>(p =>
            {
                if (PluginDragAndDrop.initiatedByPlugin)
                {
                    if (PluginPrefabManagerUI.instance.dragAndDropInitiatedByPrefabView())
                    {
                        createScatterBrushPrefabsFromPrefabsInManager();
                        PluginDragAndDrop.endDrag();
                    }
                }
            });
        }

        private void createScatterBrushPrefabsFromPrefabsInManager()
        {
            var activeProfile = ScatterBrushPrefabProfileDb.instance.activeProfile;
            if (activeProfile != null)
            {
                PluginPrefabManagerUI.instance.getVisibleSelectedPrefabs(_pluginPrefabBuffer);
                activeProfile.createPrefabs(_pluginPrefabBuffer, _brushPrefabBuffer, false, "Creating Brush Prefabs");
                foreach (var prefab in _brushPrefabBuffer)
                    _prefabView.addItem(new UIScatterBrushPrefabItemData()
                    { brushPrefab = prefab, brushPrefabProfile = activeProfile }, true);
            }
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
            var activeProfile = ScatterBrushPrefabProfileDb.instance.activeProfile;
            if (activeProfile != null)
            {
                activeProfile.getPrefabs(_brushPrefabBuffer);
                foreach (var prefab in _brushPrefabBuffer)
                {
                    _prefabView.addItem(new UIScatterBrushPrefabItemData() { brushPrefab = prefab, brushPrefabProfile = activeProfile }, true);
                }
            }
            _prefabView.onEndBuild();
            _prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
        }

        private void onSelectedPrefabItemsWillBeDeleted(GridView<UIScatterBrushPrefabItem, UIScatterBrushPrefabItemData> gridView, List<PluginGuid> itemIds)
        {
            _brushPrefabBuffer.Clear();
            foreach (var itemId in itemIds)
                _brushPrefabBuffer.Add(_prefabView.getItemData(itemId).brushPrefab);

            ScatterBrushPrefabProfileDb.instance.activeProfile.deletePrefabs(_brushPrefabBuffer);
        }

        private void getBrushPrefabs(List<UIScatterBrushPrefabItemData> itemData, List<ScatterBrushPrefab> brushPrefabs)
        {
            brushPrefabs.Clear();
            foreach (var data in itemData)
                brushPrefabs.Add(data.brushPrefab);
        }
    }
}
#endif