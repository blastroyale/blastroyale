#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public enum ObjectExtrudeSpace
    {
        Local = 0,
        Global
    }

    public enum ObjectExtrudeGizmoProjectionMode
    {
        None = 0,
        Terrains
    }

    public class ObjectExtrudeGizmoSettings : PluginSettings<ObjectExtrudeGizmoSettings>
    {
        [SerializeField]
        private ObjectExtrudeSpace                      _extrudeSpace           = defaultExtrudeSpace;
        [SerializeField]
        private ObjectExtrudeGizmoProjectionMode        _projectionMode         = defaultProjectionMode;
        [SerializeField]
        private Vector3                                 _padding                = defaultPadding;
        [SerializeField]
        private bool                                    _avoidOverlaps          = defaultAvoidOverlaps;

        public ObjectExtrudeSpace                       extrudeSpace            { get { return _extrudeSpace; } set { UndoEx.record(this); _extrudeSpace = value; EditorUtility.SetDirty(this); } }
        public ObjectExtrudeGizmoProjectionMode         projectionMode          { get { return _projectionMode; } set { UndoEx.record(this); _projectionMode = value; EditorUtility.SetDirty(this); } }
        public Vector3                                  padding                 { get { return _padding; } set { UndoEx.record(this); _padding = Vector3.Max(Vector3.zero, value); EditorUtility.SetDirty(this); } }
        public bool                                     avoidOverlaps           { get { return _avoidOverlaps; } set { UndoEx.record(this); _avoidOverlaps = value; EditorUtility.SetDirty(this); } }

        public static ObjectExtrudeSpace                defaultExtrudeSpace     { get { return ObjectExtrudeSpace.Local; } }
        public static ObjectExtrudeGizmoProjectionMode  defaultProjectionMode   { get { return ObjectExtrudeGizmoProjectionMode.None; } }
        public static Vector3Int                        defaultPatternStep      { get { return Vector3Int.zero; } }
        public static Vector3                           defaultPadding          { get { return Vector3.zero; } }
        public static bool                              defaultAvoidOverlaps    { get { return true; } }

        public void buildUI(VisualElement parent)
        {
            UI.createEnumField(typeof(ObjectExtrudeSpace), "_extrudeSpace", serializedObject, "Extrude space", "The extrude space allows you to select between global and local extrude. Local extrude " +
                "will take the object's rotation into account and it allows you to extrude along local axes. Global extrude limits extrusion to the global axes only.", parent);
            /*UI.createEnumField(typeof(ObjectExtrudeGizmoProjectionMode), "_projectionMode", serializedObject, "Projection mode",
                "Allows you to specify how the spawned objects will be projected in the scene.", parent);*/
            UI.createVector3Field("_padding", serializedObject, "Padding", "Padding between extrude cells.", Vector3.zero, parent);
            UI.createToggle("_avoidOverlaps", serializedObject, "Avoid overlaps", "If this is checked, no objects will be created in places where they would overlap with already existing objects.", parent);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {   
            extrudeSpace    = defaultExtrudeSpace;
            projectionMode  = defaultProjectionMode;
            padding         = defaultPadding;
            avoidOverlaps   = defaultAvoidOverlaps;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif