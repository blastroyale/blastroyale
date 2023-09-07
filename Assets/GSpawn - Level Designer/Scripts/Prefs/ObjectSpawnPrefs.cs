#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectSpawnPrefs : Prefs<ObjectSpawnPrefs>
    {
        [SerializeField][UIFieldConfig("Wall cell wire color", "The color which is used to draw the wall cells.", "Modular Walls Spawn", false)]
        private Color       _mdWallSpawnCellWireColor                           = defaultMdWallSpawnCellWireColor;

        [SerializeField][UIFieldConfig("Extension plane fill color", "The fill color used when drawing the extension plane.", "Segments Spawn", true)]
        private Color       _segmentsSpawnExtensionPlaneFillColor               = defaultSegmentsSpawnExtensionPlaneFillColor;
        [SerializeField][UIFieldConfig("Extension plane border color", "The border color used when drawing the extension plane.")]
        private Color       _segmentsSpawnExtensionPlaneBorderColor             = defaultSegmentsSpawnExtensionPlaneBorderColor;
        [SerializeField][Min(0.0f)][UIFieldConfig("Extension plane inflate amount", "Specifies how much the size of the extension plane is inflated.")]
        private float       _segmentsSpawnExtensionPlaneInflateAmount           = defaultSegmentsSpawnExtensionPlaneInflateAmount;
        [SerializeField][UIFieldConfig("Cell wire color", "The color which is used to draw the object spawn cells.")]
        private Color       _segmentsSpawnCellWireColor                         = defaultSegmentsSpawnCellWireColor;
        [SerializeField][UIFieldConfig("X axis color", "The color used to draw the box X axis.", "Box Spawn", true)]
        private Color       _boxSpawnXAxisColor                                 = defaultBoxSpawnXAxisColor;
        [SerializeField][UIFieldConfig("Y axis color", "The color used to draw the box Y axis. This is the height axis along which the box can be raised or lowered.")]
        private Color       _boxSpawnYAxisColor                                 = defaultBoxSpawnYAxisColor;
        [SerializeField][UIFieldConfig("Z axis color", "The color used to draw the box Z axis.")]
        private Color       _boxSpawnZAxisColor                                 = defaultBoxSpawnZAxisColor;
        [SerializeField][Min(0.0f)][UIFieldConfig("X axis length", "The length of the box X axis.")]
        private float       _boxSpawnXAxisLength                                = defaultBoxSpawnXAxisLength;
        [SerializeField][Min(0.0f)][UIFieldConfig("Y axis length", "The length of the box Y axis.")]
        private float       _boxSpawnYAxisLength                                = defaultBoxSpawnYAxisLength;
        [SerializeField][Min(0.0f)][UIFieldConfig("Z axis length", "The length of the box Z axis.")]
        private float       _boxSpawnZAxisLength                                = defaultBoxSpawnZAxisLength;
        [SerializeField][UIFieldConfig("Extension plane fill color", "The fill color used when drawing the extension plane.")]
        private Color       _boxSpawnExtensionPlaneFillColor                    = defaultBoxSpawnExtensionPlaneFillColor;
        [SerializeField][UIFieldConfig("Extension plane border color", "The border color used when drawing the extension plane.")]
        private Color       _boxSpawnExtensionPlaneBorderColor                  = defaultBoxSpawnExtensionPlaneBorderColor;
        [SerializeField][Min(0.0f)][UIFieldConfig("Extension plane inflate amount", "Specifies how much the size of the extension plane is inflated.")]
        private float       _boxSpawnExtensionPlaneInflateAmount                = defaultBoxSpawnExtensionPlaneInflateAmount;
        [SerializeField][UIFieldConfig("Cell wire color", "The color which is used to draw the object spawn cells.")]
        private Color       _boxSpawnCellWireColor                              = defaultBoxSpawnCellWireColor;
        [SerializeField][UIFieldConfig("Show info text", "If checked, the plugin will offer textual information during while using the box spawn tool.")]
        private bool        _boxSpawnShowInfoText                               = defaultBoxSpawnShowInfoText;
        [SerializeField][UIFieldConfig("Paint brush border color", "The paint brush border color.", "Tile Brush Spawn", true)]
        private Color       _trSpawnPaintBrushBorderColor                       = defaultTrSpawnPaintBrushBorderColor;
        [SerializeField][UIFieldConfig("Erase brush border color", "The erase brush border color.")]
        private Color       _trSpawnEraseBrushBorderColor                       = defaultTrSpawnEraseBrushBorderColor;
        [SerializeField][UIFieldConfig("Shadow line color", "The color of the lines that connect a bounding box (e.g. brush, connect point etc) to its shadow.")]
        private Color       _trSpawnShadowLineColor                             = defaultTrSpawnShadowLineColor;
        [SerializeField][UIFieldConfig("Shadow color", "The color of the bounding box (e.g. brush, connect point etc) shadow.")]
        private Color       _trSpawnShadowColor                                 = defaultTrSpawnShadowColor;
        [SerializeField][UIFieldConfig("Connect color", "The color used to mark the connection cells when using the Connect tool.")]
        private Color       _trSpawnConnectColor                                = defaultTrSpawnConnectColor;
        [SerializeField][UIFieldConfig("Dynamic grid", "If checked, the tile rule grid will update its Y position based on the selected paint settings such as the paint Y offset.")]
        private bool        _trSpawnDynamicGrid                                 = defaultTrSpawnDynamicGrid;
        [SerializeField][UIFieldConfig("Terrain flatten area color", "The color used when drawing the terrain flatten area indicator.", "Props Spawn", true)]
        private Color       _propsSpawnTerrainFlattenAreaColor                  = defaultPropsSpawnTerrainFlattenAreaColor;
        [SerializeField][UIFieldConfig("Brush border color", "The brush border color.", "Scatter Brush Spawn", true)]
        private Color       _scatterBrushSpawnBrushBorderColor                  = defaultScatterBrushSpawnBrushBorderColor;
        [SerializeField][UIFieldConfig("Curve color", "The curve color.", "Curve Spawn", true)]
        private Color       _curveSpawnCurveColor                               = defaultCurveSpawnCurveColor;
        [SerializeField][UIFieldConfig("Segment color", "The color of the segments that connect the curve control points.")]
        private Color       _curveSpawnSegmentColor                             = defaultCurveSpawnSegmentColor;
        [SerializeField][UIFieldConfig("Tick color", "The color of the ticks which represent the curve control points.")]
        private Color       _curveSpawnTickColor                                = defaultCurveSpawnTickColor;
        [SerializeField][UIFieldConfig("Selected tick color", "The color of the ticks which represent the selected curve control points.")]
        private Color       _curveSpawnSelectedTickColor                        = defaultCurveSpawnSelectedTickColor;
        [SerializeField][UIFieldConfig("Tick size", "The size of the ticks which represent the curve control points.")][Min(0.0f)]
        private float       _curveSpawnTickSize                                 = defaultCurveSpawnTickSize;
        [SerializeField][UIFieldConfig("Curve smoothness", "A smoothness value used when drawing the curve.")][Range(0.0f, 1.0f)]
        private float       _curveSmoothness                                    = defaultCurveSmoothness;
        [SerializeField][UIFieldConfig("Move gizmo drag radius", "This value caps the amount of distance by which control points can be moved away " + 
            "from their initial position. Useful in order to avoid situations where dragging too far off in the horizon can cause the plugin to stall for a while because " + 
            "too many objects are being spawned.")][Min(5.0f)]
        private float       _curveSpawnMoveGizmoDragRadius                      = defaultCurveSpawnMoveGizmoDragRadius;
        [SerializeField][UIFieldConfig("Refresh curves when prefab data changes", 
            "When changing curve prefab data, the plugin will automatically refresh all curves to reflect the changes.")]
        private bool        _refreshCurvesWhenPrefabDataChanges                 = defaultRefreshCurvesWhenPrefabDataChanges;
        [SerializeField][UIFieldConfig("Circle color", "The color of the spawn circle.", "Physics Spawn", true)]
        private Color       _physicsSpawnCircleColor                            = defaultPhysicsSpawnCircleColor;
        [SerializeField][UIFieldConfig("Height line color", "The color of the height line.")]
        private Color       _physicsSpawnHeightLineColor                        = defaultPhysicsSpawnHeightLineColor;
        [SerializeField][UIFieldConfig("Marker color", "The color of the marker which sits on the drop surface.")]
        private Color       _physicsSpawnMarkerColor                            = defaultPhysicsSpawnMarkerColor;
        [SerializeField][UIFieldConfig("Marker size", "The size of the marker which sits on the drop surface.")][Min(0.1f)]
        private float       _physicsSpawnMarkerSize                             = defaultPhysicsSpawnMarkerSize;

        public Color        mdWallSpawnCellWireColor                            { get { return _mdWallSpawnCellWireColor; } set { UndoEx.record(this); _mdWallSpawnCellWireColor = value; EditorUtility.SetDirty(this); } }
        public Color        segmentsSpawnExtensionPlaneFillColor                { get { return _segmentsSpawnExtensionPlaneFillColor; } set { UndoEx.record(this); _segmentsSpawnExtensionPlaneFillColor = value; EditorUtility.SetDirty(this); } }
        public Color        segmentsSpawnExtensionPlaneBorderColor              { get { return _segmentsSpawnExtensionPlaneBorderColor; } set { UndoEx.record(this); _segmentsSpawnExtensionPlaneBorderColor = value; EditorUtility.SetDirty(this); } }
        public float        segmentsSpawnExtensionPlaneInflateAmount            { get { return _segmentsSpawnExtensionPlaneInflateAmount; } set { UndoEx.record(this); _segmentsSpawnExtensionPlaneInflateAmount = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color        segmentsSpawnCellWireColor                          { get { return _segmentsSpawnCellWireColor; } set { UndoEx.record(this); _segmentsSpawnCellWireColor = value; EditorUtility.SetDirty(this); } }
        public Color        boxSpawnXAxisColor                                  { get { return _boxSpawnXAxisColor; } set { UndoEx.record(this); _boxSpawnXAxisColor = value; EditorUtility.SetDirty(this); } }
        public Color        boxSpawnYAxisColor                                  { get { return _boxSpawnYAxisColor; } set { UndoEx.record(this); _boxSpawnYAxisColor = value; EditorUtility.SetDirty(this); } }
        public Color        boxSpawnZAxisColor                                  { get { return _boxSpawnZAxisColor; } set { UndoEx.record(this); _boxSpawnZAxisColor = value; EditorUtility.SetDirty(this); } }
        public float        boxSpawnXAxisLength                                 { get { return _boxSpawnXAxisLength; } set { UndoEx.record(this); _boxSpawnXAxisLength = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public float        boxSpawnYAxisLength                                 { get { return _boxSpawnYAxisLength; } set { UndoEx.record(this); _boxSpawnYAxisLength = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public float        boxSpawnZAxisLength                                 { get { return _boxSpawnZAxisLength; } set { UndoEx.record(this); _boxSpawnZAxisLength = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color        boxSpawnExtensionPlaneFillColor                     { get { return _boxSpawnExtensionPlaneFillColor; } set { UndoEx.record(this); _boxSpawnExtensionPlaneFillColor = value; EditorUtility.SetDirty(this); } }
        public Color        boxSpawnExtensionPlaneBorderColor                   { get { return _boxSpawnExtensionPlaneBorderColor; } set { UndoEx.record(this); _boxSpawnExtensionPlaneBorderColor = value; EditorUtility.SetDirty(this); } }
        public float        boxSpawnExtensionPlaneInflateAmount                 { get { return _boxSpawnExtensionPlaneInflateAmount; } set { UndoEx.record(this); _boxSpawnExtensionPlaneInflateAmount = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color        boxSpawnCellWireColor                               { get { return _boxSpawnCellWireColor; } set { UndoEx.record(this); _boxSpawnCellWireColor = value; EditorUtility.SetDirty(this); } }
        public bool         boxSpawnShowInfoText                                { get { return _boxSpawnShowInfoText; } set { UndoEx.record(this); _boxSpawnShowInfoText = value; EditorUtility.SetDirty(this); } }
        public Color        trSpawnPaintBrushBorderColor                        { get { return _trSpawnPaintBrushBorderColor; } set { UndoEx.record(this); _trSpawnPaintBrushBorderColor = value; EditorUtility.SetDirty(this); } }
        public Color        trSpawnEraseBrushBorderColor                        { get { return _trSpawnEraseBrushBorderColor; } set { UndoEx.record(this); _trSpawnEraseBrushBorderColor = value; EditorUtility.SetDirty(this); } }        
        public Color        trSpawnShadowLineColor                              { get { return _trSpawnShadowLineColor; } set { UndoEx.record(this); _trSpawnShadowLineColor = value; EditorUtility.SetDirty(this); } }
        public Color        trSpawnShadowColor                                  { get { return _trSpawnShadowColor; } set { UndoEx.record(this); _trSpawnShadowColor = value; EditorUtility.SetDirty(this); } }
        public Color        trSpawnConnectColor                                 { get { return _trSpawnConnectColor; } set { UndoEx.record(this); _trSpawnConnectColor = value; EditorUtility.SetDirty(this); } }
        public bool         trSpawnDynamicGrid                                  { get { return _trSpawnDynamicGrid; } set { UndoEx.record(this); _trSpawnDynamicGrid = value; EditorUtility.SetDirty(this); } }
        public Color        propsSpawnTerrainFlattenAreaColor                   { get { return _propsSpawnTerrainFlattenAreaColor; } set { UndoEx.record(this); _propsSpawnTerrainFlattenAreaColor = value; EditorUtility.SetDirty(this); } }
        public Color        scatterBrushSpawnBrushBorderColor                   { get { return _scatterBrushSpawnBrushBorderColor; } set { UndoEx.record(this); _scatterBrushSpawnBrushBorderColor = value; EditorUtility.SetDirty(this); } }
        public Color        curveSpawnCurveColor                                { get { return _curveSpawnCurveColor; } set { UndoEx.record(this); _curveSpawnCurveColor = value; EditorUtility.SetDirty(this); } }
        public Color        curveSpawnSegmentColor                              { get { return _curveSpawnSegmentColor; } set { UndoEx.record(this); _curveSpawnSegmentColor = value; EditorUtility.SetDirty(this); } }
        public Color        curveSpawnTickColor                                 { get { return _curveSpawnTickColor; } set { UndoEx.record(this); _curveSpawnTickColor = value; EditorUtility.SetDirty(this); } }
        public Color        curveSpawnSelectedTickColor                         { get { return _curveSpawnSelectedTickColor; } set { UndoEx.record(this); _curveSpawnSelectedTickColor = value; EditorUtility.SetDirty(this); } }
        public float        curveSpawnTickSize                                  { get { return _curveSpawnTickSize; } set { UndoEx.record(this); _curveSpawnTickSize = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public float        curveSmoothness                                     { get { return _curveSmoothness; } set { UndoEx.record(this); _curveSmoothness = Mathf.Clamp01(value); EditorUtility.SetDirty(this); } }
        public float        curveSpawnMoveGizmoDragRadius                       { get { return _curveSpawnMoveGizmoDragRadius; } set { UndoEx.record(this); _curveSpawnMoveGizmoDragRadius = Mathf.Max(5.0f, value); EditorUtility.SetDirty(this); } }
        public bool         refreshCurvesWhenPrefabDataChanges                  { get { return _refreshCurvesWhenPrefabDataChanges; } set { UndoEx.record(this); _refreshCurvesWhenPrefabDataChanges = value; EditorUtility.SetDirty(this); } }
        public Color        physicsSpawnCircleColor                             { get { return _physicsSpawnCircleColor; } set { UndoEx.record(this); _physicsSpawnCircleColor = value; EditorUtility.SetDirty(this); } }
        public Color        physicsSpawnHeightLineColor                         { get { return _physicsSpawnHeightLineColor; } set { UndoEx.record(this); _physicsSpawnHeightLineColor = value; EditorUtility.SetDirty(this); } }
        public Color        physicsSpawnMarkerColor                             { get { return _physicsSpawnMarkerColor; } set { UndoEx.record(this); _physicsSpawnMarkerColor = value; EditorUtility.SetDirty(this); } }
        public float        physicsSpawnMarkerSize                              { get { return _physicsSpawnMarkerSize; } set { UndoEx.record(this); _physicsSpawnMarkerSize = Mathf.Max(0.1f, value); EditorUtility.SetDirty(this); } }

        public static Color defaultMdWallSpawnCellWireColor                     { get { return Color.white; } }
        public static Color defaultSegmentsSpawnExtensionPlaneFillColor         { get { return Color.green.createNewAlpha(0.05f); } }
        public static Color defaultSegmentsSpawnExtensionPlaneBorderColor       { get { return Color.black; } }
        public static float defaultSegmentsSpawnExtensionPlaneInflateAmount     { get { return 2.0f; } }
        public static Color defaultSegmentsSpawnCellWireColor                   { get { return Color.white; } }
        public static Color defaultBoxSpawnXAxisColor                           { get { return DefaultSystemValues.xAxisColor; } }
        public static Color defaultBoxSpawnYAxisColor                           { get { return DefaultSystemValues.yAxisColor; } }
        public static Color defaultBoxSpawnZAxisColor                           { get { return DefaultSystemValues.zAxisColor; } }
        public static float defaultBoxSpawnXAxisLength                          { get { return 10.0f; } }
        public static float defaultBoxSpawnYAxisLength                          { get { return 10.0f; } }
        public static float defaultBoxSpawnZAxisLength                          { get { return 10.0f; } }
        public static Color defaultBoxSpawnExtensionPlaneFillColor              { get { return Color.green.createNewAlpha(0.05f); } }
        public static Color defaultBoxSpawnExtensionPlaneBorderColor            { get { return Color.black; } }
        public static float defaultBoxSpawnExtensionPlaneInflateAmount          { get { return 2.0f; } }
        public static Color defaultBoxSpawnCellWireColor                        { get { return Color.white; } }
        public static bool  defaultBoxSpawnShowInfoText                         { get { return true; } }
        public static Color defaultTrSpawnPaintBrushBorderColor                 { get { return Color.green; } }
        public static Color defaultTrSpawnEraseBrushBorderColor                 { get { return Color.red; } } 
        public static Color defaultTrSpawnShadowLineColor                       { get { return ColorEx.create(255, 255, 255, 22); } }
        public static Color defaultTrSpawnShadowColor                           { get { return ColorEx.createNewAlpha(Color.yellow, 0.4f); } }
        public static Color defaultTrSpawnConnectColor                          { get { return Color.green; } }
        public static bool  defaultTrSpawnDynamicGrid                           { get { return false; } }
        public static Color defaultPropsSpawnTerrainFlattenAreaColor            { get { return Color.green; } }
        public static Color defaultScatterBrushSpawnBrushBorderColor            { get { return Color.green; } }
        public static Color defaultCurveSpawnCurveColor                         { get { return Color.white; } }
        public static Color defaultCurveSpawnSegmentColor                       { get { return Color.white; } }
        public static Color defaultCurveSpawnTickColor                          { get { return Color.green; } }
        public static Color defaultCurveSpawnSelectedTickColor                  { get { return DefaultSystemValues.parentHighlightColor; } }
        public static float defaultCurveSpawnTickSize                           { get { return 0.06f; } }
        public static float defaultCurveSmoothness                              { get { return 1.0f; } }
        public static float defaultCurveSpawnMoveGizmoDragRadius                { get { return 300.0f; } }
        public static bool  defaultRefreshCurvesWhenPrefabDataChanges           { get { return true; } }
        public static Color defaultPhysicsSpawnCircleColor                      { get { return Color.green; } }
        public static Color defaultPhysicsSpawnHeightLineColor                  { get { return Color.white; } }
        public static Color defaultPhysicsSpawnMarkerColor                      { get { return Color.green; } }
        public static float defaultPhysicsSpawnMarkerSize                       { get { return 0.5f; } }

        public override void useDefaults()
        {
            mdWallSpawnCellWireColor                    = defaultMdWallSpawnCellWireColor;
            segmentsSpawnExtensionPlaneFillColor        = defaultSegmentsSpawnExtensionPlaneFillColor;
            segmentsSpawnExtensionPlaneBorderColor      = defaultSegmentsSpawnExtensionPlaneBorderColor;
            segmentsSpawnExtensionPlaneInflateAmount    = defaultSegmentsSpawnExtensionPlaneInflateAmount;
            segmentsSpawnCellWireColor                  = defaultSegmentsSpawnCellWireColor;
            boxSpawnXAxisColor                          = defaultBoxSpawnXAxisColor;
            boxSpawnYAxisColor                          = defaultBoxSpawnYAxisColor;
            boxSpawnZAxisColor                          = defaultBoxSpawnZAxisColor;
            boxSpawnXAxisLength                         = defaultBoxSpawnXAxisLength;
            boxSpawnYAxisLength                         = defaultBoxSpawnYAxisLength;
            boxSpawnZAxisLength                         = defaultBoxSpawnZAxisLength;
            boxSpawnExtensionPlaneFillColor             = defaultBoxSpawnExtensionPlaneFillColor;
            boxSpawnExtensionPlaneBorderColor           = defaultBoxSpawnExtensionPlaneBorderColor;
            boxSpawnExtensionPlaneInflateAmount         = defaultBoxSpawnExtensionPlaneInflateAmount;
            boxSpawnCellWireColor                       = defaultBoxSpawnCellWireColor;
            boxSpawnShowInfoText                        = defaultBoxSpawnShowInfoText;
            trSpawnPaintBrushBorderColor                = defaultTrSpawnPaintBrushBorderColor;
            trSpawnEraseBrushBorderColor                = defaultTrSpawnEraseBrushBorderColor;
            trSpawnShadowLineColor                      = defaultTrSpawnShadowLineColor;
            trSpawnShadowColor                          = defaultTrSpawnShadowColor;
            trSpawnConnectColor                         = defaultTrSpawnConnectColor;
            trSpawnDynamicGrid                          = defaultTrSpawnDynamicGrid;
            propsSpawnTerrainFlattenAreaColor           = defaultPropsSpawnTerrainFlattenAreaColor;
            scatterBrushSpawnBrushBorderColor           = defaultScatterBrushSpawnBrushBorderColor;
            curveSpawnCurveColor                        = defaultCurveSpawnCurveColor;
            curveSpawnSegmentColor                      = defaultCurveSpawnSegmentColor;
            curveSpawnTickColor                         = defaultCurveSpawnTickColor;
            curveSpawnSelectedTickColor                 = defaultCurveSpawnSelectedTickColor;
            curveSpawnTickSize                          = defaultCurveSpawnTickSize;
            curveSmoothness                             = defaultCurveSmoothness;
            curveSpawnMoveGizmoDragRadius               = defaultCurveSpawnMoveGizmoDragRadius;
            refreshCurvesWhenPrefabDataChanges          = defaultRefreshCurvesWhenPrefabDataChanges;
            physicsSpawnCircleColor                     = defaultPhysicsSpawnCircleColor;
            physicsSpawnHeightLineColor                 = defaultPhysicsSpawnHeightLineColor;
            physicsSpawnMarkerColor                     = defaultPhysicsSpawnMarkerColor;
            physicsSpawnMarkerSize                      = defaultPhysicsSpawnMarkerSize;

            EditorUtility.SetDirty(this);
        }
    }

    class ObjectSpawnPrefsProvider : SettingsProvider
    {
        public ObjectSpawnPrefsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            UI.createPrefsTitleLabel("Object Spawn", rootElement);
            ObjectSpawnPrefs.instance.buildDefaultUI(rootElement, PluginSettingsUIBuildConfig.defaultConfig);

            const float labelWidth = 265.0f;
            rootElement.Query<Label>().ForEach(item => item.setChildLabelWidth(labelWidth));
        }

        [SettingsProvider]
        public static SettingsProvider create()
        {
            if (GSpawn.active == null) return null;
            return new ObjectSpawnPrefsProvider("Preferences/" + GSpawn.pluginName + "/Object Spawn", SettingsScope.User);
        }
    }
}
#endif