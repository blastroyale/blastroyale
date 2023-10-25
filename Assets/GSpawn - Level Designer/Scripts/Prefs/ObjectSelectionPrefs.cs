#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public enum ObjectSelectionPrefabTransformSession
    { 
        None = 0,
        ModularSnap,
        SurfaceSnap
    }

    public class ObjectSelectionPrefs : Prefs<ObjectSelectionPrefs>
    {
        [SerializeField][UIFieldConfig("Allow child select", "If checked, clicking on the same object will switch between selecting the prefab instance root and the child which was actually clicked on. " + 
            "It is recommended that you leave this unchecked as it can cause subtle errors if you select a child when you wanted to select the root instead.", "Click Select", false)]
        private bool _clickSelectAllowChildSelect   = defaultClickSelectAllowChildSelect;
       
        [SerializeField][UIFieldConfig("Parent color", "The highlight color used for parent objects.", "Selection Highlight", true)]
        private Color _parentHighlightColor         = defaultParentHighlightColor;
        [SerializeField][UIFieldConfig("Child color", "The highlight color used for child objects.")]
        private Color _childHighlightColor          = defaultChildHighlightColor;
        [SerializeField][UIFieldConfig("Opacity", "The highlight opacity.")][Range(0.0f, 1.0f)]
        private float _highlightOpacity             = defaultHighlightOpacity;
        [SerializeField][UIFieldConfig("Camera gizmo color", "The color used to draw selected camera gizmos.")]
        private Color _cameraGizmoColor             = defaultCameraGizmoColor;
        
        [SerializeField][UIFieldConfig("Border color", "The selection rectangle border color", "Selection Rectangle", true)]
        private Color _selRectBorderColor           = defaultSelRectBorderColor;
        [SerializeField][UIFieldConfig("Fill color", "The selection rectangle fill color")]
        private Color _selRectFillColor             = defaultSelRectFillColor;
        
        [SerializeField][UIFieldConfig("Tick color", "The color of the ticks that represent the segment chain nodes.", "Selection Segments", true)]
        private Color _selSegmentsTickColor         = defaultSelSegmentsTickColor;
        [SerializeField][UIFieldConfig("Segment color", "The segment color.")]
        private Color _selSegmentsSegmentColor      = defaultSelSegmentsSegmentColor;
        [SerializeField][UIFieldConfig("Tick size", "The size of the ticks that represent the segment chain nodes.")][Min(0.0f)]
        private float _selSegmentsTickSize          = defaultSelSegmentsTickSize;

        [SerializeField][UIFieldConfig("Wire color", "The color used to draw the object selection box wire representation.", "Selection Box", true)]
        private Color _selBoxWireColor              = defaultSelBoxWireColor;

        [SerializeField][UIFieldConfig("Transform session", "The transform session that should automatically be activated when spawning a prefab in object selection mode via drag and drop.", "Prefab Spawn", true)]
        private ObjectSelectionPrefabTransformSession _prefabSpawnTransformSession  = defaultPrefabSpawnTransformSession;

        public bool             clickSelectAllowChildSelect     { get { return _clickSelectAllowChildSelect; } set { UndoEx.record(this); _clickSelectAllowChildSelect = value; EditorUtility.SetDirty(this); } }
        public Color            parentHighlightColor            { get { return _parentHighlightColor; } set { UndoEx.record(this); _parentHighlightColor = value; EditorUtility.SetDirty(this); } }
        public Color            childHighlightColor             { get { return _childHighlightColor; } set { UndoEx.record(this); _childHighlightColor = value; EditorUtility.SetDirty(this); } }
        public float            highlightOpacity                { get { return _highlightOpacity; } set { UndoEx.record(this); _highlightOpacity = Mathf.Clamp01(value); EditorUtility.SetDirty(this); } }
        public Color            cameraGizmoColor                { get { return _cameraGizmoColor; } set { UndoEx.record(this); _cameraGizmoColor = value; EditorUtility.SetDirty(this); } }
       
        public Color            selRectBorderColor              { get { return _selRectBorderColor; } set { UndoEx.record(this); _selRectBorderColor = value; EditorUtility.SetDirty(this); } }
        public Color            selRectFillColor                { get { return _selRectFillColor; } set { UndoEx.record(this); _selRectFillColor = value; EditorUtility.SetDirty(this); } }
        public Color            selSegmentsTickColor            { get { return _selSegmentsTickColor; } set { UndoEx.record(this); _selSegmentsTickColor = value; EditorUtility.SetDirty(this); } }
        public Color            selSegmentsSegmentColor         { get { return _selSegmentsSegmentColor; } set { UndoEx.record(this); _selSegmentsSegmentColor = value; EditorUtility.SetDirty(this); } }
        public float            selSegmentsTickSize             { get { return _selSegmentsTickSize; } set { UndoEx.record(this); _selSegmentsTickSize = Mathf.Max(0.0f, value); EditorUtility.SetDirty(this); } }
        public Color            selBoxWireColor                 { get { return _selBoxWireColor; } set { UndoEx.record(this); _selBoxWireColor = value; EditorUtility.SetDirty(this); } }

        public ObjectSelectionPrefabTransformSession prefabSpawnTransformSession { get { return _prefabSpawnTransformSession; } set { UndoEx.record(this); _prefabSpawnTransformSession = value; EditorUtility.SetDirty(this); } }

        public static bool      defaultClickSelectAllowChildSelect  { get { return false; } }
        public static Color     defaultParentHighlightColor         { get { return DefaultSystemValues.parentHighlightColor; } }
        public static Color     defaultChildHighlightColor          { get { return DefaultSystemValues.childHighlightColor; } }
        public static float     defaultHighlightOpacity             { get { return 0.0f; } }
        public static Color     defaultCameraGizmoColor             { get { return Color.gray; } }
        public static Color     defaultObjectNameLabelColor         { get { return Color.white; } }
        
        public static Color     defaultSelRectBorderColor           { get { return ColorEx.create(99, 116, 141, 255); } }
        public static Color     defaultSelRectFillColor             { get { return ColorEx.create(99, 116, 141, 255).createNewAlpha(0.3f); } }
        public static Color     defaultSelSegmentsTickColor         { get { return Color.green; } }
        public static Color     defaultSelSegmentsSegmentColor      { get { return Color.white; } }
        public static float     defaultSelSegmentsTickSize          { get { return DefaultSystemValues.tickSize; } }
        public static Color     defaultSelBoxWireColor              { get { return Color.green; } }
       
        public static ObjectSelectionPrefabTransformSession defaultPrefabSpawnTransformSession { get { return ObjectSelectionPrefabTransformSession.ModularSnap; } }

        public override void useDefaults()
        {
            clickSelectAllowChildSelect     = defaultClickSelectAllowChildSelect;
            parentHighlightColor            = defaultParentHighlightColor;
            childHighlightColor             = defaultChildHighlightColor;
            highlightOpacity                = defaultHighlightOpacity;
            cameraGizmoColor                = defaultCameraGizmoColor;
            selRectBorderColor              = defaultSelRectBorderColor;
            selRectFillColor                = defaultSelRectFillColor;
            selSegmentsTickColor            = defaultSelSegmentsTickColor;
            selSegmentsSegmentColor         = defaultSelSegmentsSegmentColor;
            selSegmentsTickSize             = defaultSelSegmentsTickSize;
            selBoxWireColor                 = defaultSelBoxWireColor;
            prefabSpawnTransformSession     = defaultPrefabSpawnTransformSession;

            EditorUtility.SetDirty(this);
        }
    }

    class ObjectSelectionPrefsProvider : SettingsProvider
    {
        public ObjectSelectionPrefsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            UI.createPrefsTitleLabel("Object Selection", rootElement);
            ObjectSelectionPrefs.instance.buildDefaultUI(rootElement, PluginSettingsUIBuildConfig.defaultConfig);

            const float labelWidth = 140.0f;
            rootElement.Query<Label>().ForEach(item => item.setChildLabelWidth(labelWidth));
        }

        [SettingsProvider]
        public static SettingsProvider create()
        {
            if (GSpawn.active == null) return null;
            return new ObjectSelectionPrefsProvider("Preferences/" + GSpawn.pluginName + "/Object Selection", SettingsScope.User);
        }
    }
}
#endif