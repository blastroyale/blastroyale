#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectEraseBrush2DSettings : PluginSettings<ObjectEraseBrush2DSettings>
    {
        [SerializeField]
        private bool        _enableCullPlaneByDefault           = defaultEnableCullPlaneByDefault;
        [SerializeField]
        private bool        _allowPartialOverlap                = defaultAllowPartialOverlap;
        [SerializeField]
        private float       _radius                             = defaultRadius;

        public bool         enableCullPlaneByDefault            { get { return _enableCullPlaneByDefault; } set { UndoEx.record(this); _enableCullPlaneByDefault = value; EditorUtility.SetDirty(this); } }
        public bool         allowPartialOverlap                 { get { return _allowPartialOverlap; } set { UndoEx.record(this); _allowPartialOverlap = value; EditorUtility.SetDirty(this); } }
        public float        radius                              { get { return _radius; } set { UndoEx.record(this); _radius = Mathf.Max(4.0f, value); EditorUtility.SetDirty(this); } }

        public static bool  defaultEnableCullPlaneByDefault     { get { return false; } }
        public static bool  defaultAllowPartialOverlap          { get { return true; } }
        public static float defaultRadius                       { get { return 20.0f; } }

        public void buildUI(VisualElement parent)
        {
            UI.createToggle("_enableCullPlaneByDefault", serializedObject, "Enable cull plane by default", "If this is checked, the object erase cull plane " +
               " is enabled by default without having to use hotkeys. Note: Using the hotkeys will actually disable the cull plane (i.e. behavior is reversed).", parent);
            UI.createToggle("_allowPartialOverlap", serializedObject, "Allow partial overlap", "If this is checked, objects will be erased even if they are not completely overlapped by the brush. " + 
                "When unchecked, only objects that are completely overlapped by the brush will be erased.", parent);
            UI.createFloatField("_radius", serializedObject, "Radius", "The 2D brush radius.", 4.0f, parent);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {
            enableCullPlaneByDefault    = defaultEnableCullPlaneByDefault;
            allowPartialOverlap         = defaultAllowPartialOverlap;
            radius                      = defaultRadius;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif