#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectDragSpawnSettings : PluginSettings<ObjectDragSpawnSettings>
    {
        [SerializeField]
        private float   _minDragDistance            = defaultMinDragDistance;
        [SerializeField]
        private bool    _useSafeDragDistance        = defaultUseSafeDragDistance;

        public float            minDragDistance                 { get { return _minDragDistance; } set { UndoEx.record(this); _minDragDistance = Mathf.Max(1e-4f, value); EditorUtility.SetDirty(this); } }
        public bool             useSafeDragDistance             { get { return _useSafeDragDistance; } set { UndoEx.record(this); _useSafeDragDistance = value; EditorUtility.SetDirty(this); } }

        public static float     defaultMinDragDistance          { get { return 1.0f; } }
        public static bool      defaultUseSafeDragDistance      { get { return true; } }

        public override void useDefaults()
        {
            minDragDistance         = defaultMinDragDistance;
            useSafeDragDistance     = defaultUseSafeDragDistance;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 150.0f;

            var floatField  = UI.createFloatField("_minDragDistance", serializedObject, "~Min drag distance", "An approximate value that indicates the minimum drag distance that must be traversed by the mouse cursor before a new object is spawned.", 1e-4f, parent);
            floatField.setChildLabelWidth(labelWidth);
            
            var toggleField = UI.createToggle("_useSafeDragDistance", serializedObject, "Use safe drag distance", "If this is checked, " +
                "the minimum drag distance will always be clamped to the radius of the object's volume.", parent);
            toggleField.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }
    }
}
#endif