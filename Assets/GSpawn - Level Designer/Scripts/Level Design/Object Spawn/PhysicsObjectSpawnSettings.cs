#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class PhysicsObjectSpawnSettings : PluginSettings<PhysicsObjectSpawnSettings>
    {
        [SerializeField]
        private string  _randomPrefabProfileName    = defaultRandomPrefabProfileName;
        [SerializeField]
        private bool    _randomizeRotation          = defaultRanfomizeRotation;
        [SerializeField]
        private float   _simulationTime             = defaultSimulationTime;
        [SerializeField]
        private float   _simulationStep             = defaultSimulationStep;
        [SerializeField]
        private float   _outOfBoundsYCoord          = defaultOutOfBoundsYCoord;
        [SerializeField]
        private float   _dropRadius                 = defaultDropRadius;
        [SerializeField]
        private float   _dropHeight                 = defaultDropHeight;
        [SerializeField]
        private float   _dropInterval               = defaultDropInterval;
        [SerializeField]
        private bool    _instant                    = defaultInstant;

        public RandomPrefabProfile                  randomPrefabProfile
        {
            get
            {
                var profile = RandomPrefabProfileDb.instance.findProfile(_randomPrefabProfileName);
                if (profile == null) profile = RandomPrefabProfileDb.instance.defaultProfile;
                return profile;
            }
        }
        public bool             randomizeRotation                   { get { return _randomizeRotation; } set { UndoEx.record(this); _randomizeRotation = value; EditorUtility.SetDirty(this); } }
        public float            simulationTime                      { get { return _simulationTime; } set { UndoEx.record(this); _simulationTime = Mathf.Max(0.1f, value); EditorUtility.SetDirty(this); } }
        public float            simulationStep                      { get { return _simulationStep; } set { UndoEx.record(this); _simulationStep = Mathf.Max(0.01f, value); EditorUtility.SetDirty(this); } }
        public float            outOfBoundsYCoord                   { get { return _outOfBoundsYCoord; } set { UndoEx.record(this); _outOfBoundsYCoord = value; EditorUtility.SetDirty(this); } }       
        public float            dropRadius                          { get { return _dropRadius; } set { UndoEx.record(this); _dropRadius = Mathf.Max(0.1f, value); EditorUtility.SetDirty(this); } }
        public float            dropHeight                          { get { return _dropHeight; } set { UndoEx.record(this); _dropHeight = Mathf.Max(0.1f, value); EditorUtility.SetDirty(this); } }
        public float            dropInterval                        { get { return _dropInterval; } set { UndoEx.record(this); _dropInterval = Mathf.Max(0.01f, value); EditorUtility.SetDirty(this); } }
        public bool             instant                             { get { return _instant; } set { UndoEx.record(this); _instant = value; EditorUtility.SetDirty(this); } }

        public static string    defaultRandomPrefabProfileName      { get { return RandomPrefabProfileDb.defaultProfileName; } }
        public static bool      defaultRanfomizeRotation            { get { return true; } }
        public static float     defaultSimulationTime               { get { return 10.0f; } }
        public static float     defaultSimulationStep               { get { return 0.01f; } }
        public static float     defaultOutOfBoundsYCoord            { get { return -10.0f; } }
        public static float     defaultDropRadius                   { get { return 2.0f; } }
        public static float     defaultDropHeight                   { get { return 5.0f; } }
        public static float     defaultDropInterval                 { get { return 0.05f; } }
        public static bool      defaultInstant                      { get { return false; } }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 140.0f;

            IMGUIContainer randomPrefabProfileContainer = UI.createIMGUIContainer(parent);
            randomPrefabProfileContainer.onGUIHandler = () =>
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

            var randomizeRotationField = UI.createToggle("_randomizeRotation", serializedObject, "Randomize rotation", 
                "If checked, a random rotation will be applied to objects before they get dropped in the scene.", parent);
            randomizeRotationField.setChildLabelWidth(labelWidth);

            var simulationTimeField = UI.createFloatField("_simulationTime", serializedObject, "Simulation time", "The amount of time the simulation lasts in seconds. " + 
                "Note: The simulation time is always reset when new objects are spawned.", 0.1f, parent);
            simulationTimeField.setChildLabelWidth(labelWidth);

            var simulationStepField = UI.createFloatField("_simulationStep", serializedObject, "Simulation step", "The time used to advance the physics simulation.", 0.01f, parent);
            simulationStepField.setChildLabelWidth(labelWidth);

            var outOfBoundsYCoordField = UI.createFloatField("_outOfBoundsYCoord", serializedObject, "Out of bounds Y", "During a simulation, " + 
                "the plugin will destroy any objects whose positions along the Y axis become <= to this value. Useful to avoid stray objects.", parent);
            outOfBoundsYCoordField.setChildLabelWidth(labelWidth);

            var dropRadiusField = UI.createFloatField("_dropRadius", serializedObject, "Drop radius", "The radius of the circle in which objects will be spawned.", 0.1f, parent);
            dropRadiusField.setChildLabelWidth(labelWidth);

            var dropHeightField = UI.createFloatField("_dropHeight", serializedObject, "Drop height", "The height from which the object will be dropped onto the surface.", 0.1f, parent);
            dropHeightField.setChildLabelWidth(labelWidth);

            var dropIntervalField = UI.createFloatField("_dropInterval", serializedObject, "Drop interval", "The amount of time that needs to pass between successive drops.", 0.01f, parent);
            dropIntervalField.setChildLabelWidth(labelWidth);

            var instantToggle = UI.createToggle("_instant", serializedObject, "Instant", "If checked, the simulation will be applied instantly for each object that is spawned.", parent);
            instantToggle.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {
            _randomPrefabProfileName    = defaultRandomPrefabProfileName;
            simulationTime              = defaultSimulationTime;
            simulationStep              = defaultSimulationStep;
            outOfBoundsYCoord           = defaultOutOfBoundsYCoord;
            dropRadius                  = defaultDropRadius;
            dropHeight                  = defaultDropHeight;
            dropInterval                = defaultDropInterval;
            instant                     = defaultInstant;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif