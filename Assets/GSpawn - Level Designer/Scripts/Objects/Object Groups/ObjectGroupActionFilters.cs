#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public class ObjectGroupActionFilters : PluginSettings<ObjectGroupActionFilters>
    {
        [SerializeField] bool _belowGrid    = defaultBelowGrid;
        [SerializeField] bool _aboveGrid    = defaultAboveGrid;
        [SerializeField] bool _onGrid       = defaultOnGrid;

        public bool belowGrid   { get { return _belowGrid; } set { UndoEx.record(this); _belowGrid = value; EditorUtility.SetDirty(this); } }
        public bool aboveGrid   { get { return _aboveGrid; } set { UndoEx.record(this); _aboveGrid = value; EditorUtility.SetDirty(this); } }
        public bool onGrid      { get { return _onGrid; } set { UndoEx.record(this); _onGrid = value; EditorUtility.SetDirty(this); } }

        public static bool defaultBelowGrid { get { return true; } }
        public static bool defaultAboveGrid { get { return true; } }
        public static bool defaultOnGrid    { get { return true; } }

        public bool filterObject(GameObject gameObject)
        {
            if (belowGrid && aboveGrid && onGrid)       return true;
            if (!belowGrid && !aboveGrid && !onGrid)    return false;

            const float onGridEps = 1e-4f;
            PluginGrid grid = PluginScene.instance.grid;
            float d         = grid.plane.GetDistanceToPoint(gameObject.transform.position);
            if (d < -onGridEps) return belowGrid;
            else if (d > onGridEps) return aboveGrid;
            else return onGrid;
        }

        public override void useDefaults()
        {
            belowGrid   = defaultBelowGrid;
            aboveGrid   = defaultAboveGrid;
            onGrid      = defaultOnGrid;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 120.0f;

            UI.createRowSeparator(parent);

            var label       = new Label();
            parent.Add(label);
            label.text      = "Action Filters";
            label.tooltip   = "A series of filters which apply when operating on objects which are children of object groups.";
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginLeft = 4.0f;

            var toggle      = UI.createToggle("_belowGrid", serializedObject, "Below grid", "The objects will be acted upon only if their " +
                "position resides below the scene grid.", parent);
            toggle.setChildLabelWidth(labelWidth);
            toggle          = UI.createToggle("_aboveGrid", serializedObject, "Above grid", "The objects will be acted upon only if their " +
                "position resides above the scene grid.", parent);
            toggle.setChildLabelWidth(labelWidth);
            toggle          = UI.createToggle("_onGrid", serializedObject, "On grid", "The objects will be acted upon only if their " +
                "position resides on the scene grid.", parent);
            toggle.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }
    }
}
#endif