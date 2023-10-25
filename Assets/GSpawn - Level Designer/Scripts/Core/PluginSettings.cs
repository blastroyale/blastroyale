#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public struct PluginSettingsUIBuildConfig
    {
        public Action   onUseDefaults                   { get; set; }
        public bool     createUseDefaultsButton;
        public bool     applyMargins;

        public static readonly 
            PluginSettingsUIBuildConfig defaultConfig   = new PluginSettingsUIBuildConfig()
        {
            createUseDefaultsButton = true,
            applyMargins = true
        };
    }

    public abstract class PluginSettings<T> : ScriptableObject
        where T : PluginSettings<T>
    {
        private SerializedObject _serializedObject;

        public  SerializedObject serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }

        public string uiFieldContainerName { get { return typeof(T).FullName; } }

        public void buildDefaultUI(VisualElement parent, PluginSettingsUIBuildConfig config)
        {
            parent.Query<VisualElement>(uiFieldContainerName).ForEach(item => parent.Remove(item));

            var parentContainer = new ScrollView(ScrollViewMode.Vertical);
            parentContainer.name = uiFieldContainerName;
            parentContainer.contentContainer.style.flexWrap = Wrap.NoWrap;
            parentContainer.contentContainer.style.flexDirection = FlexDirection.Column;
            parent.Add(parentContainer);

            var fields = ReflectionEx.getPrivateInstanceFields(typeof(T));
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                UIFieldConfig uiBindAttrib = (UIFieldConfig)field.GetCustomAttributes(typeof(UIFieldConfig), false)[0];
                if (uiBindAttrib.rowSeparator) UI.createRowSeparator(parentContainer);
                if (uiBindAttrib.sectionLabel != string.Empty)
                {
                    var label = UI.createSectionLabel(uiBindAttrib.sectionLabel, parentContainer);
                    label.style.marginLeft = 3.0f;
                }

                if (fieldType == typeof(float))
                {
                    FloatField floatField       = null;
                    MinAttribute[] minAttribs   = (MinAttribute[])field.GetCustomAttributes(typeof(MinAttribute), false);
                    RangeAttribute[] rangeAttribs = (RangeAttribute[])field.GetCustomAttributes(typeof(RangeAttribute), false);

                    if (rangeAttribs.Length != 0) floatField = UI.createFloatField(field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, rangeAttribs[0].min, rangeAttribs[0].max, parentContainer);
                    else if (minAttribs.Length != 0) floatField = UI.createFloatField(field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, minAttribs[0].min, parentContainer);
                    else floatField = UI.createFloatField(field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, parentContainer);

                    floatField.name = field.Name;
                }
                else
                if (fieldType == typeof(int))
                {
                    IntegerField intField       = null;
                    MinAttribute[] minAttribs   = (MinAttribute[])field.GetCustomAttributes(typeof(MinAttribute), false);

                    if (minAttribs.Length != 0) intField = UI.createIntegerField(field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, (int)minAttribs[0].min, parentContainer);
                    else intField = UI.createIntegerField(field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, parentContainer);

                    intField.name = field.Name;
                }
                else
                if (fieldType == typeof(Color))
                {
                    var colorField = UI.createColorField(field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, parentContainer);
                    colorField.name = field.Name;
                }
                else
                if (fieldType == typeof(bool))
                {
                    var toggle = UI.createToggle(field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, parentContainer);
                    toggle.name = field.Name;
                }
                else if (fieldType.IsEnum)
                {
                    if (fieldType.GetCustomAttributes(typeof(FlagsAttribute), false).Length != 0)
                    {
                        var enumField = UI.createEnumFlagsField(fieldType, field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, parentContainer);
                        enumField.name = field.Name;
                    }
                    else
                    {
                        var enumField = UI.createEnumField(fieldType, field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, parentContainer);
                        enumField.name = field.Name;
                    }
                }
                else
                if (fieldType == typeof(Vector3))
                {
                    Vector3Field vecField = null;
                    MinAttribute[] minAttribs = (MinAttribute[])field.GetCustomAttributes(typeof(MinAttribute), false);
                    if (minAttribs.Length != 0) vecField = UI.createVector3Field(field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, Vector3Ex.create(minAttribs[0].min), parentContainer);
                    else vecField = UI.createVector3Field(field.Name, serializedObject, uiBindAttrib.label, uiBindAttrib.tooltip, parentContainer);
                    vecField.name = field.Name;
                }
            }

            if (config.createUseDefaultsButton)
            {
                var actions = new List<Action>();
                actions.Add(() => useDefaults());
                if (config.onUseDefaults != null) actions.Add(config.onUseDefaults);
                UI.createUseDefaultsButton(actions, parentContainer);
            }

            if (config.applyMargins)
                parent.setChildrenMarginLeft(UIValues.settingsMarginLeft);

            var labels = parent.Q<Label>();
        }

        public abstract void useDefaults();

        protected virtual void onDestroy() { }

        private void OnDestroy()
        {
            onDestroy();
        }
    }
}
#endif