#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public enum BoxObjectSpawnCellMode
    {
        SpawnGuide = 0,
        Grid
    }

    public enum BoxObjectSpawnCornerMode
    {
        Normal = 0,
        Gap
    }

    public enum BoxObjectSpawnHeightMode
    {
        Constant = 0,
        Random,
        Pattern
    }

    public enum BoxObjectSpawnFillMode
    {
        Solid = 0,
        Border,
        Hollow
    }

    public enum BoxObjectSpawnPrefabPickMode
    {
        SpawnGuide = 0,
        Random,
        HeightRange,
    }

    public enum BoxObjectSpawnProjectionMode
    {
        None = 0,
        Terrains
    }

    public class BoxObjectSpawnSettingsProfile : Profile
    {
        [SerializeField]
        private BoxObjectSpawnCellMode          _cellMode                       = defaultCellMode;
        [SerializeField]
        private bool                            _useSceneGridCellSize           = defaultUseSceneGridCellSize;
        [SerializeField]
        private Vector3                         _gridCellSize                   = defaultGridCellSize;
        [SerializeField]
        private BoxObjectSpawnCornerMode        _cornerMode                     = defaultCornerMode;
        [SerializeField]
        private int                             _cornerGapSize                  = defaultCornerGapSize;
        [SerializeField]
        private float                           _objectSkipChance               = defaultObjectSkipChance;
        [SerializeField]
        private float                           _horizontalPadding              = defaultHorizontalPadding;
        [SerializeField]
        private float                           _verticalPadding                = defaultVerticalPadding;
        [SerializeField]
        private float                           _volumelessObjectSize           = defaultVolumelessObjectSize;
        [SerializeField]
        private bool                            _avoidOverlaps                  = defaultAvoidOverlaps;
        [SerializeField]
        private int                             _maxSize                        = defaultMaxSize;

        [SerializeField]
        private BoxObjectSpawnHeightMode        _heightMode                     = defaultHeightMode;
        [SerializeField]
        private int                             _defaultHeight                  = defaultDefaultHeight;
        [SerializeField]
        private int                             _heightRaiseAmount              = defaultHeightRaiseAmount;
        [SerializeField]
        private int                             _heightLowerAmount              = defaultHeightLowerAmount;
        [SerializeField]
        private int                             _minRandomHeight                = defaultMinRandomHeight;
        [SerializeField]
        private int                             _maxRandomHeight                = defaultMaxRandomHeight;
        [SerializeField]
        private IntPatternWrapMode              _heightPatternWrapMode          = defaultHeightPatternWrapMode;
        [SerializeField]
        private IntPattern                      _heightPattern                  = null;       // Note: Must be null initially to avoid calling SO ctor.
        [SerializeField]
        private bool                            _constrainSizeToHeightPattern   = defaultConstrainSizeToHeightPattern;

        [SerializeField]
        private BoxObjectSpawnFillMode          _fillMode                       = defaultFillMode;
        [SerializeField]
        private int                             _borderWidth                    = defaultBorderWidth;

        [SerializeField]
        private BoxObjectSpawnPrefabPickMode    _prefabPickMode                 = defaultPrefabPickMode;
        [SerializeField]
        private string                          _randomPrefabProfileName        = defaultRandomPrefabProfileName;
        [SerializeField]
        private string                          _heightRangePrefabProfileName   = defaultHeightRangePrefabProfileName;

        [SerializeField]
        private BoxObjectSpawnProjectionMode    _projectionMode                 = defaultProjectionMode;

        private SerializedObject _serializedObject;
        private SerializedObject                serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }

        public BoxObjectSpawnCellMode           cellMode                        { get { return _cellMode; } set { UndoEx.record(this); _cellMode = value; EditorUtility.SetDirty(this); } }
        public bool                             useSceneGridCellSize            { get { return _useSceneGridCellSize; } set { UndoEx.record(this); _useSceneGridCellSize = value; EditorUtility.SetDirty(this); } }
        public Vector3                          gridCellSize                    { get { return _gridCellSize; } set { UndoEx.record(this); _gridCellSize = Vector3.Max(value, Vector3Ex.create(DefaultSystemValues.minGridCellSize)); EditorUtility.SetDirty(this); } }
        public BoxObjectSpawnCornerMode         cornerMode                      { get { return _cornerMode; } set { UndoEx.record(this); _cornerMode = value; EditorUtility.SetDirty(this); } }
        public int                              cornerGapSize                   { get { return _cornerGapSize; } set { UndoEx.record(this); _cornerGapSize = Mathf.Max(1, value); EditorUtility.SetDirty(this); } }
        public float                            objectSkipChance                { get { return _objectSkipChance; } set { UndoEx.record(this); _objectSkipChance = Mathf.Clamp01(value); EditorUtility.SetDirty(this); } }
        public float                            horizontalPadding               { get { return _horizontalPadding; } set { UndoEx.record(this); _horizontalPadding = value; EditorUtility.SetDirty(this); } }
        public float                            verticalPadding                 { get { return _verticalPadding; } set { UndoEx.record(this); _verticalPadding = value; EditorUtility.SetDirty(this); } }
        public float                            volumlessObjectSize             { get { return _volumelessObjectSize; } set { UndoEx.record(this); _volumelessObjectSize = Mathf.Max(1e-1f, value); EditorUtility.SetDirty(this); } }
        public bool                             avoidOverlaps                   { get { return _avoidOverlaps; } set { UndoEx.record(this); _avoidOverlaps = value; EditorUtility.SetDirty(this); } }
        public int                              maxSize                         { get { return _maxSize; } set { UndoEx.record(this); _maxSize = Mathf.Max(2, value); EditorUtility.SetDirty(this); } }
        public BoxObjectSpawnHeightMode         heightMode                      { get { return _heightMode; } set { UndoEx.record(this); _heightMode = value; EditorUtility.SetDirty(this); } }
        public int                              defaultHeight                   { get { return _defaultHeight; } set { UndoEx.record(this); _defaultHeight = value; EditorUtility.SetDirty(this); } }
        public int                              heightRaiseAmount               { get { return _heightRaiseAmount; } set { UndoEx.record(this); _heightRaiseAmount = Mathf.Max(1, value); EditorUtility.SetDirty(this); } }
        public int                              heightLowerAmount               { get { return _heightLowerAmount; } set { UndoEx.record(this); _heightLowerAmount = Mathf.Max(1, value); EditorUtility.SetDirty(this); } }
        public int                              minRandomHeight                 { get { return _minRandomHeight; } set { UndoEx.record(this); _minRandomHeight = Mathf.Min(value, _maxRandomHeight); EditorUtility.SetDirty(this); } }
        public int                              maxRandomHeight                 { get { return _maxRandomHeight; } set { UndoEx.record(this); _maxRandomHeight = Mathf.Max(value, _minRandomHeight); EditorUtility.SetDirty(this); } }
        public IntPatternWrapMode               heightPatternWrapMode           { get { return _heightPatternWrapMode; } set { UndoEx.record(this); _heightPatternWrapMode = value; EditorUtility.SetDirty(this); } }
        public IntPattern                       heightPattern
        {
            get { if (_heightPattern == null) _heightPattern = defaultHeightPattern; return _heightPattern; }
            set { UndoEx.record(this); _heightPattern = value; EditorUtility.SetDirty(this); }
        }
        public bool                             constrainSizeToHeightPattern    { get { return _constrainSizeToHeightPattern; } set { UndoEx.record(this); _constrainSizeToHeightPattern = value; EditorUtility.SetDirty(this); } }
        public BoxObjectSpawnFillMode           fillMode                        { get { return _fillMode; } set { UndoEx.record(this); _fillMode = value; EditorUtility.SetDirty(this); } }
        public int                              borderWidth                     { get { return _borderWidth; } set { UndoEx.record(this); _borderWidth = Math.Max(1, value); EditorUtility.SetDirty(this); } }
        public BoxObjectSpawnPrefabPickMode     prefabPickMode                  { get { return _prefabPickMode; } set { UndoEx.record(this); _prefabPickMode = value; EditorUtility.SetDirty(this); } }
        public RandomPrefabProfile              randomPrefabProfile
        {
            get
            {
                var profile = RandomPrefabProfileDb.instance.findProfile(_randomPrefabProfileName);
                if (profile == null) profile = RandomPrefabProfileDb.instance.defaultProfile;
                return profile;
            }
        }
        public IntRangePrefabProfile            heightRangePrefabProfile
        {
            get
            {
                var profile = IntRangePrefabProfileDb.instance.findProfile(_heightRangePrefabProfileName);
                if (profile == null) profile = IntRangePrefabProfileDb.instance.defaultProfile;
                return profile;
            }
        }
        public BoxObjectSpawnProjectionMode     projectionMode                              { get { return _projectionMode; } set { UndoEx.record(this); _projectionMode = value; EditorUtility.SetDirty(this); } }

        public static BoxObjectSpawnCellMode        defaultCellMode                         { get { return BoxObjectSpawnCellMode.SpawnGuide; } }
        public static bool                          defaultUseSceneGridCellSize             { get { return true; } }
        public static Vector3                       defaultGridCellSize                     { get { return Vector3.one; } }
        public static BoxObjectSpawnCornerMode      defaultCornerMode                       { get { return BoxObjectSpawnCornerMode.Normal; } }
        public static int                           defaultCornerGapSize                    { get { return 1; } }
        public static float                         defaultObjectSkipChance                 { get { return 0.0f; } }
        public static float                         defaultHorizontalPadding                { get { return 0.0f; } }
        public static float                         defaultVerticalPadding                  { get { return 0.0f; } }
        public static float                         defaultVolumelessObjectSize             { get { return 1.0f; } }
        public static bool                          defaultAvoidOverlaps                    { get { return true; } }
        public static int                           defaultMaxSize                          { get { return 50; } }
        public static BoxObjectSpawnHeightMode      defaultHeightMode                       { get { return BoxObjectSpawnHeightMode.Constant; } }
        public static int                           defaultDefaultHeight                    { get { return 1; } }
        public static int                           defaultHeightRaiseAmount                { get { return 1; } }
        public static int                           defaultHeightLowerAmount                { get { return 1; } }
        public static int                           defaultMinRandomHeight                  { get { return 1; } }
        public static int                           defaultMaxRandomHeight                  { get { return 5; } }
        public static IntPatternWrapMode            defaultHeightPatternWrapMode            { get { return IntPatternWrapMode.Repeat; } }
        public static IntPattern                    defaultHeightPattern                    { get { return IntPatternDb.instance.defaultPattern; } }
        public static bool                          defaultConstrainSizeToHeightPattern     { get { return false; } }
        public static BoxObjectSpawnFillMode        defaultFillMode                         { get { return BoxObjectSpawnFillMode.Solid; } }
        public static int                           defaultBorderWidth                      { get { return 1; } }
        public static BoxObjectSpawnPrefabPickMode  defaultPrefabPickMode                   { get { return BoxObjectSpawnPrefabPickMode.SpawnGuide; } }
        public static string                        defaultRandomPrefabProfileName          { get { return RandomPrefabProfileDb.defaultProfileName; } }
        public static string                        defaultHeightRangePrefabProfileName     { get { return IntRangePrefabProfileDb.defaultProfileName; } }
        public static BoxObjectSpawnProjectionMode  defaultProjectionMode                   { get { return BoxObjectSpawnProjectionMode.None; } }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 150.0f;

            //createCellModeControls(parent, labelWidth);
            createCornerModeControls(parent, labelWidth);
            createMiscControls(parent, labelWidth);
            UI.createRowSeparator(parent);
            createHeightModeControls(parent, labelWidth);
            UI.createRowSeparator(parent);
            createFillModeControls(parent, labelWidth);
            UI.createRowSeparator(parent);
            createPrefabPickModeControls(parent, labelWidth);
            UI.createRowSeparator(parent);
            createProjectionModeControls(parent, labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public void useDefaults()
        {
            cellMode                        = defaultCellMode;
            useSceneGridCellSize            = defaultUseSceneGridCellSize;
            gridCellSize                    = defaultGridCellSize;
            cornerMode                      = defaultCornerMode;
            cornerGapSize                   = defaultCornerGapSize;
            objectSkipChance                = defaultObjectSkipChance;
            horizontalPadding               = defaultHorizontalPadding;
            verticalPadding                 = defaultVerticalPadding;
            volumlessObjectSize             = defaultVolumelessObjectSize;
            avoidOverlaps                   = defaultAvoidOverlaps;
            maxSize                         = defaultMaxSize;
            heightMode                      = defaultHeightMode;
            defaultHeight                   = defaultDefaultHeight;
            heightRaiseAmount               = defaultHeightRaiseAmount;
            heightLowerAmount               = defaultHeightLowerAmount;
            minRandomHeight                 = defaultMinRandomHeight;
            maxRandomHeight                 = defaultMaxRandomHeight;
            heightPatternWrapMode           = defaultHeightPatternWrapMode;
            heightPattern                   = defaultHeightPattern;
            constrainSizeToHeightPattern    = defaultConstrainSizeToHeightPattern;
            fillMode                        = defaultFillMode;
            borderWidth                     = defaultBorderWidth;
            prefabPickMode                  = defaultPrefabPickMode;
            _randomPrefabProfileName        = defaultRandomPrefabProfileName;
            _heightRangePrefabProfileName   = defaultHeightRangePrefabProfileName;
            projectionMode                  = defaultProjectionMode;

            EditorUtility.SetDirty(this);
        }

        /*private void createCellModeControls(VisualElement parent, float labelWidth)
        {
            Vector3Field gridCellSizeField      = null;
            Toggle useSceneGridCellSizeField    = null;
            var cellModeField = UI.createEnumField(typeof(BoxObjectSpawnCellMode), "_cellMode", serializedObject,
                "Cell mode", "Allows you to specify the way in which the box cells are calculated.", parent);
            cellModeField.setChildLabelWidth(labelWidth);
            cellModeField.RegisterValueChangedCallback(p => 
            {
                gridCellSizeField.setDisplayVisible(cellMode == BoxObjectSpawnCellMode.Grid);
                useSceneGridCellSizeField.setDisplayVisible(cellMode == BoxObjectSpawnCellMode.Grid);
            });

            gridCellSizeField = UI.createVector3Field("_gridCellSize", serializedObject, "Grid cell size",
                "When the cell mode is set to Grid, this field allows you to specify the grid cell size.", 
                Vector3Ex.create(DefaultSystemValues.minGridCellSize), parent);
            gridCellSizeField.setChildLabelWidth(labelWidth);
            gridCellSizeField.setDisplayVisible(cellMode == BoxObjectSpawnCellMode.Grid);

            useSceneGridCellSizeField = UI.createToggle("_useSceneGridCellSize", serializedObject, "Use scene grid cell size",
                "If checked and the cell mode is set to Grid, the plugin will use the cell size of the scene grid. ", parent);
            useSceneGridCellSizeField.setChildLabelWidth(labelWidth);
            useSceneGridCellSizeField.setDisplayVisible(cellMode == BoxObjectSpawnCellMode.Grid);
        }*/

        private void createCornerModeControls(VisualElement parent, float labelWidth)
        {
            VisualElement gapCornerModeContainer = new VisualElement();

            var cornerModeField = UI.createEnumField(typeof(BoxObjectSpawnCornerMode), "_cornerMode", serializedObject,
                "Corner mode", "Allows you to select the box corner mode.", parent);
            cornerModeField.setChildLabelWidth(labelWidth);
            cornerModeField.RegisterValueChangedCallback((p) =>
            {
                gapCornerModeContainer.setDisplayVisible(cornerMode == BoxObjectSpawnCornerMode.Gap);
            });
            gapCornerModeContainer.setDisplayVisible(cornerMode == BoxObjectSpawnCornerMode.Gap);
            parent.Add(gapCornerModeContainer);

            var ctrl = UI.createIntegerField("_cornerGapSize", serializedObject, "Gap size", "The corner gap size.", 1, gapCornerModeContainer);
            ctrl.setChildLabelWidth(labelWidth);
        }

        private void createMiscControls(VisualElement parent, float labelWidth)
        {
            VisualElement ctrl = UI.createFloatField("_objectSkipChance", serializedObject, "Object skip chance", "Specifies the probability of an object being skipped during the spawn process.", 0.0f, 1.0f, parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createFloatField("_horizontalPadding", serializedObject, "Horizontal padding", "The amount of padding applied horizontally (i.e. along the box extension plane).", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createFloatField("_verticalPadding", serializedObject, "Vertical padding", "The amount of padding applied vertically (i.e. perpendicular to the box extension plane).", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createFloatField("_volumelessObjectSize", serializedObject, "Volumeless object size", "The size that should be used for objects that don't have a volume.", 1e-1f, parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_avoidOverlaps", serializedObject, "Avoid overlaps",
                                "If this is checked, no objects will be created in places where they would overlap with already existing objects.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createIntegerField("_maxSize", serializedObject, "Max size", "The maximum box width and/or depth. Useful to prevent " +
                "the box from getting too large for certain camera angles.", 2, parent);
            ctrl.setChildLabelWidth(labelWidth);
        }

        private void createHeightModeControls(VisualElement parent, float labelWidth)
        {
            VisualElement randomHeightModeContainer     = new VisualElement();
            VisualElement patternHeightModeContainer    = new VisualElement();

            var heightModeField = UI.createEnumField(typeof(BoxObjectSpawnHeightMode), "_heightMode", serializedObject, "Height mode", "Allows you to specify the way in which the box height is updated.", parent);
            heightModeField.setChildLabelWidth(labelWidth);
            heightModeField.RegisterValueChangedCallback((p) =>
            {
                randomHeightModeContainer.setDisplayVisible(heightMode == BoxObjectSpawnHeightMode.Random);
                patternHeightModeContainer.setDisplayVisible(heightMode == BoxObjectSpawnHeightMode.Pattern);
            });

            VisualElement ctrl = UI.createIntegerField("_defaultHeight", serializedObject, "Default height", "The default height that is assigned to the box when box building starts. " + 
                "For height modes other than Constant, this acts as an offset/base height.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createIntegerField("_heightRaiseAmount", serializedObject, "Raise amount", "Specifies how much the box is raised when using the scroll wheel.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createIntegerField("_heightLowerAmount", serializedObject, "Lower amount", "Specifies how much the box is lowered when using the scroll wheel.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            parent.Add(randomHeightModeContainer);
            parent.Add(patternHeightModeContainer);

            randomHeightModeContainer.setDisplayVisible(heightMode == BoxObjectSpawnHeightMode.Random);
            patternHeightModeContainer.setDisplayVisible(heightMode == BoxObjectSpawnHeightMode.Pattern);

            var minRandomHeightField = UI.createIntegerField("_minRandomHeight", serializedObject, "Min height", "The minimum random height.", randomHeightModeContainer);
            minRandomHeightField.setChildLabelWidth(labelWidth);

            var maxRandomHeightField = UI.createIntegerField("_maxRandomHeight", serializedObject, "Max height", "The maximum random height.", randomHeightModeContainer);
            maxRandomHeightField.setChildLabelWidth(labelWidth);

            minRandomHeightField.bindMaxValueProperty("_minRandomHeight", "_maxRandomHeight", serializedObject);
            maxRandomHeightField.bindMinValueProperty("_maxRandomHeight", "_minRandomHeight", serializedObject);

            ctrl = UI.createEnumField(typeof(IntPatternWrapMode), "_heightPatternWrapMode", serializedObject, "Wrap mode", "The wrap mode determines how the pattern is sampled outside the bounds of its value array.", patternHeightModeContainer);
            ctrl.setChildLabelWidth(labelWidth);

            IMGUIContainer imGUIContainer = UI.createIMGUIContainer(patternHeightModeContainer);
            imGUIContainer.onGUIHandler = () =>
            {
                IntPattern selectedPattern = EditorUIEx.intPatternSelectionField("Pattern", labelWidth, _heightPattern);
                if (selectedPattern != _heightPattern)
                {
                    UndoEx.record(this);
                    _heightPattern = selectedPattern;
                    EditorUtility.SetDirty(this);
                }
            };

            ctrl = UI.createToggle("_constrainSizeToHeightPattern", serializedObject, "Constrain size to pattern",
                "If this is checked, the box size will be clamped to the number of values in the selected height pattern.", patternHeightModeContainer);
            ctrl.setChildLabelWidth(labelWidth);
        }

        private void createFillModeControls(VisualElement parent, float labelWidth)
        {
            VisualElement borderFillModeContainer = new VisualElement();

            EnumField fillModeField = UI.createEnumField(typeof(BoxObjectSpawnFillMode), "_fillMode", serializedObject, "Fill mode", "Allows you to specify the box fill mode.", parent);
            fillModeField.setChildLabelWidth(labelWidth);
            fillModeField.RegisterValueChangedCallback(p =>
            {
                borderFillModeContainer.setDisplayVisible(fillMode == BoxObjectSpawnFillMode.Border);
            });
            parent.Add(borderFillModeContainer);
            borderFillModeContainer.setDisplayVisible(fillMode == BoxObjectSpawnFillMode.Border);

            var ctrl = UI.createIntegerField("_borderWidth", serializedObject, "Border width", "The box border width.", 1, borderFillModeContainer);
            ctrl.setChildLabelWidth(labelWidth);
        }

        private void createPrefabPickModeControls(VisualElement parent, float labelWidth)
        {
            VisualElement randomPrefabProfilePickModeContainer  = new VisualElement();
            VisualElement irPrefabProfilePickModeContainer      = new VisualElement();

            EnumField pickModeField = UI.createEnumField(typeof(BoxObjectSpawnPrefabPickMode), "_prefabPickMode", serializedObject, "Prefab pick mode", "Allows you to specify the prefabs that will be used to spawn objects.", parent);
            pickModeField.setChildLabelWidth(labelWidth);
            pickModeField.RegisterValueChangedCallback(p =>
            {
                randomPrefabProfilePickModeContainer.setDisplayVisible(prefabPickMode == BoxObjectSpawnPrefabPickMode.Random);
                irPrefabProfilePickModeContainer.setDisplayVisible(prefabPickMode == BoxObjectSpawnPrefabPickMode.HeightRange);
            });

            parent.Add(randomPrefabProfilePickModeContainer);
            parent.Add(irPrefabProfilePickModeContainer);
            randomPrefabProfilePickModeContainer.setDisplayVisible(prefabPickMode == BoxObjectSpawnPrefabPickMode.Random);
            irPrefabProfilePickModeContainer.setDisplayVisible(prefabPickMode == BoxObjectSpawnPrefabPickMode.HeightRange);

            IMGUIContainer imGUIContainer = UI.createIMGUIContainer(randomPrefabProfilePickModeContainer);
            imGUIContainer.onGUIHandler = () =>
            {
                string newName = EditorUIEx.profileNameSelectionField<RandomPrefabProfileDb, RandomPrefabProfile>
                    (RandomPrefabProfileDb.instance, "Random prefab profile", labelWidth, _randomPrefabProfileName);
                if (newName != _randomPrefabProfileName)
                {
                    UndoEx.record(this);
                    _randomPrefabProfileName = newName;
                    EditorUtility.SetDirty(this);
                }
            };

            imGUIContainer = UI.createIMGUIContainer(irPrefabProfilePickModeContainer);
            imGUIContainer.onGUIHandler = () =>
            {
                string newName = EditorUIEx.profileNameSelectionField<IntRangePrefabProfileDb, IntRangePrefabProfile>
                    (IntRangePrefabProfileDb.instance, "Int range prefab profile", labelWidth, _heightRangePrefabProfileName);
                if (newName != _heightRangePrefabProfileName)
                {
                    UndoEx.record(this);
                    _heightRangePrefabProfileName = newName;
                    EditorUtility.SetDirty(this);
                }
            };
        }

        private void createProjectionModeControls(VisualElement parent, float labelWidth)
        {
            VisualElement ctrl = UI.createEnumField(typeof(BoxObjectSpawnProjectionMode), "_projectionMode", serializedObject, "Projection mode", "Allows you to specify how the spawned objects will be projected in the scene.", parent);
            ctrl.setChildLabelWidth(labelWidth);
        }
    }
}
#endif