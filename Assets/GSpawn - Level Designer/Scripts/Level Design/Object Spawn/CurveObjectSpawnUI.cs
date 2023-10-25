#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class CurveObjectSpawnUI : PluginUI
    {
        [NonSerialized]
        private ProfileSelectionUI<CurveObjectSpawnSettingsProfileDb, CurveObjectSpawnSettingsProfile>  _curveCreationSettingsProfileSelectionUI;

        [SerializeField]
        private ListViewState                                           _curveViewState;
        [NonSerialized]
        private ListView<UIObjectSpawnCurveItem, ObjectSpawnCurve>      _curveView;
        [NonSerialized]
        private EntitySearchField                                       _curveSearchField;
        [NonSerialized]
        private Button                                                  _useDefaultsBtn;
        [SerializeField]
        private UISection                                               _curveCreationSettingsSection;
        [SerializeField]
        private UISection                                               _curvesSection;
        [NonSerialized]
        private VisualElement                                           _createCurveContainer;
        [NonSerialized]
        private bool                                                    _deleteCurveObjects;
        [NonSerialized]
        private List<ObjectSpawnCurve>                                  _curveBuffer                = new List<ObjectSpawnCurve>();
        [NonSerialized]
        private List<PluginGuid>                                        _curveIdBuffer              = new List<PluginGuid>();
        [NonSerialized]
        private List<CurveObjectSpawnSettings>                          _curveSpawnSettingsBuffer   = new List<CurveObjectSpawnSettings>();

        public static CurveObjectSpawnUI                                instance                    { get { return GSpawn.active.curveObjectSpawnUI; } }

        public void getVisibleSelectedCurves(List<ObjectSpawnCurve> curves)
        {
            if (_curveView == null) return;

            curves.Clear();
            var selectedCurves = new List<ObjectSpawnCurve>();
            _curveView.getVisibleSelectedItemData(selectedCurves);

            foreach (var itemData in selectedCurves)
                curves.Add(itemData);
        }

        public void getSelectedCurves(List<ObjectSpawnCurve> curves)
        {
            if (_curveView == null) return;

            curves.Clear();
            var selectedCurves = new List<ObjectSpawnCurve>();
            _curveView.getSelectedItemData(selectedCurves);

            foreach (var itemData in selectedCurves)
                curves.Add(itemData);
        }

        public void setSelectedCurve(ObjectSpawnCurve curve)
        {
            if (_curveView == null) return;

            _curveView.setAllItemsSelected(false, false, false);
            _curveView.setItemSelected(curve.guid, true, false);
        }

        public void setSelectedCurves(List<ObjectSpawnCurve> curves)
        {
            if (_curveView == null) return;

            _curveView.setAllItemsSelected(false, false, false);
            UIObjectSpawnCurveItem.getItemIds(curves, _curveIdBuffer);
            _curveView.setItemsSelected(_curveIdBuffer, true, false, false);
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow = 1.0f;

            _curveCreationSettingsSection.build("Curve Creation", TexturePool.instance.settings, true, contentContainer);

            _curveCreationSettingsProfileSelectionUI = new ProfileSelectionUI<CurveObjectSpawnSettingsProfileDb, CurveObjectSpawnSettingsProfile>();
            _curveCreationSettingsProfileSelectionUI.build(CurveObjectSpawnSettingsProfileDb.instance, "curve object spawn settings", _curveCreationSettingsSection.contentContainer);
            createCreateCurveUI(_curveCreationSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer);
            _curvesSection.build("Curves", TexturePool.instance.curveSpawn, true, contentContainer);
            createSearchToolbar(_curvesSection.contentContainer);
            createCurveViewToolbar(_curvesSection.contentContainer);
            createCurveView(_curvesSection.contentContainer);
            populateCurveView();
            createCurveSettingsControls(_curvesSection.contentContainer);
        }

        protected override void onRefresh()
        {
            if (_curveCreationSettingsProfileSelectionUI != null)
                _curveCreationSettingsProfileSelectionUI.refresh();

            createCreateCurveUI(_curveCreationSettingsSection.contentContainer);
            populateCurveView();
        }

        protected override void onEnabled()
        {
            if (_curveViewState == null)
            {
                _curveViewState = ScriptableObject.CreateInstance<ListViewState>();
                _curveViewState.name = GetType().Name + "_CurveViewState";
            }
            if (_curveCreationSettingsSection == null)
                _curveCreationSettingsSection = ScriptableObject.CreateInstance<UISection>();
            if (_curvesSection == null)
                _curvesSection = ScriptableObject.CreateInstance<UISection>();

            if (FileSystem.folderExists(PluginFolders.curveObjectSpawnSettingsProfiles))
                CurveObjectSpawnSettingsProfileDb.instance.activeProfileChanged += onActiveCurveCreationSettingsProfileChanged;
        }

        protected override void onDisabled()
        {
            if (FileSystem.folderExists(PluginFolders.curveObjectSpawnSettingsProfiles))
                CurveObjectSpawnSettingsProfileDb.instance.activeProfileChanged -= onActiveCurveCreationSettingsProfileChanged;
        }

        protected override void onUndoRedo()
        {
            if (_curveCreationSettingsProfileSelectionUI != null)
                _curveCreationSettingsProfileSelectionUI.refresh();

            populateCurveView();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_curvesSection);
            ScriptableObjectEx.destroyImmediate(_curveCreationSettingsSection);
            ScriptableObjectEx.destroyImmediate(_curveViewState);
        }

        private void createCreateCurveUI(VisualElement parent)
        {
            if (_createCurveContainer != null &&
                parent.Contains(_createCurveContainer))
            {
                parent.Remove(_createCurveContainer);
            }

            _createCurveContainer = new VisualElement();
            parent.Add(_createCurveContainer);
            CurveObjectSpawnSettingsProfileDb.instance.activeProfile.settings.buildUI(_createCurveContainer);

            var createCurveBtnAndField = new VisualElement();
            _createCurveContainer.Add(createCurveBtnAndField);
            createCurveBtnAndField.style.flexDirection = FlexDirection.Row;

            TextField curveObjectNameField      = new TextField();
            curveObjectNameField.style.flexGrow = 1.0f;
            Button createNewCurveBtn            = new Button();
            createNewCurveBtn.text              = "Create curve";
            createNewCurveBtn.tooltip           = "Create a new spawn curve by placing control points in the scene.";
            createNewCurveBtn.style.width       = UIValues.useDefaultsButtonWidth;
            createNewCurveBtn.clicked           += () => { ObjectSpawn.instance.curveObjectSpawn.createNewCurve(curveObjectNameField.text); };
            createCurveBtnAndField.Add(createNewCurveBtn);
            createCurveBtnAndField.Add(curveObjectNameField);
        }

        private void onActiveCurveCreationSettingsProfileChanged(CurveObjectSpawnSettingsProfile newActiveProfile)
        {
            onRefresh();
        }

        private void createSearchToolbar(VisualElement parent)
        {
            var toolbar                 = UI.createStylizedToolbar(parent);

            _curveSearchField           = new EntitySearchField(toolbar, (entityNames) =>
            { ObjectSpawnCurveDb.instance.getCurveNames(entityNames, null); },
            (name) => { _curveView.filterItems(filterCurve); });
        }

        private bool filterCurve(ObjectSpawnCurve curve)
        {
            if (!_curveSearchField.matchName(curve.curveName)) return false;
            return true;
        }

        private void createCurveViewToolbar(VisualElement parent)
        {
            var toolbar     = UI.createStylizedToolbar(parent);

            var btn         = UI.createToolbarButton(TexturePool.instance.refresh, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            btn.tooltip     = "Refresh selected curves.";
            btn.clicked     += () =>
            {
                if (_curveView != null)
                {
                    getVisibleSelectedCurves(_curveBuffer);
                    foreach (var curve in _curveBuffer)
                        curve.refresh(ObjectSpawnCurveRefreshReason.Refresh);
                }
            };
            UI.useDefaultMargins(btn);

            btn             = UI.createToolbarButton(TexturePool.instance.sync, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            btn.tooltip     = "Sync selected curve settings with curve creation settings.";
            btn.clicked     += () =>
            {
                if (_curveView != null)
                {
                    getVisibleSelectedCurves(_curveBuffer);
                    foreach (var curve in _curveBuffer)
                    {
                        curve.settings.copy(CurveObjectSpawnSettingsProfileDb.instance.activeProfile.settings);
                    }
                }
            };
            UI.useDefaultMargins(btn);

            btn             = UI.createToolbarButton(TexturePool.instance.delete, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            btn.tooltip     = "Delete selected curves.";
            btn.clicked     += () => 
            {
                if (_curveView != null) _curveView.deleteSelectedItems();
            };
            UI.useDefaultMargins(btn);
        }

        private void createCurveView(VisualElement parent)
        {
            _curveView                              = new ListView<UIObjectSpawnCurveItem, ObjectSpawnCurve>(_curveViewState, parent);
            _curveView.canDelete                    = true;
            _curveView.canRenameItems               = true;
            _curveView.canMultiSelect               = true;

            _curveView.canDeleteSelectedItems       += onCanDeleteSelectedItems;
            _curveView.selectedItemsWillBeDeleted   += onSelectedCurveItemsWillBeDeleted;
            _curveView.selectionChanged             += onSelectionChanged;
            _curveView.selectionDeleted             += onSelectionDeleted;

            _curveView.style.setBorderWidth(1.0f);
            _curveView.style.setBorderColor(UIValues.listViewBorderColor);
            _curveView.style.flexGrow               = 1.0f;
            _curveView.style.height                 = 200.0f;
        }

        private void createCurveSettingsControls(VisualElement parent)
        {
            const float labelWidth          = 130.0f;
            IMGUIContainer imGUIContainer   = UI.createIMGUIContainer(parent);
            imGUIContainer.style.flexGrow   = 1.0f;
            imGUIContainer.style.marginTop  = 1.0f;
            imGUIContainer.onGUIHandler     = () =>
            {
                getVisibleSelectedCurves(_curveBuffer);
                _curveSpawnSettingsBuffer.Clear();
                foreach (var curve in _curveBuffer)
                    _curveSpawnSettingsBuffer.Add(curve.settings);

                if (_curveSpawnSettingsBuffer.Count == 0)
                {
                    if (_useDefaultsBtn.isDisplayVisible()) _useDefaultsBtn.setDisplayVisible(false);
                    imGUIContainer.style.marginLeft = 0.0f;
                    EditorGUILayout.HelpBox("No curves selected. Select curves in order to change their settings.", MessageType.Info);
                    return;
                }
                else
                {
                    if (!_useDefaultsBtn.isDisplayVisible()) _useDefaultsBtn.setDisplayVisible(true);
                    imGUIContainer.style.marginLeft = 3.0f;

                    var guiContent = new GUIContent();
                    EditorUIEx.saveLabelWidth();
                    EditorUIEx.saveShowMixedValue();
                    EditorGUIUtility.labelWidth = labelWidth;
                    var diff = CurveObjectSpawnSettings.checkDiff(_curveSpawnSettingsBuffer);

                    #pragma warning disable 0612
                    // Curve prefab profile
                    EditorGUI.BeginChangeCheck();
                    string profileName = diff.curvePrefabProfileName ? string.Empty : _curveSpawnSettingsBuffer[0].curvePrefabProfileName;
                    string newProfileName = EditorUIEx.profileNameSelectionField<CurvePrefabProfileDb, CurvePrefabProfile>
                        (CurvePrefabProfileDb.instance, "Curve prefab profile", labelWidth, profileName, diff.curvePrefabProfileName);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.curvePrefabProfileName = newProfileName;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.CurvePrefabProfileChanged);
                    }

                    // Curve up axis
/*
                    EditorGUILayout.Separator();
                    CurveObjectSpawnUpAxis curveUpAxis = diff.curveUpAxis ? CurveObjectSpawnUpAxis.UIMixed : _curveSpawnSettingsBuffer[0].curveUpAxis;
                    EditorGUI.showMixedValue = diff.curveUpAxis;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Up axis";
                    guiContent.tooltip = "The curve up axis used when spawning objects. It specifies which way is up.";
                    CurveObjectSpawnUpAxis newCurveUpAxis = (CurveObjectSpawnUpAxis)EditorGUILayout.EnumPopup(guiContent, curveUpAxis, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.curveUpAxis = newCurveUpAxis;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }*/

                    // Invert up axis
/*
                    bool invertUpAxis = _curveSpawnSettingsBuffer[0].invertUpAxis;
                    EditorGUI.showMixedValue = diff.invertUpAxis;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Invert axis";
                    guiContent.tooltip = "If this is checked, the up axis will be inverted.";
                    bool newInvertAxis = EditorGUILayout.Toggle(guiContent, invertUpAxis, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.invertUpAxis = newInvertAxis;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }*/

                    // Avoid overlaps
                    bool avoidOverlaps = _curveSpawnSettingsBuffer[0].avoidOverlaps;
                    EditorGUI.showMixedValue = diff.avoidOverlaps;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Avoid overlaps";
                    guiContent.tooltip = "If this is checked, no objects will be created in places where they would overlap with already existing objects. " + 
                                         "Note: The checks are performed against objects that are not part of the curve. Objects that are part of the curve, are not affected and may overlap.";
                    bool newAvoidOverlaps = EditorGUILayout.Toggle(guiContent, avoidOverlaps, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.avoidOverlaps = newAvoidOverlaps;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }

                    // Volumeless object size
                    float vlmsObjectSize = _curveSpawnSettingsBuffer[0].volumlessObjectSize;
                    EditorGUI.showMixedValue = diff.volumelessObjectSize;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Volumeless object size";
                    guiContent.tooltip = "The size that should be used for objects that don't have a volume.";
                    float newVlmsObjectSize = EditorGUILayout.FloatField(guiContent, vlmsObjectSize, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.volumlessObjectSize = newVlmsObjectSize;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }

                    // Step
/*
                    float step = _curveSpawnSettingsBuffer[0].step;
                    EditorGUI.showMixedValue = diff.step;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Step";
                    guiContent.tooltip = "When spawning objects along a curve, the curve will be approximated " +
                        "by taking sample points using this step value. The smaller the value the better the approximation.";
                    float newStep = EditorGUILayout.FloatField(guiContent, step, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.step = newStep;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }*/

                    // Object skip chance
                    float objectSkipChance = _curveSpawnSettingsBuffer[0].objectSkipChance;
                    EditorGUI.showMixedValue = diff.objectSkipChance;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Object skip chance";
                    guiContent.tooltip = "Specifies the probability of an object being skipped during the spawn process.";
                    float newObjectSkipChance = EditorGUILayout.FloatField(guiContent, objectSkipChance, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.objectSkipChance = newObjectSkipChance;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }

                    // Curve padding mode
                    EditorGUILayout.Separator();
                    CurveObjectSpawnPaddingMode curvePaddingMode = diff.paddingMode ? CurveObjectSpawnPaddingMode.UIMixed : _curveSpawnSettingsBuffer[0].paddingMode;
                    EditorGUI.showMixedValue = diff.paddingMode;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Padding mode";
                    guiContent.tooltip = "Allows you to select the padding mode (i.e. constant or random). " +
                        "Padding represents the distance between successive objects in the curve.";
                    CurveObjectSpawnPaddingMode newCurvePaddingMode = (CurveObjectSpawnPaddingMode)EditorGUILayout.EnumPopup(guiContent, curvePaddingMode, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.paddingMode = newCurvePaddingMode;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }

                    if (newCurvePaddingMode == CurveObjectSpawnPaddingMode.Constant)
                    {
                        // Padding
                        float padding = _curveSpawnSettingsBuffer[0].padding;
                        EditorGUI.showMixedValue = diff.padding;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Padding";
                        guiContent.tooltip = "The distance between successive objects in the curve.";
                        float newPadding = EditorGUILayout.FloatField(guiContent, padding, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var s in _curveSpawnSettingsBuffer)
                                s.padding = newPadding;

                            foreach (var c in _curveBuffer)
                                c.refresh(ObjectSpawnCurveRefreshReason.Other);
                        }
                    }
                    else
                    if (newCurvePaddingMode == CurveObjectSpawnPaddingMode.Random)
                    {
                        // Min random padding
                        float minRandomPadding = _curveSpawnSettingsBuffer[0].minRandomPadding;
                        EditorGUI.showMixedValue = diff.minRandomPadding;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min padding";
                        guiContent.tooltip = "The minimum random padding.";
                        float newMinRandomPadding = EditorGUILayout.FloatField(guiContent, minRandomPadding, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var s in _curveSpawnSettingsBuffer)
                                s.minRandomPadding = newMinRandomPadding;

                            foreach (var c in _curveBuffer)
                                c.refresh(ObjectSpawnCurveRefreshReason.Other);
                        }

                        // Max random padding
                        float maxRandomPadding = _curveSpawnSettingsBuffer[0].maxRandomPadding;
                        EditorGUI.showMixedValue = diff.maxRandomPadding;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max padding";
                        guiContent.tooltip = "The maximum random padding.";
                        float newMaxRandomPadding = EditorGUILayout.FloatField(guiContent, maxRandomPadding, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var s in _curveSpawnSettingsBuffer)
                                s.maxRandomPadding = newMaxRandomPadding;

                            foreach (var c in _curveBuffer)
                                c.refresh(ObjectSpawnCurveRefreshReason.Other);
                        }
                    }

                    // Try fix overlap
                    EditorGUILayout.Separator();
                    bool tryFixOverlap = _curveSpawnSettingsBuffer[0].tryFixOverlap;
                    EditorGUI.showMixedValue = diff.tryFixOverlap;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Try fix overlap";
                    guiContent.tooltip = "If checked, the plugin will attempt to ensure that curve objects will not overlap. " +
                                            "Note: This applies only to the main lane.";
                    bool newTryFixOverlap = EditorGUILayout.Toggle(guiContent, tryFixOverlap, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.tryFixOverlap = newTryFixOverlap;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }

                    // Lane mode
                    EditorGUILayout.Separator();
                    CurveObjectSpawnLaneMode laneMode = diff.laneMode ? CurveObjectSpawnLaneMode.UIMixed : _curveSpawnSettingsBuffer[0].laneMode;
                    EditorGUI.showMixedValue = diff.laneMode;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Lane mode";
                    guiContent.tooltip = "Allows you to specify the way in which the number of lanes will be generated.";
                    CurveObjectSpawnLaneMode newLaneMode = (CurveObjectSpawnLaneMode)EditorGUILayout.EnumPopup(guiContent, laneMode, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.laneMode = newLaneMode;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }

                    if (newLaneMode == CurveObjectSpawnLaneMode.Constant)
                    {
                        // Num lanes
                        int numLanes = _curveSpawnSettingsBuffer[0].numLanes;
                        EditorGUI.showMixedValue = diff.numLanes;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Num lanes";
                        guiContent.tooltip = "The number of lanes running parallel to each other.";
                        int newNumLanes = EditorGUILayout.IntField(guiContent, numLanes, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var s in _curveSpawnSettingsBuffer)
                                s.numLanes = newNumLanes;

                            foreach (var c in _curveBuffer)
                                c.refresh(ObjectSpawnCurveRefreshReason.Other);
                        }
                    }
                    else
                    if (newLaneMode == CurveObjectSpawnLaneMode.Random)
                    {
                        // Min num random lanes
                        int minNumLanes = _curveSpawnSettingsBuffer[0].minRandomNumLanes;
                        EditorGUI.showMixedValue = diff.minRandomNumLanes;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min num lanes";
                        guiContent.tooltip = "The minimum number of lanes running parallel to each other.";
                        int newMinNumLanes = EditorGUILayout.IntField(guiContent, minNumLanes, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var s in _curveSpawnSettingsBuffer)
                                s.minRandomNumLanes = newMinNumLanes;

                            foreach (var c in _curveBuffer)
                                c.refresh(ObjectSpawnCurveRefreshReason.Other);
                        }

                        // Max num random lanes
                        int maxNumLanes = _curveSpawnSettingsBuffer[0].maxRandomNumLanes;
                        EditorGUI.showMixedValue = diff.maxRandomNumLanes;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max num lanes";
                        guiContent.tooltip = "The maximum number of lanes running parallel to each other.";
                        int newMaxNumLanes = EditorGUILayout.IntField(guiContent, maxNumLanes, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var s in _curveSpawnSettingsBuffer)
                                s.maxRandomNumLanes = newMaxNumLanes;

                            foreach (var c in _curveBuffer)
                                c.refresh(ObjectSpawnCurveRefreshReason.Other);
                        }
                    }

                    // Lane padding mode
                    CurveObjectSpawnLanePaddingMode lanePaddingMode = diff.lanePaddingMode ? CurveObjectSpawnLanePaddingMode.UIMixed : _curveSpawnSettingsBuffer[0].lanePaddingMode;
                    EditorGUI.showMixedValue = diff.lanePaddingMode;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Lane padding mode";
                    guiContent.tooltip = "Allows you to specify the lane padding mode. Lane padding is the distance between successive lanes.";
                    CurveObjectSpawnLanePaddingMode newLanePaddingMode = (CurveObjectSpawnLanePaddingMode)EditorGUILayout.EnumPopup(guiContent, lanePaddingMode, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.lanePaddingMode = newLanePaddingMode;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }
                    
                    if (newLanePaddingMode == CurveObjectSpawnLanePaddingMode.Constant)
                    {
                        // Lane padding
                        float lanePadding = _curveSpawnSettingsBuffer[0].lanePadding;
                        EditorGUI.showMixedValue = diff.lanePadding;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Lane padding";
                        guiContent.tooltip = "The distance between successive lanes.";
                        float newLanePadding = EditorGUILayout.FloatField(guiContent, lanePadding, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var s in _curveSpawnSettingsBuffer)
                                s.lanePadding = newLanePadding;

                            foreach (var c in _curveBuffer)
                                c.refresh(ObjectSpawnCurveRefreshReason.Other);
                        }
                    }
                    else
                    {
                        // Min random lane padding
                        float minRandomLanePadding = _curveSpawnSettingsBuffer[0].minRandomLanePadding;
                        EditorGUI.showMixedValue = diff.minRandomLanePadding;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Min lane padding";
                        guiContent.tooltip = "The minimum random lane padding.";
                        float newMinRandomLanePadding = EditorGUILayout.FloatField(guiContent, minRandomLanePadding, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var s in _curveSpawnSettingsBuffer)
                                s.minRandomLanePadding = newMinRandomLanePadding;

                            foreach (var c in _curveBuffer)
                                c.refresh(ObjectSpawnCurveRefreshReason.Other);
                        }

                        // Max random lane padding
                        float maxRandomLanePadding = _curveSpawnSettingsBuffer[0].maxRandomLanePadding;
                        EditorGUI.showMixedValue = diff.maxRandomLanePadding;
                        EditorGUI.BeginChangeCheck();
                        guiContent.text = "Max lane padding";
                        guiContent.tooltip = "The maximum random lane padding.";
                        float newMaxRandomLanePadding = EditorGUILayout.FloatField(guiContent, maxRandomLanePadding, GUILayout.ExpandWidth(true));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var s in _curveSpawnSettingsBuffer)
                                s.maxRandomLanePadding = newMaxRandomLanePadding;

                            foreach (var c in _curveBuffer)
                                c.refresh(ObjectSpawnCurveRefreshReason.Other);
                        }
                    }

                    // Projection mode
                    EditorGUILayout.Separator();
                    CurveObjectSpawnProjectionMode projectionMode = diff.projectionMode ? CurveObjectSpawnProjectionMode.UIMixed : _curveSpawnSettingsBuffer[0].projectionMode;
                    EditorGUI.showMixedValue = diff.projectionMode;
                    EditorGUI.BeginChangeCheck();
                    guiContent.text = "Projection mode";
                    guiContent.tooltip = "Allows you to specify how the spawned objects will be projected in the scene.";
                    CurveObjectSpawnProjectionMode newProjectionMode = (CurveObjectSpawnProjectionMode)EditorGUILayout.EnumPopup(guiContent, projectionMode, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _curveSpawnSettingsBuffer)
                            s.projectionMode = newProjectionMode;

                        foreach (var c in _curveBuffer)
                            c.refresh(ObjectSpawnCurveRefreshReason.Other);
                    }
                    #pragma warning restore 0612
                    EditorUIEx.restoreShowMixedValue();
                    EditorUIEx.restoreLabelWidth();
                }
            };

            _useDefaultsBtn = UI.createUseDefaultsButton(() =>
            {
                _curveView.getSelectedItemData(_curveBuffer);

                foreach (var itemData in _curveBuffer)
                    itemData.settings.useDefaults();

                foreach (var c in _curveBuffer)
                    c.refresh(ObjectSpawnCurveRefreshReason.UseDefaultSettings);

            }, parent);
             _useDefaultsBtn.setDisplayVisible(_curveView.getNumVisibleSelectedItems() != 0);
        }

        private void onSelectionChanged(ListView<UIObjectSpawnCurveItem, ObjectSpawnCurve> listView)
        {
            SceneView.RepaintAll();
        }

        private void onSelectionDeleted(ListView<UIObjectSpawnCurveItem, ObjectSpawnCurve> listView)
        {
            SceneView.RepaintAll();
        }

        private void populateCurveView()
        {
            if (_curveView == null) return;
            _curveSearchField.refreshMatchNames();

            _curveView.onBeginBuild();
            ObjectSpawnCurveDb.instance.getCurves(_curveBuffer);

            foreach (var curve in _curveBuffer)
                _curveView.addItem(curve, filterCurve(curve));

            _curveView.onEndBuild();
        }

        private void onCanDeleteSelectedItems(ListView<UIObjectSpawnCurveItem, ObjectSpawnCurve> listView, List<PluginGuid> itemIds, YesNoAnswer answer)
        {
            _curveView.getItemData(itemIds, _curveBuffer);

            if (!askDeleteCurveObjects(_curveBuffer, out _deleteCurveObjects))
            {
                answer.no();
                return;
            }

            answer.yes();
        }

        private bool askDeleteCurveObjects(List<ObjectSpawnCurve> curves, out bool deleteCurveObjects)
        {
            deleteCurveObjects      = false;
            int numSpawnedObjects   = ObjectSpawnCurve.calcNumSpawnedObjects(curves);
            if (numSpawnedObjects != 0)
            {
                int answerId = EditorUtility.DisplayDialogComplex("Delete Curve Objects?",
                "Warning: This operation can not be undone.\nWould you also like to delete the objects associated with the curve(s)?",
                "Yes", "No", "Cancel");

                if (answerId == 2) return false;
                deleteCurveObjects = (answerId == 0);
            }

            return true;
        }

        private void onSelectedCurveItemsWillBeDeleted(ListView<UIObjectSpawnCurveItem, ObjectSpawnCurve> listView, List<PluginGuid> itemIds)
        {
            _curveView.getItemData(itemIds, _curveBuffer);
            deleteCurves(_curveBuffer, _deleteCurveObjects);
        }

        private void deleteCurves(List<ObjectSpawnCurve> curves, bool deleteCurveObjects)
        {
            if (deleteCurveObjects)
            {
                foreach (var curve in curves)
                    curve.destroySpawnedObjectsNoUndoRedo();
            }
            ObjectSpawnCurveDb.instance.deleteCurves(curves);
        }
    }
}
#endif