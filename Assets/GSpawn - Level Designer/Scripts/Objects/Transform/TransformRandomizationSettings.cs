#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class TransformRandomizationSettings : PluginSettings<TransformRandomizationSettings>
    {
        [SerializeField]
        private bool        _randomizeRotation                          = defaultRandomizeRotation;
        [SerializeField]
        private bool        _randomizeRotationAroundX                   = defaultRandomizeRotationAroundX;
        [SerializeField]
        private bool        _randomizeRotationAroundY                   = defaultRandomizeRotationAroundY;
        [SerializeField]
        private bool        _randomizeRotationAroundZ                   = defaultRandomizeRotationAroundZ;
        [SerializeField]
        private bool        _randomizeRotationAroundSurfaceNormal       = defaultRandomizeRotationAroundSurfaceNormal;

        [SerializeField]
        private RotationRandomizationMode   _rotationRandomizationMode  = defaultRotationRandomizationMode;

        [SerializeField]
        private float       _rotationRandomizationStep                  = defaultRotationRandomizationStepValue;
        [SerializeField]    
        private float       _minRandomRotation                          = defaultMinRandomRotation;
        [SerializeField]
        private float       _maxRandomRotation                          = defaultMaxRandomRotation;
        [SerializeField]
        private bool        _randomizeScale                             = defaultRandomizeScale;
        [SerializeField]
        private float       _minRandomScale                             = defaultMinRandomScale;
        [SerializeField]
        private float       _maxRandomScale                             = defaultMaxRandomScale;

        public bool         randomizeRotation                           { get { return _randomizeRotation; } set { UndoEx.record(this); _randomizeRotation = value; EditorUtility.SetDirty(this); } }
        public bool         randomizeRotationAroundX                    { get { return _randomizeRotationAroundX; } set { UndoEx.record(this); _randomizeRotationAroundX = value; EditorUtility.SetDirty(this); } }
        public bool         randomizeRotationAroundY                    { get { return _randomizeRotationAroundY; } set { UndoEx.record(this); _randomizeRotationAroundY = value; EditorUtility.SetDirty(this); } }
        public bool         randomizeRotationAroundZ                    { get { return _randomizeRotationAroundZ; } set { UndoEx.record(this); _randomizeRotationAroundZ = value; EditorUtility.SetDirty(this); } }
        public bool         randomizeRotationAroundSurfaceNormal        { get { return _randomizeRotationAroundSurfaceNormal; } set { UndoEx.record(this); _randomizeRotationAroundSurfaceNormal = value; EditorUtility.SetDirty(this); } }
        public RotationRandomizationMode rotationRandomizationMode      { get { return _rotationRandomizationMode; } set { UndoEx.record(this); _rotationRandomizationMode = value; EditorUtility.SetDirty(this); } }
        public float        rotationRandomizationStep                   { get { return _rotationRandomizationStep; } set { UndoEx.record(this); _rotationRandomizationStep = value; EditorUtility.SetDirty(this); } }
        public float        minRandomRotation                           { get { return _minRandomRotation; } set { UndoEx.record(this); _minRandomRotation = Mathf.Min(value, _maxRandomRotation); EditorUtility.SetDirty(this); } }
        public float        maxRandomRotation                           { get { return _maxRandomRotation; } set { UndoEx.record(this); _maxRandomRotation = Mathf.Max(value, _minRandomRotation); EditorUtility.SetDirty(this); } }
        public bool         randomizeScale                              { get { return _randomizeScale; } set { UndoEx.record(this); _randomizeScale = value; EditorUtility.SetDirty(this); } }
        public float        minRandomScale                              { get { return _minRandomScale; } set { UndoEx.record(this); _minRandomScale = Mathf.Min(Mathf.Max(1e-2f, value), _maxRandomScale); EditorUtility.SetDirty(this); } }
        public float        maxRandomScale                              { get { return _maxRandomScale; } set { UndoEx.record(this); _maxRandomScale = Mathf.Max(Mathf.Max(1e-2f, value), _minRandomScale); EditorUtility.SetDirty(this); } }

        public static bool  defaultRandomizeRotation                    { get { return false; } }
        public static bool  defaultRandomizeRotationAroundX             { get { return false; } }
        public static bool  defaultRandomizeRotationAroundY             { get { return false; } }
        public static bool  defaultRandomizeRotationAroundZ             { get { return false; } }
        public static bool  defaultRandomizeRotationAroundSurfaceNormal { get { return true; } }
        public static RotationRandomizationMode defaultRotationRandomizationMode { get { return RotationRandomizationMode.Step; } }
        public static float defaultRotationRandomizationStepValue       { get { return 90.0f; } }
        public static float defaultMinRandomRotation                    { get { return 0.0f; } }
        public static float defaultMaxRandomRotation                    { get { return 360.0f; } }
        public static bool  defaultRandomizeScale                       { get { return false; } }
        public static float defaultMinRandomScale                       { get { return 0.5f; } }
        public static float defaultMaxRandomScale                       { get { return 2.0f; } }

        public override void useDefaults()
        {
            randomizeRotation                       = defaultRandomizeRotation;
            randomizeRotationAroundX                = defaultRandomizeRotationAroundX;
            randomizeRotationAroundY                = defaultRandomizeRotationAroundY;
            randomizeRotationAroundZ                = defaultRandomizeRotationAroundZ;
            randomizeRotationAroundSurfaceNormal    = defaultRandomizeRotationAroundSurfaceNormal;
            rotationRandomizationMode               = defaultRotationRandomizationMode;
            rotationRandomizationStep               = defaultRotationRandomizationStepValue;
            minRandomRotation                       = defaultMinRandomRotation;
            maxRandomRotation                       = defaultMaxRandomRotation;
            randomizeScale                          = defaultRandomizeScale;
            minRandomScale                          = defaultMinRandomScale;
            maxRandomScale                          = defaultMaxRandomScale;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent, float labelWidth)
        {
            var randomizeRotationToggle = UI.createToggle("_randomizeRotation", serializedObject, "Randomize rotation", "If checked, the rotation will be randomized.", parent);
            randomizeRotationToggle.setChildLabelWidth(labelWidth);

            var rotationSettingsContainer = new VisualElement();
            parent.Add(rotationSettingsContainer);
            rotationSettingsContainer.setDisplayVisible(randomizeRotation);
            randomizeRotationToggle.RegisterValueChangedCallback(p =>
            { rotationSettingsContainer.setDisplayVisible(p.newValue); });

            IMGUIContainer imGUIContainer       = new IMGUIContainer();
            imGUIContainer.style.marginLeft     = 4.0f;
            rotationSettingsContainer.Add(imGUIContainer);
            imGUIContainer.onGUIHandler = () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                bool axisToggle = EditorGUILayout.ToggleLeft("X", randomizeRotationAroundX, GUILayout.Width(UIValues.inlineToggleWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    randomizeRotationAroundX = axisToggle;
                    SceneView.RepaintAll();
                }

                EditorGUI.BeginChangeCheck();
                axisToggle = EditorGUILayout.ToggleLeft("Y", randomizeRotationAroundY, GUILayout.Width(UIValues.inlineToggleWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    randomizeRotationAroundY = axisToggle;
                    SceneView.RepaintAll();
                }
            
                EditorGUI.BeginChangeCheck();
                axisToggle = EditorGUILayout.ToggleLeft("Z", randomizeRotationAroundZ, GUILayout.Width(UIValues.inlineToggleWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    randomizeRotationAroundZ = axisToggle;
                    SceneView.RepaintAll();
                }

                EditorGUI.BeginChangeCheck();
                axisToggle = EditorGUILayout.ToggleLeft("Surface normal", randomizeRotationAroundSurfaceNormal);
                if (EditorGUI.EndChangeCheck())
                {
                    randomizeRotationAroundSurfaceNormal = axisToggle;
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();
            };

            var randomizationModeField  = UI.createEnumField(typeof(RotationRandomizationMode), "_rotationRandomizationMode", serializedObject, "Randomization mode", "Allows you to specify how the rotation is randomized.", rotationSettingsContainer);
            randomizationModeField.setChildLabelWidth(labelWidth);

            var stepValueField          = UI.createFloatField("_rotationRandomizationStep", serializedObject, "Rotation step", "The random rotation step value in degrees. The rotation will be set to a random multiple of this value.", rotationSettingsContainer);
            stepValueField.setChildLabelWidth(labelWidth);
            stepValueField.setDisplayVisible(rotationRandomizationMode == RotationRandomizationMode.Step);

            var minRandomRotationField  = UI.createFloatField("_minRandomRotation", serializedObject, "Min rotation", "The minimum random rotation value in degrees.", rotationSettingsContainer);
            minRandomRotationField.setChildLabelWidth(labelWidth);
            minRandomRotationField.setDisplayVisible(rotationRandomizationMode == RotationRandomizationMode.MinMax);
            minRandomRotationField.bindMaxValueProperty("_minRandomRotation", "_maxRandomRotation", serializedObject);
            var maxRandomRotationField  = UI.createFloatField("_maxRandomRotation", serializedObject, "Max rotation", "The maximum random rotation value in degrees.", rotationSettingsContainer);
            maxRandomRotationField.setChildLabelWidth(labelWidth);
            maxRandomRotationField.setDisplayVisible(rotationRandomizationMode == RotationRandomizationMode.MinMax);
            maxRandomRotationField.bindMinValueProperty("_maxRandomRotation", "_minRandomRotation", serializedObject);

            randomizationModeField.RegisterValueChangedCallback(p => 
            {
                if (rotationRandomizationMode == RotationRandomizationMode.Step)
                {
                    stepValueField.setDisplayVisible(true);
                    minRandomRotationField.setDisplayVisible(false);
                    maxRandomRotationField.setDisplayVisible(false);
                }
                else
                {
                    stepValueField.setDisplayVisible(false);
                    minRandomRotationField.setDisplayVisible(true);
                    maxRandomRotationField.setDisplayVisible(true);
                }
            });  
            
            var randomizeScaleToggle    = UI.createToggle("_randomizeScale", serializedObject, "Randomize scale", "If checked, the scale will be randomized.", parent);
            randomizeScaleToggle.setChildLabelWidth(labelWidth);
            var scaleSettingsContainer  = new VisualElement();
            parent.Add(scaleSettingsContainer);
            scaleSettingsContainer.setDisplayVisible(randomizeScale);
            randomizeScaleToggle.RegisterValueChangedCallback(p => 
            { scaleSettingsContainer.setDisplayVisible(p.newValue); });

            const float minScale        = 1e-2f;
            var minRandomScaleField     = UI.createFloatField("_minRandomScale", serializedObject, "Min scale", "The minimum random scale value.", minScale, scaleSettingsContainer);
            minRandomScaleField.setChildLabelWidth(labelWidth);
            minRandomScaleField.bindMaxValueProperty("_minRandomScale", "_maxRandomScale", minScale, serializedObject);
            var maxRandomScaleField     = UI.createFloatField("_maxRandomScale", serializedObject, "Max scale", "The maximum random scale value.", minScale, scaleSettingsContainer);
            maxRandomScaleField.setChildLabelWidth(labelWidth);
            maxRandomScaleField.bindMinValueProperty("_maxRandomScale", "_minRandomScale", minScale, serializedObject);
        }

        public void randomizeTransform(Transform transform, Vector3 surfaceNormal)
        {
            if (randomizeRotation)
            {
                Quaternion  xyzRotation         = Quaternion.identity;
                Vector3     eulerRotation       = Vector3.zero;
                Quaternion  totalRotation       = Quaternion.identity;
                Quaternion  surfaceRotation     = Quaternion.identity;

                if (rotationRandomizationMode == RotationRandomizationMode.Step)
                {
                    int numSteps = (int)(360.0f / rotationRandomizationStep + 0.5f);
                    if (randomizeRotationAroundX) eulerRotation.x = rotationRandomizationStep * Random.Range(0, numSteps + 1);
                    if (randomizeRotationAroundY) eulerRotation.y = rotationRandomizationStep * Random.Range(0, numSteps + 1);
                    if (randomizeRotationAroundZ) eulerRotation.z = rotationRandomizationStep * Random.Range(0, numSteps + 1);
                    if (randomizeRotationAroundSurfaceNormal) surfaceRotation = Quaternion.AngleAxis(rotationRandomizationStep * Random.Range(0, numSteps + 1), surfaceNormal);
                }
                else
                {
                    if (randomizeRotationAroundX) eulerRotation.x = Random.Range(minRandomRotation, maxRandomRotation);
                    if (randomizeRotationAroundY) eulerRotation.y = Random.Range(minRandomRotation, maxRandomRotation);
                    if (randomizeRotationAroundZ) eulerRotation.z = Random.Range(minRandomRotation, maxRandomRotation);
                    if (randomizeRotationAroundSurfaceNormal) surfaceRotation = Quaternion.AngleAxis(Random.Range(minRandomRotation, maxRandomRotation), surfaceNormal);
                }

                xyzRotation         = Quaternion.Euler(eulerRotation);
                totalRotation       = surfaceRotation * xyzRotation;
                transform.rotation  = totalRotation * transform.rotation;
            }
            if (randomizeScale) transform.setWorldScale(Vector3Ex.create(Random.Range(minRandomScale, maxRandomScale)));
        }
    }
}
#endif