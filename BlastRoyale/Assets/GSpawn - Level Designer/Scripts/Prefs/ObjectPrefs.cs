#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectPrefs : Prefs<ObjectPrefs>
    {
        [SerializeField][UIFieldConfig("Up axis", "The object local axis that should be treated as up for terrain meshes.", "Terrain mesh", false)]
        private Axis        _terrainMeshUpAxis                      = defaultTerrainMeshUpAxis;
        [SerializeField][UIFieldConfig("Invert axis", "If checked, the terrain mesh up axis is inverted.")]
        private bool        _invertTerrainMeshUpAxis                = defaultInvertTerrainMeshUpAxis;

        public Axis         terrainMeshUpAxis                       { get { return _terrainMeshUpAxis; } set { UndoEx.record(this); _terrainMeshUpAxis = value; EditorUtility.SetDirty(this); } }
        public bool         invertTerrainMeshUpAxis                 { get { return _invertTerrainMeshUpAxis; } set { UndoEx.record(this); _invertTerrainMeshUpAxis = value; EditorUtility.SetDirty(this); } }

        public static Axis  defaultTerrainMeshUpAxis                { get { return Axis.Y; } }
        public static bool  defaultInvertTerrainMeshUpAxis          { get { return false; } }

        public Vector3 getTerrainMeshUp(GameObject terrainMeshObject)
        {
            if (_terrainMeshUpAxis == Axis.Y) return invertTerrainMeshUpAxis ? -terrainMeshObject.transform.up : terrainMeshObject.transform.up;
            if (_terrainMeshUpAxis == Axis.X) return invertTerrainMeshUpAxis ? -terrainMeshObject.transform.right : terrainMeshObject.transform.right;
            return invertTerrainMeshUpAxis ? -terrainMeshObject.transform.forward : terrainMeshObject.transform.forward;
        }

        public Plane getTerrainMeshHorizontalPlane(GameObject terrainMeshObject)
        {
            if (_terrainMeshUpAxis == Axis.Y) return new Plane(invertTerrainMeshUpAxis ? -terrainMeshObject.transform.up : terrainMeshObject.transform.up, terrainMeshObject.transform.position);
            if (_terrainMeshUpAxis == Axis.X) return new Plane(invertTerrainMeshUpAxis ? -terrainMeshObject.transform.right : terrainMeshObject.transform.right, terrainMeshObject.transform.position);
            return new Plane(invertTerrainMeshUpAxis ? -terrainMeshObject.transform.forward : terrainMeshObject.transform.forward, terrainMeshObject.transform.position);
        }

        public override void useDefaults()
        {
            terrainMeshUpAxis           = defaultTerrainMeshUpAxis;
            invertTerrainMeshUpAxis     = defaultInvertTerrainMeshUpAxis;

            EditorUtility.SetDirty(this);
        }
    }

    class ObjectPrefsProvider : SettingsProvider
    {
        public ObjectPrefsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            UI.createPrefsTitleLabel("Object", rootElement);
            ObjectPrefs.instance.buildDefaultUI(rootElement, PluginSettingsUIBuildConfig.defaultConfig);

            const float labelWidth = 155.0f;
            rootElement.Query<Label>().ForEach(item => item.setChildLabelWidth(labelWidth));
        }

        [SettingsProvider]
        public static SettingsProvider create()
        {
            if (GSpawn.active == null) return null;
            return new ObjectPrefsProvider("Preferences/" + GSpawn.pluginName + "/Object", SettingsScope.User);
        }
    }
}
#endif