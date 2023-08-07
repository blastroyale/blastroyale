#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;

namespace GSpawn
{
    [Flags]
    public enum ObjectModularSnapSurfaceTypes
    {
        None = 0,
        Grid = 1,
        Objects = 2,
        All = ~0
    }

    public class ObjectModularSnapSettings : PluginSettings<ObjectModularSnapSettings>
    {
        [SerializeField]
        private bool                                _snapSingleTargetToCursor           = defaultSnapSingleTargetToCursor;
        [SerializeField]
        private bool                                _gridSnapClimb                      = defaultGridSnapClimb;
        [SerializeField]
        private float                               _snapRadius                         = defaultSnapRadius;

        [SerializeField]
        private ObjectModularSnapSurfaceTypes       _surfaceTypes                       = defaultSurfaceTypes;
        [SerializeField]
        private int                                 _destinationLayers                  = defaultDestinationLayers;

        public bool                                 snapSingleTargetToCursor            { get { return _snapSingleTargetToCursor; } set { UndoEx.record(this); _snapSingleTargetToCursor = value; EditorUtility.SetDirty(this); } }
        public bool                                 gridSnapClimb                       { get { return _gridSnapClimb; } set { UndoEx.record(this); _gridSnapClimb = value; EditorUtility.SetDirty(this); } }
        public float                                snapRadius                          { get { return _snapRadius; } set { UndoEx.record(this); _snapRadius = Mathf.Clamp(value, 1e-4f, 1.0f); EditorUtility.SetDirty(this); } }
        public ObjectModularSnapSurfaceTypes        surfaceTypes                        { get { return _surfaceTypes; } set { UndoEx.record(this); _surfaceTypes = value; EditorUtility.SetDirty(this); } }
        public bool                                 allowsGridSurface                   { get { return (surfaceTypes & ObjectModularSnapSurfaceTypes.Grid) != 0; } }
        public bool                                 allowsObjectSurface                 { get { return (surfaceTypes & ObjectModularSnapSurfaceTypes.Objects) != 0; } }
        public int                                  destinationLayers                   { get { return _destinationLayers; } set { UndoEx.record(this); _destinationLayers = value; EditorUtility.SetDirty(this); } }

        public static bool                          defaultSnapSingleTargetToCursor     { get { return true; } }
        public static bool                          defaultInheritGridRotation          { get { return false; } }
        public static bool                          defaultGridSnapClimb                { get { return false; } }
        public static float                         defaultSnapRadius                   { get { return 0.7f; } }
        public static ObjectModularSnapSurfaceTypes defaultSurfaceTypes                 { get { return ObjectModularSnapSurfaceTypes.All; } }
        public static int                           defaultDestinationLayers            { get { return ~0; } }

        public override void useDefaults()
        {
            snapSingleTargetToCursor        = defaultSnapSingleTargetToCursor;
            gridSnapClimb                   = defaultGridSnapClimb;
            snapRadius                      = defaultSnapRadius;
            surfaceTypes                    = defaultSurfaceTypes;
            destinationLayers               = defaultDestinationLayers;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 120.0f;

            var guiContainer = UI.createIMGUIContainer(parent);
            guiContainer.onGUIHandler = () =>
            {
                var guiContent = new GUIContent();
                guiContent.text = "Grid snap climb";
                guiContent.tooltip = ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectTransformSessionsShortcutNames.modularSnap_ToggleGridSnapClimb,
                    "If checked, the targets will be able to climb objects hovered by the mouse cursor when grid snap mode is active.");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(guiContent, GUILayout.Width(labelWidth));
                EditorGUI.BeginChangeCheck();
                var newBool = EditorGUILayout.Toggle("", gridSnapClimb);
                if (EditorGUI.EndChangeCheck())  gridSnapClimb = newBool;
                EditorGUILayout.EndHorizontal();
            };

            var floatField      = UI.createFloatField("_snapRadius", serializedObject, "Snap radius", "The object-to-object snap radius.", 1e-4f, 1.0f, parent);
            floatField.setChildLabelWidth(labelWidth);

            var enumField       = UI.createEnumFlagsField(typeof(ObjectModularSnapSurfaceTypes), "_surfaceTypes", serializedObject, "Surface types", "Allows you to specify the types of entities that can be used as snap surface when object-to-object snap is enabled.", parent);
            enumField.setChildLabelWidth(labelWidth);

            var layerMaskField  = UI.createLayerMaskField(destinationLayers, "_destinationLayers", serializedObject, "Destination layers", "Allows you to specify the layers that can be used as object-to-object snap destination.", parent);
            layerMaskField.setChildLabelWidth(labelWidth);
            
            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }
    }
}
#endif