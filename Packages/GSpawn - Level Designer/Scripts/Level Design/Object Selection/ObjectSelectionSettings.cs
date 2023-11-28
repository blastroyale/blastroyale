#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectSelectionSettings : PluginSettings<ObjectSelectionSettings>
    {
        [SerializeField]
        private bool        _canSelectLights                    = defaultCanSelectLights;
        [SerializeField]
        private bool        _canSelectParticleSystems           = defaultCanSelectParticleSystems;
        [SerializeField]
        private bool        _canSelectCameras                   = defaultCanSelectCameras;
        [SerializeField]
        private bool        _canSelectUnityTerrains             = defaultCanSelectUnityTerrains;
        [SerializeField]
        private bool        _canSelectTerrainMeshes             = defaultCanSelectTerrainMeshes;
        [SerializeField]
        private bool        _canSelectSphericalMeshes           = defaultCanSelectSphericalMeshes;

        public bool         canSelectLights                     { get { return _canSelectLights; } set { UndoEx.record(this); _canSelectLights = value; EditorUtility.SetDirty(this); } }
        public bool         canSelectParticleSystems            { get { return _canSelectParticleSystems; } set { UndoEx.record(this); _canSelectParticleSystems = value; EditorUtility.SetDirty(this); } }
        public bool         canSelectCameras                    { get { return _canSelectCameras; } set { UndoEx.record(this); _canSelectCameras = value; EditorUtility.SetDirty(this); } }
        public bool         canSelectUnityTerrains              { get { return _canSelectUnityTerrains; } set { UndoEx.record(this); _canSelectUnityTerrains = value; EditorUtility.SetDirty(this); } }
        public bool         canSelectTerrainMeshes              { get { return _canSelectTerrainMeshes; } set { UndoEx.record(this); _canSelectTerrainMeshes = value; EditorUtility.SetDirty(this); } }
        public bool         canSelectSphericalMeshes            { get { return _canSelectSphericalMeshes; } set { UndoEx.record(this); _canSelectSphericalMeshes = value; EditorUtility.SetDirty(this); } }

        public static bool  defaultCanSelectLights              { get { return false; } }
        public static bool  defaultCanSelectParticleSystems     { get { return false; } }
        public static bool  defaultCanSelectCameras             { get { return false; } }
        public static bool  defaultCanSelectUnityTerrains       { get { return false; } }
        public static bool  defaultCanSelectTerrainMeshes       { get { return false; } }
        public static bool  defaultCanSelectSphericalMeshes     { get { return false; } }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 180.0f;

            UI.createRowSeparator(parent);
            var sectionLabel = UI.createSectionLabel("Selection Filters", parent);

            VisualElement ctrl = UI.createToggle("_canSelectLights", serializedObject, "Lights", "If this is checked, light objects can be selected.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_canSelectParticleSystems", serializedObject, "Particle systems", "If this is checked, particle system objects can be selected.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_canSelectCameras", serializedObject, "Cameras", "If this is checked, camera objects can be selected.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_canSelectUnityTerrains", serializedObject, "Unity terrains", "If this is checked, Unity terrain objects can be selected.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_canSelectTerrainMeshes", serializedObject, "Terrain meshes", "If this is checked, terrain meshes can be selected.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_canSelectSphericalMeshes", serializedObject, "Spherical meshes", "If this is checked, spherical meshes can be selected.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {
            canSelectLights                 = defaultCanSelectLights;
            canSelectParticleSystems        = defaultCanSelectParticleSystems;
            canSelectCameras                = defaultCanSelectCameras;
            canSelectUnityTerrains          = defaultCanSelectUnityTerrains;
            canSelectTerrainMeshes          = defaultCanSelectTerrainMeshes;
            canSelectSphericalMeshes        = defaultCanSelectSphericalMeshes;

            EditorUtility.SetDirty(this);
        }

        public bool isGameObjectSelectable(GameObject gameObject, GameObjectType gameObjectType)
        {
            if (gameObjectType == GameObjectType.Light) return canSelectLights;
            if (gameObjectType == GameObjectType.ParticleSystem) return canSelectParticleSystems;
            if (gameObjectType == GameObjectType.Camera) return canSelectCameras;
            if (gameObjectType == GameObjectType.Terrain) return canSelectUnityTerrains;
            if (gameObjectType == GameObjectType.Mesh)
            {
                if (gameObject.isTerrainMesh()) return canSelectTerrainMeshes;
                if (gameObject.isSphericalMesh()) return canSelectSphericalMeshes;
            }

            return !TileRuleGridDb.instance.isObjectChildOfTileRuleGrid(gameObject);
        }
    }
}
#endif