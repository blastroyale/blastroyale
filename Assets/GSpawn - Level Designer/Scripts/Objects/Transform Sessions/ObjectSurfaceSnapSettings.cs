#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;

namespace GSpawn
{
    [Flags]
    public enum ObjectSurfaceSnapSurfaceTypes
    {
        None = 0,
        Grid = 1,
        Mesh = 2,
        Sprite = 4,
        Terrain = 8,
        All = ~0
    }

    public enum ObjectSurfaceSnapMode
    {
        Pivot = 0,
        Volume
    }

    public class ObjectSurfaceSnapSettings : PluginSettings<ObjectSurfaceSnapSettings>
    {
        [SerializeField]
        private ObjectSurfaceSnapMode               _snapMode                           = defaultSnapMode;
        [SerializeField]
        private bool                                _snapSingleTargetToCursor           = defaultSnapSingleTargetToCursor;
        [SerializeField]
        private bool                                _alignAxis                          = defaultAlignAxis;
        [SerializeField]
        private FlexiAxis                           _alignmentAxis                      = defaultAlignmentAxis;
        [SerializeField]
        private bool                                _invertAlignmentAxis                = defaultInvertAlignmentAxis;
        [SerializeField]
        private float                               _implicitOffsetFromSurface          = defaultImplicitOffsetFromSurface;
        [SerializeField]
        private bool                                _embedInSurface                     = defaultEmbedInSurface;
        [SerializeField]
        private ObjectSurfaceSnapSurfaceTypes       _surfaceTypes                       = defaultSurfaceTypes;
        [SerializeField]
        private int                                 _surfaceLayers                      = defaultSurfaceLayers;

        public ObjectSurfaceSnapMode                snapMode                            { get { return _snapMode; } set { UndoEx.record(this); _snapMode = value; EditorUtility.SetDirty(this); } }
        public bool                                 snapSingleTargetToCursor            { get { return _snapSingleTargetToCursor; } set { UndoEx.record(this); _snapSingleTargetToCursor = value; EditorUtility.SetDirty(this); } }
        public bool                                 alignAxis                           { get { return _alignAxis; } set { UndoEx.record(this); _alignAxis = value; EditorUtility.SetDirty(this); } }
        public FlexiAxis                            alignmentAxis                       { get { return _alignmentAxis; } set { UndoEx.record(this); _alignmentAxis = value; EditorUtility.SetDirty(this); } }
        public bool                                 invertAlignmentAxis                 { get { return _invertAlignmentAxis; } set { UndoEx.record(this); _invertAlignmentAxis = value; EditorUtility.SetDirty(this); } }
        public float                                implicitOffsetFromSurface           { get { return _implicitOffsetFromSurface; } set { UndoEx.record(this); _implicitOffsetFromSurface = value; EditorUtility.SetDirty(this); } }
        public bool                                 embedInSurface                      { get { return _embedInSurface; } set { UndoEx.record(this); _embedInSurface = value; EditorUtility.SetDirty(this); } }
        public ObjectSurfaceSnapSurfaceTypes        surfaceTypes                        { get { return _surfaceTypes; } set { UndoEx.record(this); _surfaceTypes = value; EditorUtility.SetDirty(this); } }
        public bool                                 allowsGridSurface                   { get { return (surfaceTypes & ObjectSurfaceSnapSurfaceTypes.Grid) != 0; } }
        public bool                                 allowsMeshSurface                   { get { return (surfaceTypes & ObjectSurfaceSnapSurfaceTypes.Mesh) != 0; } }
        public bool                                 allowsSpriteSurface                 { get { return (surfaceTypes & ObjectSurfaceSnapSurfaceTypes.Sprite) != 0; } }
        public bool                                 allowsTerrainSurface                { get { return (surfaceTypes & ObjectSurfaceSnapSurfaceTypes.Terrain) != 0; } }
        public bool                                 allowsObjectSurface                 { get { return allowsMeshSurface || allowsTerrainSurface; } }
        public int                                  surfaceLayers                       { get { return _surfaceLayers; } set { UndoEx.record(this); _surfaceLayers = value; EditorUtility.SetDirty(this); } }

        public static ObjectSurfaceSnapMode         defaultSnapMode                     { get { return ObjectSurfaceSnapMode.Volume; } }
        public static bool                          defaultSnapSingleTargetToCursor     { get { return true; } }
        public static bool                          defaultSnapAsUnit                   { get { return false; } }
        public static bool                          defaultAlignAxis                    { get { return false; } }
        public static FlexiAxis                     defaultAlignmentAxis                { get { return FlexiAxis.Y; } }
        public static bool                          defaultInvertAlignmentAxis          { get { return false; } }
        public static float                         defaultImplicitOffsetFromSurface    { get { return 0.0f; } }
        public static bool                          defaultEmbedInSurface               { get { return true; } }
        public static ObjectSurfaceSnapSurfaceTypes defaultSurfaceTypes                 { get { return ObjectSurfaceSnapSurfaceTypes.All; } }
        public static int                           defaultSurfaceLayers                { get { return ~0; } }

        public override void useDefaults()
        {
            snapMode                    = defaultSnapMode;
            snapSingleTargetToCursor    = defaultSnapSingleTargetToCursor;
            alignAxis                   = defaultAlignAxis;
            alignmentAxis               = defaultAlignmentAxis;
            invertAlignmentAxis         = defaultInvertAlignmentAxis;
            implicitOffsetFromSurface   = defaultImplicitOffsetFromSurface;
            embedInSurface              = defaultEmbedInSurface;
            surfaceTypes                = defaultSurfaceTypes;
            surfaceLayers               = defaultSurfaceLayers;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 120.0f;

            VisualElement ctrl = UI.createEnumField(typeof(ObjectSurfaceSnapMode), "_snapMode", serializedObject, "Snap mode", 
                "Controls the way on which objects are snapped to the surface. 'Pivot' snaps the object position, while 'Volume' snaps the entire object volume.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            var guiContainer = UI.createIMGUIContainer(parent);
            guiContainer.onGUIHandler = () =>
            {
                var guiContent = new GUIContent();
                guiContent.text = "Align axis";
                guiContent.tooltip = ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectTransformSessionsShortcutNames.surfaceSnap_ToggleAxisAlignment,
                    "If this is checked, the objects will have their axis aligned to the snap surface normal.");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(guiContent, GUILayout.Width(labelWidth));
                EditorGUI.BeginChangeCheck();
                var newBool = EditorGUILayout.Toggle("", alignAxis);
                if (EditorGUI.EndChangeCheck()) alignAxis = newBool;
                EditorGUILayout.EndHorizontal();
            };
           
            ctrl = UI.createEnumField(typeof(FlexiAxis), "_alignmentAxis", serializedObject, "Alignment axis", "If axis alignment is turned on, this is the axis which will be used for alignment.", parent);
            ctrl.setChildLabelWidth(labelWidth);
            
            ctrl = UI.createToggle("_invertAlignmentAxis", serializedObject, "Invert axis", "If this is checked, the alignment axis will be inverted.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createFloatField("_implicitOffsetFromSurface", serializedObject, "Implicit offset", "An offset value that will be applied to all object positions to push them away from or inside the snap surface.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_embedInSurface", serializedObject, "Embed in surface", 
                "If checked, objects that float above the snap surface will be pushed inside to prevent floating. Note: Only applies when snap mode is set to 'Volume'.", parent);
            ctrl.setChildLabelWidth(labelWidth);
            
            ctrl = UI.createEnumFlagsField(typeof(ObjectSurfaceSnapSurfaceTypes), "_surfaceTypes", serializedObject, "Surface types", "Allows you to specify the types of entities that can be used as snap surface.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createLayerMaskField(surfaceLayers, "_surfaceLayers", serializedObject, "Surface layers", "Allows you to specify the layers that can be used as snap surface.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }
    }
}
#endif