#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;

namespace GSpawn
{
    public enum ScatterBrushOverlapPrecision
    {
        BoundsVSBounds = 0,
        BoundsVSGeometry
    }

    [Flags]
    public enum ScatterBrushObjectSpawnSurfaceTypes
    {
        None = 0,
        Grid = 1,
        Meshes = 2,
        Terrains = 4,
        All = ~0
    }

    public class ScatterBrushObjectSpawnSettings : PluginSettings<ScatterBrushObjectSpawnSettings>
    {
        [SerializeField]
        private float           _brushRadius                            = defaultBrushRadius;
        [SerializeField]
        private int             _maxNumObjects                          = defaultMaxNumObjects;        
        [SerializeField]
        private float           _minDragDistance                        = defaultMinDragDistance;
        [SerializeField]
        private ScatterBrushObjectSpawnSurfaceTypes _surfaceTypes       = defaultSurfaceTypes;
        [SerializeField]
        private int             _surfaceLayers                          = defaultSurfaceLayers;
        [SerializeField]
        private string          _scatterBrushPrefabProfileName          = defaultScatterBrushPrefabProfileName;
        [SerializeField]
        private ScatterBrushOverlapPrecision    _overlapTestPrecision   = defaultOverlapTestPrecision;

        public float            brushRadius                             { get { return _brushRadius; } set { UndoEx.record(this); _brushRadius = Mathf.Max(0.1f, value); EditorUtility.SetDirty(this); } }
        public int              maxNumObjects                           { get { return _maxNumObjects; } set { UndoEx.record(this); _maxNumObjects = Math.Max(1, value); EditorUtility.SetDirty(this); } }
        public float            minDragDistance                         { get { return _minDragDistance; } set { UndoEx.record(this); _minDragDistance = Mathf.Max(1e-4f, value); EditorUtility.SetDirty(this); } }
        public ScatterBrushObjectSpawnSurfaceTypes surfaceTypes         { get { return _surfaceTypes; } set { UndoEx.record(this); _surfaceTypes = value; EditorUtility.SetDirty(this); } }
        public bool             allowsGridSurface                       { get { return (surfaceTypes & ScatterBrushObjectSpawnSurfaceTypes.Grid) != 0; } }
        public bool             allowsMeshSurface                       { get { return (surfaceTypes & ScatterBrushObjectSpawnSurfaceTypes.Meshes) != 0; } }
        public bool             allowsTerrainSurface                    { get { return (surfaceTypes & ScatterBrushObjectSpawnSurfaceTypes.Terrains) != 0; } }
        public int              surfaceLayers                           { get { return _surfaceLayers; } set { UndoEx.record(this); _surfaceLayers = value; EditorUtility.SetDirty(this); } }
        public ScatterBrushPrefabProfile scatterBrushPrefabProfile
        {
            get
            {
                var profile = ScatterBrushPrefabProfileDb.instance.findProfile(_scatterBrushPrefabProfileName);
                if (profile == null) profile = ScatterBrushPrefabProfileDb.instance.defaultProfile;
                return profile;
            }
        }
        public ScatterBrushOverlapPrecision overlapTestPrecision        { get { return _overlapTestPrecision; } set { UndoEx.record(this); _overlapTestPrecision = value; EditorUtility.SetDirty(this); } }

        public static float     defaultBrushRadius                      { get { return 10; } }
        public static int       defaultMaxNumObjects                    { get { return 100; } }
        public static float     defaultMinDragDistance                  { get { return 1.0f; } }
        public static ScatterBrushObjectSpawnSurfaceTypes defaultSurfaceTypes { get { return ScatterBrushObjectSpawnSurfaceTypes.All; } }
        public static int       defaultSurfaceLayers                    { get { return ~0; } }
        public static string    defaultScatterBrushPrefabProfileName    { get { return ScatterBrushPrefabProfileDb.defaultProfileName; } }
        public static ScatterBrushOverlapPrecision defaultOverlapTestPrecision  { get { return ScatterBrushOverlapPrecision.BoundsVSBounds; } }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 140.0f;

            IMGUIContainer imGUIContainer = UI.createIMGUIContainer(parent);
            imGUIContainer.onGUIHandler = () =>
            {
                string newName = EditorUIEx.profileNameSelectionField<ScatterBrushPrefabProfileDb, ScatterBrushPrefabProfile>
                    (ScatterBrushPrefabProfileDb.instance, "Scatter brush profile", labelWidth, _scatterBrushPrefabProfileName);
                if (newName != _scatterBrushPrefabProfileName)
                {
                    UndoEx.record(this);
                    _scatterBrushPrefabProfileName = newName;
                    EditorUtility.SetDirty(this);
                }
            };

            var floatField = UI.createFloatField("_brushRadius", serializedObject, "Brush radius", "The paint brush radius.", 0.1f, parent);
            floatField.setFieldLabelWidth(labelWidth);

            var intField = UI.createIntegerField("_maxNumObjects", serializedObject, "Max num objects", "The maximum number of objects that can be spawned inside the brush during a single drag step.", 1, parent);
            intField.setFieldLabelWidth(labelWidth);

            floatField = UI.createFloatField("_minDragDistance", serializedObject, "~Min drag distance", "An approximate value that indicates the minimum drag distance that must be traversed by the mouse cursor before new objects are spawned.", 1e-4f, parent);
            floatField.setFieldLabelWidth(labelWidth);

            var enumFlagsField = UI.createEnumFlagsField(typeof(ScatterBrushObjectSpawnSurfaceTypes), "_surfaceTypes", serializedObject, "Surface types", "Allows you to specify the types of entities that can be used as surfaces on which the paint brush can sit.", parent);
            enumFlagsField.setFieldLabelWidth(labelWidth);

            var layerMaskField = UI.createLayerMaskField(surfaceLayers, "_surfaceLayers", serializedObject, "Surface layers", "Only objects belonging to any of these layers can be used as a surface for the paint brush.", parent);
            layerMaskField.setFieldLabelWidth(labelWidth);

            var overlapPrecisField = UI.createEnumField(typeof(ScatterBrushOverlapPrecision), "_overlapTestPrecision", serializedObject, "Overlap test precision",
                "The precision mode used when testing for overlaps with scene objects.", parent);
            overlapPrecisField.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {
            brushRadius                     = defaultBrushRadius;
            maxNumObjects                   = defaultMaxNumObjects;
            minDragDistance                 = defaultMinDragDistance;
            surfaceTypes                    = defaultSurfaceTypes;
            surfaceLayers                   = defaultSurfaceLayers;
            _scatterBrushPrefabProfileName  = defaultScatterBrushPrefabProfileName;
            overlapTestPrecision            = defaultOverlapTestPrecision;
            
            EditorUtility.SetDirty(this);
        }
    }
}
#endif