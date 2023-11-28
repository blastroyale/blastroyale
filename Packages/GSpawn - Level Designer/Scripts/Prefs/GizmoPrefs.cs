#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace GSpawn
{
    public class GizmoPrefs : Prefs<GizmoPrefs>
    {
        [SerializeField][UIFieldConfig("Wire color", "The wire color used to draw the extrude gizmo box.", "Extrude Gizmo", false)]
        private Color       _extrudeWireColor               = defaultExtrudeWireColor;
        [SerializeField][UIFieldConfig("X axis color", "The color of the X axis handles.")]
        private Color       _extrudeXAxisColor              = defaultExtrudeXAxisColor;
        [SerializeField][UIFieldConfig("Y axis color", "The color of the Y axis handles.")]
        private Color       _extrudeYAxisColor              = defaultExtrudeYAxisColor;
        [SerializeField][UIFieldConfig("Z axis color", "The color of the Z axis handles.")]
        private Color       _extrudeZAxisColor              = defaultExtrudeZAxisColor;
        [SerializeField][UIFieldConfig("Sgl-axis handle size", "The size of the single-axis handles.")][Min(1e-4f)]
        private float       _extrudeSglAxisSize             = defaultExtrudeSglAxisSize;
        [SerializeField][UIFieldConfig("Extrude cell wire color", "The wire color used to draw the extrude cells.")]
        private Color       _extrudeCellWireColor           = defaultExtrudeCellWireColor;
        [SerializeField][UIFieldConfig("Extrude cell fill color", "The fill color used to draw the extrude cells.")]
        private Color       _extrudeCellFillColor           = defaultExtrudeCellFillColor;
        [SerializeField][UIFieldConfig("Dbl-axis tick size", "The size of the double-axis ticks.")][Min(1e-4f)]
        private float       _extrudeDblAxisSize             = defaultExtrudeDblAxisSize;
        [SerializeField][UIFieldConfig("Show info text", "If checked, the plugin will offer textual information during while using the extrude gizmo.")]
        private bool        _extrudeShowInfoText            = defaultExtrudeShowInfoText;
        [SerializeField][UIFieldConfig("Select spawned", "If this is checked, all objects which were spawned as a result of the extrusion, will be selected.")]
        private bool        _extrudeSelectSpawned           = defaultExtrudeSelectSpawned;
        [SerializeField][UIFieldConfig("Plane size", "The mirror plane size.", "Mirror Gizmo", true)][Min(1e-4f)]
        private float       _mirrorPlaneSize                = defaultMirrorPlaneSize;
        [SerializeField][UIFieldConfig("XY plane color", "The color of the mirror XY plane.")]
        private Color       _mirrorXYPlaneColor             = defaultMirrorXYPlaneColor;
        [SerializeField][UIFieldConfig("YZ plane color", "The color of the mirror YZ plane.")]
        private Color       _mirrorYZPlaneColor             = defaultMirrorYZPlaneColor;
        [SerializeField][UIFieldConfig("ZX plane color", "The color of the mirror ZX plane.")]
        private Color       _mirrorZXPlaneColor             = defaultMirrorZXPlaneColor;
        [SerializeField][UIFieldConfig("Plane border color", "The border color of the mirror planes.")]
        private Color       _mirrorPlaneBorderColor         = defaultMirrorPlaneBorderColor;
        [SerializeField][UIFieldConfig("Indicator wire color", "The wire color used to draw the mirrored object volumes.")]
        private Color       _mirrorIndicatorWireColor       = defaultMirrorIndicatorWireColor;
        [SerializeField][UIFieldConfig("Indicator fill color", "The fill color used to draw the mirrored object volumes.")]
        private Color       _mirrorIndicatorFillColor               = defaultMirrorIndicatorFillColor;
        [SerializeField][UIFieldConfig("Symmetric pair highlight radius", "The radius used to collect symmetric object pairs.")][Min(0.1f)]
        private float       _mirrorSymmetricPairHighlightRadius    = defaultMirrorSymmetricPairHighlightRadius;
        [SerializeField][UIFieldConfig("Show info text", "If checked, the plugin will offer textual information during while using the mirror gizmo.")]
        private bool        _mirrorShowInfoText             = defaultMirrorShowInfoText;

        public Color        extrudeWireColor                            { get { return _extrudeWireColor; } set { UndoEx.record(this); _extrudeWireColor = value; EditorUtility.SetDirty(this); } }
        public Color        extrudeXAxisColor                           { get { return _extrudeXAxisColor; } set { UndoEx.record(this); _extrudeXAxisColor = value; EditorUtility.SetDirty(this); } }
        public Color        extrudeYAxisColor                           { get { return _extrudeYAxisColor; } set { UndoEx.record(this); _extrudeYAxisColor = value; EditorUtility.SetDirty(this); } }
        public Color        extrudeZAxisColor                           { get { return _extrudeZAxisColor; } set { UndoEx.record(this); _extrudeZAxisColor = value; EditorUtility.SetDirty(this); } }
        public float        extrudeSglHandleSize                        { get { return _extrudeSglAxisSize; } set { UndoEx.record(this); _extrudeSglAxisSize = Mathf.Max(1e-4f, value); EditorUtility.SetDirty(this); } }
        public Color        extrudeCellWireColor                        { get { return _extrudeCellWireColor; } set { UndoEx.record(this); _extrudeCellWireColor = value; EditorUtility.SetDirty(this); } }
        public Color        extrudeCellFillColor                        { get { return _extrudeCellFillColor; } set { UndoEx.record(this); _extrudeCellFillColor = value; EditorUtility.SetDirty(this); } }
        public float        extrudeDblAxisSize                          { get { return _extrudeDblAxisSize; } set { UndoEx.record(this); _extrudeDblAxisSize = Mathf.Max(1e-4f, value); EditorUtility.SetDirty(this); } }
        public bool         extrudeShowInfoText                         { get { return _extrudeShowInfoText; } set { UndoEx.record(this); _extrudeShowInfoText = value; EditorUtility.SetDirty(this); } }
        public bool         extrudeSelectSpawned                        { get { return _extrudeSelectSpawned; } set { UndoEx.record(this); _extrudeSelectSpawned = value; EditorUtility.SetDirty(this); } }
        public float        mirrorPlaneSize                             { get { return _mirrorPlaneSize; } set { UndoEx.record(this); _mirrorPlaneSize = Mathf.Max(1e-4f, value); EditorUtility.SetDirty(this); } }
        public Color        mirrorXYPlaneColor                          { get { return _mirrorXYPlaneColor; } set { UndoEx.record(this); _mirrorXYPlaneColor = value; EditorUtility.SetDirty(this); } }
        public Color        mirrorYZPlaneColor                          { get { return _mirrorYZPlaneColor; } set { UndoEx.record(this); _mirrorYZPlaneColor = value; EditorUtility.SetDirty(this); } }
        public Color        mirrorZXPlaneColor                          { get { return _mirrorZXPlaneColor; } set { UndoEx.record(this); _mirrorZXPlaneColor = value; EditorUtility.SetDirty(this); } }
        public Color        mirrorPlaneBorderColor                      { get { return _mirrorPlaneBorderColor; } set { UndoEx.record(this); _mirrorPlaneBorderColor = value; EditorUtility.SetDirty(this); } }
        public Color        mirrorIndicatorWireColor                    { get { return _mirrorIndicatorWireColor; } set { UndoEx.record(this); _mirrorIndicatorWireColor = value; EditorUtility.SetDirty(this); } }
        public Color        mirrorIndicatorFillColor                    { get { return _mirrorIndicatorFillColor; } set { UndoEx.record(this); _mirrorIndicatorFillColor = value; EditorUtility.SetDirty(this); } }
        public float        mirrorSymmetricPairHighlightRadius          { get { return _mirrorSymmetricPairHighlightRadius; } set { UndoEx.record(this); _mirrorSymmetricPairHighlightRadius = Mathf.Max(value, 0.1f); EditorUtility.SetDirty(this); } }
        public bool         mirrorShowInfoText                          { get { return _mirrorShowInfoText; } set { UndoEx.record(this); _mirrorShowInfoText = value; EditorUtility.SetDirty(this); } }

        public static Color defaultExtrudeWireColor                     { get { return Color.white; } }
        public static Color defaultExtrudeXAxisColor                    { get { return DefaultSystemValues.xAxisColor; } }
        public static Color defaultExtrudeYAxisColor                    { get { return DefaultSystemValues.yAxisColor; } }
        public static Color defaultExtrudeZAxisColor                    { get { return DefaultSystemValues.zAxisColor; } }
        public static float defaultExtrudeSglAxisSize                   { get { return 0.18f; } }
        public static Color defaultExtrudeCellWireColor                 { get { return Color.white; } }
        public static Color defaultExtrudeCellFillColor                 { get { return Color.gray.createNewAlpha(0.0f); } }
        public static float defaultExtrudeDblAxisSize                   { get { return DefaultSystemValues.tickSize; } }
        public static bool  defaultExtrudeShowInfoText                  { get { return true; } }
        public static bool  defaultExtrudeSelectSpawned                 { get { return true; } }
        public static float defaultMirrorPlaneSize                      { get { return 2.0f; } }
        public static Color defaultMirrorXYPlaneColor                   { get { return DefaultSystemValues.zAxisColor.createNewAlpha(0.4f); } }
        public static Color defaultMirrorYZPlaneColor                   { get { return DefaultSystemValues.xAxisColor.createNewAlpha(0.4f); } }
        public static Color defaultMirrorZXPlaneColor                   { get { return DefaultSystemValues.yAxisColor.createNewAlpha(0.4f); } }
        public static Color defaultMirrorPlaneBorderColor               { get { return Color.black; } }
        public static Color defaultMirrorIndicatorWireColor             { get { return Color.white.createNewAlpha(0.3f); } }
        public static Color defaultMirrorIndicatorFillColor             { get { return Color.gray.createNewAlpha(0.0f); } }
        public static float defaultMirrorSymmetricPairHighlightRadius   { get { return 80.0f; } }
        public static bool  defaultMirrorShowInfoText                   { get { return false; } }

        public override void useDefaults()
        {
            extrudeWireColor                    = defaultExtrudeWireColor;
            extrudeXAxisColor                   = defaultExtrudeXAxisColor;
            extrudeYAxisColor                   = defaultExtrudeYAxisColor;
            extrudeZAxisColor                   = defaultExtrudeZAxisColor;
            extrudeSglHandleSize                = defaultExtrudeSglAxisSize;
            extrudeCellWireColor                = defaultExtrudeCellWireColor;
            extrudeCellFillColor                = defaultExtrudeCellFillColor;
            extrudeDblAxisSize                  = defaultExtrudeDblAxisSize;
            extrudeShowInfoText                 = defaultExtrudeShowInfoText;
            extrudeSelectSpawned                = defaultExtrudeSelectSpawned;
            mirrorPlaneSize                     = defaultMirrorPlaneSize;
            mirrorXYPlaneColor                  = defaultMirrorXYPlaneColor;
            mirrorYZPlaneColor                  = defaultMirrorYZPlaneColor;
            mirrorZXPlaneColor                  = defaultMirrorZXPlaneColor;
            mirrorPlaneBorderColor              = defaultMirrorPlaneBorderColor;
            mirrorIndicatorWireColor            = defaultMirrorIndicatorWireColor;
            mirrorIndicatorFillColor            = defaultMirrorIndicatorFillColor;
            mirrorSymmetricPairHighlightRadius  = defaultMirrorSymmetricPairHighlightRadius;
            mirrorShowInfoText                  = defaultMirrorShowInfoText;

            EditorUtility.SetDirty(this);
        }
    }

    class GizmoPrefsProvider : SettingsProvider
    {
        public GizmoPrefsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            UI.createPrefsTitleLabel("Gizmos", rootElement);
            var uiBuildConfig = PluginSettingsUIBuildConfig.defaultConfig;
            uiBuildConfig.onUseDefaults += () => { PluginInspectorUI.instance.refresh(); };

            GizmoPrefs.instance.buildDefaultUI(rootElement, uiBuildConfig);

            const float labelWidth = 200.0f;
            rootElement.Query<Label>().ForEach(item => item.setChildLabelWidth(labelWidth));

            // Register additional callbacks to repaint inspector
            rootElement.Query<ColorField>("_mirrorXYPlaneColor").ForEach(item => item.RegisterValueChangedCallback(p => { PluginInspectorUI.instance.refresh(); }));
            rootElement.Query<ColorField>("_mirrorYZPlaneColor").ForEach(item => item.RegisterValueChangedCallback(p => { PluginInspectorUI.instance.refresh(); }));
            rootElement.Query<ColorField>("_mirrorZXPlaneColor").ForEach(item => item.RegisterValueChangedCallback(p => { PluginInspectorUI.instance.refresh(); }));
        }

        [SettingsProvider]
        public static SettingsProvider create()
        {
            if (GSpawn.active == null) return null;
            return new GizmoPrefsProvider("Preferences/" + GSpawn.pluginName + "/Gizmos", SettingsScope.User);
        }
    }
}
#endif