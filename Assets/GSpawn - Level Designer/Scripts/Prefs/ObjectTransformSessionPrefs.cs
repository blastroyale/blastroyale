#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectTransformSessionPrefs : Prefs<ObjectTransformSessionPrefs>
    {
        [SerializeField][UIFieldConfig("Rotation relative to grid", "If this is checked, the scene grid will act as a parent for objects involved in modular snap. Those " + 
            "objects will have their rotations applied relative to the grid. Useful when the grid contains arbitrary rotations.", "Modular Snap", false)]
        private bool        _modularSnapRotationRelativeToGrid                  = defaultModularSnapRotationRelativeToGrid;
        [SerializeField][UIFieldConfig("Draw object pivot ticks", "If this is checked, a tick will be drawn at the position of each target object.")]
        private bool        _modularSnapDrawObjectPivotTicks                    = defaultModularSnapDrawObjectPivotTicks;
        [SerializeField][UIFieldConfig("Draw projected object pivot ticks", "If this is checked, a tick will be drawn for the projected position of each target object. This is the position of the object projected onto the scene grid.")]
        private bool        _modularSnapDrawProjectedObjectPivotTicks           = defaultModularSnapDrawProjectedObjectPivotTicks;
        [SerializeField][UIFieldConfig("Draw object pivot projection lines", "If this is checked, lines will be drawn from each object position to its projected position onto the scene grid.")]
        private bool        _modularSnapDrawObjectPivotProjectionLines          = defaultModularSnapDrawObjectPivotProjectionLines;
        [SerializeField][UIFieldConfig("Draw object boxes", "If this is checked, a box will be drawn around each target object.")]
        private bool        _modularSnapDrawObjectBoxes                         = defaultModularSnapDrawObjectBoxes;
        [SerializeField][UIFieldConfig("Object pivot tick color", "The color of the object pivot ticks.")]
        private Color       _modularSnapObjectPivotTickColor                    = defaultModularSnapObjectPivotTickColor;
        [SerializeField][UIFieldConfig("Object pivot tick size", "The size of the object pivot ticks.")][Min(0.0f)]
        private float       _modularSnapObjectPivotTickSize                     = defaultModularSnapObjectPivotTickSize;
        [SerializeField][UIFieldConfig("Projected object pivot tick color", "The color of the projected object pivot ticks.")]
        private Color       _modularSnapProjectedObjectPivotTickColor           = defaultModularSnapProjectedObjectPivotTickColor;
        [SerializeField][UIFieldConfig("Projected object pivot tick size", "The size of the projected object pivot ticks.")][Min(0.0f)]
        private float       _modularSnapProjectedObjectPivotTickSize            = defaultModularSnapProjectedObjectPivotTickSize;
        [SerializeField][UIFieldConfig("Object pivot projection line color", "The color of the object pivot projection lines.")]
        private Color       _modularSnapObjectPivotProjectionLineColor          = defaultModularSnapObjectPivotProjectionLineColor;
        [SerializeField][UIFieldConfig("Object box wire color", "The wire color of the target object boxes.")]
        private Color       _modularSnapObjectBoxWireColor                      = defaultModularSnapObjectBoxWireColor;
        [SerializeField][UIFieldConfig("Show info text", "If checked, the plugin will offer textual information during a modular snap session.")]
        private bool        _modularSnapShowInfoText                            = defaultModularSnapShowInfoText;
        [SerializeField][UIFieldConfig("Draw alignment highlights", "If checked, a highlight will be drawn for objects whose positions are aligned with the target objects.")]
        private bool        _modularSnapDrawAlignmentHighlights                 = defaultModularSnapDrawAlignmentHighlights;
        [SerializeField][UIFieldConfig("Alignment highlight radius", "If alignment highlights are turned on, only objects that fall within this radius will be highlighted.")][Min(0.1f)]
        private float       _modularSnapAlignmentHighlightRadius                = defaultModularSnapAlignmentHighlightRadius;
        [SerializeField][UIFieldConfig("Show alignment hints", "If alignment highlights are turned on, having this checked will display hints related to the highlighted objects.")]
        private bool        _modularSnapShowAlignmentHints                      = defaultModularSnapShowAlignmentHints;
        [SerializeField][UIFieldConfig("Max number of alignment hints", "If alignment hints are turned on, this is the maximum number of hints that will be displayed.")][Min(1)]
        private int         _modularSnapMaxNumAlignmentHints                    = defaultModularSnapMaxNumAlignmentHints;
        
        [SerializeField] [UIFieldConfig("Draw anchor lines", "If this is checked, lines will be drawn from the anchor pivot to each of the target objects.", "Surface Snap", true)]
        private bool        _surfaceSnapDrawAnchorLines                         = defaultSurfaceSnapDrawAnchorLines;
        [SerializeField] [UIFieldConfig("Draw anchor tick", "If this is checked, a tick will be drawn at the intersection point between the mouse cursor and the snap surface. This is the anchor point.")]
        private bool        _surfaceSnapDrawAnchorTick                          = defaultSurfaceSnapDrawAnchorTick;
        [SerializeField] [UIFieldConfig("Draw object pivot ticks", "If this is checked, a tick will be drawn at the position of each target object.")]
        private bool        _surfaceSnapDrawObjectPivotTicks                    = defaultSurfaceSnapDrawObjectPivotTicks;
        [SerializeField] [UIFieldConfig("Draw object boxes", "If this is checked, a box will be drawn around each target object.")]
        private bool        _surfaceSnapDrawObjectBoxes                         = defaultSurfaceSnapDrawObjectBoxes;
        [SerializeField] [UIFieldConfig("Anchor line color", "The color of the anchor lines.")]
        private Color       _surfaceSnapAnchorLineColor                         = defaultSurfaceSnapAnchorLineColor;
        [SerializeField] [UIFieldConfig("Anchor tick color", "The color of the anchor tick.")]
        private Color       _surfaceSnapAnchorTickColor                         = defaultSurfaceSnapAnchorTickColor;
        [SerializeField] [UIFieldConfig("Anchor tick size", "The size of the anchor tick.")] [Min(0.0f)]
        private float       _surfaceSnapAnchorTickSize                          = defaultSurfaceSnapAnchorTickSize;
        [SerializeField] [UIFieldConfig("Object pivot tick color", "The color of the target object pivot ticks.")]
        private Color       _surfaceSnapObjectPivotTickColor                    = defaultSurfaceSnapObjectPivotTickColor;
        [SerializeField] [UIFieldConfig("Object pivot tick size", "The size of the target object pivot ticks.")] [Min(0.0f)]
        private float       _surfaceSnapObjectPivotTickSize                     = defaultSurfaceSnapObjectPivotTickSize;
        [SerializeField] [UIFieldConfig("Object box wire color", "The wire color of the target object boxes.")]
        private Color       _surfaceSnapObjectBoxWireColor                      = defaultSurfaceSnapObjectBoxWireColor;
        [SerializeField][UIFieldConfig("Show info text", "If checked, the plugin will offer textual information during a surface snap session.")]
        private bool        _surfaceSnapShowInfoText                            = defaultSurfaceSnapShowInfoText;
        [SerializeField] [UIFieldConfig("Tick color", "The snap pivot tick color.", "Vertex Snap", true)]
        private Color       _vertSnapTickColor                                  = defaultVertSnapTickColor;
        [SerializeField] [UIFieldConfig("Tick size", "The snap pivot tick size.")] [Min(0.0f)]
        private float       _vertSnapTickSize                                   = defaultVertSnapTickSize;
        [SerializeField] [UIFieldConfig("Triangle wire color", "The wire color of the triangle that contains the selected vertex.")]
        private Color       _vertSnapTriangleWireColor                          = defaultVertSnapTriangleWireColor;
        [SerializeField] [UIFieldConfig("Tick color", "The snap pivot tick color.", "Box Snap", true)]
        private Color       _boxSnapTickColor                                   = defaultBoxSnapTickColor;
        [SerializeField] [UIFieldConfig("Center tick color", "The snap pivot tick color when it coincides with the center of one of the target object boxes.")]
        private Color       _boxSnapCenterTickColor                             = defaultBoxSnapCenterTickColor;
        [SerializeField] [UIFieldConfig("Tick size", "The snap pivot tick size.")] [Min(0.0f)]
        private float       _boxSnapTickSize                                    = defaultBoxSnapTickSize;
        [SerializeField] [UIFieldConfig("Object box wire color", "The wire color of the object boxes.")]
        private Color       _boxSnapObjectBoxWireColor                          = defaultBoxSnapObjectBoxWireColor;

        public bool         modularSnapRotationRelativeToGrid                   { get { return _modularSnapRotationRelativeToGrid; } set { UndoEx.record(this); _modularSnapRotationRelativeToGrid = value; EditorUtility.SetDirty(this); } }
        public bool         modularSnapDrawObjectPivotTicks                     { get { return _modularSnapDrawObjectPivotTicks; } set { UndoEx.record(this); _modularSnapDrawObjectPivotTicks = value; EditorUtility.SetDirty(this); } }
        public bool         modularSnapDrawProjectedObjectPivotTicks            { get { return _modularSnapDrawProjectedObjectPivotTicks; } set { UndoEx.record(this); _modularSnapDrawProjectedObjectPivotTicks = value; EditorUtility.SetDirty(this); } }
        public bool         modularSnapDrawObjectPivotProjectionLines           { get { return _modularSnapDrawObjectPivotProjectionLines; } set { UndoEx.record(this); _modularSnapDrawObjectPivotProjectionLines = value; EditorUtility.SetDirty(this); } }
        public bool         modularSnapDrawObjectBoxes                          { get { return _modularSnapDrawObjectBoxes; } set { UndoEx.record(this); _modularSnapDrawObjectBoxes = value; EditorUtility.SetDirty(this); } }
        public Color        modularSnapObjectPivotTickColor                     { get { return _modularSnapObjectPivotTickColor; } set { UndoEx.record(this); _modularSnapObjectPivotTickColor = value; EditorUtility.SetDirty(this); } }
        public float        modularSnapObjectPivotTickSize                      { get { return _modularSnapObjectPivotTickSize; } set { UndoEx.record(this); _modularSnapObjectPivotTickSize = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color        modularSnapProjectedObjectPivotTickColor            { get { return _modularSnapProjectedObjectPivotTickColor; } set { UndoEx.record(this); _modularSnapProjectedObjectPivotTickColor = value; EditorUtility.SetDirty(this); } }
        public float        modularSnapProjectedObjectPivotTickSize             { get { return _modularSnapProjectedObjectPivotTickSize; } set { UndoEx.record(this); _modularSnapProjectedObjectPivotTickSize = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color        modularSnapObjectPivotProjectionLineColor           { get { return _modularSnapObjectPivotProjectionLineColor; } set { UndoEx.record(this); _modularSnapObjectPivotProjectionLineColor = value; EditorUtility.SetDirty(this); } }
        public Color        modularSnapObjectBoxWireColor                       { get { return _modularSnapObjectBoxWireColor; } set { UndoEx.record(this); _modularSnapObjectBoxWireColor = value; EditorUtility.SetDirty(this); } }
        public bool         modularSnapShowInfoText                             { get { return _modularSnapShowInfoText; } set { UndoEx.record(this); _modularSnapShowInfoText = value; EditorUtility.SetDirty(this); } }
        public bool         modularSnapDrawAlingmentHighlights                  { get { return _modularSnapDrawAlignmentHighlights; } set { UndoEx.record(this); _modularSnapDrawAlignmentHighlights = value; EditorUtility.SetDirty(this); } }
        public float        modularSnapAlignmentHighlightRadius                 { get { return _modularSnapAlignmentHighlightRadius; } set { UndoEx.record(this); _modularSnapAlignmentHighlightRadius = Mathf.Max(value, 0.1f); EditorUtility.SetDirty(this); } }
        public bool         modularSnapShowAlignmentHints                       { get { return _modularSnapShowAlignmentHints; } set { UndoEx.record(this); _modularSnapShowAlignmentHints = value; EditorUtility.SetDirty(this); } }
        public int          modularSnapMaxNumAlignmentHints                     { get { return _modularSnapMaxNumAlignmentHints; } set { UndoEx.record(this); _modularSnapMaxNumAlignmentHints = Mathf.Max(1, value); EditorUtility.SetDirty(this); } }

        public bool         surfaceSnapDrawAnchorLines                          { get { return _surfaceSnapDrawAnchorLines; } set { UndoEx.record(this); _surfaceSnapDrawAnchorLines = value; EditorUtility.SetDirty(this); } }
        public bool         surfaceSnapDrawAnchorTick                           { get { return _surfaceSnapDrawAnchorTick; } set { UndoEx.record(this); _surfaceSnapDrawAnchorTick = value; EditorUtility.SetDirty(this); } }
        public bool         surfaceSnapDrawObjectPivotTicks                     { get { return _surfaceSnapDrawObjectPivotTicks; } set { UndoEx.record(this); _surfaceSnapDrawObjectPivotTicks = value; EditorUtility.SetDirty(this); } }
        public bool         surfaceSnapDrawObjectBoxes                          { get { return _surfaceSnapDrawObjectBoxes; } set { UndoEx.record(this); _surfaceSnapDrawObjectBoxes = value; EditorUtility.SetDirty(this); } }
        public Color        surfaceSnapAnchorLineColor                          { get { return _surfaceSnapAnchorLineColor; } set { UndoEx.record(this); _surfaceSnapAnchorLineColor = value; EditorUtility.SetDirty(this); } }
        public Color        surfaceSnapAnchorTickColor                          { get { return _surfaceSnapAnchorTickColor; } set { UndoEx.record(this); _surfaceSnapAnchorTickColor = value; EditorUtility.SetDirty(this); } }
        public float        surfaceSnapAnchorTickSize                           { get { return _surfaceSnapAnchorTickSize; } set { UndoEx.record(this); _surfaceSnapAnchorTickSize = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color        surfaceSnapObjectPivotTickColor                     { get { return _surfaceSnapObjectPivotTickColor; } set { UndoEx.record(this); _surfaceSnapObjectPivotTickColor = value; EditorUtility.SetDirty(this); } }
        public float        surfaceSnapObjectPivotTickSize                      { get { return _surfaceSnapObjectPivotTickSize; } set { UndoEx.record(this); _surfaceSnapObjectPivotTickSize = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color        surfaceSnapObjectBoxWireColor                       { get { return _surfaceSnapObjectBoxWireColor; } set { UndoEx.record(this); _surfaceSnapObjectBoxWireColor = value; EditorUtility.SetDirty(this); } }
        public bool         surfaceSnapShowInfoText                             { get { return _surfaceSnapShowInfoText; } set { UndoEx.record(this); _surfaceSnapShowInfoText = value; EditorUtility.SetDirty(this); } }
        public Color        vertSnapTickColor                                   { get { return _vertSnapTickColor; } set { UndoEx.record(this); _vertSnapTickColor = value; EditorUtility.SetDirty(this); } }
        public float        vertSnapTickSize                                    { get { return _vertSnapTickSize; } set { UndoEx.record(this); _vertSnapTickSize = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color        vertSnapTriangleWireColor                           { get { return _vertSnapTriangleWireColor; } set { UndoEx.record(this); _vertSnapTriangleWireColor = value; EditorUtility.SetDirty(this); } }
        public Color        boxSnapTickColor                                    { get { return _boxSnapTickColor; } set { UndoEx.record(this); _boxSnapTickColor = value; EditorUtility.SetDirty(this); } }
        public Color        boxSnapCenterTickColor                              { get { return _boxSnapCenterTickColor; } set { UndoEx.record(this); _boxSnapCenterTickColor = value; EditorUtility.SetDirty(this); } }
        public float        boxSnapTickSize                                     { get { return _boxSnapTickSize; } set { UndoEx.record(this); _boxSnapTickSize = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color        boxSnapObjectBoxWireColor                           { get { return _boxSnapObjectBoxWireColor; } set { UndoEx.record(this); _boxSnapObjectBoxWireColor = value; EditorUtility.SetDirty(this); } }

        public static bool  defaultModularSnapRotationRelativeToGrid            { get { return true; } }
        public static bool  defaultModularSnapDrawObjectPivotTicks              { get { return true; } }
        public static bool  defaultModularSnapDrawProjectedObjectPivotTicks     { get { return true; } }
        public static bool  defaultModularSnapDrawObjectPivotProjectionLines    { get { return true; } }
        public static bool  defaultModularSnapDrawObjectBoxes                   { get { return true; } }
        public static Color defaultModularSnapObjectPivotProjectionLineColor    { get { return Color.white.createNewAlpha(0.25f); } }
        public static Color defaultModularSnapObjectPivotTickColor              { get { return Color.green; } }
        public static float defaultModularSnapObjectPivotTickSize               { get { return DefaultSystemValues.tickSize; } }
        public static Color defaultModularSnapProjectedObjectPivotTickColor     { get { return ColorEx.orange; } }
        public static float defaultModularSnapProjectedObjectPivotTickSize      { get { return DefaultSystemValues.tickSize; } }
        public static Color defaultModularSnapObjectBoxWireColor                { get { return Color.white.createNewAlpha(0.25f); } }
        public static bool  defaultModularSnapShowInfoText                      { get { return true; } }
        public static bool  defaultModularSnapDrawAlignmentHighlights           { get { return true; } }
        public static float defaultModularSnapAlignmentHighlightRadius          { get { return 80.0f; } }
        public static bool  defaultModularSnapShowAlignmentHints                { get { return true; } }
        public static int   defaultModularSnapMaxNumAlignmentHints              { get { return 2; } }
        
        public static bool  defaultSurfaceSnapDrawAnchorLines                   { get { return true; } }
        public static bool  defaultSurfaceSnapDrawAnchorTick                    { get { return true; } }
        public static bool  defaultSurfaceSnapDrawObjectPivotTicks              { get { return true; } }
        public static bool  defaultSurfaceSnapDrawObjectBoxes                   { get { return true; } }
        public static Color defaultSurfaceSnapAnchorLineColor                   { get { return Color.white.createNewAlpha(0.25f); } }
        public static Color defaultSurfaceSnapAnchorTickColor                   { get { return ColorEx.orange; } }
        public static float defaultSurfaceSnapAnchorTickSize                    { get { return DefaultSystemValues.tickSize; } }
        public static Color defaultSurfaceSnapObjectPivotTickColor              { get { return Color.green; } }
        public static float defaultSurfaceSnapObjectPivotTickSize               { get { return DefaultSystemValues.tickSize; } }
        public static Color defaultSurfaceSnapObjectBoxWireColor                { get { return Color.white.createNewAlpha(0.25f); } }
        public static bool  defaultSurfaceSnapShowInfoText                      { get { return true; } }
        public static Color defaultVertSnapTickColor                            { get { return Color.green; } }
        public static float defaultVertSnapTickSize                             { get { return DefaultSystemValues.tickSize; } }
        public static Color defaultVertSnapTriangleWireColor                    { get { return Color.green; } }
        public static Color defaultBoxSnapTickColor                             { get { return Color.green; } }
        public static Color defaultBoxSnapCenterTickColor                       { get { return Color.magenta; } }
        public static float defaultBoxSnapTickSize                              { get { return DefaultSystemValues.tickSize; } }
        public static Color defaultBoxSnapObjectBoxWireColor                    { get { return Color.yellow; } }

        public override void useDefaults()
        {
            modularSnapRotationRelativeToGrid           = defaultModularSnapRotationRelativeToGrid;
            modularSnapDrawObjectPivotTicks             = defaultModularSnapDrawObjectPivotTicks;
            modularSnapDrawProjectedObjectPivotTicks    = defaultModularSnapDrawProjectedObjectPivotTicks;
            modularSnapDrawObjectPivotProjectionLines   = defaultModularSnapDrawObjectPivotProjectionLines;
            modularSnapDrawObjectBoxes                  = defaultModularSnapDrawObjectBoxes;
            modularSnapObjectPivotTickColor             = defaultModularSnapObjectPivotTickColor;
            modularSnapObjectPivotTickSize              = defaultModularSnapObjectPivotTickSize;
            modularSnapProjectedObjectPivotTickColor    = defaultModularSnapProjectedObjectPivotTickColor;
            modularSnapProjectedObjectPivotTickSize     = defaultModularSnapProjectedObjectPivotTickSize;
            modularSnapObjectPivotProjectionLineColor   = defaultModularSnapObjectPivotProjectionLineColor;
            modularSnapObjectBoxWireColor               = defaultModularSnapObjectBoxWireColor;
            modularSnapShowInfoText                     = defaultModularSnapShowInfoText;
            modularSnapDrawAlingmentHighlights          = defaultModularSnapDrawAlignmentHighlights;
            modularSnapAlignmentHighlightRadius         = defaultModularSnapAlignmentHighlightRadius;
            modularSnapShowAlignmentHints               = defaultModularSnapShowAlignmentHints;
            modularSnapMaxNumAlignmentHints             = defaultModularSnapMaxNumAlignmentHints;
            surfaceSnapDrawAnchorLines                  = defaultSurfaceSnapDrawAnchorLines;
            surfaceSnapDrawAnchorTick                   = defaultSurfaceSnapDrawAnchorTick;
            surfaceSnapDrawObjectPivotTicks             = defaultSurfaceSnapDrawObjectPivotTicks;
            surfaceSnapDrawObjectBoxes                  = defaultSurfaceSnapDrawObjectBoxes;
            surfaceSnapAnchorLineColor                  = defaultSurfaceSnapAnchorLineColor;
            surfaceSnapAnchorTickColor                  = defaultSurfaceSnapAnchorTickColor;
            surfaceSnapAnchorTickSize                   = defaultSurfaceSnapAnchorTickSize;
            surfaceSnapObjectPivotTickColor             = defaultSurfaceSnapObjectPivotTickColor;
            surfaceSnapObjectPivotTickSize              = defaultSurfaceSnapObjectPivotTickSize;
            surfaceSnapObjectBoxWireColor               = defaultSurfaceSnapObjectBoxWireColor;
            surfaceSnapShowInfoText                     = defaultSurfaceSnapShowInfoText;
            vertSnapTickColor                           = defaultVertSnapTickColor;
            vertSnapTickSize                            = defaultVertSnapTickSize;
            vertSnapTriangleWireColor                   = defaultVertSnapTriangleWireColor;
            boxSnapTickColor                            = defaultBoxSnapTickColor;
            boxSnapCenterTickColor                      = defaultBoxSnapCenterTickColor;
            boxSnapTickSize                             = defaultBoxSnapTickSize;
            boxSnapObjectBoxWireColor                   = defaultBoxSnapObjectBoxWireColor;

            EditorUtility.SetDirty(this);
        }
    }

    class ObjectTransformSessionPrefsProvider : SettingsProvider
    {
        public ObjectTransformSessionPrefsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            UI.createPrefsTitleLabel("Object Transform Sessions", rootElement);
            ObjectTransformSessionPrefs.instance.buildDefaultUI(rootElement, PluginSettingsUIBuildConfig.defaultConfig);

            const float labelWidth = 210.0f;
            rootElement.Query<Label>().ForEach(item => item.setChildLabelWidth(labelWidth));
        }

        [SettingsProvider]
        public static SettingsProvider create()
        {
            if (GSpawn.active == null) return null;
            return new ObjectTransformSessionPrefsProvider("Preferences/" + GSpawn.pluginName + "/Object Transform Sessions", SettingsScope.User);
        }
    }
}
#endif