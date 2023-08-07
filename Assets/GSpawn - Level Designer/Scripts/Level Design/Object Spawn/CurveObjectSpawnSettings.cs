#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public struct CurveObjectSpawnSettingsDiff
    {
        public bool curvePrefabProfileName;
        public bool curveUpAxis;
        public bool invertUpAxis;
        public bool avoidOverlaps;
        public bool volumelessObjectSize;
        public bool step;
        public bool objectSkipChance;
        public bool paddingMode;
        public bool padding;
        public bool minRandomPadding;
        public bool maxRandomPadding;
        public bool tryFixOverlap;
        public bool laneMode;
        public bool numLanes;
        public bool minRandomNumLanes;
        public bool maxRandomNumLanes;
        public bool lanePaddingMode;
        public bool lanePadding;
        public bool minRandomLanePadding;
        public bool maxRandomLanePadding;
        public bool projectionMode;
    }

    public enum CurveObjectSpawnUpAxis
    {
        X = 0,
        Y,
        Z,

        [Obsolete]
        UIMixed
    }

    public enum CurveObjectSpawnPaddingMode
    {
        Constant = 0,
        Random,

        [Obsolete]
        UIMixed
    }

    public enum CurveObjectSpawnLanePaddingMode
    {
        Constant = 0,
        Random,

        [Obsolete]
        UIMixed
    }

    public enum CurveObjectSpawnProjectionMode
    {
        None = 0,
        Terrains,

        [Obsolete]
        UIMixed
    }

    public enum CurveObjectSpawnLaneMode
    {
        Constant = 0,
        Random,

        [Obsolete]
        UIMixed
    }

    public class CurveObjectSpawnSettings : PluginSettings<CurveObjectSpawnSettings>
    {
        [SerializeField]
        private string                          _curvePrefabProfileName = defaultCurvePrefabProfileName;
        [SerializeField]
        private CurveObjectSpawnUpAxis          _curveUpAxis            = defaultCurveUpAxis;
        [SerializeField]
        private bool                            _invertUpAxis           = defaultInvertUpAxis;
        [SerializeField]
        private bool                            _avoidOverlaps          = defaultAvoidOverlaps;
        [SerializeField]
        private float                           _volumelessObjectSize   = defaultVolumelessObjectSize;
        [SerializeField]
        private float                           _step                   = defaultStep;
        [SerializeField]
        private float                           _objectSkipChance       = defaultObjectSkipChance;
        [SerializeField]
        private CurveObjectSpawnPaddingMode     _paddingMode            = defaultPaddingMode;
        [SerializeField]
        private float                           _padding                = defaultPadding;
        [SerializeField]
        private float                           _minRandomPadding       = defaultMinRandomPadding;
        [SerializeField]
        private float                           _maxRandomPadding       = defaultMaxRandomPadding;
        [SerializeField]
        private bool                            _tryFixOverlap          = defaultTryFixOverlap;

        [SerializeField]
        private CurveObjectSpawnLaneMode        _laneMode               = defaultLaneMode;
        [SerializeField]
        private int                             _numLanes               = defaultNumLanes;
        [SerializeField]
        private int                             _minRandomNumLanes      = defaultMinRandomNumLanes;
        [SerializeField]
        private int                             _maxRandomNumLanes      = defaultMaxRandomNumLanes;
        [SerializeField]
        private CurveObjectSpawnLanePaddingMode _lanePaddingMode        = defaultLanePaddingMode;
        [SerializeField]
        private float                           _lanePadding            = defaultLanePadding;
        [SerializeField]
        private float                           _minRandomLanePadding   = defaultMinRandomLanePadding;
        [SerializeField]
        private float                           _maxRandomLanePadding   = defaultMaxRandomLanePadding;
        [SerializeField]
        private CurveObjectSpawnProjectionMode  _projectionMode         = defaultProjectionMode;

        public string                           curvePrefabProfileName 
        { 
            get { return _curvePrefabProfileName; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;

                UndoEx.record(this); 
                _curvePrefabProfileName = value; 
                EditorUtility.SetDirty(this); 
            } 
        }
        public CurvePrefabProfile               curvePrefabProfile
        {
            get
            {
                var profile = CurvePrefabProfileDb.instance.findProfile(_curvePrefabProfileName);
                if (profile == null) profile = CurvePrefabProfileDb.instance.defaultProfile;
                return profile;
            }
        }
        public CurveObjectSpawnUpAxis           curveUpAxis             { get { return _curveUpAxis; } set { UndoEx.record(this); _curveUpAxis = value; EditorUtility.SetDirty(this); } }
        public bool                             invertUpAxis            { get { return _invertUpAxis; } set { UndoEx.record(this); _invertUpAxis = value; EditorUtility.SetDirty(this); } }
        public bool                             avoidOverlaps           { get { return _avoidOverlaps; } set { UndoEx.record(this); _avoidOverlaps = value; EditorUtility.SetDirty(this); } }
        public float                            volumlessObjectSize     { get { return _volumelessObjectSize; } set { UndoEx.record(this); _volumelessObjectSize = Mathf.Max(1e-1f, value); EditorUtility.SetDirty(this); } }
        public float                            step                    { get { return _step; } set { UndoEx.record(this); _step = Math.Max(0.01f, value); EditorUtility.SetDirty(this); } }
        public float                            objectSkipChance        { get { return _objectSkipChance; } set { UndoEx.record(this); _objectSkipChance = Mathf.Clamp01(value); EditorUtility.SetDirty(this); } }
        public CurveObjectSpawnPaddingMode      paddingMode             { get { return _paddingMode; } set { UndoEx.record(this); _paddingMode = value; EditorUtility.SetDirty(this); } }
        public float                            padding                 { get { return _padding; } set { UndoEx.record(this); _padding = value; EditorUtility.SetDirty(this); } }
        public float                            minRandomPadding 
        { 
            get { return _minRandomPadding; } 
            set 
            { 
                UndoEx.record(this); 
                _minRandomPadding = value;
                if (_maxRandomPadding < _minRandomPadding)
                    _maxRandomPadding = _minRandomPadding;
                EditorUtility.SetDirty(this); 
            } 
        }
        public float                            maxRandomPadding 
        { 
            get { return _maxRandomPadding; } 
            set 
            { 
                UndoEx.record(this); 
                _maxRandomPadding = value;
                if (_minRandomPadding > _maxRandomPadding)
                    _minRandomPadding = _maxRandomPadding;
                EditorUtility.SetDirty(this); 
            } 
        }
        public bool                             tryFixOverlap           { get { return _tryFixOverlap; } set { UndoEx.record(this); _tryFixOverlap = value; EditorUtility.SetDirty(this); } }
        public CurveObjectSpawnLaneMode         laneMode                { get { return _laneMode; } set { UndoEx.record(this); _laneMode = value; EditorUtility.SetDirty(this); } }
        public int                              numLanes                { get { return _numLanes; } set { UndoEx.record(this); _numLanes = Mathf.Max(1, value); EditorUtility.SetDirty(this); } }
        public int                              minRandomNumLanes
        {
            get { return _minRandomNumLanes; }
            set
            {
                UndoEx.record(this);
                _minRandomNumLanes = Math.Max(1, value);
                if (_maxRandomNumLanes < _minRandomNumLanes)
                    _maxRandomNumLanes = _minRandomNumLanes;
                EditorUtility.SetDirty(this);
            }
        }
        public int                              maxRandomNumLanes
        {
            get { return _maxRandomNumLanes; }
            set
            {
                UndoEx.record(this);
                _maxRandomNumLanes = Math.Max(1, value);
                if (_minRandomNumLanes > _maxRandomNumLanes)
                    _minRandomNumLanes = _maxRandomNumLanes;
                EditorUtility.SetDirty(this);
            }
        }
        public CurveObjectSpawnLanePaddingMode  lanePaddingMode         { get { return _lanePaddingMode; } set { UndoEx.record(this); _lanePaddingMode = value; EditorUtility.SetDirty(this); } }
        public float                            lanePadding             { get { return _lanePadding; } set { UndoEx.record(this); _lanePadding = Math.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public float                            minRandomLanePadding
        {
            get { return _minRandomLanePadding; }
            set
            {
                UndoEx.record(this);
                _minRandomLanePadding = Math.Max(0.0f, value);
                if (_maxRandomLanePadding < _minRandomLanePadding)
                    _maxRandomLanePadding = _minRandomLanePadding;
                EditorUtility.SetDirty(this);
            }
        }
        public float                            maxRandomLanePadding
        {
            get { return _maxRandomLanePadding; }
            set
            {
                UndoEx.record(this);
                _maxRandomLanePadding = Math.Max(0.0f, value);
                if (_minRandomLanePadding > _maxRandomLanePadding)
                    _minRandomLanePadding = _maxRandomLanePadding;
                EditorUtility.SetDirty(this);
            }
        }
        public CurveObjectSpawnProjectionMode   projectionMode          { get { return _projectionMode; } set { UndoEx.record(this); _projectionMode = value; EditorUtility.SetDirty(this); } }

        public static string                            defaultCurvePrefabProfileName   { get { return CurvePrefabProfileDb.defaultProfileName; } }
        public static CurveObjectSpawnUpAxis            defaultCurveUpAxis              { get { return CurveObjectSpawnUpAxis.Y; } }
        public static bool                              defaultInvertUpAxis             { get { return false; } }
        public static bool                              defaultAvoidOverlaps            { get { return false; } }
        public static float                             defaultVolumelessObjectSize     { get { return 1.0f; } }
        public static float                             defaultStep                     { get { return 0.1f; } }
        public static float                             defaultObjectSkipChance         { get { return 0.0f; } }
        public static CurveObjectSpawnPaddingMode       defaultPaddingMode              { get { return CurveObjectSpawnPaddingMode.Constant; } }
        public static float                             defaultPadding                  { get { return 0.0f; } }
        public static float                             defaultMinRandomPadding         { get { return 0.0f; } }
        public static float                             defaultMaxRandomPadding         { get { return 0.5f; } }
        public static bool                              defaultTryFixOverlap            { get { return false; } }
        public static CurveObjectSpawnLaneMode          defaultLaneMode                 { get { return CurveObjectSpawnLaneMode.Constant; } }
        public static int                               defaultNumLanes                 { get { return 1; } }
        public static int                               defaultMinRandomNumLanes        { get { return 1; } }
        public static int                               defaultMaxRandomNumLanes        { get { return 4; } }
        public static CurveObjectSpawnLanePaddingMode   defaultLanePaddingMode          { get { return CurveObjectSpawnLanePaddingMode.Constant; } }
        public static float                             defaultLanePadding              { get { return 0.0f; } }
        public static float                             defaultMinRandomLanePadding     { get { return 0.0f; } }
        public static float                             defaultMaxRandomLanePadding     { get { return 0.5f; } }
        public static CurveObjectSpawnProjectionMode    defaultProjectionMode           { get { return CurveObjectSpawnProjectionMode.None; } }

        public void copy(CurveObjectSpawnSettings src)
        {
            if (this == src) return;

            _curvePrefabProfileName     = src._curvePrefabProfileName;
            curveUpAxis                 = src.curveUpAxis;
            invertUpAxis                = src.invertUpAxis;
            avoidOverlaps               = src.avoidOverlaps;
            volumlessObjectSize         = src.volumlessObjectSize;
            step                        = src.step;
            objectSkipChance            = src.objectSkipChance;
            paddingMode                 = src.paddingMode;
            padding                     = src.padding;
            minRandomPadding            = src.minRandomPadding;
            maxRandomPadding            = src.maxRandomPadding;
            tryFixOverlap               = src.tryFixOverlap;
            laneMode                    = src.laneMode;
            numLanes                    = src.numLanes;
            minRandomNumLanes           = src.minRandomNumLanes;
            maxRandomNumLanes           = src.maxRandomNumLanes;
            lanePaddingMode             = src.lanePaddingMode;
            lanePadding                 = src.lanePadding;
            minRandomLanePadding        = src.minRandomLanePadding;
            maxRandomLanePadding        = src.maxRandomLanePadding;
            projectionMode              = src.projectionMode;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 150.0f;

            IMGUIContainer prefabProfileContainer = UI.createIMGUIContainer(parent);
            prefabProfileContainer.onGUIHandler = () =>
            {
                string newName = EditorUIEx.profileNameSelectionField<CurvePrefabProfileDb, CurvePrefabProfile>
                    (CurvePrefabProfileDb.instance, "Curve prefab profile", labelWidth, _curvePrefabProfileName);
                if (newName != _curvePrefabProfileName)
                {
                    UndoEx.record(this);
                    _curvePrefabProfileName = newName;
                    EditorUtility.SetDirty(this);
                }
            };

            UI.createRowSeparator(parent);
/*

            var curveUpAxisField = UI.createEnumField(typeof(CurveObjectSpawnUpAxis), "_curveUpAxis", serializedObject, "Up axis", "The curve up axis used when spawning objects. It specifies which way is up.", parent);
            curveUpAxisField.setChildLabelWidth(labelWidth);

            var invertUpAxisField = UI.createToggle("_invertUpAxis", serializedObject, "Invert axis", "If this is checked, the up axis will be inverted.", parent);
            invertUpAxisField.setChildLabelWidth(labelWidth);*/

            var avoidOverlapsField = UI.createToggle("_avoidOverlaps", serializedObject, "Avoid overlaps", "If this is checked, no objects will be created in places where they would overlap with already existing objects. " + 
                "Note: The checks are performed against objects that are not part of the curve. Objects that are part of the curve, are not affected and may overlap.", parent);
            avoidOverlapsField.setChildLabelWidth(labelWidth);

            var volumelessObjectSizeField = UI.createFloatField("_volumelessObjectSize", serializedObject, "Volumeless object size",
                 "The size that should be used for objects that don't have a volume.", parent);
            volumelessObjectSizeField.setChildLabelWidth(labelWidth);

/*
            var stepField = UI.createFloatField("_step", serializedObject, "Step", "When spawning objects along a curve, the curve will be approximated " + 
                "by taking sample points using this step value. The smaller the value the better the approximation.", 0.01f, parent);
            stepField.setChildLabelWidth(labelWidth);*/

            var objectSkipChanceField = UI.createFloatField("_objectSkipChance", serializedObject, "Object skip chance", "Specifies the probability of an object being skipped during the spawn process.", parent);
            objectSkipChanceField.setChildLabelWidth(labelWidth);

            UI.createRowSeparator(parent);

            FloatField paddingField = null, minPaddingField = null, maxPaddingField = null;
            var paddingModeField = UI.createEnumField(typeof(CurveObjectSpawnPaddingMode), "_paddingMode", serializedObject, "Padding mode", "Allows you to select the padding mode (i.e. constant or random). " + 
                "Padding represents the distance between successive objects in the curve.", parent);
            paddingModeField.setChildLabelWidth(labelWidth);
            paddingModeField.RegisterValueChangedCallback((p) => 
            {
                paddingField.setDisplayVisible(paddingMode == CurveObjectSpawnPaddingMode.Constant);
                minPaddingField.setDisplayVisible(paddingMode == CurveObjectSpawnPaddingMode.Random);
                maxPaddingField.setDisplayVisible(paddingMode == CurveObjectSpawnPaddingMode.Random);
            });

            paddingField = UI.createFloatField("_padding", serializedObject, "Padding", "The distance between successive objects in the curve.", 0.0f, parent);
            paddingField.setChildLabelWidth(labelWidth);

            minPaddingField = UI.createFloatField("_minRandomPadding", serializedObject, "Min padding", "The minimum random padding.", 0.0f, parent);
            minPaddingField.setChildLabelWidth(labelWidth);

            maxPaddingField = UI.createFloatField("_maxRandomPadding", serializedObject, "Max padding", "The maximum random padding.", 0.0f, parent);
            maxPaddingField.setChildLabelWidth(labelWidth);

            minPaddingField.bindMaxValueProperty("_minRandomPadding", "_maxRandomPadding", serializedObject);
            maxPaddingField.bindMinValueProperty("_maxRandomPadding", "_minRandomPadding", serializedObject);

            paddingField.setDisplayVisible(paddingMode == CurveObjectSpawnPaddingMode.Constant);
            minPaddingField.setDisplayVisible(paddingMode == CurveObjectSpawnPaddingMode.Random);
            maxPaddingField.setDisplayVisible(paddingMode == CurveObjectSpawnPaddingMode.Random);

            UI.createRowSeparator(parent);

            var tryFixOverlapField = UI.createToggle("_tryFixOverlap", serializedObject, "Try fix overlap", 
                "If checked, the plugin will attempt to ensure that curve objects will not overlap. Note: This applies only to the main lane.", parent);
            tryFixOverlapField.setChildLabelWidth(labelWidth);

            UI.createRowSeparator(parent);

            var laneModeField = UI.createEnumField(typeof(CurveObjectSpawnLaneMode), "_laneMode", serializedObject,
                "Lane mode", "Allows you to specify the way in which the number of lanes will be generated.", parent);
            laneModeField.setChildLabelWidth(labelWidth);

            IntegerField numLanesField = null, minRandomNumLanesField = null, maxRandomNumLanesField = null;
            EnumField lanePaddingModeField = null;
            FloatField lanePaddingField = null, minLanePaddingField = null, maxLanePaddingField = null;
            numLanesField = UI.createIntegerField("_numLanes", serializedObject, "Num lanes",
                "The number of lanes running parallel to each other.", 1, parent);
            numLanesField.setChildLabelWidth(labelWidth);

            laneModeField.RegisterValueChangedCallback(p => 
            {
                numLanesField.setDisplayVisible(laneMode == CurveObjectSpawnLaneMode.Constant);
                minRandomNumLanesField.setDisplayVisible(laneMode == CurveObjectSpawnLaneMode.Random);
                maxRandomNumLanesField.setDisplayVisible(laneMode == CurveObjectSpawnLaneMode.Random);
            });

            minRandomNumLanesField = UI.createIntegerField("_minRandomNumLanes", serializedObject, "Min num lanes",
                "The minimum number of lanes running parallel to each other.", 1, parent);
            minRandomNumLanesField.setChildLabelWidth(labelWidth);

            maxRandomNumLanesField = UI.createIntegerField("_maxRandomNumLanes", serializedObject, "Max num lanes",
                "The maximum number of lanes running parallel to each other.", 1, parent);
            maxRandomNumLanesField.setChildLabelWidth(labelWidth);

            numLanesField.setDisplayVisible(laneMode == CurveObjectSpawnLaneMode.Constant);
            minRandomNumLanesField.setDisplayVisible(laneMode == CurveObjectSpawnLaneMode.Random);
            maxRandomNumLanesField.setDisplayVisible(laneMode == CurveObjectSpawnLaneMode.Random);

            lanePaddingModeField = UI.createEnumField(typeof(CurveObjectSpawnLanePaddingMode), "_lanePaddingMode", serializedObject, "Lane padding mode",
                "Allows you to specify the lane padding mode. Lane padding is the distance between successive lanes.", parent);
            lanePaddingModeField.setChildLabelWidth(labelWidth);
            lanePaddingModeField.RegisterValueChangedCallback(p => 
            {
                lanePaddingField.setDisplayVisible(lanePaddingMode == CurveObjectSpawnLanePaddingMode.Constant);
                minLanePaddingField.setDisplayVisible(lanePaddingMode == CurveObjectSpawnLanePaddingMode.Random);
                maxLanePaddingField.setDisplayVisible(lanePaddingMode == CurveObjectSpawnLanePaddingMode.Random);
            });

            lanePaddingField = UI.createFloatField("_lanePadding", serializedObject, "Lane padding", "The distance between successive lanes.", 0.0f, parent);
            lanePaddingField.setChildLabelWidth(labelWidth);

            minLanePaddingField = UI.createFloatField("_minRandomLanePadding", serializedObject, "Min lane padding", "The minimum random lane padding.", 0.0f, parent);
            minLanePaddingField.setChildLabelWidth(labelWidth);

            maxLanePaddingField = UI.createFloatField("_maxRandomLanePadding", serializedObject, "Max lane padding", "The maximum random lane padding.", 0.0f, parent);
            maxLanePaddingField.setChildLabelWidth(labelWidth);

            minLanePaddingField.bindMaxValueProperty("_minRandomLanePadding", "_maxRandomLanePadding", serializedObject);
            maxLanePaddingField.bindMinValueProperty("_maxRandomLanePadding", "_minRandomLanePadding", serializedObject);

            lanePaddingField.setDisplayVisible(lanePaddingMode == CurveObjectSpawnLanePaddingMode.Constant);
            minLanePaddingField.setDisplayVisible(lanePaddingMode == CurveObjectSpawnLanePaddingMode.Random);
            maxLanePaddingField.setDisplayVisible(lanePaddingMode == CurveObjectSpawnLanePaddingMode.Random);

            UI.createRowSeparator(parent);

            var projectionModeField = UI.createEnumField(typeof(CurveObjectSpawnProjectionMode), "_projectionMode", serializedObject,
                "Projection mode", "Allows you to specify how the spawned objects will be projected in the scene.", parent);
            projectionModeField.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {
            _curvePrefabProfileName     = defaultCurvePrefabProfileName;
            curveUpAxis                 = defaultCurveUpAxis;
            invertUpAxis                = defaultInvertUpAxis;
            avoidOverlaps               = defaultAvoidOverlaps;
            volumlessObjectSize         = defaultVolumelessObjectSize;
            step                        = defaultStep;
            objectSkipChance            = defaultObjectSkipChance;
            paddingMode                 = defaultPaddingMode;
            padding                     = defaultPadding;
            minRandomPadding            = defaultMinRandomPadding;
            maxRandomPadding            = defaultMaxRandomPadding;
            tryFixOverlap               = defaultTryFixOverlap;
            laneMode                    = defaultLaneMode;
            numLanes                    = defaultNumLanes;
            minRandomNumLanes           = defaultMinRandomNumLanes;
            maxRandomNumLanes           = defaultMaxRandomNumLanes;
            lanePaddingMode             = defaultLanePaddingMode;
            lanePadding                 = defaultLanePadding;
            minRandomLanePadding        = defaultMinRandomLanePadding;
            maxRandomLanePadding        = defaultMaxRandomLanePadding;
            projectionMode              = defaultProjectionMode;

            EditorUtility.SetDirty(this);
        }

        public static CurveObjectSpawnSettingsDiff checkDiff(List<CurveObjectSpawnSettings> settings)
        {
            int maxNumDiffs                     = typeof(CurveObjectSpawnSettingsDiff).GetFields().Length;
            CurveObjectSpawnSettingsDiff diff   = new CurveObjectSpawnSettingsDiff();
            int numSettings                     = settings.Count;

            for (int i = 0; i < numSettings; ++i)
            {
                var s0 = settings[i];
                for (int j = i + 1; j < numSettings; ++j)
                {
                    var otherSettings = settings[j];
                    int diffCount = 0;

                    if (s0._curvePrefabProfileName != otherSettings._curvePrefabProfileName)
                    {
                        ++diffCount;
                        diff.curvePrefabProfileName = true;
                    }
                    if (s0.curveUpAxis != otherSettings.curveUpAxis)
                    {
                        ++diffCount;
                        diff.curveUpAxis = true;
                    }
                    if (s0.invertUpAxis != otherSettings.invertUpAxis)
                    {
                        ++diffCount;
                        diff.invertUpAxis = true;
                    }
                    if (s0.avoidOverlaps != otherSettings.avoidOverlaps)
                    {
                        ++diffCount;
                        diff.avoidOverlaps = true;
                    }
                    if (s0.volumlessObjectSize != otherSettings.volumlessObjectSize)
                    {
                        ++diffCount;
                        diff.volumelessObjectSize = true;
                    }
                    if (s0.step != otherSettings.step)
                    {
                        ++diffCount;
                        diff.step = true;
                    }
                    if (s0.objectSkipChance != otherSettings.objectSkipChance)
                    {
                        ++diffCount;
                        diff.objectSkipChance = true;
                    }
                    if (s0.paddingMode != otherSettings.paddingMode)
                    {
                        ++diffCount;
                        diff.paddingMode = true;
                    }
                    if (s0.padding != otherSettings.padding)
                    {
                        ++diffCount;
                        diff.padding = true;
                    }
                    if (s0.minRandomPadding != otherSettings.minRandomPadding)
                    {
                        ++diffCount;
                        diff.minRandomPadding = true;
                    }
                    if (s0.maxRandomPadding != otherSettings.maxRandomPadding)
                    {
                        ++diffCount;
                        diff.maxRandomPadding = true;
                    }
                    if (s0.tryFixOverlap != otherSettings.tryFixOverlap)
                    {
                        ++diffCount;
                        diff.tryFixOverlap = true;
                    }
                    if (s0.laneMode != otherSettings.laneMode)
                    {
                        ++diffCount;
                        diff.laneMode = true;
                    }
                    if (s0.numLanes != otherSettings.numLanes)
                    {
                        ++diffCount;
                        diff.numLanes = true;
                    }
                    if (s0.minRandomNumLanes != otherSettings.minRandomNumLanes)
                    {
                        ++diffCount;
                        diff.minRandomNumLanes = true;
                    }
                    if (s0.maxRandomNumLanes != otherSettings.maxRandomNumLanes)
                    {
                        ++diffCount;
                        diff.maxRandomNumLanes = true;
                    }
                    if (s0.lanePaddingMode != otherSettings.lanePaddingMode)
                    {
                        ++diffCount;
                        diff.lanePaddingMode = true;
                    }
                    if (s0.lanePadding != otherSettings.lanePadding)
                    {
                        ++diffCount;
                        diff.lanePadding = true;
                    }
                    if (s0.minRandomLanePadding != otherSettings.minRandomLanePadding)
                    {
                        ++diffCount;
                        diff.minRandomLanePadding = true;
                    }
                    if (s0.maxRandomLanePadding != otherSettings.maxRandomLanePadding)
                    {
                        ++diffCount;
                        diff.maxRandomLanePadding = true;
                    }
                    if (s0.projectionMode != otherSettings.projectionMode)
                    {
                        ++diffCount;
                        diff.projectionMode = true;
                    }

                    if (diffCount == maxNumDiffs) return diff;
                }
            }

            return diff;
        }
    }
}
#endif