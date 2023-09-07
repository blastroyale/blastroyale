#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public enum TileRuleSegmentBrushHeightMode
    {
        Constant = 0,
        Random,
        Pattern
    }

    public enum TileRuleBrushFillMode
    {
        Solid = 0,
        Hollow
    }

    public enum TileRuleConnectMode
    {
        Manhattan = 0,
    }

    public enum TileRuleConnectMajorAxis
    {
        X = 0,
        Z
    }

    public class TileRuleObjectSpawnSettings : PluginSettings<TileRuleObjectSpawnSettings>
    {
        private const int _maxBrushSize         = 10;

        //[NonSerialized]
        //private EnumField _connectMajorAxisField;

        [SerializeField]
        private int     _brushYOffset                   = defaultBrushYOffset;
        [SerializeField]
        private bool    _snapBrushYOffsetToTiles        = defaultSnapBrushYOffsetToTiles;
        [SerializeField]
        private int     _brushSize                      = defaultBrushSize;
        [SerializeField]
        private int     _brushHeight                    = defaultBrushHeight;
        
        [SerializeField]
        private TileRuleBrushFillMode               _flexiBoxBrushFillMode          = defaultFlexiBoxBrushFillMode;
        [SerializeField]
        private bool                                _segBrushFillCorners            = defaultSegBrushFillCorners;
        [SerializeField]
        private TileRuleSegmentBrushHeightMode      _segBrushHeightMode             = defaultSegBrushHeightMode;
        [SerializeField]
        private int                                 _segBrushMinRandomHeight        = defaultSegBrushMinRandomHeight;
        [SerializeField]
        private int                                 _segBrushMaxRandomHeight        = defaultSegBrushMaxRandomHeight;
        [SerializeField]
        private IntPatternWrapMode                  _segBrushHeightPatternWrapMode  = defaultHeightPatternWrapMode;
        [SerializeField]
        private IntPattern                          _segBrushHeightPattern          = null;
        [SerializeField]
        private bool                                _eraseForeignObjects            = defaultEraseForeignObjects;
        [SerializeField]
        private int                                 _connectYOffset                 = defaultConnectYOffset;
        [SerializeField]
        private TileRuleConnectMode                 _connectMode                    = defaultConnectMode;
        [SerializeField]
        private TileRuleConnectMajorAxis            _connectMajorAxis               = defaultConnectMajorAxis;
        [SerializeField]
        private bool                                _connectFillCorners             = defaultConnectFillCorners;
        [SerializeField]
        private bool                                _connectGenerateRamps           = defaultConnectGenerateRamps;

        public int                                  brushYOffset                    { get { return _brushYOffset; } set { UndoEx.record(this); _brushYOffset = value; EditorUtility.SetDirty(this); } }
        public int                                  brushSize                       { get { return _brushSize; } set { UndoEx.record(this); _brushSize = Mathf.Clamp(value, 1, _maxBrushSize); EditorUtility.SetDirty(this); } }
        public int                                  brushHeight                     { get { return _brushHeight; } set { UndoEx.record(this); _brushHeight = Mathf.Max(value, 1); EditorUtility.SetDirty(this); } }       
        public bool                                 snapBrushYOffsetToTiles         { get { return _snapBrushYOffsetToTiles; } set { UndoEx.record(this); _snapBrushYOffsetToTiles = value; EditorUtility.SetDirty(this); } }
        public TileRuleBrushFillMode                flexiBoxBrushFillMode           { get { return _flexiBoxBrushFillMode; } set { UndoEx.record(this); _flexiBoxBrushFillMode = value; EditorUtility.SetDirty(this); } }
        public bool                                 segBrushFillCorners             { get { return _segBrushFillCorners; } set { UndoEx.record(this); _segBrushFillCorners = value; EditorUtility.SetDirty(this); } }
        public TileRuleSegmentBrushHeightMode       segBrushHeightMode              { get { return _segBrushHeightMode; } set { UndoEx.record(this); _segBrushHeightMode = value; EditorUtility.SetDirty(this); } }
        public int                                  segBrushMinRandomHeight         { get { return _segBrushMinRandomHeight; } set { UndoEx.record(this); UndoEx.record(this); _segBrushMinRandomHeight = Mathf.Min(value, _segBrushMaxRandomHeight); EditorUtility.SetDirty(this); } }
        public int                                  segBrushMaxRandomHeight         { get { return _segBrushMaxRandomHeight; } set { UndoEx.record(this); _segBrushMaxRandomHeight = Mathf.Max(value, _segBrushMinRandomHeight); EditorUtility.SetDirty(this); } }
        public IntPatternWrapMode                   segBrushHeightPatternWrapMode   { get { return _segBrushHeightPatternWrapMode; } set { UndoEx.record(this); _segBrushHeightPatternWrapMode = value; EditorUtility.SetDirty(this); } }
        public IntPattern                           segBrushHeightPattern
        {
            get { if (_segBrushHeightPattern == null) _segBrushHeightPattern = defaultHeightPattern; return _segBrushHeightPattern; }
            set { UndoEx.record(this); _segBrushHeightPattern = value; EditorUtility.SetDirty(this); }
        }
        public bool                                 eraseForeignObjects             { get { return _eraseForeignObjects; } set { UndoEx.record(this); _eraseForeignObjects = value; EditorUtility.SetDirty(this); } }
        public int                                  connectYOffset                  { get { return _connectYOffset; } set { UndoEx.record(this); _connectYOffset = value; EditorUtility.SetDirty(this); } }
        public TileRuleConnectMode                  connectMode                     { get { return _connectMode; } set { UndoEx.record(this); _connectMode = value; EditorUtility.SetDirty(this); } }
        public TileRuleConnectMajorAxis             connectMajorAxis                { get { return _connectMajorAxis; } set { UndoEx.record(this); _connectMajorAxis = value; EditorUtility.SetDirty(this); } }
        public bool                                 connectFillCorners              { get { return _connectFillCorners; } set { UndoEx.record(this); _connectFillCorners = value; EditorUtility.SetDirty(this); } }
        public bool                                 connectGenerateRamps            { get { return _connectGenerateRamps; } set { UndoEx.record(this); _connectGenerateRamps = value; EditorUtility.SetDirty(this); } }

        public static int                                   defaultBrushYOffset                 { get { return 0; } }
        public static bool                                  defaultSnapBrushYOffsetToTiles      { get { return true; } }
        public static int                                   defaultBrushSize                    { get { return 1; } }
        public static int                                   defaultBrushHeight                  { get { return 1; } }
        public static TileRuleBrushFillMode                 defaultFlexiBoxBrushFillMode        { get { return TileRuleBrushFillMode.Solid; } }
        public static bool                                  defaultSegBrushFillCorners          { get { return true; } }
        public static TileRuleSegmentBrushHeightMode        defaultSegBrushHeightMode           { get { return TileRuleSegmentBrushHeightMode.Constant; } }
        public static int                                   defaultSegBrushMinRandomHeight      { get { return 1; } }
        public static int                                   defaultSegBrushMaxRandomHeight      { get { return 5; } }
        public static IntPatternWrapMode                    defaultHeightPatternWrapMode        { get { return IntPatternWrapMode.Repeat; } }
        public static IntPattern                            defaultHeightPattern                { get { return IntPatternDb.instance.defaultPattern; } }
        public static bool                                  defaultEraseForeignObjects          { get { return true; } }
        public static int                                   defaultConnectYOffset               { get { return 0; } }
        public static TileRuleConnectMode                   defaultConnectMode                  { get { return TileRuleConnectMode.Manhattan; } }
        public static TileRuleConnectMajorAxis              defaultConnectMajorAxis             { get { return TileRuleConnectMajorAxis.X; } }
        public static bool                                  defaultConnectFillCorners           { get { return false; } }
        public static bool                                  defaultConnectGenerateRamps         { get { return true; } }

        public void onIntPatternsWillBeDeleted(List<IntPattern> patterns)
        {
            if (patterns.Contains(segBrushHeightPattern))
            {
                UndoEx.record(this);
                segBrushHeightPattern = IntPatternDb.instance.defaultPattern;
            }
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 150.0f;

            UI.createRowSeparator(parent);
            UI.createSectionLabel("Brush (General)", parent);
            var intField    = UI.createIntegerField("_brushYOffset", serializedObject, "Y offset", "The brush offset expressed in tile Y coordinates. Applies to all brush types.", parent);
            intField.setChildLabelWidth(labelWidth);
            var snapToTilesToggle = UI.createToggle("_snapBrushYOffsetToTiles", serializedObject, "Snap to tiles", "If checked, the brush Y offset will snap " + 
                "to the tile hovered by the mouse cursor.", parent);
            snapToTilesToggle.setChildLabelWidth(labelWidth);
            intField        = UI.createIntegerField("_brushSize", serializedObject, "Size", "The horizontal (XZ) brush size. Applies to the Box brush only.", 1, _maxBrushSize, parent);
            intField.setChildLabelWidth(labelWidth);
            intField        = UI.createIntegerField("_brushHeight", serializedObject, "Height", "The vertical (Y) brush size. Applies to all brush types. When Segments brush is used, this " + 
                "field is ignored if the height mode is anything other than Constant.", 1, parent);
            intField.setChildLabelWidth(labelWidth);

            UI.createRowSeparator(parent);
            UI.createSectionLabel("Flexi Box Brush", parent);
            var fillModeField       = UI.createEnumField(typeof(TileRuleBrushFillMode), "_flexiBoxBrushFillMode", serializedObject, "Fill mode", "The Flexi Box brush brush fill mode.", parent);
            fillModeField.setChildLabelWidth(labelWidth);

            UI.createRowSeparator(parent);
            UI.createSectionLabel("Segments Brush", parent);

            var toggleField = UI.createToggle("_segBrushFillCorners", serializedObject, "Fill corners",
                "The segment brush uses a line drawing algorithm to paint tiles and this can create gaps between successive tiles inside a segment. If this " + 
                "is checked, the plugin will add extra tiles in order to fill in the gaps.", parent);
            toggleField.setChildLabelWidth(labelWidth);

            var segBrushHeightModeField = UI.createEnumField(typeof(TileRuleSegmentBrushHeightMode), "_segBrushHeightMode",
                serializedObject, "Height mode", "Allows you to specify the way in which the height of the segments is updated when using the segments brush.", parent);
            segBrushHeightModeField.setChildLabelWidth(labelWidth);

            IntegerField minRandomHeightField = UI.createIntegerField("_segBrushMinRandomHeight", serializedObject, "Min height", "The minimum random height.", parent);
            minRandomHeightField.setChildLabelWidth(labelWidth);

            IntegerField maxRandomHeightField = UI.createIntegerField("_segBrushMaxRandomHeight", serializedObject, "Max height", "The maximum random height.", parent);
            maxRandomHeightField.setChildLabelWidth(labelWidth);

            minRandomHeightField.bindMaxValueProperty("_segBrushMinRandomHeight", "_segBrushMaxRandomHeight", serializedObject);
            maxRandomHeightField.bindMinValueProperty("_segBrushMaxRandomHeight", "_segBrushMinRandomHeight", serializedObject);

            EnumField intPatternWrapModeField = UI.createEnumField(typeof(IntPatternWrapMode), "_segBrushHeightPatternWrapMode", serializedObject, "Wrap mode", "The wrap mode determines how the pattern is sampled outside the bounds of its value array.", parent);
            intPatternWrapModeField.setChildLabelWidth(labelWidth);

            IMGUIContainer heightPatternContainer = UI.createIMGUIContainer(parent);
            heightPatternContainer.name = "_heightPatternContainer";
            heightPatternContainer.onGUIHandler = () =>
            {
                IntPattern selectedPattern = EditorUIEx.intPatternSelectionField("Pattern", labelWidth, segBrushHeightPattern);
                if (selectedPattern != segBrushHeightPattern)
                {
                    UndoEx.record(this);
                    segBrushHeightPattern = selectedPattern;
                    EditorUtility.SetDirty(this);
                }
            };

            segBrushHeightModeField.RegisterValueChangedCallback(p => 
            {
                minRandomHeightField.setDisplayVisible(segBrushHeightMode == TileRuleSegmentBrushHeightMode.Random);
                maxRandomHeightField.setDisplayVisible(segBrushHeightMode == TileRuleSegmentBrushHeightMode.Random);

                intPatternWrapModeField.setDisplayVisible(segBrushHeightMode == TileRuleSegmentBrushHeightMode.Pattern);
                heightPatternContainer.setDisplayVisible(segBrushHeightMode == TileRuleSegmentBrushHeightMode.Pattern);
            });

            minRandomHeightField.setDisplayVisible(segBrushHeightMode == TileRuleSegmentBrushHeightMode.Random);
            maxRandomHeightField.setDisplayVisible(segBrushHeightMode == TileRuleSegmentBrushHeightMode.Random);

            intPatternWrapModeField.setDisplayVisible(segBrushHeightMode == TileRuleSegmentBrushHeightMode.Pattern);
            heightPatternContainer.setDisplayVisible(segBrushHeightMode == TileRuleSegmentBrushHeightMode.Pattern);

            UI.createRowSeparator(parent);
            UI.createSectionLabel("Erase", parent);
            toggleField = UI.createToggle("_eraseForeignObjects", serializedObject, "Erase foreign objects", "If checked, the erase brush is allowed to erase foreign objects. " +
                "Those are non-tile objects that have been placed using other spawn tools such as modular snap for example.", parent);
            toggleField.setChildLabelWidth(labelWidth);

            UI.createRowSeparator(parent);
            UI.createSectionLabel("Connect", parent);
            intField = UI.createIntegerField("_connectYOffset", serializedObject, "Y offset", "The connection cell Y offset expressed tile coordinates. Note: Applies to both start and end cells.", parent);
            intField.setChildLabelWidth(labelWidth);

/*
            _connectMajorAxisField  = UI.createEnumField(typeof(TileRuleConnectMajorAxis), "_connectMajorAxis", serializedObject, "Major axis", "", parent);
            _connectMajorAxisField.setChildLabelWidth(labelWidth);*/

            toggleField = UI.createToggle("_connectFillCorners", serializedObject, 
                "Fill corners", "If checked, the plugin will fill in the corner gaps which appear when tiles move up or down along the grid vertical axis.", parent);
            toggleField.setChildLabelWidth(labelWidth);

            toggleField = UI.createToggle("_connectGenerateRamps", serializedObject,
                "Generate ramps", "If checked, ramps will be generated in places where the tile path moves on tile up or down.", parent);
            toggleField.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => { useDefaults(); }, parent);

            refreshTooltips();
        }

        public override void useDefaults()
        {
            brushYOffset                    = defaultBrushYOffset;
            snapBrushYOffsetToTiles         = defaultSnapBrushYOffsetToTiles;   
            brushSize                       = defaultBrushSize;
            brushHeight                     = defaultBrushHeight;
            flexiBoxBrushFillMode           = defaultFlexiBoxBrushFillMode;
            segBrushFillCorners             = defaultSegBrushFillCorners;
            segBrushHeightMode              = defaultSegBrushHeightMode;
            segBrushMinRandomHeight         = defaultSegBrushMinRandomHeight;
            segBrushMaxRandomHeight         = defaultSegBrushMaxRandomHeight;
            segBrushHeightPatternWrapMode   = defaultHeightPatternWrapMode;
            segBrushHeightPattern           = defaultHeightPattern;
            eraseForeignObjects             = defaultEraseForeignObjects;
            connectYOffset                  = defaultConnectYOffset;
            connectMajorAxis                = defaultConnectMajorAxis;
            connectFillCorners              = defaultConnectFillCorners;
            connectGenerateRamps            = defaultConnectGenerateRamps;

            EditorUtility.SetDirty(this);
        }

        public void refreshTooltips()
        {
            /*if (_connectMajorAxisField != null)
                _connectMajorAxisField.tooltip = ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSpawnShortcutNames.tileRuleSpawn_Connect_ChangeMajorAxis,
                "Allows you to specify the connection major axis. This is the axis which will be used to create the first segment in the perpendicular segment pair.");*/
        }
    }
}
#endif