#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class TerrainFlattenSettings : PluginSettings<TerrainFlattenSettings>
    {
        [SerializeField]
        private bool                        _flattenTerrain             = defaultFlattenTerrain;
        [SerializeField]
        private int                         _terrainQuadRadius          = defaultTerrainQuadRadius;
        [SerializeField]
        private TerrainFlattenMode          _mode                       = defaultMode;
        [SerializeField]
        private bool                        _applyFalloff               = defaultApplyFalloff;

        public bool                         flattenTerrain              { get { return _flattenTerrain; } set { UndoEx.record(this); _flattenTerrain = value; EditorUtility.SetDirty(this); } }
        public int                          terrainQuadRadius           { get { return _terrainQuadRadius; } set { UndoEx.record(this); _terrainQuadRadius = Mathf.Max(value, 0); EditorUtility.SetDirty(this); } }
        public TerrainFlattenMode           mode                        { get { return _mode; } set { UndoEx.record(this); _mode = value; EditorUtility.SetDirty(this); } }
        public bool                         applyFalloff                { get { return _applyFalloff; } set { UndoEx.record(this); _applyFalloff = value; EditorUtility.SetDirty(this); } }

        public static bool                  defaultFlattenTerrain       { get { return false; } }
        public static int                   defaultTerrainQuadRadius    { get { return 0; } }
        public static TerrainFlattenMode    defaultMode                 { get { return TerrainFlattenMode.Average; } }
        public static bool                  defaultApplyFalloff         { get { return true; } }

        public void buildUI(VisualElement parent)
        {
            HelpBox info        = new HelpBox();
            info.messageType    = HelpBoxMessageType.Info;
            info.text           = "Terrain flattening can be SLOW for terrains that use a large heightmap resolution. Also, flattening doesn't handle the boundary between tiled terrains.";
            parent.Add(info);
            info.setDisplayVisible(flattenTerrain);

            var flattenToggle = UI.createToggle("_flattenTerrain", serializedObject, "Flatten", "When this is checked, terrain flattening is enabled.", parent);
            flattenToggle.RegisterValueChangedCallback(p => 
            {
                info.setDisplayVisible(flattenTerrain);
            });
            
            UI.createIntegerField("_terrainQuadRadius", serializedObject, "Terrain quad radius", "The terrain flatten radius expressed in number of terrain quads.", 0, parent);
            UI.createEnumField(typeof(TerrainFlattenMode), "_mode", serializedObject, "Mode", "The terrain flatten mode which controls the way " +
                "in which the terrain height is calculated.", parent);
            UI.createToggle("_applyFalloff", serializedObject, "Apply falloff", "Check this to smooth out hard transitions.", parent);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {
            flattenTerrain      = defaultFlattenTerrain;
            terrainQuadRadius   = defaultTerrainQuadRadius;
            mode                = defaultMode;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif