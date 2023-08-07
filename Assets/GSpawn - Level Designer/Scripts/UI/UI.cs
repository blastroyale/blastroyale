#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public static class UI
    {
        public class HeaderColumnDesc
        {
            public string   text;
            public float    width;
        }

        public enum ButtonStyle
        {
            Normal = 0,
            Push,
        }

        public static string uiSectionRowSeparatorName { get { return "UISectionRowSeparator"; } }

        public static ToolbarButton createSmallToolbarFilterPrefixButton(string tooltip, bool hasShiftClickAction, Toolbar toolbar)
        {
            var btn = UI.createToolbarButton(TexturePool.instance.filter, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            btn.tooltip = tooltip;
            if (hasShiftClickAction) btn.tooltip += "\nLeft click to enable all.\nShift + left click to disable all.";
            useDefaultMargins(btn);

            return btn;
        }

        public static ToolbarButton createSmallPrefabAssetPingToolbarButton(Toolbar toolbar)
        {
            var pingButton          = UI.createToolbarButton(TexturePool.instance.ping, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            pingButton.tooltip      = "Ping prefab in project view.";
            useDefaultMargins(pingButton);

            return pingButton;
        }

        public static ToolbarButton createSmallResetPrefabPreviewToolbarButton(Toolbar toolbar)
        {
            var cameraButton        = UI.createToolbarButton(TexturePool.instance.camera, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            cameraButton.tooltip    = "Reset preview.";
            useDefaultMargins(cameraButton);

            return cameraButton;
        }

        public static ToolbarButton createSmallResetPrefabPreviewsToolbarButton(Toolbar toolbar)
        {
            var cameraButton        = UI.createToolbarButton(TexturePool.instance.camera, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            cameraButton.tooltip    = "Reset prefab previews.";
            useDefaultMargins(cameraButton);

            return cameraButton;
        }

        public static ToolbarButton createSmallCreateNewToolbarButton(Toolbar toolbar)
        {
            var createNewButton     = UI.createToolbarButton(TexturePool.instance.createAddNew, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(createNewButton);

            return createNewButton;
        }

        public static ToolbarButton createSmallLoadPrefabsButton(Toolbar toolbar)
        {
            var loadPrefabsButton       = UI.createToolbarButton(TexturePool.instance.loadPrefabs, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            loadPrefabsButton.tooltip   = "Load the prefabs that are selected in the prefab manager.";
            UI.useDefaultMargins(loadPrefabsButton);

            return loadPrefabsButton;
        }

        public static Toolbar createToolSelectionToolbar(VisualElement parent)
        {
            var toolbar                         = UI.createStylizedToolbar(parent);
            parent.Add(toolbar);
            toolbar.style.height                = UIValues.mediumToolbarButtonSize + 2.0f;
            toolbar.style.flexGrow              = 1.0f;
            toolbar.style.borderBottomWidth     = 1.0f;
            toolbar.style.borderBottomColor     = UIValues.toolbarBorderColor;

            return toolbar;
        }

        public static ToolbarButton createSmallToolSelectionToolbarButton(Texture2D texture, string tooltip, Toolbar parent)
        {
            var btn                     = UI.createToolbarButton(texture, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, parent);
            UI.useDefaultMargins(btn);
            btn.tooltip                 = tooltip;
            btn.style.marginLeft        = 5.0f;
            btn.style.alignSelf         = Align.Center;

            return btn;
        }

        public static ToolbarButton createMediumToolSelectionToolbarButton(Texture2D texture, string tooltip, Toolbar parent)
        {
            var btn = UI.createToolbarButton(texture, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, parent);
            UI.useDefaultMargins(btn);
            btn.tooltip             = tooltip;
            btn.style.marginLeft    = 1.0f;
            btn.style.alignSelf     = Align.Center;

            return btn;
        }

        public static IMGUIContainer createIMGUIContainer(VisualElement parent)
        {
            var container               = new IMGUIContainer();
            parent.Add(container);
            container.style.marginLeft  = UIValues.imGUIContainerMarginLeft;

            return container;
        }

        public static VisualElement createFlexGrow(VisualElement parent)
        {
            VisualElement grow          = new VisualElement();
            parent.Add(grow);
            grow.style.flexGrow         = 1.0f;

            return grow;
        }

        public static VisualElement createRowSeparator(VisualElement parent)
        {
            VisualElement separator     = new VisualElement();
            parent.Add(separator);
            separator.style.height      = 10.0f;

            return separator;
        }

        public static VisualElement createUISectionRowSeparator(VisualElement parent)
        {
            VisualElement separator     = new VisualElement();
            separator.name              = uiSectionRowSeparatorName;
            parent.Add(separator);
            separator.style.height      = UIValues.uiSectionSeparatorSize;

            return separator;
        }

        public static VisualElement createUISectionRowSeparator(VisualElement parent, string name)
        {
            VisualElement separator     = new VisualElement();
            separator.name              = name;
            parent.Add(separator);
            separator.style.height      = UIValues.uiSectionSeparatorSize;

            return separator;
        }

        public static VisualElement createLineColumnSeparator(VisualElement parent)
        {
            VisualElement separator             = new VisualElement();
            parent.Add(separator);
            separator.style.width               = 1.0f;
            separator.style.borderRightWidth    = 1.0f;
            separator.style.borderRightColor    = UIValues.lineSeparatorBorderColor;

            return separator;
        }

        public static VisualElement createColumnSeparator(VisualElement parent)
        {
            VisualElement separator     = new VisualElement();
            parent.Add(separator);
            separator.style.width       = 5.0f;

            return separator;
        }

        public static Label createSectionLabel(string text, VisualElement parent)
        {
            Label label                         = new Label(text);
            parent.Add(label);
            label.style.marginLeft              = 3.0f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

            return label;
        }

        public static VisualElement createWindowMarginContainer(VisualElement parent)
        {
            var container               = new VisualElement();
            container.style.setMargins(UIValues.wndMargin);
            container.style.flexGrow    = 1.0f;
            parent.Add(container);

            return container;
        }

        public static VisualElement createColumnHeader(string text, VisualElement parent)
        {
            var header                          = new VisualElement();
            header.style.backgroundColor        = UIValues.headerBkColor;
            header.style.setBorderWidth(1.0f);
            header.style.setBorderColor(UIValues.listViewBorderColor);
            header.style.height                 = UIValues.headerHeight;
            parent.Add(header);

            var label                           = new Label(text);
            label.style.marginLeft              = UIValues.listItemLeftMargin;
            label.style.height                  = UIValues.headerHeight;
            label.style.marginTop               = header.style.height.value.value * 0.5f - label.style.height.value.value * 0.5f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign          = TextAnchor.MiddleLeft;
            header.Add(label);

            return header;
        }

        public static VisualElement createColumnHeader(List<HeaderColumnDesc> headerColumns, VisualElement parent)
        {
            var header                      = new VisualElement();
            header.style.backgroundColor    = UIValues.headerBkColor;
            header.style.setBorderWidth(1.0f);
            header.style.setBorderColor(UIValues.listViewBorderColor);
            header.style.height             = UIValues.headerHeight;
            header.style.flexDirection      = FlexDirection.Row;
            parent.Add(header);

            Label label = null;
            for (int sectionIndex = 0; sectionIndex < headerColumns.Count - 1; ++sectionIndex)
            {
                var section = headerColumns[sectionIndex];

                label                               = new Label(section.text);
                label.style.marginLeft              = UIValues.listItemLeftMargin;
                label.style.height                  = UIValues.headerHeight;
                label.style.marginTop               = header.style.height.value.value * 0.5f - label.style.height.value.value * 0.5f;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.unityTextAlign          = TextAnchor.MiddleLeft;
                label.style.width                   = section.width;
                header.Add(label);
            }

            label                                   = new Label(headerColumns[headerColumns.Count - 1].text);
            label.style.marginLeft                  = UIValues.listItemLeftMargin;
            label.style.height                      = UIValues.headerHeight;
            label.style.marginTop                   = header.style.height.value.value * 0.5f - label.style.height.value.value * 0.5f;
            label.style.unityFontStyleAndWeight     = FontStyle.Bold;
            label.style.unityTextAlign              = TextAnchor.MiddleLeft;
            header.Add(label);

            return header;
        }

        public static VisualElement createToolbarSpacer(Toolbar toolbar)
        {
            var spacer                      = new VisualElement();
            spacer.style.flexGrow           = 1.0f;
            spacer.style.flexDirection      = FlexDirection.Row;
            spacer.style.borderLeftColor    = UIValues.toolbarSpacerColor;
            spacer.style.borderLeftWidth    = 1.0f;
            toolbar.Add(spacer);

            return spacer;
        }

        public static VisualElement createHorizontalSpacer(VisualElement parent)
        {
            var spacer                      = new VisualElement();
            spacer.style.flexGrow           = 1.0f;
            spacer.style.flexDirection      = FlexDirection.Row;
            parent.Add(spacer);

            return spacer;
        }

        public static VisualElement createVerticalSpacer(VisualElement parent)
        {
            var spacer                      = new VisualElement();
            spacer.style.flexGrow           = 1.0f;
            spacer.style.flexDirection      = FlexDirection.Column;
            parent.Add(spacer);

            return spacer;
        }

        public static Toolbar createStylizedToolbar(VisualElement parent)
        {
            Toolbar toolbar                 = new Toolbar();
            toolbar.style.backgroundColor   = UIValues.toolbarBkColor;
            toolbar.style.flexShrink        = 0.0f;
            toolbar.style.borderBottomWidth = 1.0f;
            toolbar.style.borderBottomColor = UIValues.toolbarBorderColor;
            parent.Add(toolbar);

            return toolbar;
        }

        public static TextField createToolbarTextField(Toolbar toolbar)
        {
            var textField = new TextField();
            toolbar.Add(textField);

            return textField;
        }

        public static ToolbarButton createToolbarButton(string text, Toolbar toolbar)
        {
            var btn                 = new ToolbarButton();
            toolbar.Add(btn);
            btn.text                = text;
            btn.style.flexShrink    = 0.0f;

            return btn;
        }

        public static ToolbarButton createToolbarButton(Texture2D image, ButtonStyle btnStyle, float buttonSize, Toolbar toolbar)
        {
            var button                      = new ToolbarButton();
            button.style.setBackgroundImage(image, false);
            button.style.backgroundColor    = Color.white.createNewAlpha(0.0f);
            button.style.setBorderWidth(0.0f);
            button.style.setBorderRadius(0.0f);
            button.style.width              = buttonSize;
            button.style.height             = buttonSize;
            button.style.flexShrink         = 0.0f;

            if (btnStyle == ButtonStyle.Push) button.styleSheets.Add(UIValues.buttonStyles);
            if (toolbar != null) toolbar.Add(button);

            return button;
        }

        public static VisualElement createIcon(Texture2D image, VisualElement parent)
        {
            var icon                        = new VisualElement();
            icon.style.setBackgroundImage(image, true);
            icon.style.setBorderWidth(0.0f);
            icon.style.setBorderRadius(0.0f);
            icon.style.minWidth             = image.width;
            icon.style.minHeight            = image.height;
            icon.style.backgroundColor      = Color.white.createNewAlpha(0.0f);

            if (parent != null) parent.Add(icon);

            return icon;
        }

        public static VisualElement createIcon(Texture2D image, float iconSize, VisualElement parent)
        {
            var icon                    = new VisualElement();
            icon.style.setBackgroundImage(image, false);
            icon.style.setBorderWidth(0.0f);
            icon.style.setBorderRadius(0.0f);
            icon.style.minWidth         = iconSize;
            icon.style.minHeight        = iconSize;
            icon.style.width            = iconSize;
            icon.style.height           = iconSize;
            icon.style.backgroundColor  = Color.white.createNewAlpha(0.0f);

            if (parent != null) parent.Add(icon);

            return icon;
        }

        public static Button createButton(Texture2D image, ButtonStyle btnStyle, VisualElement parent)
        {
            var button                  = new Button();
            button.style.setBackgroundImage(image, true);
            button.style.setBorderWidth(0.0f);
            button.style.setBorderRadius(0.0f);
            button.style.minWidth       = image.width;
            button.style.minHeight      = image.height;

            if (btnStyle == ButtonStyle.Push) button.styleSheets.Add(UIValues.buttonStyles);
            if (parent != null) parent.Add(button);

            return button;
        }

        public static Button createButton(Texture2D image, ButtonStyle btnStyle, float buttonSize, VisualElement parent)
        {
            var button                  = new Button();
            button.style.setBackgroundImage(image, false);
            button.style.setBorderWidth(0.0f);
            button.style.setBorderRadius(0.0f);
            button.style.minWidth       = buttonSize;
            button.style.minHeight      = buttonSize;
            button.style.width          = buttonSize;
            button.style.height         = buttonSize;

            if (btnStyle == ButtonStyle.Push) button.styleSheets.Add(UIValues.buttonStyles);
            if (parent != null) parent.Add(button);

            return button;
        }

        public static Button createIconButton(Texture2D image, VisualElement parent)
        {
            var button                      = new Button();
            button.style.setBackgroundImage(image, true);
            button.style.setBorderWidth(0.0f);
            button.style.setBorderRadius(0.0f);
            button.style.minWidth           = image.width;
            button.style.minHeight          = image.height;
            button.style.backgroundColor    = Color.white.createNewAlpha(0.0f);

            if (parent != null) parent.Add(button);

            return button;
        }

        public static Button createIconButton(Texture2D image, float iconSize, VisualElement parent)
        {
            var button                      = new Button();
            button.style.setBackgroundImage(image, false);
            button.style.setBorderWidth(0.0f);
            button.style.setBorderRadius(0.0f);
            button.style.minWidth           = iconSize;
            button.style.minHeight          = iconSize;
            button.style.width              = iconSize;
            button.style.height             = iconSize;
            button.style.backgroundColor    = Color.white.createNewAlpha(0.0f);

            if (parent != null) parent.Add(button);

            return button;
        }

        public static void useDefaultMargins(ToolbarButton tbButton)
        {
            tbButton.style.marginLeft   = 3.0f;
            tbButton.style.marginTop    = 2.0f;
        }

        public static void useDefaultMargins(VisualElement element)
        {
            element.style.marginLeft    = 3.0f;
            element.style.marginTop     = 2.0f;
        }

        public static void clampTextField(TextField textField, float widthCut, VisualElement parent)
        {
            textField.style.width = parent.localBound.width - widthCut;
        }

        public static void clampSearchField(ToolbarSearchField searchField, VisualElement parent)
        {
            searchField.style.width = parent.localBound.width - 8;
        }

        public static void clampSearchField(EntitySearchField searchField, VisualElement parent)
        {
            searchField.style.width = parent.localBound.width - 8;
        }

        public static Label createPrefsTitleLabel(string title, VisualElement parent)
        {
            var label                           = new Label();
            parent.Add(label);
            label.text                          = title;
            label.style.fontSize                = 20;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginLeft              = UIValues.settingsMarginLeft;
            label.style.marginTop               = UIValues.prefsTitleMarginTop;

            return label;
        }

        public static Button createUseDefaultsButton(Action onClickedAction, VisualElement parent)
        {
            var useDefButton            = new Button();
            parent.Add(useDefButton);
            useDefButton.style.width    = UIValues.useDefaultsButtonWidth;
            useDefButton.text           = "Use Defaults";
            useDefButton.tooltip        = "Use default settings.";

            if (onClickedAction != null) useDefButton.clicked += onClickedAction;
            useDefButton.clicked        += () => SceneView.RepaintAll();

            return useDefButton;
        }

        public static Button createUseDefaultsButton(List<Action> onClickedActions, VisualElement parent)
        {
            var useDefButton            = new Button();
            parent.Add(useDefButton);
            useDefButton.style.width    = UIValues.useDefaultsButtonWidth;
            useDefButton.text           = "Use Defaults";
            useDefButton.tooltip        = "Use default settings.";

            if (onClickedActions != null && onClickedActions.Count != 0)
            {
                foreach (var action in onClickedActions)
                    useDefButton.clicked += action;
            }
            useDefButton.clicked += () => SceneView.RepaintAll();

            return useDefButton;
        }

        public static VisualElement createPositionField(string propertyName, SerializedObject serializedObject, string tooltip, VisualElement parent)
        {
            VisualElement rowFlex       = new VisualElement();
            rowFlex.style.flexDirection = FlexDirection.Row;
            parent.Add(rowFlex);

            Button refreshBtn           = createIconButton(TexturePool.instance.refresh, rowFlex);
            refreshBtn.tooltip          = "Reset position to 0.";
            refreshBtn.clicked          += () => { serializedObject.FindProperty(propertyName).vector3Value = Vector3.zero; serializedObject.ApplyModifiedProperties(); };
            useDefaultMargins(refreshBtn);

            var posField                = createVector3Field(propertyName, serializedObject, "Position", tooltip, rowFlex);
            posField.style.flexGrow     = 1.0f;
            return posField;
        }

        public static VisualElement createRotationField(string propertyName, SerializedObject serializedObject, string tooltip, VisualElement parent)
        {
            VisualElement rowFlex       = new VisualElement();
            rowFlex.style.flexDirection = FlexDirection.Row;
            parent.Add(rowFlex);

            Button refreshBtn           = createIconButton(TexturePool.instance.refresh, rowFlex);
            refreshBtn.tooltip          = "Reset rotation to 0.";
            refreshBtn.clicked          += () => { serializedObject.FindProperty(propertyName).vector3Value = Vector3.zero; serializedObject.ApplyModifiedProperties(); };
            useDefaultMargins(refreshBtn);

            var rotField = createVector3Field(propertyName, serializedObject, "Rotation", tooltip, rowFlex);
            rotField.style.flexGrow     = 1.0f;
            return rotField;
        }

        public static ColorField createColorField(string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var colorField              = new ColorField();
            parent.Add(colorField);
            colorField.label            = label;
            colorField.tooltip          = tooltip;
            colorField.name             = propertyName;

            bindColorProperty(colorField, propertyName, serializedObject);
            return colorField;
        }

        public static Toggle createToggle(string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var toggle                  = new Toggle();
            parent.Add(toggle);
            toggle.label                = label;
            toggle.tooltip              = tooltip;
            toggle.name                 = propertyName;

            bindBoolProperty(toggle, propertyName, serializedObject);
            return toggle;
        }

        public static MinMaxSlider createMinMaxSlider(string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var minMaxSlider            = new MinMaxSlider();
            parent.Add(minMaxSlider);
            minMaxSlider.label          = label;
            minMaxSlider.tooltip        = tooltip;
            minMaxSlider.name           = propertyName;

            bindVector2Property(minMaxSlider, propertyName, serializedObject);
            return minMaxSlider;
        }

        public static Slider createSlider(string propertyName, SerializedObject serializedObject, string label, string tooltip, float minVal, float maxVal, VisualElement parent)
        {
            var slider              = new Slider();
            parent.Add(slider);
            slider.lowValue         = minVal;
            slider.highValue        = maxVal;
            slider.name             = propertyName;
            slider.tooltip          = tooltip;

            bindFloatProperty(slider, propertyName, serializedObject);
            return slider;
        }

        public static IntegerField createIntegerField(string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var intField            = new IntegerField();
            parent.Add(intField);
            intField.label          = label;
            intField.tooltip        = tooltip;
            intField.name           = propertyName;

            bindIntProperty(intField, propertyName, serializedObject);
            return intField;
        }

        public static IntegerField createIntegerField(string propertyName, SerializedObject serializedObject, string label, string tooltip, int minValue, VisualElement parent)
        {
            var intField            = new IntegerField();
            parent.Add(intField);
            intField.label          = label;
            intField.tooltip        = tooltip;
            intField.name           = propertyName;
            intField.isDelayed      = true;

            bindIntProperty(intField, propertyName, serializedObject, minValue);
            return intField;
        }

        public static IntegerField createIntegerField(string propertyName, SerializedObject serializedObject, string label, string tooltip, int minValue, int maxValue, VisualElement parent)
        {
            var intField            = new IntegerField();
            parent.Add(intField);
            intField.label          = label;
            intField.tooltip        = tooltip;
            intField.name           = propertyName;

            bindIntProperty(intField, propertyName, serializedObject, minValue, maxValue);
            return intField;
        }

        public static FloatField createFloatField(string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var floatField          = new FloatField();
            parent.Add(floatField);
            floatField.label        = label;
            floatField.tooltip      = tooltip;
            floatField.name         = propertyName;

            bindFloatProperty(floatField, propertyName, serializedObject);
            return floatField;
        }

        public static FloatField createFloatField(string propertyName, SerializedObject serializedObject, string label, string tooltip, float minValue, VisualElement parent)
        {
            var floatField          = new FloatField();
            parent.Add(floatField);
            floatField.label        = label;
            floatField.tooltip      = tooltip;
            floatField.name         = propertyName;
            floatField.isDelayed    = true;        // Note: To allow for minValue.

            bindFloatProperty(floatField, propertyName, serializedObject, minValue);
            return floatField;
        }

        public static FloatField createFloatField(string propertyName, SerializedObject serializedObject, string label, string tooltip, float minValue, float maxValue, VisualElement parent)
        {
            var floatField          = new FloatField();
            parent.Add(floatField);
            floatField.label        = label;
            floatField.tooltip      = tooltip;
            floatField.name         = propertyName;
            floatField.isDelayed    = true;        // Note: To allow for minValue and maxValue.

            bindFloatProperty(floatField, propertyName, serializedObject, minValue, maxValue);
            return floatField;
        }

        public static EnumField createEnumField(Type enumType, string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var enumField           = new EnumField();
            enumField.Init(EnumEx.findValue(0, enumType));
            parent.Add(enumField);
            enumField.label         = label;
            enumField.tooltip       = tooltip;
            enumField.name          = propertyName;

            bindEnumProperty(enumField, enumType, propertyName, serializedObject);
            return enumField;
        }

        public static EnumFlagsField createEnumFlagsField(Type enumType, string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var enumField           = new EnumFlagsField();
            enumField.Init(EnumEx.findValue(0, enumType));
            parent.Add(enumField);
            enumField.label         = label;
            enumField.tooltip       = tooltip;
            enumField.name          = propertyName;

            bindEnumFlagsProperty(enumField, enumType, propertyName, serializedObject);
            return enumField;
        }

        public static LayerField createLayerField(int defaultValue, string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var layerField          = new LayerField(label, defaultValue);
            parent.Add(layerField);
            layerField.tooltip      = tooltip;
            layerField.name         = propertyName;

            bindLayerProperty(layerField, propertyName, serializedObject);
            return layerField;
        }

        public static LayerMaskField createLayerMaskField(int defaultValue, string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var layerMask           = new LayerMaskField(label, defaultValue);
            parent.Add(layerMask);
            layerMask.tooltip       = tooltip;

            bindLayerMaskProperty(layerMask, propertyName, serializedObject);
            return layerMask;
        }

        public static Vector3Field createVector3Field(string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var vector3Field        = new Vector3Field();
            parent.Add(vector3Field);
            vector3Field.label      = label;
            vector3Field.tooltip    = tooltip;
            vector3Field.name       = propertyName;

            bindVector3Property(vector3Field, propertyName, serializedObject);
            return vector3Field;
        }

        public static Vector3Field createVector3Field(string propertyName, SerializedObject serializedObject, string label, string tooltip, Vector3 minValue, VisualElement parent)
        {
            var vector3Field        = new Vector3Field();
            parent.Add(vector3Field);
            vector3Field.label      = label;
            vector3Field.tooltip    = tooltip;
            vector3Field.name       = propertyName;

            bindVector3Property(vector3Field, propertyName, minValue, serializedObject);
            return vector3Field;
        }

        public static Vector3Field createVector3Field(string propertyName, SerializedObject serializedObject, string label, string tooltip, Vector3Int minValue, VisualElement parent)
        {
            var vector3Field         = new Vector3Field();
            parent.Add(vector3Field);
            vector3Field.label      = label;
            vector3Field.tooltip    = tooltip;
            vector3Field.name       = propertyName;

            bindVector3Property(vector3Field, propertyName, serializedObject);
            return vector3Field;
        }

        public static Vector3IntField createVector3IntField(string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var vector3Field        = new Vector3IntField();
            parent.Add(vector3Field);
            vector3Field.label      = label;
            vector3Field.tooltip    = tooltip;
            vector3Field.name       = propertyName;

            bindVector3IntProperty(vector3Field, propertyName, serializedObject);
            return vector3Field;
        }

        public static Vector3IntField createVector3IntField(string propertyName, SerializedObject serializedObject, string label, string tooltip, Vector3Int minValue, VisualElement parent)
        {
            var vector3Field        = new Vector3IntField();
            parent.Add(vector3Field);
            vector3Field.label      = label;
            vector3Field.tooltip    = tooltip;
            vector3Field.name       = propertyName;

            bindVector3IntProperty(vector3Field, propertyName, minValue, serializedObject);
            return vector3Field;
        }

        public static TextField createTextField(string propertyName, SerializedObject serializedObject, string label, string tooltip, VisualElement parent)
        {
            var textField           = new TextField();
            parent.Add(textField);
            textField.label         = label;
            textField.tooltip       = tooltip;
            textField.name          = propertyName;

            bindStringProperty(textField, propertyName, serializedObject);
            return textField;
        }

        public static ObjectField createGameObjectField(string propertyName, SerializedObject serializedObject, bool onlySceneObjects, string label, string tooltip, VisualElement parent)
        {
            ObjectField objectField         = new ObjectField();
            parent.Add(objectField);
            objectField.label               = label;
            objectField.tooltip             = tooltip;
            objectField.name                = propertyName;
            objectField.allowSceneObjects   = true;
            objectField.objectType          = typeof(GameObject);

            bindGameObjectProperty(objectField, propertyName, serializedObject, onlySceneObjects);
            return objectField;
        }

        public static void bindColorProperty(ColorField colorField, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            colorField.BindProperty(property);
            colorField.RegisterValueChangedCallback(p => { property.colorValue = p.newValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindBoolProperty(Toggle toggle, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            toggle.BindProperty(property);
            toggle.RegisterValueChangedCallback(p => { property.boolValue = p.newValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindIntProperty(IntegerField intField, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            intField.BindProperty(property);
            intField.RegisterValueChangedCallback(p => { property.intValue = p.newValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindIntProperty(IntegerField intField, string propertyName, SerializedObject serializedObject, int minValue)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            intField.BindProperty(property);
            intField.RegisterValueChangedCallback(p => { property.intValue = Mathf.Max(p.newValue, minValue); serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindIntProperty(IntegerField intField, string propertyName, SerializedObject serializedObject, int minValue, int maxValue)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            intField.BindProperty(property);
            intField.RegisterValueChangedCallback(p => { property.intValue = Mathf.Clamp(p.newValue, minValue, maxValue); serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindFloatProperty(Slider slider, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            slider.BindProperty(property);
            slider.RegisterValueChangedCallback(p => { property.floatValue = p.newValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindFloatProperty(FloatField floatField, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            floatField.BindProperty(property);
            floatField.RegisterValueChangedCallback(p => { property.floatValue = p.newValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindFloatProperty(FloatField floatField, string propertyName, SerializedObject serializedObject, float minValue)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            floatField.BindProperty(property);
            floatField.RegisterValueChangedCallback(p => { property.floatValue = Mathf.Max(minValue, p.newValue); floatField.value = property.floatValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindFloatProperty(FloatField floatField, string propertyName, SerializedObject serializedObject, float minValue, float maxValue)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            floatField.BindProperty(property);
            floatField.RegisterValueChangedCallback(p => { property.floatValue = Mathf.Clamp(p.newValue, minValue, maxValue); floatField.value = property.floatValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindEnumProperty(EnumField enumField, Type enumType, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property            = serializedObject.FindProperty(propertyName);
            enumField.value         = EnumEx.findValue(property.enumValueIndex, enumType);
            enumField.bindingPath   = propertyName;
            enumField.Bind(serializedObject);
            enumField.RegisterCallback<ChangeEvent<Enum>>(p => { property.enumValueIndex = EnumEx.findValueIndex(p.newValue, enumType); serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindEnumFlagsProperty(EnumFlagsField enumField, Type enumType, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property            = serializedObject.FindProperty(propertyName);
            enumField.value         = (Enum)Enum.ToObject(enumType, property.intValue);
            enumField.bindingPath   = propertyName;
            enumField.Bind(serializedObject);
            enumField.RegisterCallback<ChangeEvent<Enum>>(p => 
            { 
                property.intValue = Convert.ToInt32(p.newValue); serializedObject.ApplyModifiedProperties(); 
            });
        }

        public static void bindLayerProperty(LayerField layerField, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            layerField.BindProperty(property);
        }

        public static void bindLayerMaskProperty(LayerMaskField layerMaskField, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            layerMaskField.BindProperty(property);
        }

        public static void bindVector2Property(MinMaxSlider minMaxSlider, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            minMaxSlider.BindProperty(property);
            minMaxSlider.RegisterValueChangedCallback(p => { property.vector2Value = p.newValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindVector3Property(Vector3Field vector3Field, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            vector3Field.BindProperty(property);
            vector3Field.RegisterValueChangedCallback(p => { property.vector3Value = p.newValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindVector3Property(Vector3Field vector3Field, string propertyName, Vector3 minValue, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            vector3Field.BindProperty(property);
            vector3Field.RegisterValueChangedCallback(p => { property.vector3Value = Vector3.Max(p.newValue, minValue); vector3Field.value = property.vector3Value; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindVector3IntProperty(Vector3IntField vector3Field, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            vector3Field.BindProperty(property);
            vector3Field.RegisterValueChangedCallback(p => { property.vector3IntValue = p.newValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindVector3IntProperty(Vector3IntField vector3Field, string propertyName, Vector3Int minValue, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            vector3Field.BindProperty(property);
            vector3Field.RegisterValueChangedCallback(p => { property.vector3IntValue = Vector3Int.Max(p.newValue, minValue); vector3Field.value = property.vector3IntValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindStringProperty(TextField textField, string propertyName, SerializedObject serializedObject)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            textField.BindProperty(property);
            textField.RegisterValueChangedCallback(p => { property.stringValue = p.newValue; serializedObject.ApplyModifiedProperties(); });
        }

        public static void bindGameObjectProperty(ObjectField objectField, string propertyName, SerializedObject serializedObject, bool onlySceneObjects)
        {
            serializedObject.UpdateIfRequiredOrScript();        // Note: To allow for correct handling of Undo/Redo callbacks
            var property = serializedObject.FindProperty(propertyName);
            objectField.BindProperty(property);
            objectField.RegisterValueChangedCallback(p => 
            {
                GameObject newGameObject = p.newValue as GameObject;
                if (newGameObject != null)
                {
                    if (!onlySceneObjects || newGameObject.isSceneObject())
                    {
                        property.objectReferenceValue = p.newValue;
                        serializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        // Note: This seems to be ignored. It resets the property to null
                        //       even though p.previousValue is not null.
                        property.objectReferenceValue = p.previousValue;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            });
        }
    }
}
#endif