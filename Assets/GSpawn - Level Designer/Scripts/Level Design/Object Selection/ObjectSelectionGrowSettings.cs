#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public enum ObjectSelectionGrowRotationConstraintMode
    {
        None = 0,
        Exact,
        Flexible
    }

    public class ObjectSelectionGrowSettings : PluginSettings<ObjectSelectionGrowSettings>
    {
        private const float _minDistanceThreshold                                   = 0.0005f;
        private const float _minAngleThreshold                                      = 1e-5f;
        private const float _maxAngleThreshold                                      = 90.0f;
        private const float _minPositionThreshold                                   = 1e-5f;

        [SerializeField]
        private float       _distanceThreshold                                      = defaultDistanceThreshold;

        [SerializeField]
        private bool        _xPositionConstraint                                    = defaultXPositionConstraint;
        [SerializeField]
        private bool        _useXPositionThreshold                                  = defaultUseXPositionThreshold;
        [SerializeField]
        private float       _xPositionThreshold                                     = defaultXPositionThreshold;
        [SerializeField]
        private bool        _growRight                                              = defaultGrowRight;
        [SerializeField]
        private bool        _growLeft                                               = defaultGrowLeft;

        [SerializeField]
        private bool        _yPositionConstraint                                    = defaultYPositionConstraint;
        [SerializeField]
        private bool        _useYPositionThreshold                                  = defaultUseYPositionThreshold;
        [SerializeField]
        private float       _yPositionThreshold                                     = defaultYPositionThreshold;
        [SerializeField]
        private bool        _growUp                                                 = defaultGrowUp;
        [SerializeField]
        private bool        _growDown                                               = defaultGrowDown;

        [SerializeField]
        private bool        _zPositionConstraint                                    = defaultZPositionConstraint;
        [SerializeField]
        private bool        _useZPositionThreshold                                  = defaultUseZPositionThreshold;
        [SerializeField]
        private float       _zPositionThreshold                                     = defaultZPositionThreshold;
        [SerializeField]
        private bool        _growForward                                            = defaultGrowForward;
        [SerializeField]
        private bool        _growBackward                                           = defaultGrowBackward;

        [SerializeField]
        private bool        _prefabConstraint                                       = defaultPrefabConstraint;
        [SerializeField]
        private ObjectSelectionGrowRotationConstraintMode _rotationConstraintMode   = defaultRotationConstraintMode;
        [SerializeField]
        private float       _angleThreshold                                         = defaultAngleThreshold;        
        [SerializeField]
        private bool        _useMaxCountConstraint                                  = defaultUseMaxCountConstraint;
        [SerializeField]
        private int         _maxCount                                               = defaultMaxCount;
        [SerializeField]
        private bool        _ignoreOutOfView                                        = defaultIgnoreOutOfView;

        public float        distanceThreshold                                       { get { return _distanceThreshold; } set { UndoEx.record(this); _distanceThreshold = Mathf.Max(value, _minDistanceThreshold); EditorUtility.SetDirty(this); } }
        
        public bool         xPositionConstraint                                     { get { return _xPositionConstraint; } set { UndoEx.record(this); _xPositionConstraint = value; EditorUtility.SetDirty(this); } }
        public float        xPositionThreshold                                      { get { return _xPositionThreshold; } set { UndoEx.record(this); _xPositionThreshold = Mathf.Max(_minPositionThreshold, value); EditorUtility.SetDirty(this); } }
        public bool         useXPositionThreshold                                   { get { return _useXPositionThreshold; } set { UndoEx.record(this); _useXPositionThreshold = value; EditorUtility.SetDirty(this); } }
        public bool         growRight                                               { get { return _growRight; } set { UndoEx.record(this); _growRight = value; EditorUtility.SetDirty(this); } }
        public bool         growLeft                                                { get { return _growLeft; } set { UndoEx.record(this); _growLeft = value; EditorUtility.SetDirty(this); } }

        public bool         yPositionConstraint                                     { get { return _yPositionConstraint; } set { UndoEx.record(this); _yPositionConstraint = value; EditorUtility.SetDirty(this); } }
        public bool         useYPositionThreshold                                   { get { return _useYPositionThreshold; } set { UndoEx.record(this); _useYPositionThreshold = value; EditorUtility.SetDirty(this); } }
        public float        yPositionThreshold                                      { get { return _yPositionThreshold; } set { UndoEx.record(this); _yPositionThreshold = Mathf.Max(_minPositionThreshold, value); EditorUtility.SetDirty(this); } }
        public bool         growUp                                                  { get { return _growUp; } set { UndoEx.record(this); _growUp = value; EditorUtility.SetDirty(this); } }
        public bool         growDown                                                { get { return _growDown; } set { UndoEx.record(this); _growDown = value; EditorUtility.SetDirty(this); } }
       
        public bool         zPositionConstraint                                     { get { return _zPositionConstraint; } set { UndoEx.record(this); _zPositionConstraint = value; EditorUtility.SetDirty(this); } }
        public bool         useZPositionThreshold                                   { get { return _useZPositionThreshold; } set { UndoEx.record(this); _useZPositionThreshold = value; EditorUtility.SetDirty(this); } }
        public float        zPositionThreshold                                      { get { return _zPositionThreshold; } set { UndoEx.record(this); _zPositionThreshold = Mathf.Max(_minPositionThreshold, value); EditorUtility.SetDirty(this); } }
        public bool         growForward                                             { get { return _growForward; } set { UndoEx.record(this); _growForward = value; EditorUtility.SetDirty(this); } }
        public bool         growBackward                                            { get { return _growBackward; } set { UndoEx.record(this); _growBackward = value; EditorUtility.SetDirty(this); } }

        public bool         usePrefabConstraint                                     { get { return _prefabConstraint; } set { UndoEx.record(this); _prefabConstraint = value; EditorUtility.SetDirty(this); } }
        public ObjectSelectionGrowRotationConstraintMode rotationConstraintMode     { get { return _rotationConstraintMode; } set { UndoEx.record(this); _rotationConstraintMode = value; EditorUtility.SetDirty(this); } }
        public float        angleThreshold                                          { get { return _angleThreshold; } set { UndoEx.record(this); _angleThreshold = Mathf.Clamp(value, _minAngleThreshold, _maxAngleThreshold); } }
        public bool         useMaxCountConstraint                                   { get { return _useMaxCountConstraint; } set { UndoEx.record(this); _useMaxCountConstraint = value; EditorUtility.SetDirty(this); } }
        public int          maxCount                                                { get { return _maxCount; } set { UndoEx.record(this); _maxCount = Mathf.Max(value, 1); EditorUtility.SetDirty(this); } }
        public bool         ignoreOutOfView                                         { get { return _ignoreOutOfView; } set { UndoEx.record(this); _ignoreOutOfView = value; EditorUtility.SetDirty(this); } }

        public static float defaultDistanceThreshold                                { get { return _minDistanceThreshold; } }

        public static bool  defaultXPositionConstraint                              { get { return false; } }
        public static bool  defaultUseXPositionThreshold                            { get { return false; } }
        public static float defaultXPositionThreshold                               { get { return 0.05f; } }
        public static bool  defaultGrowRight                                        { get { return true; } }
        public static bool  defaultGrowLeft                                         { get { return true; } }

        public static bool  defaultYPositionConstraint                              { get { return false; } }
        public static bool  defaultUseYPositionThreshold                            { get { return false; } }
        public static float defaultYPositionThreshold                               { get { return 0.05f; } }
        public static bool  defaultGrowUp                                           { get { return true; } }
        public static bool  defaultGrowDown                                         { get { return true; } }

        public static bool  defaultZPositionConstraint                              { get { return false; } }
        public static bool  defaultUseZPositionThreshold                            { get { return false; } }
        public static float defaultZPositionThreshold                               { get { return 0.05f; } }
        public static bool  defaultGrowForward                                      { get { return true; } }
        public static bool  defaultGrowBackward                                     { get { return true; } }

        public static bool  defaultPrefabConstraint                                 { get { return false; } }
        public static ObjectSelectionGrowRotationConstraintMode defaultRotationConstraintMode   { get { return ObjectSelectionGrowRotationConstraintMode.None; } }
        public static float defaultAngleThreshold                                   { get { return _minAngleThreshold; } }            
        public static bool  defaultUseMaxCountConstraint                            { get { return false; } }
        public static int   defaultMaxCount                                         { get { return 50; } }
        public static bool  defaultIgnoreOutOfView                                  { get { return false; } }

        public void buildUI(VisualElement parent)
        {
            const float labelSize   = 155.0f;

            var floatField = UI.createFloatField("_distanceThreshold", serializedObject, "Distance threshold", 
                "The maximum allowed distance between objects.", _minDistanceThreshold, parent);
            floatField.setChildLabelWidth(labelSize);

            var toggle = UI.createToggle("_prefabConstraint", serializedObject, "Prefab constraint", 
                "If checked, the grow operation will only include objects that belong to prefabs that reside in the original selection.", parent);
            toggle.setChildLabelWidth(labelSize);
          
            // X/Y/Z position constraints
            UI.createRowSeparator(parent);
            createPositionConstraintControls(0, labelSize, parent);
            createPositionConstraintControls(1, labelSize, parent);
            createPositionConstraintControls(2, labelSize, parent);

            UI.createRowSeparator(parent);
            FloatField angleThresholdField = null;
            var rotationConstraintModeField = UI.createEnumField(typeof(ObjectSelectionGrowRotationConstraintMode), "_rotationConstraintMode", serializedObject, "Rotation constraint mode",
                "The type of rotation constraint to be applied.", parent);
            rotationConstraintModeField.setChildLabelWidth(labelSize);
            rotationConstraintModeField.RegisterValueChangedCallback(p => 
            {
                angleThresholdField.setDisplayVisible(rotationConstraintMode == ObjectSelectionGrowRotationConstraintMode.Flexible);
            });

            angleThresholdField = UI.createFloatField("_angleThreshold", serializedObject, "Angle threshold", 
                "Allows you to specify an angle threshold value in degrees used when checking alignment.", _minAngleThreshold, _maxAngleThreshold, parent);
            angleThresholdField.setChildLabelWidth(labelSize);
            angleThresholdField.setDisplayVisible(rotationConstraintMode == ObjectSelectionGrowRotationConstraintMode.Flexible);

            IntegerField maxCountField = null;
            toggle = UI.createToggle("_useMaxCountConstraint", serializedObject, "Max count constraint",
                "If checked, the grow operation will stop when a maximum number of objects have been selected.", parent);
            toggle.setChildLabelWidth(labelSize);
            toggle.RegisterValueChangedCallback(p => 
            {
                maxCountField.setDisplayVisible(useMaxCountConstraint);
            });

            maxCountField = UI.createIntegerField("_maxCount", serializedObject, "Max count", 
                "The maximum number of objects that can be selected when the max count constraint is enabled.", 1, parent);
            maxCountField.setChildLabelWidth(labelSize);
            maxCountField.setDisplayVisible(useMaxCountConstraint);

            toggle = UI.createToggle("_ignoreOutOfView", serializedObject, "Ignore out of view",
                "If checked, the grow operation will not include objects that are culled from the perspective of the scene camera.", parent);
            toggle.setChildLabelWidth(labelSize);

            UI.createUseDefaultsButton(() => { useDefaults(); }, parent);
        }

        public override void useDefaults()
        {
            distanceThreshold           = defaultDistanceThreshold;

            xPositionConstraint         = defaultXPositionConstraint;
            useXPositionThreshold       = defaultUseXPositionThreshold;
            xPositionThreshold          = defaultXPositionThreshold;
            growRight                   = defaultGrowRight;
            growLeft                    = defaultGrowLeft;

            yPositionConstraint         = defaultYPositionConstraint;
            useYPositionThreshold       = defaultUseYPositionThreshold;
            yPositionThreshold          = defaultYPositionThreshold;
            growUp                      = defaultGrowUp;
            growDown                    = defaultGrowDown;

            zPositionConstraint         = defaultZPositionConstraint;
            useZPositionThreshold       = defaultUseZPositionThreshold;
            zPositionThreshold          = defaultZPositionThreshold;
            growForward                 = defaultGrowForward;
            growBackward                = defaultGrowBackward;

            usePrefabConstraint         = defaultPrefabConstraint;
            rotationConstraintMode      = defaultRotationConstraintMode;
            useMaxCountConstraint       = defaultUseMaxCountConstraint;
            maxCount                    = defaultMaxCount;
            ignoreOutOfView             = defaultIgnoreOutOfView;

            EditorUtility.SetDirty(this);
        }

        private static string[] _posConstr_PropertyNames        = new string[] { "_xPositionConstraint", "_yPositionConstraint", "_zPositionConstraint" };
        private static string[] _usePosThreshold_PropertyNames  = new string[] { "_useXPositionThreshold", "_useYPositionThreshold", "_useZPositionThreshold" };
        private static string[] _posThreshold_PropertyNames     = new string[] { "_xPositionThreshold", "_yPositionThreshold", "_zPositionThreshold" };
        private void createPositionConstraintControls(int axis, float labelSize, VisualElement parent)
        {
            const float indent  = 20.0f;
            string axisName     = StringEx.axisIndexToAxisName(axis);
            string label        = axisName + " constraint";
            string tooltip      = string.Format("If checked, the grow operation will filter objects based on their {0} coordinates using different criteria.", axisName);
            var posConstrToggle = UI.createToggle(_posConstr_PropertyNames[axis], serializedObject, label, tooltip, parent);
            posConstrToggle.setChildLabelWidth(labelSize);

            var ctrlContainer = new VisualElement();
            parent.Add(ctrlContainer);

            var thresholdContainer                  = new VisualElement();
            ctrlContainer.Add(thresholdContainer);
            thresholdContainer.style.flexDirection  = FlexDirection.Row;
            thresholdContainer.style.marginLeft     = indent;
            var thresholdToggle                     = UI.createToggle(_usePosThreshold_PropertyNames[axis], serializedObject, "", string.Format("Toggle {0} position threshold.", axisName), thresholdContainer);
            thresholdToggle.style.marginRight       = 0.0f;
            thresholdToggle.style.flexGrow          = 0.0f;
            tooltip = string.Format("Specifies how much the object {0} coordinates can differ along the {0} axis relative to the original selection.", axisName);
            var thresholdField                      = UI.createFloatField(_posThreshold_PropertyNames[axis], serializedObject, 
                string.Format("{0} threshold", axisName), tooltip, _minPositionThreshold, thresholdContainer);
            thresholdField.setChildLabelWidth(labelSize - indent);

            string growPstv_PropertyName    = string.Empty;
            string growNgtv_PropertyName    = string.Empty;
            string growPstv_Label           = string.Empty;
            string growNgtv_Label           = string.Empty;
            string growPstv_Dir             = string.Empty;
            string growNgtv_Dir             = string.Empty;
            if (axis == 0)
            {
                growPstv_PropertyName   = "_growRight";
                growNgtv_PropertyName   = "_growLeft";
                growPstv_Label          = "Grow right";
                growNgtv_Label          = "Grow left";
                growPstv_Dir            = "rightward";
                growNgtv_Dir            = "leftward";
            }
            else 
            if (axis == 1)
            {
                growPstv_PropertyName   = "_growUp";
                growNgtv_PropertyName   = "_growDown";
                growPstv_Label          = "Grow up";
                growNgtv_Label          = "Grow down";
                growPstv_Dir            = "upward";
                growNgtv_Dir            = "downward";
            }
            else
            if (axis == 2)
            {
                growPstv_PropertyName   = "_growForward";
                growNgtv_PropertyName   = "_growBackward";
                growPstv_Label          = "Grow forward";
                growNgtv_Label          = "Grow back";
                growPstv_Dir            = "forward";
                growNgtv_Dir            = "backward";
            }

            tooltip = string.Format("");
            var growPstvField = UI.createToggle(growPstv_PropertyName, serializedObject, growPstv_Label, 
                string.Format("If checked, the grow operation can extend {0} relative to the original selection.", growPstv_Dir), ctrlContainer);
            growPstvField.setChildLabelWidth(labelSize);
            growPstvField.style.marginLeft = indent;
            var growNgtvField = UI.createToggle(growNgtv_PropertyName, serializedObject, growNgtv_Label, 
                string.Format("If checked, the grow operation can extend {0} relative to the original selection.", growNgtv_Dir), ctrlContainer);
            growNgtvField.setChildLabelWidth(labelSize);
            growNgtvField.style.marginLeft = indent;
            posConstrToggle.RegisterValueChangedCallback(p => { ctrlContainer.setDisplayVisible(p.newValue);});
            ctrlContainer.setDisplayVisible(posConstrToggle.value);
        }
    }
}
#endif