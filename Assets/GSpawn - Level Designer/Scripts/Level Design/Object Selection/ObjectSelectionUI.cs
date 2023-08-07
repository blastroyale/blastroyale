#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectSelectionUI : PluginUI
    {
        private enum ViewId
        {
            Settings = 0,
            TransformTools,
            Misc
        }

        private ToolbarButton       _moveGimzoBtn;
        private ToolbarButton       _rotationGizmoBtn;
        private ToolbarButton       _scaleGizmoBtn;
        private ToolbarButton       _universalGizmoBtn;
        private ToolbarButton       _extrudeGizmoBtn;
        private ToolbarButton       _mirrorGizmoBtn;

        private ToolbarButton       _gizmoPivotBtn;
        private ToolbarButton       _gizmoTransformSpaceBtn;

        private ToolbarButton       _selectionRectBtn;
        private ToolbarButton       _selectionSegmentsBtn;
        private ToolbarButton       _selectionBoxBtn;

        private ToolbarButton       _settingsBtn;
        private ToolbarButton       _transformToolsBtn;
        private ToolbarButton       _miscBtn;

        private Button              _projectOnGridBtn;
        private Button              _projectOnObjectBtn;

        [SerializeField]
        private ViewId              _activeViewId       = ViewId.Settings;
        [SerializeField]
        private UISection           _transformSection;
        [SerializeField]
        private ObjectTransformUI   _transformUI;

        [SerializeField]
        private UISection           _selectionSettingsSection;
        [SerializeField]
        private UISection           _extrudeGizmoSettingsSection;
        [SerializeField]
        private UISection           _mirrorGizmoSettingsSection;
        [SerializeField]
        private UISection           _modularSnapSettingsSection;
        [SerializeField]
        private UISection           _surfaceSnapSettingsSection;
        [SerializeField]
        private UISection           _projectionSettingsSection;
        [SerializeField]
        private UISection           _vertexSnapSettingsSection;
        [SerializeField]
        private UISection           _boxSnapSettingsSection;
        [SerializeField]
        private UISection           _selectionGrowSettingsSection;

        private UISection           transformSection                        { get { if (_transformSection == null) _transformSection = ScriptableObject.CreateInstance<UISection>(); return _transformSection; } }
        private ObjectTransformUI   transformUI                             { get { if (_transformUI == null) _transformUI = ScriptableObject.CreateInstance<ObjectTransformUI>(); return _transformUI; } }
        private UISection           selectionSettingsSection                { get { if (_selectionSettingsSection == null) _selectionSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _selectionSettingsSection; } }
        private UISection           extrudeGizmoSettingsSection             { get { if (_extrudeGizmoSettingsSection == null) _extrudeGizmoSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _extrudeGizmoSettingsSection; } }
        private UISection           mirrorGizmoSettingsSection              { get { if (_mirrorGizmoSettingsSection == null) _mirrorGizmoSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _mirrorGizmoSettingsSection; } }
        private UISection           modularSnapSettingsSection              { get { if (_modularSnapSettingsSection == null) _modularSnapSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _modularSnapSettingsSection; } }
        private UISection           surfaceSnapSettingsSection              { get { if (_surfaceSnapSettingsSection == null) _surfaceSnapSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _surfaceSnapSettingsSection; } }
        private UISection           projectionSettingsSection               { get { if (_projectionSettingsSection == null) _projectionSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _projectionSettingsSection; } }
        private UISection           vertexSnapSettingsSection               { get { if (_vertexSnapSettingsSection == null) _vertexSnapSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _vertexSnapSettingsSection; } }
        private UISection           boxSnapSettingsSection                  { get { if (_boxSnapSettingsSection == null) _boxSnapSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _boxSnapSettingsSection; } }
        private UISection           selectionGrowSettingsSection            { get { if (_selectionGrowSettingsSection == null) _selectionGrowSettingsSection = ScriptableObject.CreateInstance<UISection>(); return _selectionGrowSettingsSection; } }

        private static string       uiSectionRowSeparator_Transform         { get { return "A"; } }
        private static string       uiSectionRowSeparator_Settings          { get { return "B"; } }
        private static string       uiSectionRowSeparator_TransformTools    { get { return "C"; } }
        private static string       uiSectionRowSeparator_Misc              { get { return "D"; } }

        public ObjectTransformUI        objectTransformUI                   { get { return transformUI; } }

        public static ObjectSelectionUI instance                            { get { return GSpawn.active.objectSelectionUI; } }

        public void setObjectTransformUIEnabled(bool enabled)
        {
            if (ready)
            {
                transformSection.contentContainer.SetEnabled(enabled);
            }
        }

        public void refreshObjectTransformUI()
        {
            if (ready)
            {
                transformUI.refresh();
                transformSection.setTitle(getTransformUISectionTitle());
            }
        }

        protected override void onRefresh()
        {
            refreshGizmoSelectionButtons();
            refreshGizmoPivotButton();
            refreshGizmoTransformSpaceButton();
            refreshObjectSelectShapeButtons();
            refreshViewSelectionButtons();
            refreshTooltips();
            transformUI.refreshTooltips();
            updateVisibility();
        }

        protected override void onBuild()
        {
            var toolbarContainer                    = new VisualElement();
            toolbarContainer.style.flexDirection    = FlexDirection.Row;
            contentContainer.Add(toolbarContainer);

            Toolbar toolbar                         = UI.createStylizedToolbar(toolbarContainer);
            toolbar.style.height                    = UIValues.mediumToolbarButtonSize + 2.0f;
            toolbar.style.borderRightColor          = UIValues.toolbarBorderColor;
            toolbar.style.borderRightWidth          = 1.0f;
            toolbar.style.borderBottomWidth         = 1.0f;
            toolbar.style.borderBottomColor         = UIValues.toolbarBorderColor;

            _moveGimzoBtn                           = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.moveGizmo, "", toolbar);
            _moveGimzoBtn.clicked                   += () =>
            {
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Move, true, true, ObjectSelectionGizmoId.Mirror);
                SceneViewEx.focus();
            };

            _rotationGizmoBtn                       = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.rotationGizmo, "", toolbar);
            _rotationGizmoBtn.clicked               += () =>
            {
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Rotate, true, true, ObjectSelectionGizmoId.Mirror);
                SceneViewEx.focus();
            };

            _scaleGizmoBtn                          = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.scaleGizmo, "", toolbar);
            _scaleGizmoBtn.clicked                  += () =>
            {
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Scale, true, true, ObjectSelectionGizmoId.Mirror);
                SceneViewEx.focus();
            };

            _universalGizmoBtn                      = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.universalGizmo, "", toolbar);
            _universalGizmoBtn.clicked              += () =>
            {
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Universal, true, true, ObjectSelectionGizmoId.Mirror);
                SceneViewEx.focus();
            };

            _extrudeGizmoBtn                        = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.extrudeGizmo, "", toolbar);
            _extrudeGizmoBtn.clicked                += () =>
            {
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Extrude, true, true, ObjectSelectionGizmoId.Mirror);
                SceneViewEx.focus();
            };

            _mirrorGizmoBtn                         = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.mirrorGizmo, "", toolbar);
            _mirrorGizmoBtn.clicked                 += () =>
            {
                ObjectSelectionGizmos.instance.setGizmoEnabled(ObjectSelectionGizmoId.Mirror, !ObjectSelectionGizmos.instance.isGizmoEnabled(ObjectSelectionGizmoId.Mirror), false);
                SceneViewEx.focus();
            };

            refreshGizmoSelectionButtons();

            toolbar                                 = UI.createStylizedToolbar(toolbarContainer);
            toolbar.style.height                    = UIValues.mediumToolbarButtonSize + 2.0f;
            toolbar.style.flexGrow                  = 1.0f;
            toolbar.style.borderBottomWidth         = 1.0f;
            toolbar.style.borderBottomColor         = UIValues.toolbarBorderColor;

            _gizmoPivotBtn                          = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.gizmoCenterPivot, "", toolbar);
            _gizmoPivotBtn.clicked                  += () => 
            { 
                ObjectSelectionGizmos.instance.transformPivot = ObjectSelectionGizmos.instance.transformPivot == ObjectGizmoTransformPivot.Center ? ObjectGizmoTransformPivot.Mesh : ObjectGizmoTransformPivot.Center; 
                SceneViewEx.focus();
            };
            _gizmoTransformSpaceBtn                 = UI.createSmallToolSelectionToolbarButton(TexturePool.instance.gizmoLocalTransformSpace, "", toolbar);;
            _gizmoTransformSpaceBtn.clicked         += () => 
            { 
                ObjectSelectionGizmos.instance.transformSpace = ObjectSelectionGizmos.instance.transformSpace == ObjectGizmoTransformSpace.Global ? ObjectGizmoTransformSpace.Local : ObjectGizmoTransformSpace.Global; 
                SceneViewEx.focus();
            };

            toolbar                                 = UI.createToolSelectionToolbar(contentContainer);
            _selectionRectBtn                       = UI.createMediumToolSelectionToolbarButton(TexturePool.instance.selectionRect, "", toolbar);
            _selectionRectBtn.style.marginTop       = 1.0f;
            _selectionRectBtn.clicked               += () => 
            { 
                ObjectSelection.instance.selectionShapeType = ObjectSelectionShape.Type.Rect;
                SceneViewEx.focus();
            };

            _selectionSegmentsBtn                   = UI.createMediumToolSelectionToolbarButton(TexturePool.instance.selectionSegments, "", toolbar);
            _selectionSegmentsBtn.style.marginTop   = 1.0f;
            _selectionSegmentsBtn.clicked           += () => 
            { 
                ObjectSelection.instance.selectionShapeType = ObjectSelectionShape.Type.Segments;
                SceneViewEx.focus();
            };

            _selectionBoxBtn                        = UI.createMediumToolSelectionToolbarButton(TexturePool.instance.selectionBox, "", toolbar);
            _selectionBoxBtn.style.marginTop        = 1.0f;
            _selectionBoxBtn.clicked                += () => 
            { 
                ObjectSelection.instance.selectionShapeType = ObjectSelectionShape.Type.Box;
                SceneViewEx.focus();
            };

            refreshGizmoPivotButton();
            refreshGizmoTransformSpaceButton();
            refreshObjectSelectShapeButtons();

            toolbar                                 = UI.createToolSelectionToolbar(contentContainer);
            _settingsBtn                            = UI.createToolbarButton(TexturePool.instance.settings, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_settingsBtn);
            _settingsBtn.style.marginTop            = 1.0f;
            _settingsBtn.tooltip                    = "Settings";
            _settingsBtn.clicked                    += () =>
            {
                _activeViewId = ViewId.Settings;
                updateVisibility();
                refreshViewSelectionButtons();
                SceneViewEx.focus();
            };

            _transformToolsBtn                      = UI.createToolbarButton(TexturePool.instance.transform, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_transformToolsBtn);
            _transformToolsBtn.style.marginTop      = 1.0f;
            _transformToolsBtn.tooltip              = "Transform tools";
            _transformToolsBtn.clicked              += () =>
            {
                _activeViewId = ViewId.TransformTools;
                updateVisibility();
                refreshViewSelectionButtons();
                SceneViewEx.focus();
            };

            _miscBtn                                = UI.createToolbarButton(TexturePool.instance.misc, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_miscBtn);
            _miscBtn.tooltip                        = "Misc";
            _miscBtn.clicked                        += () =>
            {
                _activeViewId = ViewId.Misc;
                updateVisibility();
                refreshViewSelectionButtons();
                SceneViewEx.focus();
            };
       
            transformSection.build(getTransformUISectionTitle(), TexturePool.instance.transform, true, contentContainer);
            transformUI.build(ObjectSelection.instance.objectCollection, transformSection.contentContainer);
            transformUI.visibilityCondition = () => { return uiVisibleAndReady; };
      
            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_Settings);
            selectionSettingsSection.build("Selection", TexturePool.instance.settings, true, contentContainer);
            ObjectSelection.instance.settings.buildUI(selectionSettingsSection.contentContainer);            

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_Settings);
            extrudeGizmoSettingsSection.build("Extrude Gizmo", TexturePool.instance.extrudeGizmo, true, contentContainer);
            ObjectSelectionGizmos.instance.extrudeGizmoSettings.buildUI(extrudeGizmoSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_Settings);
            mirrorGizmoSettingsSection.build("Mirror Gizmo", TexturePool.instance.mirrorGizmo, true, contentContainer);
            UI.createPositionField(PluginGizmo.positionPropertyName, ObjectSelectionGizmos.instance.mirrorGizmoSerializedObject, "Mirror gizmo position", mirrorGizmoSettingsSection.contentContainer);
            UI.createRotationField(PluginGizmo.rotationPropertyName, ObjectSelectionGizmos.instance.mirrorGizmoSerializedObject, "Mirror gizmo rotation", mirrorGizmoSettingsSection.contentContainer);
            ObjectSelectionGizmos.instance.mirrorGizmoSettings.buildUI(mirrorGizmoSettingsSection.contentContainer);
            UI.createUISectionRowSeparator(mirrorGizmoSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_TransformTools);
            modularSnapSettingsSection.build("Modular Snap", TexturePool.instance.modularSnapSpawn, true, contentContainer);
            ObjectSelection.instance.modularSnapSettings.buildUI(modularSnapSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_TransformTools);
            surfaceSnapSettingsSection.build("Surface Snap", TexturePool.instance.objectSurfaceSnap, true, contentContainer);
            ObjectSelection.instance.surfaceSnapSettings.buildUI(surfaceSnapSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_TransformTools);
            projectionSettingsSection.build("Projection", TexturePool.instance.projection, true, contentContainer);
            ObjectSelection.instance.projectionSettings.buildUI(projectionSettingsSection.contentContainer);

            VisualElement buttonsContainer          = new VisualElement();
            buttonsContainer.style.flexDirection    = FlexDirection.Row;
            projectionSettingsSection.contentContainer.Add(buttonsContainer);

            _projectOnGridBtn                       = new Button();
            _projectOnGridBtn.text                  = "Project on grid";
            _projectOnGridBtn.style.width           = UIValues.useDefaultsButtonWidth;
            _projectOnGridBtn.clicked               += () => { ObjectSelection.instance.projectOnGrid(PluginScene.instance.grid); };
            buttonsContainer.Add(_projectOnGridBtn);

            _projectOnObjectBtn                     = new Button();
            _projectOnObjectBtn.text                = "Project on object";            
            _projectOnObjectBtn.style.width         = 110.0f;
            _projectOnObjectBtn.style.marginLeft    = UIValues.actionButtonLeftMargin;
            _projectOnObjectBtn.clicked             += () => { ObjectSelection.instance.beginTransformSession(ObjectTransformSessionType.Projection); };
            buttonsContainer.Add(_projectOnObjectBtn);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_TransformTools);
            vertexSnapSettingsSection.build("Vertex Snap", TexturePool.instance.vertexSnap, true, contentContainer);
            ObjectSelection.instance.vertexSnapSettings.buildUI(vertexSnapSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_TransformTools);
            boxSnapSettingsSection.build("Box Snap", TexturePool.instance.boxSnap, true, contentContainer);
            ObjectSelection.instance.boxSnapSettings.buildUI(boxSnapSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer, uiSectionRowSeparator_Misc);
            selectionGrowSettingsSection.build("Selection Grow", TexturePool.instance.selectionGrow, true, contentContainer);
            ObjectSelection.instance.growSettings.buildUI(selectionGrowSettingsSection.contentContainer);

            var growBtn                 = new Button();
            selectionGrowSettingsSection.contentContainer.Add(growBtn);
            growBtn.text                = "Grow";
            growBtn.style.width         = UIValues.useDefaultsButtonWidth;
            growBtn.tooltip             = "Grow selection" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.grow);
            growBtn.clicked             += () => 
            {
                ObjectSelection.instance.grow();
            };

            updateVisibility();
        }

        private void refreshTooltips()
        {
            _projectOnGridBtn.tooltip       = ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.projectOnGrid, "Project selected objects on the scene grid.");
            _projectOnObjectBtn.tooltip     = ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.projectOnObject, "Project selected objects on an object that you pick in the scene.");
        }

        private string getTransformUISectionTitle()
        {
            if (ObjectSelection.instance.numSelectedObjects == 1)
                return "Transform (" + ObjectSelection.instance.numSelectedObjects + ") - " + ObjectSelection.instance.getSelectedObject(0).name;
            else 
                return ObjectSelection.instance.numSelectedObjects != 0 ? "Transform (" + ObjectSelection.instance.numSelectedObjects + ")" : "Transform";
        }

        private void refreshGizmoSelectionButtons()
        {
            _moveGimzoBtn.tooltip = "Move" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.moveGizmo);
            _moveGimzoBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

            _rotationGizmoBtn.tooltip = "Rotate" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.rotationGizmo);
            _rotationGizmoBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

            _scaleGizmoBtn.tooltip = "Scale" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.scaleGizmo);
            _scaleGizmoBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

            _universalGizmoBtn.tooltip = "Move/Rotate/Scale" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.universalGizmo);
            _universalGizmoBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

            _extrudeGizmoBtn.tooltip = "Extrude" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.extrudeGizmo);
            _extrudeGizmoBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

            _mirrorGizmoBtn.tooltip = "Mirror" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(GlobalShortcutNames.mirrorGizmo_Toggle);
            _mirrorGizmoBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

            var selectionGizmos = ObjectSelectionGizmos.instance;
            if (selectionGizmos.isGizmoEnabled(ObjectSelectionGizmoId.Move)) _moveGimzoBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
            if (selectionGizmos.isGizmoEnabled(ObjectSelectionGizmoId.Rotate)) _rotationGizmoBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
            if (selectionGizmos.isGizmoEnabled(ObjectSelectionGizmoId.Scale)) _scaleGizmoBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
            if (selectionGizmos.isGizmoEnabled(ObjectSelectionGizmoId.Universal)) _universalGizmoBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
            if (selectionGizmos.isGizmoEnabled(ObjectSelectionGizmoId.Extrude)) _extrudeGizmoBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
            if (selectionGizmos.isGizmoEnabled(ObjectSelectionGizmoId.Mirror)) _mirrorGizmoBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
        }

        private void refreshGizmoPivotButton()
        {
            if (ObjectSelectionGizmos.instance.transformPivot == ObjectGizmoTransformPivot.Center)
            {
                _gizmoPivotBtn.tooltip = "Center Pivot\r\n\r\nThe gizmo's position in placed in the center of the object selection.";
                _gizmoPivotBtn.style.backgroundImage = TexturePool.instance.gizmoCenterPivot;
            }
            else
            {
                _gizmoPivotBtn.tooltip = "Mesh Pivot\r\n\r\nThe gizmo's position is defined by the object's mesh pivot.";
                _gizmoPivotBtn.style.backgroundImage = TexturePool.instance.gizmoMeshPivot;
            }
        }

        private void refreshGizmoTransformSpaceButton()
        {
            if (ObjectSelectionGizmos.instance.transformSpace == ObjectGizmoTransformSpace.Global)
            {
                _gizmoTransformSpaceBtn.tooltip = "Global\r\n\r\nThe gizmo axes are aligned to the global coordinate system.";
                _gizmoTransformSpaceBtn.style.backgroundImage = TexturePool.instance.gizmoGlobalTransformSpace;
            }
            else
            {
                _gizmoTransformSpaceBtn.tooltip = "Local\r\n\r\nThe gizmo inherits the rotation of the object.";
                _gizmoTransformSpaceBtn.style.backgroundImage = TexturePool.instance.gizmoLocalTransformSpace;
            }
        }

        private void refreshObjectSelectShapeButtons()
        {
            _selectionRectBtn.tooltip = "Selection Rectangle" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.selectionRect, "Multi-object selection is performed using a selection rectangle.");
            _selectionRectBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

            _selectionSegmentsBtn.tooltip = "Selection Segments" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.selectionSegments, "Multi-object selection is performed using a chain of segments.");
            _selectionSegmentsBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

            _selectionBoxBtn.tooltip = "Selection Box" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.selectionBox, "Multi-object selection is performed using a selection box.");
            _selectionBoxBtn.style.unityBackgroundImageTintColor = UIValues.inactiveButtonTintColor;

            if (ObjectSelection.instance.selectionShapeType == ObjectSelectionShape.Type.Rect) _selectionRectBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
            else if (ObjectSelection.instance.selectionShapeType == ObjectSelectionShape.Type.Segments) _selectionSegmentsBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
            else if (ObjectSelection.instance.selectionShapeType == ObjectSelectionShape.Type.Box) _selectionBoxBtn.style.unityBackgroundImageTintColor = UIValues.activeButtonColor;
        }

        private void refreshViewSelectionButtons()
        {
            if (_activeViewId == ViewId.Settings)
            {
                _settingsBtn.style.backgroundColor          = UIValues.activeButtonColor;
                _transformToolsBtn.style.backgroundColor    = UIValues.inactiveButtonColor;
                _miscBtn.style.backgroundColor              = UIValues.inactiveButtonColor;
            }
            else
            if (_activeViewId == ViewId.TransformTools)
            {
                _settingsBtn.style.backgroundColor          = UIValues.inactiveButtonColor;
                _transformToolsBtn.style.backgroundColor    = UIValues.activeButtonColor;
                _miscBtn.style.backgroundColor              = UIValues.inactiveButtonColor;
            }
            else
            if (_activeViewId == ViewId.Misc)
            {
                _settingsBtn.style.backgroundColor          = UIValues.inactiveButtonColor;
                _transformToolsBtn.style.backgroundColor    = UIValues.inactiveButtonColor;
                _miscBtn.style.backgroundColor              = UIValues.activeButtonColor;
            }
        }

        private void updateVisibility()
        {
            bool visible = _activeViewId == ViewId.Settings;
            contentContainer.setChildrenDisplayVisible(uiSectionRowSeparator_Settings, visible);
            selectionSettingsSection.setVisible(visible);
            extrudeGizmoSettingsSection.setVisible(visible);
            mirrorGizmoSettingsSection.setVisible(visible);

            visible = _activeViewId == ViewId.TransformTools;
            contentContainer.setChildrenDisplayVisible(uiSectionRowSeparator_TransformTools, visible);
            modularSnapSettingsSection.setVisible(visible);
            surfaceSnapSettingsSection.setVisible(visible);
            projectionSettingsSection.setVisible(visible);
            vertexSnapSettingsSection.setVisible(visible);
            boxSnapSettingsSection.setVisible(visible);

            visible = _activeViewId == ViewId.Misc;
            contentContainer.setChildrenDisplayVisible(uiSectionRowSeparator_Misc, visible);
            selectionGrowSettingsSection.setVisible(visible);
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_transformSection);
            ScriptableObjectEx.destroyImmediate(_transformUI);
            ScriptableObjectEx.destroyImmediate(_selectionSettingsSection);
            ScriptableObjectEx.destroyImmediate(_extrudeGizmoSettingsSection);
            ScriptableObjectEx.destroyImmediate(_mirrorGizmoSettingsSection);
            ScriptableObjectEx.destroyImmediate(_modularSnapSettingsSection);
            ScriptableObjectEx.destroyImmediate(_surfaceSnapSettingsSection);
            ScriptableObjectEx.destroyImmediate(_projectionSettingsSection);
            ScriptableObjectEx.destroyImmediate(_vertexSnapSettingsSection);
            ScriptableObjectEx.destroyImmediate(_boxSnapSettingsSection);
            ScriptableObjectEx.destroyImmediate(_selectionGrowSettingsSection);
        }
    }
}
#endif