#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectSpawnUI : PluginUI
    {
        private ToolbarButton   _modularSnapBtn;
        private ToolbarButton   _modularWallsBtn;
        private ToolbarButton   _segmentsBtn;
        private ToolbarButton   _boxBtn;
        private ToolbarButton   _propsBtn;
        private ToolbarButton   _scatterBrushBtn;
        private ToolbarButton   _tileRulesBtn;
        private ToolbarButton   _curveBtn;
        private ToolbarButton   _physicsBtn;

        private Toolbar         _curveSpawnGizmoSelectToolbar;
        private ToolbarButton   _curveSpawnMoveGimzoBtn;
        private ToolbarButton   _curveSpawnRotationGizmoBtn;
        private ToolbarButton   _curveSpawnScaleGizmoBtn;

        private Toolbar         _trSpawnToolSelectToolbar;
        private ToolbarButton   _trSpawnPaintBtn;
        private ToolbarButton   _trSpawnRampPaintBtn;
        private ToolbarButton   _trSpawnEraseBtn;
        private ToolbarButton   _trSpawnConnectBtn;
        private Toolbar         _trSpawnBrushSelectToolbar;
        private ToolbarButton   _trSpawnBoxBrushBtn;
        private ToolbarButton   _trSpawnFlexiBoxBrushBtn;
        private ToolbarButton   _trSpawnSegmentsBrushBtn;

        [SerializeField]
        private UISection       _modularSnapSpawnSettingsSection;
        [SerializeField]
        private UISection       _modularSnapSpawnGuideSettingsSection;
        [SerializeField]
        private UISection       _modularSnapSpawnMirrorGizmoSettingsSection;
        [SerializeField]
        private UISection       _segmentsSpawnModularSnapSettingsSection;
        [SerializeField]
        private UISection       _segmentsSpawnSettingsProfileSection;
        [SerializeField]
        private UISection       _segmentsSpawnMirrorGizmoSettingsSection;
        [SerializeField]
        private UISection       _boxSpawnModularSnapSettingsSection;
        [SerializeField]
        private UISection       _boxSpawnSettingsProfileSection;
        [SerializeField]
        private UISection       _boxSpawnMirrorGizmoSettingsSection;
        [SerializeField]
        private UISection       _propsSpawnSurfaceSnapSettingsSection;
        [SerializeField]
        private UISection       _propsSpawnDragSpawnSettingsSection;
        [SerializeField]
        private UISection       _propsSpawnTerrainFlattenSettingsSection;
        [SerializeField]
        private UISection       _propsSpawnGuideSettingsSection;
        [SerializeField]
        private UISection       _propsSpawnMirrorGizmoSettingsSection;
        [SerializeField]
        private UISection       _scatterBrushSpawnSettingsSection;
        [SerializeField]
        private UISection       _physicsSimulationSettingsSection;

        private UISection       modularSnapSpawnSettingsSection                 { get { if (_modularSnapSpawnSettingsSection == null) _modularSnapSpawnSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _modularSnapSpawnSettingsSection; } }
        private UISection       modularSnapSpawnGuideSettingsSection            { get { if (_modularSnapSpawnGuideSettingsSection == null) _modularSnapSpawnGuideSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _modularSnapSpawnGuideSettingsSection; } }
        private UISection       modularSnapSpawnMirrorGizmoSettingsSection      { get { if (_modularSnapSpawnMirrorGizmoSettingsSection == null) _modularSnapSpawnMirrorGizmoSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _modularSnapSpawnMirrorGizmoSettingsSection; } }
        private UISection       segmentsSpawnModularSnapSettingsSection         { get { if (_segmentsSpawnModularSnapSettingsSection == null) _segmentsSpawnModularSnapSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _segmentsSpawnModularSnapSettingsSection; } }
        private UISection       segmentsSpawnSettingsProfileSection             { get { if (_segmentsSpawnSettingsProfileSection == null) _segmentsSpawnSettingsProfileSection = ScriptableObject.CreateInstance<UISection>(); return _segmentsSpawnSettingsProfileSection; } }
        private UISection       segmentsSpawnMirrorGizmoSettingsSection         { get { if (_segmentsSpawnMirrorGizmoSettingsSection == null) _segmentsSpawnMirrorGizmoSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _segmentsSpawnMirrorGizmoSettingsSection; } }
        private UISection       boxSpawnModularSnapSettingsSection              { get { if (_boxSpawnModularSnapSettingsSection == null) _boxSpawnModularSnapSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _boxSpawnModularSnapSettingsSection; } }
        private UISection       boxSpawnSettingsProfileSection                  { get { if (_boxSpawnSettingsProfileSection == null) _boxSpawnSettingsProfileSection = ScriptableObject.CreateInstance<UISection>(); return _boxSpawnSettingsProfileSection; } }
        private UISection       boxSpawnMirrorGizmoSettingsSection              { get { if (_boxSpawnMirrorGizmoSettingsSection == null) _boxSpawnMirrorGizmoSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _boxSpawnMirrorGizmoSettingsSection; } }
        private UISection       propsSpawnSurfaceSnapSettingsSection            { get { if (_propsSpawnSurfaceSnapSettingsSection == null) _propsSpawnSurfaceSnapSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _propsSpawnSurfaceSnapSettingsSection; } }
        private UISection       propsSpawnDragSpawnSettingsSection              { get { if (_propsSpawnDragSpawnSettingsSection == null) _propsSpawnDragSpawnSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _propsSpawnDragSpawnSettingsSection; } }
        private UISection       propsSpawnTerrainFlattenSettingsSection         { get { if (_propsSpawnTerrainFlattenSettingsSection == null) _propsSpawnTerrainFlattenSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _propsSpawnTerrainFlattenSettingsSection; } }
        private UISection       propsSpawnGuideSettingsSection                  { get { if (_propsSpawnGuideSettingsSection == null) _propsSpawnGuideSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _propsSpawnGuideSettingsSection; } }
        private UISection       propsSpawnMirrorGizmoSettingsSection            { get { if (_propsSpawnMirrorGizmoSettingsSection == null) _propsSpawnMirrorGizmoSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _propsSpawnMirrorGizmoSettingsSection; } }
        private UISection       scatterBrushSpawnSettingsSection                { get { if (_scatterBrushSpawnSettingsSection == null) _scatterBrushSpawnSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _scatterBrushSpawnSettingsSection; } }               
        private UISection       physicsSimulationSettingsSection                { get { if (_physicsSimulationSettingsSection == null) _physicsSimulationSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _physicsSimulationSettingsSection; } }

        private static string   uiSectionRowSeparator_ModularSnapSpawnName      { get { return "A"; } }
        private static string   uiSectionRowSeparator_SegmentsSpawnName         { get { return "B"; } }
        private static string   uiSectionRowSeparator_BoxSpawnName              { get { return "C"; } }
        private static string   uiSectionRowSeparator_PropsSpawnName            { get { return "D"; } }
        private static string   uiSectionRowSeparator_ScatterBrushSpawnName     { get { return "E"; } }
        private static string   uiSectionRowSeparator_PhysicsSpawnName          { get { return "F"; } }

        public static ObjectSpawnUI instance                                    { get { return GSpawn.active.objectSpawnUI; } }

        public void onTileRuleSpawnActiveToolIdChanged()
        {
            refreshToolButtons();
            updateVisibility();
        }

        public void onTileRuleSpawnActiveBrushIdChanged()
        {
            refreshToolButtons();
        }

        protected override void onRefresh()
        {
            refreshSpawnModeButtons();
            refreshToolButtons();
            refreshToolTips();
            updateVisibility();
        }

        protected override void onBuild()
        {
            Toolbar modularSpawnToolsToolbar            = UI.createToolSelectionToolbar(contentContainer);
            modularSpawnToolsToolbar.style.height       = UIValues.mediumToolbarButtonSize + 3.0f;

            Toolbar otherSpawnToolsToolbar              = UI.createToolSelectionToolbar(contentContainer);
            otherSpawnToolsToolbar.style.height         = UIValues.mediumToolbarButtonSize + 3.0f;

            _modularSnapBtn             = UI.createToolbarButton(TexturePool.instance.modularSnapSpawn, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, modularSpawnToolsToolbar);
            UI.useDefaultMargins(_modularSnapBtn);
            _modularSnapBtn.clicked     += () => { ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.ModularSnap; SceneViewEx.focus(); };

            _modularWallsBtn = UI.createToolbarButton(TexturePool.instance.modularWallSpawn, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, modularSpawnToolsToolbar);
            UI.useDefaultMargins(_modularWallsBtn);
            _modularWallsBtn.clicked    += () => { ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.ModularWalls; SceneViewEx.focus(); };

            _segmentsBtn                = UI.createToolbarButton(TexturePool.instance.segmentsSpawn, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, modularSpawnToolsToolbar);
            UI.useDefaultMargins(_segmentsBtn);
            _segmentsBtn.clicked        += () => { ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Segments; SceneViewEx.focus(); };

            _boxBtn                     = UI.createToolbarButton(TexturePool.instance.boxSpawn, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, modularSpawnToolsToolbar);
            UI.useDefaultMargins(_boxBtn);
            _boxBtn.clicked             += () => { ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Box; SceneViewEx.focus(); };

            _tileRulesBtn               = UI.createToolbarButton(TexturePool.instance.tileRuleBrushSpawn, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, modularSpawnToolsToolbar);
            UI.useDefaultMargins(_tileRulesBtn);
            _tileRulesBtn.clicked       += () => { ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.TileRules; SceneViewEx.focus(); };

            _propsBtn                   = UI.createToolbarButton(TexturePool.instance.propsSpawn, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, otherSpawnToolsToolbar);
            UI.useDefaultMargins(_propsBtn);
            _propsBtn.clicked           += () => { ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Props; SceneViewEx.focus(); };

            _scatterBrushBtn            = UI.createToolbarButton(TexturePool.instance.scatterBrushSpawn, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, otherSpawnToolsToolbar);
            UI.useDefaultMargins(_scatterBrushBtn);
            _scatterBrushBtn.clicked    += () => { ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.ScatterBrush; SceneViewEx.focus(); };

            _curveBtn                   = UI.createToolbarButton(TexturePool.instance.curveSpawn, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, otherSpawnToolsToolbar);
            UI.useDefaultMargins(_curveBtn);
            _curveBtn.clicked           += () => { ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Curve; SceneViewEx.focus(); };

            _physicsBtn                 = UI.createToolbarButton(TexturePool.instance.physicsSimulation, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, otherSpawnToolsToolbar);
            UI.useDefaultMargins(_physicsBtn);
            _physicsBtn.clicked         += () => { ObjectSpawn.instance.activeToolId = ObjectSpawnToolId.Physics; SceneViewEx.focus(); };

            // Create the toolbar for curve spawn gizmo buttons
            _curveSpawnGizmoSelectToolbar           = UI.createToolSelectionToolbar(contentContainer);
            _curveSpawnMoveGimzoBtn                 = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.moveGizmo, "Move", _curveSpawnGizmoSelectToolbar);
            _curveSpawnMoveGimzoBtn.clicked         += () => 
            { 
                ObjectSpawn.instance.curveObjectSpawn.activeGizmoId = ObjectSpawnCurveGizmoId.Move;
                SceneViewEx.focus();
            };
            _curveSpawnRotationGizmoBtn             = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.rotationGizmo, "Rotate", _curveSpawnGizmoSelectToolbar);
            _curveSpawnRotationGizmoBtn.clicked     += () => 
            { 
                ObjectSpawn.instance.curveObjectSpawn.activeGizmoId = ObjectSpawnCurveGizmoId.Rotate;
                SceneViewEx.focus();
            };
            _curveSpawnScaleGizmoBtn                = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.scaleGizmo, "Scale", _curveSpawnGizmoSelectToolbar);
            _curveSpawnScaleGizmoBtn.clicked        += () => 
            { 
                ObjectSpawn.instance.curveObjectSpawn.activeGizmoId = ObjectSpawnCurveGizmoId.Scale;
                SceneViewEx.focus();
            };

            // Create the toolbar for the tile rule spawn tool selection buttons
            _trSpawnToolSelectToolbar              = UI.createToolSelectionToolbar(contentContainer);
            _trSpawnPaintBtn                       = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.brush, "Paint", _trSpawnToolSelectToolbar);
            _trSpawnPaintBtn.clicked               += () => 
            { 
                ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId = TileRuleSpawnToolId.Paint;
                SceneViewEx.focus();
            };
            _trSpawnRampPaintBtn                   = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.ramp, "Ramp Paint", _trSpawnToolSelectToolbar);
            _trSpawnRampPaintBtn.clicked           += () => 
            { 
                ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId = TileRuleSpawnToolId.RampPaint;
                SceneViewEx.focus();
            };
            _trSpawnEraseBtn                       = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.eraser_gray, "Erase", _trSpawnToolSelectToolbar);
            _trSpawnEraseBtn.clicked               += () => 
            { 
                ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId = TileRuleSpawnToolId.Erase;
                SceneViewEx.focus();
            };
            _trSpawnConnectBtn                     = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.tileRuleSpawnConnect, "Connect", _trSpawnToolSelectToolbar);
            _trSpawnConnectBtn.clicked             += () => 
            { 
                ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId = TileRuleSpawnToolId.Connect;
                SceneViewEx.focus();
            };

            // Create the toolbar for the tile rule spawn brush type selection buttons
            _trSpawnBrushSelectToolbar                  = UI.createToolSelectionToolbar(contentContainer);
            _trSpawnBoxBrushBtn                         = UI.createMediumToolSelectionToolbarButton(TexturePool.instance.tileRuleSpawnBoxBrush, "Box Brush", _trSpawnBrushSelectToolbar);
            _trSpawnBoxBrushBtn.clicked                 += () => 
            { 
                ObjectSpawn.instance.tileRuleObjectSpawn.activeBrushId = TileRuleSpawnBrushId.Box;
                SceneViewEx.focus();
            };
            _trSpawnBoxBrushBtn.style.marginTop         = -0.5f;
            _trSpawnFlexiBoxBrushBtn                    = UI.createMediumToolSelectionToolbarButton(TexturePool.instance.tileRuleSpawnFlexiBoxBrush, "Flexi Box Brush", _trSpawnBrushSelectToolbar);
            _trSpawnFlexiBoxBrushBtn.clicked            += () => 
            { 
                ObjectSpawn.instance.tileRuleObjectSpawn.activeBrushId = TileRuleSpawnBrushId.FlexiBox;
                SceneViewEx.focus();
            };
            _trSpawnFlexiBoxBrushBtn.style.marginTop    = -0.5f;
            _trSpawnSegmentsBrushBtn                    = UI.createMediumToolSelectionToolbarButton(TexturePool.instance.tileRuleSpawnSegmentBrush, "Segments Brush", _trSpawnBrushSelectToolbar);
            _trSpawnSegmentsBrushBtn.clicked            += () => 
            { 
                ObjectSpawn.instance.tileRuleObjectSpawn.activeBrushId = TileRuleSpawnBrushId.Segments;
                SceneViewEx.focus();
            };
            _trSpawnSegmentsBrushBtn.style.marginTop    = -0.5f;

            // Modular snap spawn
            modularSnapSpawnSettingsSection.build("Modular Snap", TexturePool.instance.modularSnapSpawn, true, contentContainer);
            ObjectSpawn.instance.modularSnapObjectSpawn.modularSnapSettings.buildUI(modularSnapSpawnSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_ModularSnapSpawnName);
            modularSnapSpawnGuideSettingsSection.build("Spawn Guide", TexturePool.instance.location, true, contentContainer);
            ObjectSpawn.instance.modularSnapObjectSpawn.spawnGuideSettings.buildUI(modularSnapSpawnGuideSettingsSection.contentContainer, ObjectSpawnGuideSettingsUsage.Other);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_ModularSnapSpawnName);
            modularSnapSpawnMirrorGizmoSettingsSection.build("Mirror Gizmo", TexturePool.instance.mirrorGizmo, true, contentContainer);
            UI.createPositionField(PluginGizmo.positionPropertyName, ObjectSpawn.instance.modularSnapObjectSpawn.mirrorGizmo.serializedObject, "Mirror gizmo position", modularSnapSpawnMirrorGizmoSettingsSection.contentContainer);
            UI.createRotationField(PluginGizmo.rotationPropertyName, ObjectSpawn.instance.modularSnapObjectSpawn.mirrorGizmo.serializedObject, "Mirror gizmo rotation", modularSnapSpawnMirrorGizmoSettingsSection.contentContainer);
            ObjectSpawn.instance.modularSnapObjectSpawn.mirrorGizmoSettings.buildUI(modularSnapSpawnMirrorGizmoSettingsSection.contentContainer);
            /*var replicateBtn = createMirrorGizmoSettingsReplicateButton(modularSnapSpawnMirrorGizmoSettingsSection.contentContainer);
            replicateBtn.clicked += () => 
            {
                var srcSettings = ObjectSpawn.instance.modularSnapObjectSpawn.mirrorGizmoSettings;
                ObjectSpawn.instance.segmentsObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
                ObjectSpawn.instance.boxObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
                ObjectSpawn.instance.propsObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
            };*/

            // Modular walls spawn
            ModularWallsObjectSpawnUI.instance.build(contentContainer, PluginInspectorUI.instance.targetEditor);

            // Segments spawn
            segmentsSpawnModularSnapSettingsSection.build("Modular Snap", TexturePool.instance.modularSnapSpawn, true, contentContainer);
            ObjectSpawn.instance.segmentsObjectSpawn.modularSnapSettings.buildUI(segmentsSpawnModularSnapSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_SegmentsSpawnName);
            segmentsSpawnSettingsProfileSection.build("Segments", TexturePool.instance.segmentsSpawn, true, contentContainer);
            SegmentsObjectSpawnSettingsProfileDbUI.instance.build(segmentsSpawnSettingsProfileSection.contentContainer, GSpawn.active.inspectorUI.targetEditor);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_SegmentsSpawnName);
            segmentsSpawnMirrorGizmoSettingsSection.build("Mirror Gizmo", TexturePool.instance.mirrorGizmo, true, contentContainer);
            UI.createPositionField(PluginGizmo.positionPropertyName, ObjectSpawn.instance.segmentsObjectSpawn.mirrorGizmo.serializedObject, "Mirror gizmo position", segmentsSpawnMirrorGizmoSettingsSection.contentContainer);
            UI.createRotationField(PluginGizmo.rotationPropertyName, ObjectSpawn.instance.segmentsObjectSpawn.mirrorGizmo.serializedObject, "Mirror gizmo rotation", segmentsSpawnMirrorGizmoSettingsSection.contentContainer);
            ObjectSpawn.instance.segmentsObjectSpawn.mirrorGizmoSettings.buildUI(segmentsSpawnMirrorGizmoSettingsSection.contentContainer);
            /*replicateBtn = createMirrorGizmoSettingsReplicateButton(segmentsSpawnMirrorGizmoSettingsSection.contentContainer);
            replicateBtn.clicked += () =>
            {
                var srcSettings = ObjectSpawn.instance.segmentsObjectSpawn.mirrorGizmoSettings;
                ObjectSpawn.instance.modularSnapObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
                ObjectSpawn.instance.boxObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
                ObjectSpawn.instance.propsObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
            };*/

            // Box spawn
            boxSpawnModularSnapSettingsSection.build("Modular Snap", TexturePool.instance.modularSnapSpawn, true, contentContainer);
            ObjectSpawn.instance.boxObjectSpawn.modularSnapSettings.buildUI(boxSpawnModularSnapSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_BoxSpawnName);
            boxSpawnSettingsProfileSection.build("Box", TexturePool.instance.boxSpawn, true, contentContainer);
            BoxObjectSpawnSettingsProfileDbUI.instance.build(boxSpawnSettingsProfileSection.contentContainer, GSpawn.active.inspectorUI.targetEditor);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_BoxSpawnName);
            boxSpawnMirrorGizmoSettingsSection.build("Mirror Gizmo", TexturePool.instance.mirrorGizmo, true, contentContainer);
            UI.createPositionField(PluginGizmo.positionPropertyName, ObjectSpawn.instance.boxObjectSpawn.mirrorGizmo.serializedObject, "Mirror gizmo position", boxSpawnMirrorGizmoSettingsSection.contentContainer);
            UI.createRotationField(PluginGizmo.rotationPropertyName, ObjectSpawn.instance.boxObjectSpawn.mirrorGizmo.serializedObject, "Mirror gizmo rotation", boxSpawnMirrorGizmoSettingsSection.contentContainer);
            ObjectSpawn.instance.boxObjectSpawn.mirrorGizmoSettings.buildUI(boxSpawnMirrorGizmoSettingsSection.contentContainer);
            /*replicateBtn = createMirrorGizmoSettingsReplicateButton(boxSpawnMirrorGizmoSettingsSection.contentContainer);
            replicateBtn.clicked += () =>
            {
                var srcSettings = ObjectSpawn.instance.boxObjectSpawn.mirrorGizmoSettings;
                ObjectSpawn.instance.modularSnapObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
                ObjectSpawn.instance.segmentsObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
                ObjectSpawn.instance.propsObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
            };*/

            // Props spawn
            propsSpawnSurfaceSnapSettingsSection.build("Surface Snap", TexturePool.instance.objectSurfaceSnap, true, contentContainer);
            ObjectSpawn.instance.propsObjectSpawn.surfaceSnapSettings.buildUI(propsSpawnSurfaceSnapSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_PropsSpawnName);
            propsSpawnDragSpawnSettingsSection.build("Drag Spawn", TexturePool.instance.dragArrow, true, contentContainer);
            ObjectSpawn.instance.propsObjectSpawn.dragSpawnSettings.buildUI(propsSpawnDragSpawnSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_PropsSpawnName);
            propsSpawnTerrainFlattenSettingsSection.build("Terrain Flatten", TexturePool.instance.terrainFlatten, true, contentContainer);
            ObjectSpawn.instance.propsObjectSpawn.terrainFlattenSettings.buildUI(propsSpawnTerrainFlattenSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_PropsSpawnName);
            propsSpawnGuideSettingsSection.build("Spawn Guide", TexturePool.instance.location, true, contentContainer);
            ObjectSpawn.instance.propsObjectSpawn.spawnGuideSettings.buildUI(propsSpawnGuideSettingsSection.contentContainer, ObjectSpawnGuideSettingsUsage.PropsSpawn);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_PropsSpawnName);
            propsSpawnMirrorGizmoSettingsSection.build("Mirror Gizmo", TexturePool.instance.mirrorGizmo, true, contentContainer);
            UI.createPositionField(PluginGizmo.positionPropertyName, ObjectSpawn.instance.propsObjectSpawn.mirrorGizmo.serializedObject, "Mirror gizmo position", propsSpawnMirrorGizmoSettingsSection.contentContainer);
            UI.createRotationField(PluginGizmo.rotationPropertyName, ObjectSpawn.instance.propsObjectSpawn.mirrorGizmo.serializedObject, "Mirror gizmo rotation", propsSpawnMirrorGizmoSettingsSection.contentContainer);
            ObjectSpawn.instance.propsObjectSpawn.mirrorGizmoSettings.buildUI(propsSpawnMirrorGizmoSettingsSection.contentContainer);
            /*replicateBtn = createMirrorGizmoSettingsReplicateButton(propsSpawnMirrorGizmoSettingsSection.contentContainer);
            replicateBtn.clicked += () =>
            {
                var srcSettings = ObjectSpawn.instance.propsObjectSpawn.mirrorGizmoSettings;
                ObjectSpawn.instance.modularSnapObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
                ObjectSpawn.instance.segmentsObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
                ObjectSpawn.instance.boxObjectSpawn.mirrorGizmoSettings.copy(srcSettings);
            };*/

            // Scatter brush spawn
            scatterBrushSpawnSettingsSection.build("Scatter Brush", TexturePool.instance.scatterBrushSpawn, true, contentContainer);
            ObjectSpawn.instance.scatterBrushObjectSpawn.settings.buildUI(scatterBrushSpawnSettingsSection.contentContainer);

            // Tile rule brush spawn
            TileRuleObjectSpawnUI.instance.build(contentContainer, PluginInspectorUI.instance.targetEditor);

            // Curve spawn
            CurveObjectSpawnUI.instance.build(contentContainer, PluginInspectorUI.instance.targetEditor);

            // Physics spawn
            physicsSimulationSettingsSection.build("Physics", TexturePool.instance.physicsSimulation, true, contentContainer);
            ObjectSpawn.instance.physicsObjectSpawn.settings.buildUI(physicsSimulationSettingsSection.contentContainer);

            var stopSimluBtn            = new Button();
            physicsSimulationSettingsSection.contentContainer.Add(stopSimluBtn);
            stopSimluBtn.style.width    = UIValues.useDefaultsButtonWidth;
            stopSimluBtn.text           = "Stop simulation";
            stopSimluBtn.tooltip        = "Stop the physics simulation.";
            stopSimluBtn.clicked        += () => { PhysicsSimulation.instance.stop(); };

            refreshSpawnModeButtons();
            refreshToolButtons();
            refreshToolTips();
            updateVisibility();
        }

        private Button createMirrorGizmoSettingsReplicateButton(VisualElement parent)
        {
            var btn             = new Button();
            btn.text            = "Replicate";
            btn.tooltip         = "Replicates the settings across all mirror gizmo settings associated with different spawn modes (except Tile Rule Spawn). Note: " + 
                                  "Mirror gizmo position and rotation are not affected.";
            btn.style.width     = UIValues.useDefaultsButtonWidth;
            parent.Add(btn);

            return btn;
        }

        private void refreshSpawnModeButtons()
        {
            _modularSnapBtn.tooltip                 = "Modular Snap";
            _modularSnapBtn.style.backgroundColor   = UIValues.inactiveButtonColor;

            _modularWallsBtn.tooltip                = "Modular Walls";
            _modularWallsBtn.style.backgroundColor  = UIValues.inactiveButtonColor;

            _segmentsBtn.tooltip                    = "Segments";
            _segmentsBtn.style.backgroundColor      = UIValues.inactiveButtonColor;

            _boxBtn.tooltip                         = "Box";
            _boxBtn.style.backgroundColor           = UIValues.inactiveButtonColor;

            _propsBtn.tooltip                       = "Props";
            _propsBtn.style.backgroundColor         = UIValues.inactiveButtonColor;

            _scatterBrushBtn.tooltip                    = "Scatter Brush";
            _scatterBrushBtn.style.backgroundColor      = UIValues.inactiveButtonColor;

            _tileRulesBtn.tooltip                   = "Tile Rules";
            _tileRulesBtn.style.backgroundColor     = UIValues.inactiveButtonColor;

            _curveBtn.tooltip                       = "Curve";
            _curveBtn.style.backgroundColor         = UIValues.inactiveButtonColor;

            _physicsBtn.tooltip                     = "Physics";
            _physicsBtn.style.backgroundColor       = UIValues.inactiveButtonColor;

            var objectSpawnToolId = ObjectSpawn.instance.activeToolId;
            if (objectSpawnToolId == ObjectSpawnToolId.ModularSnap)         _modularSnapBtn.style.backgroundColor       = UIValues.activeButtonColor;
            else if (objectSpawnToolId == ObjectSpawnToolId.ModularWalls)   _modularWallsBtn.style.backgroundColor      = UIValues.activeButtonColor;
            else if (objectSpawnToolId == ObjectSpawnToolId.Segments)       _segmentsBtn.style.backgroundColor          = UIValues.activeButtonColor;
            else if (objectSpawnToolId == ObjectSpawnToolId.Box)            _boxBtn.style.backgroundColor               = UIValues.activeButtonColor;
            else if (objectSpawnToolId == ObjectSpawnToolId.Props)          _propsBtn.style.backgroundColor             = UIValues.activeButtonColor;
            else if (objectSpawnToolId == ObjectSpawnToolId.ScatterBrush)   _scatterBrushBtn.style.backgroundColor      = UIValues.activeButtonColor;
            else if (objectSpawnToolId == ObjectSpawnToolId.TileRules)      _tileRulesBtn.style.backgroundColor         = UIValues.activeButtonColor;
            else if (objectSpawnToolId == ObjectSpawnToolId.Curve)          _curveBtn.style.backgroundColor             = UIValues.activeButtonColor;
            else if (objectSpawnToolId == ObjectSpawnToolId.Physics)        _physicsBtn.style.backgroundColor           = UIValues.activeButtonColor;
        }

        private void refreshToolTips()
        {
            ObjectSpawn.instance.tileRuleObjectSpawn.settings.refreshTooltips();
        }

        private void refreshToolButtons()
        {
            if (ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                _trSpawnPaintBtn.tooltip   = "Paint" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.tileRuleSpawn_Paint);
                _trSpawnPaintBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

                _trSpawnRampPaintBtn.tooltip    = "Ramp Paint" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.tileRuleSpawn_Ramp);
                _trSpawnRampPaintBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

                _trSpawnEraseBtn.tooltip   = "Erase" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.tileRuleSpawn_Erase);
                _trSpawnEraseBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

                _trSpawnConnectBtn.tooltip = "Connect" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.tileRuleSpawn_Connect);
                _trSpawnConnectBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

                var activeToolId = ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId;
                if (activeToolId == TileRuleSpawnToolId.Paint)          _trSpawnPaintBtn.style.unityBackgroundImageTintColor        = UIValues.activeButtonColor;
                else if (activeToolId == TileRuleSpawnToolId.RampPaint) _trSpawnRampPaintBtn.style.unityBackgroundImageTintColor    = UIValues.activeButtonColor;
                else if (activeToolId == TileRuleSpawnToolId.Erase)     _trSpawnEraseBtn.style.unityBackgroundImageTintColor        = UIValues.activeButtonColor;
                else if (activeToolId == TileRuleSpawnToolId.Connect)   _trSpawnConnectBtn.style.unityBackgroundImageTintColor      = UIValues.activeButtonColor;

                _trSpawnBoxBrushBtn.tooltip = "Box Brush" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.tileRuleSpawn_BoxBrush);
                _trSpawnBoxBrushBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

                _trSpawnFlexiBoxBrushBtn.tooltip = "Flexi Box Brush" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.tileRuleSpawn_FlexiBoxBrush);
                _trSpawnFlexiBoxBrushBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

                _trSpawnSegmentsBrushBtn.tooltip = "Segments Brush" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.tileRuleSpawn_SegmentsBrush);
                _trSpawnSegmentsBrushBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

                var activeBrushId = ObjectSpawn.instance.tileRuleObjectSpawn.activeBrushId;
                if (activeBrushId == TileRuleSpawnBrushId.Box) _trSpawnBoxBrushBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
                else if (activeBrushId == TileRuleSpawnBrushId.FlexiBox) _trSpawnFlexiBoxBrushBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
                else if (activeBrushId == TileRuleSpawnBrushId.Segments) _trSpawnSegmentsBrushBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
            }
            else if (ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve)
            {
                _curveSpawnMoveGimzoBtn.tooltip                                 = "Move" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.curveSpawn_MoveGizmo);
                _curveSpawnMoveGimzoBtn.style.unityBackgroundImageTintColor     = UIValues.inactiveButtonTintColor;

                _curveSpawnRotationGizmoBtn.tooltip                             = "Rotate" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.curveSpawn_RotationGizmo);
                _curveSpawnRotationGizmoBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

                _curveSpawnScaleGizmoBtn.tooltip                                = "Scale" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.curveSpawn_ScaleGizmo);
                _curveSpawnScaleGizmoBtn.style.unityBackgroundImageTintColor    = UIValues.inactiveButtonTintColor;

                var actveGizmoId = ObjectSpawn.instance.curveObjectSpawn.activeGizmoId;
                if (actveGizmoId == ObjectSpawnCurveGizmoId.Move)           _curveSpawnMoveGimzoBtn.style.unityBackgroundImageTintColor     = UIValues.activeButtonColor;
                else if (actveGizmoId == ObjectSpawnCurveGizmoId.Rotate)    _curveSpawnRotationGizmoBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
                else if (actveGizmoId == ObjectSpawnCurveGizmoId.Scale)     _curveSpawnScaleGizmoBtn.style.unityBackgroundImageTintColor    = UIValues.activeButtonColor;
            }
        }

        private void updateVisibility()
        {
            var objectSpawnToolId = ObjectSpawn.instance.activeToolId;
            bool visible = (objectSpawnToolId == ObjectSpawnToolId.ModularSnap);
            contentContainer.setChildrenDisplayVisible(uiSectionRowSeparator_ModularSnapSpawnName, visible);
            modularSnapSpawnSettingsSection.setVisible(visible);
            modularSnapSpawnGuideSettingsSection.setVisible(visible);
            modularSnapSpawnMirrorGizmoSettingsSection.setVisible(visible);

            visible = (objectSpawnToolId == ObjectSpawnToolId.ModularWalls);
            ModularWallsObjectSpawnUI.instance.setVisible(visible);

            visible = (objectSpawnToolId == ObjectSpawnToolId.Segments);
            contentContainer.setChildrenDisplayVisible(uiSectionRowSeparator_SegmentsSpawnName, visible);
            segmentsSpawnModularSnapSettingsSection.setVisible(visible);
            segmentsSpawnSettingsProfileSection.setVisible(visible);
            segmentsSpawnMirrorGizmoSettingsSection.setVisible(visible);

            visible = (objectSpawnToolId == ObjectSpawnToolId.Box);
            contentContainer.setChildrenDisplayVisible(uiSectionRowSeparator_BoxSpawnName, visible);
            boxSpawnModularSnapSettingsSection.setVisible(visible);
            boxSpawnSettingsProfileSection.setVisible(visible);
            boxSpawnMirrorGizmoSettingsSection.setVisible(visible);

            visible = (objectSpawnToolId == ObjectSpawnToolId.Props);
            contentContainer.setChildrenDisplayVisible(uiSectionRowSeparator_PropsSpawnName, visible);
            propsSpawnSurfaceSnapSettingsSection.setVisible(visible);
            propsSpawnDragSpawnSettingsSection.setVisible(visible);
            propsSpawnTerrainFlattenSettingsSection.setVisible(visible);
            propsSpawnGuideSettingsSection.setVisible(visible);
            propsSpawnMirrorGizmoSettingsSection.setVisible(visible);

            visible = (objectSpawnToolId == ObjectSpawnToolId.TileRules);
            _trSpawnToolSelectToolbar.setDisplayVisible(visible);
            _trSpawnBrushSelectToolbar.setDisplayVisible(visible);
            TileRuleObjectSpawnUI.instance.setVisible(visible);

            visible = (objectSpawnToolId == ObjectSpawnToolId.ScatterBrush);
            contentContainer.setChildrenDisplayVisible(uiSectionRowSeparator_ScatterBrushSpawnName, visible);
            scatterBrushSpawnSettingsSection.setVisible(visible);

            visible = (objectSpawnToolId == ObjectSpawnToolId.Curve);
            _curveSpawnGizmoSelectToolbar.setDisplayVisible(visible);
            CurveObjectSpawnUI.instance.setVisible(visible);

            visible = (objectSpawnToolId == ObjectSpawnToolId.Physics);
            contentContainer.setChildrenDisplayVisible(uiSectionRowSeparator_PhysicsSpawnName, visible);
            physicsSimulationSettingsSection.setVisible(visible);

            if (ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules)
            {
                var activeToolId = ObjectSpawn.instance.tileRuleObjectSpawn.activeToolId;
                if (activeToolId == TileRuleSpawnToolId.Connect || activeToolId == TileRuleSpawnToolId.RampPaint) _trSpawnBrushSelectToolbar.setDisplayVisible(false);
                else _trSpawnBrushSelectToolbar.setDisplayVisible(true);
            }
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_modularSnapSpawnSettingsSection);
            ScriptableObjectEx.destroyImmediate(_modularSnapSpawnGuideSettingsSection);
            ScriptableObjectEx.destroyImmediate(_modularSnapSpawnMirrorGizmoSettingsSection);
            ScriptableObjectEx.destroyImmediate(_segmentsSpawnModularSnapSettingsSection);
            ScriptableObjectEx.destroyImmediate(_segmentsSpawnSettingsProfileSection);
            ScriptableObjectEx.destroyImmediate(_segmentsSpawnMirrorGizmoSettingsSection);
            ScriptableObjectEx.destroyImmediate(_boxSpawnModularSnapSettingsSection);
            ScriptableObjectEx.destroyImmediate(_boxSpawnSettingsProfileSection);
            ScriptableObjectEx.destroyImmediate(_boxSpawnMirrorGizmoSettingsSection);
            ScriptableObjectEx.destroyImmediate(_propsSpawnSurfaceSnapSettingsSection);
            ScriptableObjectEx.destroyImmediate(_propsSpawnDragSpawnSettingsSection);
            ScriptableObjectEx.destroyImmediate(_propsSpawnTerrainFlattenSettingsSection);
            ScriptableObjectEx.destroyImmediate(_propsSpawnGuideSettingsSection);
            ScriptableObjectEx.destroyImmediate(_propsSpawnMirrorGizmoSettingsSection);
            ScriptableObjectEx.destroyImmediate(_scatterBrushSpawnSettingsSection);
            ScriptableObjectEx.destroyImmediate(_physicsSimulationSettingsSection);
        }
    }
}
#endif