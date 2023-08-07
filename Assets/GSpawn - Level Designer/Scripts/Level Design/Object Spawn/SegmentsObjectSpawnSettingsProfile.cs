#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public enum SegmentsObjectSpawnCellMode
    {
        SpawnGuide = 0,
        Grid
    }

    public enum SegmentsObjectSpawnJitterMode
    { 
        None = 0,
        All,
        HeightRange
    }

    public enum SegmentsObjectSpawnHeightMode
    {
        Constant = 0,
        Random,
        Pattern
    }

    public enum SegmentsObjectSpawnCornerConnection
    {
        Normal = 0,
        Overlap,
        Gap
    }

    public enum SegmentsObjectSpawnProjectionMode
    {
        None = 0,
        Terrains
    }

    public enum SegmentsObjectSpawnMajorAxis
    {
        Longest = 0,
        Shortest
    }

    public enum SegmentsObjectSpawnFillMode
    {
        Solid = 0,
        Border
    }

    public enum SegmentsObjectSpawnPrefabPickMode
    {
        SpawnGuide = 0,
        Random,
        HeightRange,
    }

    public class SegmentsObjectSpawnSettingsProfile : Profile
    {
        [SerializeField]
        private SegmentsObjectSpawnCellMode                     _cellMode                           = defaultCellMode;
        [SerializeField]
        private bool                                            _useSceneGridCellSize               = defaultUseSceneGridCellSize;
        [SerializeField]
        private Vector3                                         _gridCellSize                       = defaultGridCellSize;
        [SerializeField]
        private SegmentsObjectSpawnMajorAxis                    _majorAxis                          = defaultMajorAxis;
        [SerializeField]
        private SegmentsObjectSpawnCornerConnection             _cornerConnection                   = defaultCornerConnection;
        [SerializeField]
        private bool                                            _rotateAtCorners                    = defaultRotateAtCorners;
        [SerializeField]
        private float                                           _objectSkipChance                   = defaultObjectSkipChance;
        [SerializeField]
        private float                                           _horizontalPadding                  = defaultHorizontalPadding;
        [SerializeField]
        private float                                           _verticalPadding                    = defaultVerticalPadding;
        [SerializeField]
        private float                                           _volumelessObjectSize               = defaultVolumelessObjectSize;
        [SerializeField]
        private bool                                            _avoidOverlaps                      = defaultAvoidOverlaps;
        [SerializeField]
        private int                                             _maxSegmentLength                   = defaultMaxSegmentLength;
        [SerializeField]
        private SegmentsObjectSpawnHeightMode                   _heightMode                         = defaultHeightMode;
        [SerializeField]
        private int                                             _defaultHeight                      = defaultDefaultHeight;
        [SerializeField]
        private int                                             _heightRaiseAmount                  = defaultHeightRaiseAmount;
        [SerializeField]
        private int                                             _heightLowerAmount                  = defaultHeightLowerAmount;
        [SerializeField]
        private int                                             _minRandomHeight                    = defaultMinRandomHeight;
        [SerializeField]
        private int                                             _maxRandomHeight                    = defaultMaxRandomHeight;
        [SerializeField]
        private IntPatternWrapMode                              _heightPatternWrapMode              = defaultHeightPatternWrapMode;
        [SerializeField]
        private IntPattern                                      _heightPattern                      = null;       // Note: Must be null initially to avoid calling SO ctor.
        [SerializeField]
        private SegmentsObjectSpawnJitterMode                   _jitterMode                         = defaultJitterMode;
        [SerializeField]
        private float                                           _minJitter                          = defaultMinJitter;
        [SerializeField]
        private float                                           _maxJitter                          = defaultMaxJitter;
        [SerializeField]
        private int                                             _minJitterHeight                    = defaultMinJitterHeight;
        [SerializeField]
        private int                                             _maxJitterHeight                    = defaultMaxJitterHeight;
        [SerializeField]
        private SegmentsObjectSpawnFillMode                     _fillMode                           = defaultFillMode;
        [SerializeField]
        private int                                             _beginBorderWidth                   = defaultBeginBorderWidth;
        [SerializeField]
        private int                                             _endBorderWidth                     = defaultEndBorderWidth;
        [SerializeField]
        private int                                             _topBorderWidth                     = defaultTopBorderWidth;
        [SerializeField]
        private int                                             _bottomBorderWidth                  = defaultBottomBorderWidth;
        [SerializeField]
        private int                                             _segmentCapBorderWidth              = defaultSegmentCapBorderWidth;
        [SerializeField]
        private SegmentsObjectSpawnPrefabPickMode               _prefabPickMode                     = defaultPrefabPickMode;
        [SerializeField]
        private string                                          _randomPrefabProfileName            = defaultRandomPrefabProfileName;
        [SerializeField]
        private string                                          _heightRangePrefabProfileName       = defaultHeightRangePrefabProfileName;
        [SerializeField]
        private SegmentsObjectSpawnProjectionMode               _projectionMode                     = defaultProjectionMode;

        private SerializedObject                                _serializedObject;
        private SerializedObject                                serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }
      
        public SegmentsObjectSpawnCellMode                      cellMode                        { get { return _cellMode; } set { UndoEx.record(this); _cellMode = value; EditorUtility.SetDirty(this); } }
        public bool                                             useSceneGridCellSize            { get { return _useSceneGridCellSize; } set { UndoEx.record(this); _useSceneGridCellSize = value; EditorUtility.SetDirty(this); } }
        public Vector3                                          gridCellSize                    { get { return _gridCellSize; } set { UndoEx.record(this); _gridCellSize = Vector3.Max(value, Vector3Ex.create(DefaultSystemValues.minGridCellSize)); EditorUtility.SetDirty(this); } }
        public SegmentsObjectSpawnMajorAxis                     majorAxis                       { get { return _majorAxis; } set { UndoEx.record(this); _majorAxis = value; EditorUtility.SetDirty(this); } }
        public SegmentsObjectSpawnCornerConnection              cornerConnection                { get { return _cornerConnection; } set { UndoEx.record(this); _cornerConnection = value; EditorUtility.SetDirty(this); } }
        public bool                                             rotateAtCorners                 { get { return _rotateAtCorners; } set { UndoEx.record(this); _rotateAtCorners = value; EditorUtility.SetDirty(this); } }
        public float                                            objectSkipChance                { get { return _objectSkipChance; } set { UndoEx.record(this); _objectSkipChance = Mathf.Clamp01(value); EditorUtility.SetDirty(this); } }
        public float                                            horizontalPadding               { get { return _horizontalPadding; } set { UndoEx.record(this); _horizontalPadding = value; EditorUtility.SetDirty(this); } }
        public float                                            verticalPadding                 { get { return _verticalPadding; } set { UndoEx.record(this); _verticalPadding = value; EditorUtility.SetDirty(this); } }
        public float                                            volumlessObjectSize             { get { return _volumelessObjectSize; } set { UndoEx.record(this); _volumelessObjectSize = Mathf.Max(1e-1f, value); EditorUtility.SetDirty(this); } }
        public bool                                             avoidOverlaps                   { get { return _avoidOverlaps; } set { UndoEx.record(this); _avoidOverlaps = value; EditorUtility.SetDirty(this); } }
        public int                                              maxSegmentLength                { get { return _maxSegmentLength; } set { UndoEx.record(this); _maxSegmentLength = Mathf.Max(2, value); EditorUtility.SetDirty(this); } }
        public SegmentsObjectSpawnHeightMode                    heightMode                      { get { return _heightMode; } set { UndoEx.record(this); _heightMode = value; EditorUtility.SetDirty(this); } }
        public int                                              defaultHeight                   { get { return _defaultHeight; } set { UndoEx.record(this); _defaultHeight = value; EditorUtility.SetDirty(this); } }
        public int                                              heightRaiseAmount               { get { return _heightRaiseAmount; } set { UndoEx.record(this); _heightRaiseAmount = Mathf.Max(1, value); EditorUtility.SetDirty(this); } }
        public int                                              heightLowerAmount               { get { return _heightLowerAmount; } set { UndoEx.record(this); _heightLowerAmount = Mathf.Max(1, value); EditorUtility.SetDirty(this); } }
        public int                                              minRandomHeight                 { get { return _minRandomHeight; } set { UndoEx.record(this); _minRandomHeight = Mathf.Min(value, _maxRandomHeight); EditorUtility.SetDirty(this); } }
        public int                                              maxRandomHeight                 { get { return _maxRandomHeight; } set { UndoEx.record(this); _maxRandomHeight = Mathf.Max(value, _minRandomHeight); EditorUtility.SetDirty(this); } }
        public IntPatternWrapMode                               heightPatternWrapMode           { get { return _heightPatternWrapMode; } set { UndoEx.record(this); _heightPatternWrapMode = value; EditorUtility.SetDirty(this); } }
        public IntPattern                                       heightPattern
        {
            get { if (_heightPattern == null) _heightPattern = defaultHeightPattern; return _heightPattern; }
            set { UndoEx.record(this); _heightPattern = value; EditorUtility.SetDirty(this); }
        }
        public SegmentsObjectSpawnJitterMode                    jitterMode                      { get { return _jitterMode; } set { UndoEx.record(this); _jitterMode = value; EditorUtility.SetDirty(this); } }
        public float                                            minJitter 
        { 
            get { return _minJitter; }
            set 
            {
                UndoEx.record(this);
                _minJitter = Mathf.Max(value, 0.0f);
                if (_maxJitter < _minJitter) _maxJitter = _minJitter;
                EditorUtility.SetDirty(this); 
            } 
        }
        public float                                            maxJitter 
        { 
            get { return _maxJitter; } 
            set 
            { 
                UndoEx.record(this); 
                _maxJitter = Mathf.Max(value, 0.0f);
                if (_minJitter > _maxJitter) _minJitter = _maxJitter;
                EditorUtility.SetDirty(this); 
            } 
        }
        public int                                              minJitterHeight             { get { return _minJitterHeight; } set { UndoEx.record(this); _minJitterHeight = Mathf.Min(value, _maxJitterHeight); EditorUtility.SetDirty(this); } }
        public int                                              maxJitterHeight             { get { return _maxJitterHeight; } set { UndoEx.record(this); _maxJitterHeight = Mathf.Max(value, _minJitterHeight); EditorUtility.SetDirty(this); } }
        public SegmentsObjectSpawnFillMode                      fillMode                    { get { return _fillMode; } set { UndoEx.record(this); _fillMode = value; EditorUtility.SetDirty(this); } }
        public int                                              beginBorderWidth            { get { return _beginBorderWidth; } set { UndoEx.record(this); _beginBorderWidth = Mathf.Max(0, value); EditorUtility.SetDirty(this); } }
        public int                                              endBorderWidth              { get { return _endBorderWidth; } set { UndoEx.record(this); _endBorderWidth = Mathf.Max(0, value); EditorUtility.SetDirty(this); } }
        public int                                              topBorderWidth              { get { return _topBorderWidth; } set { UndoEx.record(this); _topBorderWidth = Mathf.Max(0, value); EditorUtility.SetDirty(this); } }
        public int                                              bottomBorderWidth           { get { return _bottomBorderWidth; } set { UndoEx.record(this); _bottomBorderWidth = Mathf.Max(0, value); EditorUtility.SetDirty(this); } }
        public int                                              segmentCapBorderWidth       { get { return _segmentCapBorderWidth; } set { UndoEx.record(this); _segmentCapBorderWidth = Mathf.Max(0, value); EditorUtility.SetDirty(this); } }
        public SegmentsObjectSpawnPrefabPickMode                prefabPickMode              { get { return _prefabPickMode; } set { UndoEx.record(this); _prefabPickMode = value; EditorUtility.SetDirty(this); } }
        public RandomPrefabProfile                              randomPrefabProfile
        {
            get
            {
                var profile = RandomPrefabProfileDb.instance.findProfile(_randomPrefabProfileName);
                if (profile == null) profile = RandomPrefabProfileDb.instance.defaultProfile;
                return profile;
            }
        }
        public IntRangePrefabProfile                            heightRangePrefabProfile
        {
            get
            {
                var profile = IntRangePrefabProfileDb.instance.findProfile(_heightRangePrefabProfileName);
                if (profile == null) profile = IntRangePrefabProfileDb.instance.defaultProfile;
                return profile;
            }
        }
        public SegmentsObjectSpawnProjectionMode                    projectionMode                          { get { return _projectionMode; } set { UndoEx.record(this); _projectionMode = value; EditorUtility.SetDirty(this); } }

        public static SegmentsObjectSpawnCellMode                   defaultCellMode                         { get { return SegmentsObjectSpawnCellMode.SpawnGuide; } }
        public static bool                                          defaultUseSceneGridCellSize             { get { return true; } }
        public static Vector3                                       defaultGridCellSize                     { get { return Vector3.one; } }    
        public static SegmentsObjectSpawnMajorAxis                  defaultMajorAxis                        { get { return SegmentsObjectSpawnMajorAxis.Longest; } }
        public static SegmentsObjectSpawnCornerConnection           defaultCornerConnection                 { get { return SegmentsObjectSpawnCornerConnection.Normal; } }
        public static bool                                          defaultRotateAtCorners                  { get { return false; } }
        public static float                                         defaultObjectSkipChance                 { get { return 0.0f; } }
        public static float                                         defaultHorizontalPadding                { get { return 0.0f; } }
        public static float                                         defaultVerticalPadding                  { get { return 0.0f; } }
        public static float                                         defaultVolumelessObjectSize             { get { return 1.0f; } }
        public static bool                                          defaultAvoidOverlaps                    { get { return true; } }
        public static int                                           defaultMaxSegmentLength                 { get { return 200; } }
        public static SegmentsObjectSpawnHeightMode                 defaultHeightMode                       { get { return SegmentsObjectSpawnHeightMode.Constant; } }
        public static int                                           defaultDefaultHeight                    { get { return 1; } }
        public static int                                           defaultHeightRaiseAmount                { get { return 1; } }
        public static int                                           defaultHeightLowerAmount                { get { return 1; } }
        public static int                                           defaultMinRandomHeight                  { get { return 1; } }
        public static int                                           defaultMaxRandomHeight                  { get { return 5; } }
        public static IntPatternWrapMode                            defaultHeightPatternWrapMode            { get { return IntPatternWrapMode.Repeat; } }
        public static IntPattern                                    defaultHeightPattern                    { get { return IntPatternDb.instance.defaultPattern; } }
        public static SegmentsObjectSpawnJitterMode                 defaultJitterMode                       { get { return SegmentsObjectSpawnJitterMode.None; } }
        public static float                                         defaultMinJitter                        { get { return 0.0f; } }
        public static float                                         defaultMaxJitter                        { get { return 0.1f; } }
        public static int                                           defaultMinJitterHeight                  { get { return 1; } }
        public static int                                           defaultMaxJitterHeight                  { get { return 1; } }
        public static SegmentsObjectSpawnFillMode                   defaultFillMode                         { get { return SegmentsObjectSpawnFillMode.Solid; } }
        public static int                                           defaultBeginBorderWidth                 { get { return 1; } }
        public static int                                           defaultEndBorderWidth                   { get { return 1; } }
        public static int                                           defaultTopBorderWidth                   { get { return 1; } }
        public static int                                           defaultBottomBorderWidth                { get { return 1; } }
        public static int                                           defaultSegmentCapBorderWidth            { get { return 0; } }
        public static SegmentsObjectSpawnPrefabPickMode             defaultPrefabPickMode                   { get { return SegmentsObjectSpawnPrefabPickMode.SpawnGuide; } }
        public static string                                        defaultRandomPrefabProfileName          { get { return RandomPrefabProfileDb.defaultProfileName; } }
        public static string                                        defaultHeightRangePrefabProfileName     { get { return IntRangePrefabProfileDb.defaultProfileName; } }
        public static SegmentsObjectSpawnProjectionMode             defaultProjectionMode                   { get { return SegmentsObjectSpawnProjectionMode.None; } }

        public void onIntPatternsWillBeDeleted(List<IntPattern> patterns)
        {
            if (patterns.Contains(heightPattern))
            {
                UndoEx.record(this);
                _heightPattern = IntPatternDb.instance.defaultPattern;
            }
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 150.0f;

            //createCellModeControls(parent, labelWidth);
            createMiscControls(parent, labelWidth);
            var separator   = UI.createRowSeparator(parent);
            separator.name  = "Separator_Misc_Height";
            createHeightModeControls(parent, labelWidth);
            separator       = UI.createRowSeparator(parent);
            separator.name  = "Separator_Height_Jitter";
            createJitterModeControls(parent, labelWidth);
            separator       = UI.createRowSeparator(parent);
            separator.name  = "Separator_Jitter_FillMode";
            createFillModeControls(parent, labelWidth);
            separator       = UI.createRowSeparator(parent);
            separator.name  = "Separator_FillMode_PrefabPickMode";
            createPrefabPickModeControls(parent, labelWidth);
            UI.createRowSeparator(parent);
            createProjectionModeControls(parent, labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public void useDefaults()
        {
            cellMode                        = defaultCellMode;
            gridCellSize                    = defaultGridCellSize;
            useSceneGridCellSize            = defaultUseSceneGridCellSize;
            majorAxis                       = defaultMajorAxis;
            cornerConnection                = defaultCornerConnection;
            rotateAtCorners                 = defaultRotateAtCorners;
            objectSkipChance                = defaultObjectSkipChance;
            horizontalPadding               = defaultHorizontalPadding;
            verticalPadding                 = defaultVerticalPadding;
            volumlessObjectSize             = defaultVolumelessObjectSize;
            avoidOverlaps                   = defaultAvoidOverlaps;
            maxSegmentLength                = defaultMaxSegmentLength;
            heightMode                      = defaultHeightMode;
            defaultHeight                   = defaultDefaultHeight;
            heightRaiseAmount               = defaultHeightRaiseAmount;
            heightLowerAmount               = defaultHeightLowerAmount;
            minRandomHeight                 = defaultMinRandomHeight;
            maxRandomHeight                 = defaultMaxRandomHeight;
            heightPatternWrapMode           = defaultHeightPatternWrapMode;
            heightPattern                   = defaultHeightPattern;
            jitterMode                      = defaultJitterMode;
            minJitter                       = defaultMinJitter;
            maxJitter                       = defaultMaxJitter;
            minJitterHeight                 = defaultMinJitterHeight;
            maxJitterHeight                 = defaultMaxJitterHeight;
            fillMode                        = defaultFillMode;
            beginBorderWidth                = defaultBeginBorderWidth;
            endBorderWidth                  = defaultEndBorderWidth;
            topBorderWidth                  = defaultTopBorderWidth;
            bottomBorderWidth               = defaultBottomBorderWidth;
            segmentCapBorderWidth           = defaultSegmentCapBorderWidth;
            prefabPickMode                  = defaultPrefabPickMode;
            _randomPrefabProfileName        = defaultRandomPrefabProfileName;
            _heightRangePrefabProfileName   = defaultHeightRangePrefabProfileName;
            projectionMode                  = defaultProjectionMode;

            EditorUtility.SetDirty(this);
        }

/*
        private void createCellModeControls(VisualElement parent, float labelWidth)
        {
            Vector3Field gridCellSizeField      = null;
            Toggle useSceneGridCellSizeField    = null;

            // Note: Currently used. Doesn't seem to work 100%.
            var cellModeField = UI.createEnumField(typeof(SegmentsObjectSpawnCellMode), "_cellMode", serializedObject,
                "Cell mode", "Allows you to specify the way in which the segment cells are calculated.", parent);
            cellModeField.setChildLabelWidth(labelWidth);
            cellModeField.RegisterValueChangedCallback(p =>
            {
                gridCellSizeField.setDisplayVisible(cellMode == SegmentsObjectSpawnCellMode.Grid);
                useSceneGridCellSizeField.setDisplayVisible(cellMode == SegmentsObjectSpawnCellMode.Grid);
            });

            gridCellSizeField = UI.createVector3Field("_gridCellSize", serializedObject, "Grid cell size",
                "When the cell mode is set to Grid, this field allows you to specify the grid cell size.",
                Vector3Ex.create(DefaultSystemValues.minGridCellSize), parent);
            gridCellSizeField.setChildLabelWidth(labelWidth);
            gridCellSizeField.setDisplayVisible(cellMode == SegmentsObjectSpawnCellMode.Grid);

            useSceneGridCellSizeField = UI.createToggle("_useSceneGridCellSize", serializedObject, "Use scene grid cell size",
                "If checked and the cell mode is set to Grid, the plugin will use the cell size of the scene grid. ", parent);
            useSceneGridCellSizeField.setChildLabelWidth(labelWidth);
            useSceneGridCellSizeField.setDisplayVisible(cellMode == SegmentsObjectSpawnCellMode.Grid);
        }*/

        private void createMiscControls(VisualElement parent, float labelWidth)
        {
            VisualElement ctrl = UI.createEnumField(typeof(SegmentsObjectSpawnMajorAxis), "_majorAxis", serializedObject,
                "Major axis", "Allows you to specify the initial extension axis for the first segment in the chain.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createEnumField(typeof(SegmentsObjectSpawnCornerConnection), "_cornerConnection", serializedObject, "Corner connection", "Controls the way in which segments are connected at corners.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_rotateAtCorners", serializedObject, "Rotate at corners", "If checked, the objects will be rotated at each 90 degree turn.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createFloatField("_objectSkipChance", serializedObject, "Object skip chance", "Specifies the probability of an object being skipped during the spawn process.", 0.0f, 1.0f, parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createFloatField("_horizontalPadding", serializedObject, "Horizontal padding", "The amount of padding applied horizontally (i.e. along the extension plane).", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createFloatField("_verticalPadding", serializedObject, "Vertical padding", "The amount of padding applied vertically (i.e. perpendicular to the extension plane).", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createFloatField("_volumelessObjectSize", serializedObject, "Volumeless object size", "The size that should be used for objects that don't have a volume.", 1e-1f, parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_avoidOverlaps", serializedObject, "Avoid overlaps",
                                "If this is checked, no objects will be created in places where they would overlap with already existing objects.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createIntegerField("_maxSegmentLength", serializedObject, "Max segment length", "The maximum length a segment can have. Useful to prevent " +
                "segments from getting too long for certain camera angles.", 2, parent);
            ctrl.setChildLabelWidth(labelWidth);
        }

        private void createHeightModeControls(VisualElement parent, float labelWidth)
        {
            var heightModeField = UI.createEnumField(typeof(SegmentsObjectSpawnHeightMode), "_heightMode", serializedObject, "Height mode", "Allows you to specify the way in which the segments' height is updated.", parent);
            heightModeField.setChildLabelWidth(labelWidth);

            IntegerField defaultHeightField = UI.createIntegerField("_defaultHeight", serializedObject, "Default height", "The default height that will be used when segment creation starts. " + 
                "For height modes other than Constant, this acts as an offset/base height.", parent);
            defaultHeightField.setChildLabelWidth(labelWidth);

            IntegerField raiseAmountField = UI.createIntegerField("_heightRaiseAmount", serializedObject, "Raise amount", "Specifies how much the segments are raised when using the scroll wheel.", parent);
            raiseAmountField.setChildLabelWidth(labelWidth);

            IntegerField lowerAmountField = UI.createIntegerField("_heightLowerAmount", serializedObject, "Lower amount", "Specifies how much the segments are lowered when using the scroll wheel.", parent);
            lowerAmountField.setChildLabelWidth(labelWidth);

            IntegerField minRandomHeightField = UI.createIntegerField("_minRandomHeight", serializedObject, "Min height", "The minimum random height.", parent);
            minRandomHeightField.setChildLabelWidth(labelWidth);

            IntegerField maxRandomHeightField = UI.createIntegerField("_maxRandomHeight", serializedObject, "Max height", "The maximum random height.", parent);
            maxRandomHeightField.setChildLabelWidth(labelWidth);

            minRandomHeightField.bindMaxValueProperty("_minRandomHeight", "_maxRandomHeight", serializedObject);
            maxRandomHeightField.bindMinValueProperty("_maxRandomHeight", "_minRandomHeight", serializedObject);

            EnumField intPatternWrapModeField = UI.createEnumField(typeof(IntPatternWrapMode), "_heightPatternWrapMode", serializedObject, "Wrap mode", "The wrap mode determines how the pattern is sampled outside the bounds of its value array.", parent);
            intPatternWrapModeField.setChildLabelWidth(labelWidth);

            IMGUIContainer heightPatternContainer   = UI.createIMGUIContainer(parent);
            heightPatternContainer.name             = "_heightPatternContainer";
            heightPatternContainer.onGUIHandler     = () =>
            {
                IntPattern selectedPattern = EditorUIEx.intPatternSelectionField("Pattern", labelWidth, _heightPattern);
                if (selectedPattern != _heightPattern)
                {
                    UndoEx.record(this);
                    _heightPattern = selectedPattern;
                    EditorUtility.SetDirty(this);
                }
            };

            minRandomHeightField.setDisplayVisible(heightMode == SegmentsObjectSpawnHeightMode.Random);
            maxRandomHeightField.setDisplayVisible(heightMode == SegmentsObjectSpawnHeightMode.Random);

            heightPatternContainer.setDisplayVisible(heightMode == SegmentsObjectSpawnHeightMode.Pattern);
            intPatternWrapModeField.setDisplayVisible(heightMode == SegmentsObjectSpawnHeightMode.Pattern);

            heightModeField.RegisterValueChangedCallback((p) =>
            {
                minRandomHeightField.setDisplayVisible(heightMode == SegmentsObjectSpawnHeightMode.Random);
                maxRandomHeightField.setDisplayVisible(heightMode == SegmentsObjectSpawnHeightMode.Random);

                heightPatternContainer.setDisplayVisible(heightMode == SegmentsObjectSpawnHeightMode.Pattern);
                intPatternWrapModeField.setDisplayVisible(heightMode == SegmentsObjectSpawnHeightMode.Pattern);
            });
        }

        private void createJitterModeControls(VisualElement parent, float labelWidth)
        {
            var jitterModeField = UI.createEnumField(typeof(SegmentsObjectSpawnJitterMode), "_jitterMode", serializedObject, "Jitter mode", "Allows you to specify " +
                "the way in which the plugin will apply random offsets to the objects' positions in a direction perpendicular to each segment.", parent);
            jitterModeField.setChildLabelWidth(labelWidth);

            var minJitterField = UI.createFloatField("_minJitter", serializedObject, "Min jitter", "Minimum jitter amount.", 0.0f, parent);
            minJitterField.setChildLabelWidth(labelWidth);
            minJitterField.bindMaxValueProperty("_minJitter", "_maxJitter", 0.0f, serializedObject);

            var maxJitterField = UI.createFloatField("_maxJitter", serializedObject, "Max jitter", "Maximum jitter amount.", 0.0f, parent);
            maxJitterField.setChildLabelWidth(labelWidth);
            maxJitterField.bindMinValueProperty("_maxJitter", "_minJitter", 0.0f, serializedObject);

            minJitterField.setDisplayVisible(jitterMode != SegmentsObjectSpawnJitterMode.None);
            maxJitterField.setDisplayVisible(jitterMode != SegmentsObjectSpawnJitterMode.None);

            var minJitterHeightField = UI.createIntegerField("_minJitterHeight", serializedObject, "Min height", "Jitter will be applied to objects in the [Min height, Max height] range.", parent);
            minJitterHeightField.setChildLabelWidth(labelWidth);
            minJitterHeightField.bindMaxValueProperty("_minJitterHeight", "_maxJitterHeight", serializedObject);

            var maxJitterHeightField = UI.createIntegerField("_maxJitterHeight", serializedObject, "Max height", "Jitter will be applied to objects in the [Min height, Max height] range.", parent);
            maxJitterHeightField.setChildLabelWidth(labelWidth);
            maxJitterHeightField.bindMinValueProperty("_maxJitterHeight", "_minJitterHeight", serializedObject);

            minJitterHeightField.setDisplayVisible(jitterMode == SegmentsObjectSpawnJitterMode.HeightRange);
            maxJitterHeightField.setDisplayVisible(jitterMode == SegmentsObjectSpawnJitterMode.HeightRange);

            jitterModeField.RegisterValueChangedCallback((p) =>
            {
                minJitterField.setDisplayVisible(jitterMode != SegmentsObjectSpawnJitterMode.None);
                maxJitterField.setDisplayVisible(jitterMode != SegmentsObjectSpawnJitterMode.None);
                minJitterHeightField.setDisplayVisible(jitterMode == SegmentsObjectSpawnJitterMode.HeightRange);
                maxJitterHeightField.setDisplayVisible(jitterMode == SegmentsObjectSpawnJitterMode.HeightRange);
            });
        }

        private void createFillModeControls(VisualElement parent, float labelWidth)
        {
            EnumField fillModeField = UI.createEnumField(typeof(SegmentsObjectSpawnFillMode), "_fillMode", serializedObject, "Fill mode", "Allows you to specify the segment fill mode.", parent);
            fillModeField.setChildLabelWidth(labelWidth);

            IntegerField beginBorderWidthField = UI.createIntegerField("_beginBorderWidth", serializedObject, "Begin width", "The border width applied at the beginning of the segment chain.", 0, parent);
            beginBorderWidthField.setChildLabelWidth(labelWidth);

            IntegerField endBorderWidthField = UI.createIntegerField("_endBorderWidth", serializedObject, "End width", "The border width applied at the end of the segment chain.", 0, parent);
            endBorderWidthField.setChildLabelWidth(labelWidth);

            IntegerField topBorderWidthField = UI.createIntegerField("_topBorderWidth", serializedObject, "Top width", "The border width applied at the top of the segments. Note: Top means the last cell in a stack. If a stack " +
                "grows downwards, top will be treated as the bottom most cell.", 0, parent);
            topBorderWidthField.setChildLabelWidth(labelWidth);

            IntegerField bottomBorderWidthField = UI.createIntegerField("_bottomBorderWidth", serializedObject, "Bottom width", "The border width applied at the bottom of the segments. Note: Bottom means the first cell in a stack.", 0, parent);
            bottomBorderWidthField.setChildLabelWidth(labelWidth);

            IntegerField segmentCapBorderWidthField = UI.createIntegerField("_segmentCapBorderWidth", serializedObject, "Segment cap width", "The border width applied to cap a segment when the chain reaches a dead-end.", 0, parent);
            segmentCapBorderWidthField.setChildLabelWidth(labelWidth);

            beginBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);
            endBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);
            topBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);
            bottomBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);
            segmentCapBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);

            fillModeField.RegisterValueChangedCallback(p =>
            {
                beginBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);
                endBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);
                topBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);
                bottomBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);
                segmentCapBorderWidthField.setDisplayVisible(fillMode == SegmentsObjectSpawnFillMode.Border);
            });
        }

        private void createPrefabPickModeControls(VisualElement parent, float labelWidth)
        {
            EnumField pickModeField = UI.createEnumField(typeof(SegmentsObjectSpawnPrefabPickMode), "_prefabPickMode", serializedObject, "Prefab pick mode", "Allows you to specify the prefabs that will be used to spawn objects in the segment chain.", parent);
            pickModeField.setChildLabelWidth(labelWidth);

            IMGUIContainer randomPrefabProfileContainer     = UI.createIMGUIContainer(parent);
            randomPrefabProfileContainer.name               = "_randomPrefabProfileContainer";
            randomPrefabProfileContainer.onGUIHandler       = () =>
            {
                string newName = EditorUIEx.profileNameSelectionField<RandomPrefabProfileDb, RandomPrefabProfile>
                    (RandomPrefabProfileDb.instance, "Random prefab profile", labelWidth, _randomPrefabProfileName);
                if (newName != _randomPrefabProfileName)
                {
                    UndoEx.record(this);
                    _randomPrefabProfileName = newName;
                    EditorUtility.SetDirty(this);
                }
                GUI.enabled = true;
            };

            IMGUIContainer intRangePrefabProfileContainer   = UI.createIMGUIContainer(parent);
            intRangePrefabProfileContainer.name             = "_intRangePrefabProfileContainer";
            intRangePrefabProfileContainer.onGUIHandler     = () =>
            {
                string newName = EditorUIEx.profileNameSelectionField<IntRangePrefabProfileDb, IntRangePrefabProfile>
                    (IntRangePrefabProfileDb.instance, "Int range prefab profile", labelWidth, _heightRangePrefabProfileName);
                if (newName != _heightRangePrefabProfileName)
                {
                    UndoEx.record(this);
                    _heightRangePrefabProfileName = newName;
                    EditorUtility.SetDirty(this);
                }
                GUI.enabled = true;
            };

            randomPrefabProfileContainer.setDisplayVisible(prefabPickMode == SegmentsObjectSpawnPrefabPickMode.Random);
            intRangePrefabProfileContainer.setDisplayVisible(prefabPickMode == SegmentsObjectSpawnPrefabPickMode.HeightRange);

            pickModeField.RegisterValueChangedCallback(p =>
            {
                randomPrefabProfileContainer.setDisplayVisible(prefabPickMode == SegmentsObjectSpawnPrefabPickMode.Random);
                intRangePrefabProfileContainer.setDisplayVisible(prefabPickMode == SegmentsObjectSpawnPrefabPickMode.HeightRange);
            });
        }

        private void createProjectionModeControls(VisualElement parent, float labelWidth)
        {
            VisualElement ctrl = UI.createEnumField(typeof(SegmentsObjectSpawnProjectionMode), "_projectionMode", serializedObject, "Projection mode", "Allows you to specify how the spawned objects will be projected in the scene.", parent);
            ctrl.setChildLabelWidth(labelWidth);
        }
    }
}
#endif