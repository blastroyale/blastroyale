#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectEraseCursorSettings : PluginSettings<ObjectEraseCursorSettings>
    {
        [SerializeField]
        private bool        _enableCullPlaneByDefault       = defaultEnableCullPlaneByDefault;

        public bool         enableCullPlaneByDefault        { get { return _enableCullPlaneByDefault; } set { UndoEx.record(this); _enableCullPlaneByDefault = value; EditorUtility.SetDirty(this); } }

        public static bool  defaultEnableCullPlaneByDefault { get { return false; } }

        public void buildUI(VisualElement parent)
        {
            UI.createToggle("_enableCullPlaneByDefault", serializedObject, "Enable cull plane by default", "If this is checked, the object erase cull plane " +
                " is enabled by default without having to use hotkeys. Note: Using the hotkeys will actually disable the cull plane (i.e. behavior is reversed).", parent);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {
            enableCullPlaneByDefault = defaultEnableCullPlaneByDefault;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif