#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class GridPrefs : Prefs<GridPrefs>
    {
        [SerializeField][UIFieldConfig("Wire color", "The grid wire color (i.e. cell line color).")]
        private Color           _wireColor              = defaultWireColor;
        [SerializeField][UIFieldConfig("Fill color", "The grid fill color (i.e. cell area color).")]
        private Color           _fillColor              = defaultFillColor;
        [SerializeField][UIFieldConfig("Draw coordinate system", "If this is checked, the grid will draw its local coordinate system.")]
        private bool            _drawCoordSystem        = defaultDrawCoordSystem;
        [SerializeField][UIFieldConfig("X axis color", "The color of the grid's local coordinate system X axis.")]
        private Color           _xAxisColor             = defaultXAxisColor;
        [SerializeField][UIFieldConfig("Y axis color", "The color of the grid's local coordinate system Y axis.")]
        private Color           _yAxisColor             = defaultYAxisColor;
        [SerializeField][UIFieldConfig("Z axis color", "The color of the grid's local coordinate system Z axis.")]
        private Color           _zAxisColor             = defaultZAxisColor;
        [SerializeField][UIFieldConfig("Infinite X axis", "Allows you to specify whether the local X axis must be drawn as an infinite line segment.")]
        private bool            _infiniteXAxis          = defaultInfiniteXAxis;
        [SerializeField][UIFieldConfig("Infinite Y axis", "Allows you to specify whether the local Y axis must be drawn as an infinite line segment.")]
        private bool            _infiniteYAxis          = defaultInfiniteYAxis;
        [SerializeField][UIFieldConfig("Infinite Z axis", "Allows you to specify whether the local Z axis must be drawn as an infinite line segment.")]
        private bool            _infiniteZAxis          = defaultInfiniteZAxis;
        [SerializeField][Min(0.0f)][UIFieldConfig("Finite axis length", "This value is used to draw finite grid local axes.")]
        private float           _finiteAxisLength       = defaultFiniteAxisLength;

        public Color            wireColor               { get { return _wireColor; } set { UndoEx.record(this); _wireColor = value; EditorUtility.SetDirty(this); } }
        public Color            fillColor               { get { return _fillColor; } set { UndoEx.record(this); _fillColor = value; EditorUtility.SetDirty(this); } }
        public bool             drawCoordSystem         { get { return _drawCoordSystem; } set { UndoEx.record(this); _drawCoordSystem = value; EditorUtility.SetDirty(this); } }
        public Color            xAxisColor              { get { return _xAxisColor; } set { UndoEx.record(this); _xAxisColor = value; EditorUtility.SetDirty(this); } }
        public Color            yAxisColor              { get { return _yAxisColor; } set { UndoEx.record(this); _yAxisColor = value; EditorUtility.SetDirty(this); } }
        public Color            zAxisColor              { get { return _zAxisColor; } set { UndoEx.record(this); _zAxisColor = value; EditorUtility.SetDirty(this); } }
        public bool             infiniteXAxis           { get { return _infiniteXAxis; } set { UndoEx.record(this); _infiniteXAxis = value; EditorUtility.SetDirty(this); } }
        public bool             infiniteYAxis           { get { return _infiniteYAxis; } set { UndoEx.record(this); _infiniteYAxis = value; EditorUtility.SetDirty(this); } }
        public bool             infiniteZAxis           { get { return _infiniteZAxis; } set { UndoEx.record(this); _infiniteZAxis = value; EditorUtility.SetDirty(this); } }
        public float            finiteAxisLength        { get { return _finiteAxisLength; } set { UndoEx.record(this); _finiteAxisLength = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        
        public static Color     defaultWireColor        { get { return Color.black; } }
        public static Color     defaultFillColor        { get { return Color.gray.createNewAlpha(0.0f); } }
        public static bool      defaultDrawCoordSystem  { get { return true; } }
        public static Color     defaultXAxisColor       { get { return DefaultSystemValues.xAxisColor; } }
        public static Color     defaultYAxisColor       { get { return DefaultSystemValues.yAxisColor; } }
        public static Color     defaultZAxisColor       { get { return DefaultSystemValues.zAxisColor; } }
        public static bool      defaultInfiniteXAxis    { get { return true; } }
        public static bool      defaultInfiniteYAxis    { get { return false; } }
        public static bool      defaultInfiniteZAxis    { get { return true; } }
        public static float     defaultFiniteAxisLength { get { return 10.0f; } }

        public override void useDefaults()
        {
            wireColor           = defaultWireColor;
            fillColor           = defaultFillColor;
            drawCoordSystem     = defaultDrawCoordSystem;
            xAxisColor          = defaultXAxisColor;
            yAxisColor          = defaultYAxisColor;
            zAxisColor          = defaultZAxisColor;
            infiniteXAxis       = defaultInfiniteXAxis;
            infiniteYAxis       = defaultInfiniteYAxis;
            infiniteZAxis       = defaultInfiniteZAxis;
            finiteAxisLength    = defaultFiniteAxisLength;

            EditorUtility.SetDirty(this);
        }
    }

    class GridPrefsProvider : SettingsProvider
    {
        public GridPrefsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            UI.createPrefsTitleLabel("Grid", rootElement);
            GridPrefs.instance.buildDefaultUI(rootElement, PluginSettingsUIBuildConfig.defaultConfig);

            const float labelWidth = 150.0f;
            rootElement.Query<Label>().ForEach(item => item.setChildLabelWidth(labelWidth));
        }

        [SettingsProvider]
        public static SettingsProvider create()
        {
            if (GSpawn.active == null) return null;
            return new GridPrefsProvider("Preferences/" + GSpawn.pluginName + "/Grid", SettingsScope.User);
        }
    }
}
#endif