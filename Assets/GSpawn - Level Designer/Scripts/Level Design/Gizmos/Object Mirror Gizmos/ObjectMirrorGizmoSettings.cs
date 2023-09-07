#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectMirrorGizmoSettings : PluginSettings<ObjectMirrorGizmoSettings>
    {
        [SerializeField]
        private bool            _useXYPlane             = defaultUseXYPlane;
        [SerializeField]
        private bool            _useYZPlane             = defaultUseYZPlane;
        [SerializeField]
        private bool            _useZXPlane             = defaultUseZXPlane;
        [SerializeField]
        private bool            _mirrorRotation         = defaultMirrorRotation;
        [SerializeField]
        private bool            _mirrorSpanning         = defaultMirrorSpanning;
        [SerializeField]
        private Vector3         _moveSnapStep           = defaultMoveSnapStep;
        [SerializeField]
        private bool            _hasRotationHandles     = defaultHasRotationHandles;
        [SerializeField]
        private Vector3         _rotationSnapStep       = defaultRotationSnapStep;

        public bool             useXYPlane                  { get { return _useXYPlane; } set { UndoEx.record(this); _useXYPlane = value; EditorUtility.SetDirty(this); } }
        public bool             useYZPlane                  { get { return _useYZPlane; } set { UndoEx.record(this); _useYZPlane = value; EditorUtility.SetDirty(this); } }
        public bool             useZXPlane                  { get { return _useZXPlane; } set { UndoEx.record(this); _useZXPlane = value; EditorUtility.SetDirty(this); } }
        public bool             mirrorRotation              { get { return _mirrorRotation; } set { UndoEx.record(this); _mirrorRotation = value; EditorUtility.SetDirty(this); } }
        public bool             mirrorSpanning              { get { return _mirrorSpanning; } set { UndoEx.record(this); _mirrorSpanning = value; EditorUtility.SetDirty(this); } }
        public Vector3          moveSnapStep                { get { return _moveSnapStep; } set { UndoEx.record(this); _moveSnapStep = Vector3.Max(value, Vector3Ex.create(0.0001f)); EditorUtility.SetDirty(this); } }
        public bool             hasRotationHandles          { get { return _hasRotationHandles; } set { UndoEx.record(this); _hasRotationHandles = value; EditorUtility.SetDirty(this); } }
        public Vector3          rotationSnapStep            { get { return _rotationSnapStep; } set { UndoEx.record(this); _rotationSnapStep = Vector3.Max(value, Vector3Ex.create(0.0001f)); EditorUtility.SetDirty(this); } }

        public static bool      defaultUseXYPlane           { get { return true; } }
        public static bool      defaultUseYZPlane           { get { return false; } }
        public static bool      defaultUseZXPlane           { get { return false; } }
        public static bool      defaultMirrorRotation       { get { return false; } }
        public static bool      defaultMirrorSpanning       { get { return false; } }
        public static Vector3   defaultMoveSnapStep         { get { return Vector3.one; } }
        public static bool      defaultHasRotationHandles   { get { return false; } }
        public static Vector3   defaultRotationSnapStep     { get { return Vector3Ex.create(90.0f); } }

        public void buildUI(VisualElement parent)
        {           
            UI.createToggle("_mirrorRotation", serializedObject, "Mirror rotation", "If this is checked, the rotation of the objects will also be mirrored.", parent);
            UI.createToggle("_mirrorSpanning", serializedObject, "Mirror spanning", "If checked, objects whose bounds span the mirror plane will be mirrored. Uncheck this " +
                "if you would like to prevent spanning objects from being mirrored.", parent);

            UI.createUISectionRowSeparator(parent);
            UI.createVector3Field("_moveSnapStep", serializedObject, "Move snap step", "The move step used when snapping is enabled.", Vector3Ex.create(0.0001f), parent);
            var btn         = new Button();
            parent.Add(btn);
            btn.text        = "Use grid cell size";
            btn.tooltip     = "Sets the move step to be equal to the scene grid cell size. Note: You will need to press this button again if you decide to change the grid cell size later.";
            btn.style.width = UIValues.useDefaultsButtonWidth;
            btn.clicked     += () => { moveSnapStep = PluginScene.instance.grid.activeSettings.cellSize; };

            UI.createUISectionRowSeparator(parent);
            UI.createToggle("_hasRotationHandles", serializedObject, "Rotation handles", "If checked, the mirror gizmo will draw rotation handles that can be used to rotate the mirror.", parent);
            UI.createVector3Field("_rotationSnapStep", serializedObject, "Rotation snap step", "The rotation step used when snapping is enabled.", Vector3Ex.create(0.0001f), parent);

            UI.createUISectionRowSeparator(parent);
            IMGUIContainer imGUIContainer = new IMGUIContainer();
            imGUIContainer.style.marginLeft = 4.0f;
            parent.Add(imGUIContainer);
            imGUIContainer.onGUIHandler = () =>
            {
                EditorUIEx.objectMirrorGizmoPlaneToggle(this);
            };

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {
            useXYPlane          = defaultUseXYPlane;
            useYZPlane          = defaultUseYZPlane;
            useZXPlane          = defaultUseZXPlane;
            mirrorRotation      = defaultMirrorRotation;
            mirrorSpanning      = defaultMirrorSpanning;
            moveSnapStep        = defaultMoveSnapStep;
            hasRotationHandles  = defaultHasRotationHandles;
            rotationSnapStep    = defaultRotationSnapStep;

            EditorUtility.SetDirty(this);
        }

        public void copy(ObjectMirrorGizmoSettings src)
        {
            if (src == this) return;

            useXYPlane          = src.useXYPlane;
            useYZPlane          = src.useYZPlane;
            useZXPlane          = src.useZXPlane;
            mirrorRotation      = src.mirrorRotation;
            mirrorSpanning      = src.mirrorSpanning;
            moveSnapStep        = src.moveSnapStep;
            hasRotationHandles  = src.hasRotationHandles;
            rotationSnapStep    = src.rotationSnapStep;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif