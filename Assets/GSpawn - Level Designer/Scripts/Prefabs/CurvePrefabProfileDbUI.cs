#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class CurvePrefabProfileDbUI : PluginUI
    {
        [NonSerialized]
        private TwoPaneSplitView            _splitView;
        [NonSerialized]
        private VisualElement               _prefabViewContainer;
        [NonSerialized]
        private VisualElement               _prefabSettingsContainer;
        [NonSerialized]
        private Button                      _useDefaultsBtn;

        [SerializeField]
        private float                       _prefabViewContainerWidth   = 300.0f;
        [SerializeField]
        private float                       _prefabPreviewScale         = UIValues.defaultPrefabPreviewScale;
        [SerializeField]
        private Vector2                     _prefabSettingsScrollPos;
        private Slider                      _previewScaleSlider;

        private EntitySearchField                                               _prefabSearchField;
        private ProfileSelectionUI<CurvePrefabProfileDb, CurvePrefabProfile>    _profileSelectionUI;

        [SerializeField]
        private GridViewState                                       _prefabViewState;
        [NonSerialized]
        private GridView<UICurvePrefabItem, UICurvePrefabItemData>  _prefabView;

        [NonSerialized]
        private List<CurvePrefab>           _curvePrefabBuffer          = new List<CurvePrefab>();
        [NonSerialized]
        private List<PluginPrefab>          _pluginPrefabBuffer         = new List<PluginPrefab>();
        [NonSerialized]
        private List<UICurvePrefabItemData> _curvePrefabItemDataBuffer  = new List<UICurvePrefabItemData>();

        public float                            prefabPreviewScale      { get { return _prefabPreviewScale; } set { UndoEx.record(this); _prefabPreviewScale = Mathf.Clamp(value, UIValues.minPrefabPreviewScale, UIValues.maxPrefabPreviewScale); EditorUtility.SetDirty(this); } }
       
        public static CurvePrefabProfileDbUI    instance                { get { return CurvePrefabProfileDb.instance.ui; } }

        public void getVisibleSelectedPrefabs(List<CurvePrefab> curvePrefabs)
        {
            curvePrefabs.Clear();
            if (_prefabView != null)
            {
                _prefabView.getVisibleSelectedItemData(_curvePrefabItemDataBuffer);
                foreach (var itemData in _curvePrefabItemDataBuffer)
                    curvePrefabs.Add(itemData.curvePrefab);
            }
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            if (_prefabView != null) _prefabView.deleteItems(itemData => itemData.curvePrefab.prefabAsset == prefabAsset);
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow = 1.0f;
            _profileSelectionUI             = new ProfileSelectionUI<CurvePrefabProfileDb, CurvePrefabProfile>();
            _profileSelectionUI.build(CurvePrefabProfileDb.instance, "curve spawn prefabs", contentContainer);

            createTopToolbar();
            createSearchToolbar();

            _splitView                      = new TwoPaneSplitView();
            _splitView.orientation          = TwoPaneSplitViewOrientation.Horizontal;
            contentContainer.Add(_splitView);

            _prefabViewContainer                      = new VisualElement();
            _prefabViewContainer.style.flexShrink     = 0.0f;
            _prefabViewContainer.style.flexGrow       = 1.0f;
            _splitView.Add(_prefabViewContainer);

            _prefabSettingsContainer                    = new VisualElement();
            _prefabSettingsContainer.style.flexGrow     = 1.0f;
            _splitView.Add(_prefabSettingsContainer);
            _splitView.fixedPaneIndex                   = _splitView.IndexOf(_prefabViewContainer);
            _splitView.fixedPaneInitialDimension        = _prefabViewContainerWidth;

            _prefabViewContainer.RegisterCallback<MouseDownEvent>(p =>
            {
                if (p.button == (int)MouseButton.RightMouse)
                {
                    getVisibleSelectedPrefabs(_curvePrefabBuffer);

                    PluginGenericMenu menu = new PluginGenericMenu();
                    menu.addItem(GenericMenuItemCategory.VisiblePrefabs, GenericMenuItemId.HighlightSelectedInManager, _curvePrefabBuffer.Count != 0,
                    () =>
                    {
                        CurvePrefab.getPluginPrefabs(_curvePrefabBuffer, _pluginPrefabBuffer);
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
            CurvePrefabProfileDb.instance.activeProfileChanged += onActiveProfileChanged;

            if (_prefabViewState == null)
            {
                _prefabViewState        = ScriptableObject.CreateInstance<GridViewState>();
                _prefabViewState.name   = GetType().Name + "_PrefabViewState";
                AssetDbEx.addObjectToAsset(_prefabViewState, CurvePrefabProfileDb.instance);
            }
        }

        protected override void onDisabled()
        {
            EditorApplication.update -= onEditorUpdate;
            CurvePrefabProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
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

            var resetPreviewsBtn        = UI.createSmallResetPrefabPreviewsToolbarButton(toolbar);
            resetPreviewsBtn.clicked    += () => { CurvePrefabProfileDb.instance.activeProfile.resetPrefabPreviews(); };

            var loadPrefabsBtn          = UI.createSmallLoadPrefabsButton(toolbar);
            loadPrefabsBtn.clicked      += () => { createCurvePrefabsFromPrefabsInManager(); };
        }

        private void createSearchToolbar()
        {
            var searchToolbar               = new Toolbar();
            searchToolbar.style.flexShrink  = 0.0f;
            contentContainer.Add(searchToolbar);

            _prefabSearchField = new EntitySearchField(searchToolbar,
                (nameList) => { CurvePrefabProfileDb.instance.activeProfile.getPrefabNames(nameList); },
                (name) => { _prefabView.filterItems(filterPrefabViewItem); });
        }

        private void createPrefabSettingsControls()
        {
            const float labelWidth          = 170.0f;
            IMGUIContainer imGUIContainer   = UI.createIMGUIContainer(_prefabSettingsContainer);
            imGUIContainer.style.flexGrow   = 1.0f;
            imGUIContainer.style.marginTop  = 3.0f;
            imGUIContainer.style.marginLeft = 3.0f;
            imGUIContainer.onGUIHandler     = () =>
            {
                getVisibleSelectedPrefabs(_curvePrefabBuffer);
                _useDefaultsBtn.setDisplayVisible(_curvePrefabBuffer.Count != 0);

                if (_curvePrefabBuffer.Count == 0)
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
                    var diff = CurvePrefab.checkDiff(_curvePrefabBuffer);

                    #pragma warning disable 0612
                    // Used
                    bool used = _curvePrefabBuffer[0].used;
                    EditorGUI.showMixedValue = diff.used;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Used";
                    guiContent.tooltip = "If checked, the prefab will be used when populating a curve. Otherwise, it will be ignored.";
                    bool newUsed = EditorGUILayout.Toggle(guiContent, used, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.used = newUsed;

                        CurvePrefabProfileDb.instance.activeProfile.onPrefabsUsedStateChanged();

                        foreach (var selectedItemId in _prefabViewState.selectedItems)
                            _prefabView.refreshItemUI(selectedItemId);

                        onPrefabDataChanged(ObjectSpawnCurveRefreshReason.CurvePrefabUsedStateChanged);
                    }

                    // Spawn chance
                    float spawnChance = _curvePrefabBuffer[0].spawnChance;
                    EditorGUI.showMixedValue = diff.spawnChance;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Spawn chance";
                    guiContent.tooltip = "The prefab's chance to be spawned along a curve.";
                    float newSpawnChance = EditorGUILayout.FloatField(guiContent, spawnChance, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.spawnChance = newSpawnChance;

                        CurvePrefabProfileDb.instance.activeProfile.onPrefabsSpawnChanceChanged();
                        onPrefabDataChanged(ObjectSpawnCurveRefreshReason.CurvePrefabSpawnChanceChanged);
                    }

                    // Align axes
                    EditorGUILayout.Separator();
                    bool alignAxes = _curvePrefabBuffer[0].alignAxes;
                    EditorGUI.showMixedValue = diff.alignAxes;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Align axes";
                    guiContent.tooltip = "If this is checked, the prefab's up and forward axes will be aligned to the curve's up and forward axes respectively.";
                    bool newAlignAxes = EditorGUILayout.Toggle(guiContent, alignAxes, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.alignAxes = newAlignAxes;

                        onPrefabDataChanged();
                    }

                    // Up alignment axis
                    FlexiAxis upAlignmentAxis = diff.upAxis ? FlexiAxis.UIMixed : _curvePrefabBuffer[0].upAxis;
                    EditorGUI.showMixedValue = diff.upAxis;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Up axis";
                    guiContent.tooltip = "The prefab axis that is mapped to the curve up axis.";
                    FlexiAxis newUpAlignmentAxis = (FlexiAxis)EditorGUILayout.EnumPopup(guiContent, upAlignmentAxis, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.upAxis = newUpAlignmentAxis;

                        onPrefabDataChanged();
                    }

                    // Invert up alignment axis
                    bool invertUpAlignmentAxis = _curvePrefabBuffer[0].invertUpAxis;
                    EditorGUI.showMixedValue = diff.invertUpAxis;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Invert axis";
                    guiContent.tooltip = "If this is checked, the up axis will be inverted.";
                    bool newInvertUpAlignmentAxis = EditorGUILayout.Toggle(guiContent, invertUpAlignmentAxis, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.invertUpAxis = newInvertUpAlignmentAxis;

                        onPrefabDataChanged();
                    }

                    // Align up axis when projected
                    bool alignUpAxisWhenProjected = _curvePrefabBuffer[0].alignUpAxisWhenProjected;
                    EditorGUI.showMixedValue = diff.alignUpAxisWhenProjected;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Align up axis when projected";
                    guiContent.tooltip = "If this is checked, the up axis will be aligned to the projection surface normal if projection is used.";
                    bool newAlignUpAxisWhenProjected = EditorGUILayout.Toggle(guiContent, alignUpAxisWhenProjected, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.alignUpAxisWhenProjected = newAlignUpAxisWhenProjected;

                        onPrefabDataChanged();
                    }

                    // Forward alignment axis
                    FlexiAxis forwardAlignmentAxis = diff.forwardAxis ? FlexiAxis.UIMixed : _curvePrefabBuffer[0].forwardAxis;
                    EditorGUI.showMixedValue = diff.forwardAxis;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Forward axis";
                    guiContent.tooltip = "The prefab axis that is mapped to the curve's forward direction.";
                    FlexiAxis newForwardAlignmentAxis = (FlexiAxis)EditorGUILayout.EnumPopup(guiContent, forwardAlignmentAxis, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.forwardAxis = newForwardAlignmentAxis;

                        onPrefabDataChanged();
                    }

                    // Invert forward alignment axis
                    bool invertForwardAlignmentAxis = _curvePrefabBuffer[0].invertForwardAxis;
                    EditorGUI.showMixedValue = diff.invertForwardAxis;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Invert axis";
                    guiContent.tooltip = "If this is checked, the forward axis will be inverted.";
                    bool newInvertForwardAlignmentAxis = EditorGUILayout.Toggle(guiContent, invertForwardAlignmentAxis, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.invertForwardAxis = newInvertForwardAlignmentAxis;

                        onPrefabDataChanged();
                    }

                    // Jitter mode
                    EditorGUILayout.Separator();
                    CurvePrefabJitterMode jitterMode = diff.jitterMode ? CurvePrefabJitterMode.UIMixed : _curvePrefabBuffer[0].jitterMode;
                    EditorGUI.showMixedValue = diff.jitterMode;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Jitter mode";
                    guiContent.tooltip = "Allows you to specify the jitter mode. Jittering applies an offset to the prefab position along the curve's side axis.";
                    CurvePrefabJitterMode newJitterMode = (CurvePrefabJitterMode)EditorGUILayout.EnumPopup(guiContent, jitterMode, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.jitterMode = newJitterMode;

                        onPrefabDataChanged();
                    }

                    // Jitter
                    if (newJitterMode == CurvePrefabJitterMode.Constant)
                    {
                        float jitter = _curvePrefabBuffer[0].jitter;
                        EditorGUI.showMixedValue = diff.jitter;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Jitter";
                        guiContent.tooltip = "The constant jitter amount.";
                        float newJitter = EditorGUILayout.FloatField(guiContent, jitter, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.jitter = newJitter;

                            onPrefabDataChanged();
                        }
                    }
                    else
                    if (newJitterMode == CurvePrefabJitterMode.Random)
                    {
                        // Min random jitter
                        float minRandomJitter = _curvePrefabBuffer[0].minRandomJitter;
                        EditorGUI.showMixedValue = diff.minRandomJitter;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min jitter";
                        guiContent.tooltip = "The minimum random jitter.";
                        float newMinJitter = EditorGUILayout.FloatField(guiContent, minRandomJitter, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.minRandomJitter = newMinJitter;

                            onPrefabDataChanged();
                        }

                        // Max random jitter
                        float maxRandomJitter = _curvePrefabBuffer[0].maxRandomJitter;
                        EditorGUI.showMixedValue = diff.maxRandomJitter;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max jitter";
                        guiContent.tooltip = "The maximum random jitter.";
                        float newMaxJitter = EditorGUILayout.FloatField(guiContent, maxRandomJitter, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.maxRandomJitter = newMaxJitter;

                            onPrefabDataChanged();
                        }
                    }

                    // Randomize rotation around forward axis
                    EditorGUILayout.Separator();
                    bool randFWAxisRotation = _curvePrefabBuffer[0].randomizeForwardAxisRotation;
                    EditorGUI.showMixedValue = diff.randomizeForwardAxisRotation;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Randomize fw axis rotation";
                    guiContent.tooltip = "If this is checked, the prefab will have its rotation randomized around its forward axis. Note: This only works when 'Align up axis when projected' is turned off.";
                    bool newRandFWAxisRotation = EditorGUILayout.Toggle(guiContent, randFWAxisRotation, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.randomizeForwardAxisRotation = newRandFWAxisRotation;

                        onPrefabDataChanged();
                    }

                    if (newRandFWAxisRotation)
                    {
                        // Min random FW axis rotation
                        float minFWAxisRotation = _curvePrefabBuffer[0].minRandomForwardAxisRotation;
                        EditorGUI.showMixedValue = diff.minRandomForwardAxisRotation;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min rotation";
                        guiContent.tooltip = "The minimum rotation to apply around the forward axis.";
                        float newMinFWAxisRotation = EditorGUILayout.DelayedFloatField(guiContent, minFWAxisRotation, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.minRandomForwardAxisRotation = newMinFWAxisRotation;

                            onPrefabDataChanged();
                        }

                        // Max random FW axis rotation
                        float maxFWAxisRotation = _curvePrefabBuffer[0].maxRandomForwardAxisRotation;
                        EditorGUI.showMixedValue = diff.maxRandomForwardAxisRotation;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max rotation";
                        guiContent.tooltip = "The maximum rotation to apply around the forward axis.";
                        float newMaxFWAxisRotation = EditorGUILayout.DelayedFloatField(guiContent, maxFWAxisRotation, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.maxRandomForwardAxisRotation = newMaxFWAxisRotation;

                            onPrefabDataChanged();
                        }
                    }

                    // Randomize rotation around up axis
                    bool randUpAxisRotation = _curvePrefabBuffer[0].randomizeUpAxisRotation;
                    EditorGUI.showMixedValue = diff.randomizeUpAxisRotation;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Randomize up axis rotation";
                    guiContent.tooltip = "If this is checked, the prefab will have its rotation randomized around its up axis.";
                    bool newRandUpAxisRotation = EditorGUILayout.Toggle(guiContent, randUpAxisRotation, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.randomizeUpAxisRotation = newRandUpAxisRotation;

                        onPrefabDataChanged();
                    }

                    if (newRandUpAxisRotation)
                    {
                        // Min random up axis rotation
                        float minUpAxisRotation = _curvePrefabBuffer[0].minRandomUpAxisRotation;
                        EditorGUI.showMixedValue = diff.minRandomUpAxisRotation;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min rotation";
                        guiContent.tooltip = "The minimum rotation to apply around the up axis.";
                        float newMinUpAxisRotation = EditorGUILayout.DelayedFloatField(guiContent, minUpAxisRotation, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.minRandomUpAxisRotation = newMinUpAxisRotation;

                            onPrefabDataChanged();
                        }

                        // Max random up axis rotation
                        float maxUpAxisRotation = _curvePrefabBuffer[0].maxRandomUpAxisRotation;
                        EditorGUI.showMixedValue = diff.maxRandomUpAxisRotation;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max rotation";
                        guiContent.tooltip = "The maximum rotation to apply around the up axis.";
                        float newMaxUpAxisRotation = EditorGUILayout.DelayedFloatField(guiContent, maxUpAxisRotation, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.maxRandomUpAxisRotation = newMaxUpAxisRotation;

                            onPrefabDataChanged();
                        }
                    }

                    // Up axis offset mode
                    EditorGUILayout.Separator();
                    CurvePrefabUpAxisOffsetMode upAxisOffsetMode = diff.upAxisOffsetMode ? CurvePrefabUpAxisOffsetMode.UIMixed : _curvePrefabBuffer[0].upAxisOffsetMode;
                    EditorGUI.showMixedValue = diff.upAxisOffsetMode;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Up axis offset mode";
                    guiContent.tooltip = "Allows you to select the up axis offset mode (i.e. constant or random). ";
                    CurvePrefabUpAxisOffsetMode newUpAxisOffsetMode = (CurvePrefabUpAxisOffsetMode)EditorGUILayout.EnumPopup(guiContent, upAxisOffsetMode, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.upAxisOffsetMode = newUpAxisOffsetMode;

                        onPrefabDataChanged();
                    }

                    if (newUpAxisOffsetMode == CurvePrefabUpAxisOffsetMode.Constant)
                    {
                        // Constant up axis offset
                        float upAxisOffset = _curvePrefabBuffer[0].upAxisOffset;
                        EditorGUI.showMixedValue = diff.upAxisOffset;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Offset";
                        guiContent.tooltip = "The offset that will be applied along the up axis.";
                        float newUpAxisOffset = EditorGUILayout.FloatField(guiContent, upAxisOffset, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.upAxisOffset = newUpAxisOffset;

                            onPrefabDataChanged();
                        }
                    }
                    else
                    if (newUpAxisOffsetMode == CurvePrefabUpAxisOffsetMode.Random)
                    {
                        // Min random up axis offset
                        float minRandomUpAxisOffset = _curvePrefabBuffer[0].minRandomUpAxisOffset;
                        EditorGUI.showMixedValue = diff.minRandomUpAxisOffset;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min offset";
                        guiContent.tooltip = "The minimum offset to apply along the up axis.";
                        float newMinRandomUpAxisOffset = EditorGUILayout.FloatField(guiContent, minRandomUpAxisOffset, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.minRandomUpAxisOffset = newMinRandomUpAxisOffset;

                            onPrefabDataChanged();
                        }

                        // Max random up axis offset
                        float maxRandomUpAxisOffset = _curvePrefabBuffer[0].maxRandomUpAxisOffset;
                        EditorGUI.showMixedValue = diff.maxRandomUpAxisOffset;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max offset";
                        guiContent.tooltip = "The maximum offset to apply along the up axis.";
                        float newMaxRandomUpAxisOffset = EditorGUILayout.FloatField(guiContent, maxRandomUpAxisOffset, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.maxRandomUpAxisOffset = newMaxRandomUpAxisOffset;

                            onPrefabDataChanged();
                        }
                    }

                    // Randomize scale
                    EditorGUILayout.Separator();
                    bool randomizeScale = _curvePrefabBuffer[0].randomizeScale;
                    EditorGUI.showMixedValue = diff.randomizeScale;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Randomize scale";
                    guiContent.tooltip = "If checked, the prefab scale will be randomized.";
                    bool newRandomizeScale = EditorGUILayout.Toggle(guiContent, randomizeScale, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var curvePrefab in _curvePrefabBuffer)
                            curvePrefab.randomizeScale = newRandomizeScale;

                        onPrefabDataChanged();
                    }

                    if (newRandomizeScale)
                    {
                        // Min random scale
                        float minRandomScale = _curvePrefabBuffer[0].minRandomScale;
                        EditorGUI.showMixedValue = diff.minRandomScale;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min scale";
                        guiContent.tooltip = "The minimum random scale value.";
                        float newMinRandomScale = EditorGUILayout.DelayedFloatField(guiContent, minRandomScale, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.minRandomScale = newMinRandomScale;

                            onPrefabDataChanged();
                        }

                        // Max random scale
                        float maxRandomScale = _curvePrefabBuffer[0].maxRandomScale;
                        EditorGUI.showMixedValue = diff.maxRandomScale;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max scale";
                        guiContent.tooltip = "The maximum random scale value.";
                        float newMaxRandomScale = EditorGUILayout.DelayedFloatField(guiContent, maxRandomScale, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var curvePrefab in _curvePrefabBuffer)
                                curvePrefab.maxRandomScale = newMaxRandomScale;

                            onPrefabDataChanged();
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
                getVisibleSelectedPrefabs(_curvePrefabBuffer);
                foreach (var curvePrefab in _curvePrefabBuffer)
                    curvePrefab.useDefaults();

                _prefabView.refreshUI();

                onPrefabDataChanged();

            }, _prefabSettingsContainer);
        }

        private bool filterPrefabViewItem(UICurvePrefabItemData itemData)
        {
            return filterPrefab(itemData.curvePrefabProfile, itemData.curvePrefab);
        }

        private bool filterPrefab(CurvePrefabProfile curvePrefabProfile, CurvePrefab curvePrefab)
        {
            if (!_prefabSearchField.matchName(curvePrefab.prefabAsset.name)) return false;
            return true;
        }

        private void onActiveProfileChanged(CurvePrefabProfile newActiveProfile)
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            newActiveProfile.onPrefabsSpawnChanceChanged();
            populatePrefabView();
        }

        private void createPrefabView()
        {
            _prefabView                             = new GridView<UICurvePrefabItem, UICurvePrefabItemData>(_prefabViewState, _prefabViewContainer);
            _prefabView.selectedItemsWillBeDeleted  += onSelectedPrefabItemsWillBeDeleted;
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
                        createCurvePrefabsFromPrefabsInManager();
                        PluginDragAndDrop.endDrag();
                    }
                }
            });
        }

        private void createCurvePrefabsFromPrefabsInManager()
        {
            var activeProfile = CurvePrefabProfileDb.instance.activeProfile;
            if (activeProfile != null)
            {
                PluginPrefabManagerUI.instance.getVisibleSelectedPrefabs(_pluginPrefabBuffer);
                activeProfile.createPrefabs(_pluginPrefabBuffer, _curvePrefabBuffer, false, "Creating Curve Prefabs");
                foreach (var prefab in _curvePrefabBuffer)
                    _prefabView.addItem(new UICurvePrefabItemData()
                    { curvePrefab = prefab, curvePrefabProfile = activeProfile }, true);

                onPrefabDataChanged(ObjectSpawnCurveRefreshReason.Refresh);
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
            var activeProfile = CurvePrefabProfileDb.instance.activeProfile;
            if (activeProfile != null)
            {
                activeProfile.getPrefabs(_curvePrefabBuffer);
                foreach (var prefab in _curvePrefabBuffer)
                {
                    _prefabView.addItem(new UICurvePrefabItemData() { curvePrefab = prefab, curvePrefabProfile = activeProfile }, true);
                }
            }
            _prefabView.onEndBuild();
            _prefabView.setImageSize(Vector2Ex.create(PrefabPreviewFactory.previewSize * prefabPreviewScale));
        }

        private void onSelectedPrefabItemsWillBeDeleted(GridView<UICurvePrefabItem, UICurvePrefabItemData> gridView, List<PluginGuid> itemIds)
        {
            _curvePrefabBuffer.Clear();
            foreach (var itemId in itemIds)
                _curvePrefabBuffer.Add(_prefabView.getItemData(itemId).curvePrefab);

            CurvePrefabProfileDb.instance.activeProfile.deletePrefabs(_curvePrefabBuffer);

            onPrefabDataChanged(ObjectSpawnCurveRefreshReason.CurvePrefabsDeleted);
        }

        private void onPrefabDataChanged(ObjectSpawnCurveRefreshReason refreshReason = ObjectSpawnCurveRefreshReason.Other)
        {
            if (ObjectSpawnPrefs.instance.refreshCurvesWhenPrefabDataChanges)
            {
                int numCurves = ObjectSpawnCurveDb.instance.numCurves;
                for (int i = 0; i < numCurves; ++i)
                {
                    var curve = ObjectSpawnCurveDb.instance.getCurve(i);
                    if (curve.settings.curvePrefabProfile == CurvePrefabProfileDb.instance.activeProfile)
                        curve.refresh(refreshReason);
                }
            }
        }
    }
}
#endif