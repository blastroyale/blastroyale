#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public enum ObjectProjectionHalfSpace
    {
        InFront = 1,
        Behind
    }

    public class ObjectProjectionSettings : PluginSettings<ObjectProjectionSettings>
    {
        [SerializeField]
        private ObjectProjectionHalfSpace           _halfSpace                  = defaultHalfSpace;
        [SerializeField]
        private bool                                _alignAxis                  = defaultAlignAxis;
        [SerializeField]
        private FlexiAxis                           _alignmentAxis              = defaultAlignmentAxis;
        [SerializeField]
        private bool                                _invertAlignmentAxis        = defaultInvertAlignmentAxis;
        [SerializeField]
        private bool                                _projectAsUnit              = defaultProjectAsUnit;
        [SerializeField]
        private bool                                _embedInSurface             = defaultEmbedInSurface;
        [SerializeField]
        private float                               _inFrontOffset              = defaultInFrontOffset;
        [SerializeField]
        private float                               _behindOffset               = defaultBehindOffset;

        public ObjectProjectionHalfSpace            halfSpace                   { get { return _halfSpace; } set { UndoEx.record(this); _halfSpace = value; EditorUtility.SetDirty(this); } }
        public bool                                 alignAxis                   { get { return _alignAxis; } set { UndoEx.record(this); _alignAxis = value; EditorUtility.SetDirty(this); } }
        public FlexiAxis                            alignmentAxis               { get { return _alignmentAxis; } set { UndoEx.record(this); _alignmentAxis = value; EditorUtility.SetDirty(this); } }
        public bool                                 invertAlignmentAxis         { get { return _invertAlignmentAxis; } set { UndoEx.record(this); _invertAlignmentAxis = value; EditorUtility.SetDirty(this); } }
        public bool                                 projectAsUnit               { get { return _projectAsUnit; } set { UndoEx.record(this); _projectAsUnit = value; EditorUtility.SetDirty(this); } }
        public bool                                 embedInSurface              { get { return _embedInSurface; } set { UndoEx.record(this); _embedInSurface = value; EditorUtility.SetDirty(this); } }
        public float                                inFrontOffset               { get { return _inFrontOffset; } set { UndoEx.record(this); _inFrontOffset = value; EditorUtility.SetDirty(this); } }
        public float                                behindOffset                { get { return _behindOffset; } set { UndoEx.record(this); _behindOffset = value; EditorUtility.SetDirty(this); } }

        public static ObjectProjectionHalfSpace     defaultHalfSpace            { get { return ObjectProjectionHalfSpace.InFront; } }
        public static bool                          defaultAlignAxis            { get { return false; } }
        public static FlexiAxis                     defaultAlignmentAxis        { get { return FlexiAxis.Y; } }
        public static bool                          defaultInvertAlignmentAxis  { get { return false; } }
        public static bool                          defaultProjectAsUnit        { get { return false; } }
        public static bool                          defaultEmbedInSurface       { get { return true; } }
        public static float                         defaultInFrontOffset        { get { return 0.0f; } }
        public static float                         defaultBehindOffset         { get { return 0.0f; } }

        public override void useDefaults()
        {
            halfSpace           = defaultHalfSpace;
            alignAxis           = defaultAlignAxis;
            alignmentAxis       = defaultAlignmentAxis;
            invertAlignmentAxis = defaultInvertAlignmentAxis;
            projectAsUnit       = defaultProjectAsUnit;
            embedInSurface      = defaultEmbedInSurface;
            inFrontOffset       = defaultInFrontOffset;
            behindOffset        = defaultBehindOffset;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            UI.createToggle("_projectAsUnit", serializedObject, "Project as unit", "Useful when projecting multiple objects that resemble a structure. If checked, all objects will be treated as a single entity " +
                "and their relative positions will be maintained.", parent);
            UI.createToggle("_embedInSurface", serializedObject, "Embed in surface", "Useful when projecting rotated objects. If checked, the objects will be embedded inside the projection surface " + 
                "by a certain amount such that they do not float above the projection surface.", parent);
            UI.createToggle("_alignAxis", serializedObject, "Align axis", "If this is checked, the objects will have their axis aligned to the projection surface normal.", parent);
            UI.createToggle("_invertAlignmentAxis", serializedObject, "Invert axis", "If this is checked, the alignment axis will be inverted.", parent);
            UI.createEnumField(typeof(FlexiAxis), "_alignmentAxis", serializedObject, "Alignment axis", "If axis alignment is turned on, this is the axis which will be used for alignment.", parent);
            UI.createEnumField(typeof(ObjectProjectionHalfSpace), "_halfSpace", serializedObject, "Half space", "Controls whether the objects will be projected in front or behind the projection plane.", parent);
            UI.createFloatField("_inFrontOffset", serializedObject, "In front offset", "The offset from the projection surface when projecting in front.", parent);
            UI.createFloatField("_behindOffset", serializedObject, "Behind offset", "The offset from the projection surface when projecting behind.", parent);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }
    }
}
#endif