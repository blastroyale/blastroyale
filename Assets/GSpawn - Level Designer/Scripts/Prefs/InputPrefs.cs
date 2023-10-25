#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class InputPrefs : Prefs<InputPrefs>
    {
        //[SerializeField][UIFieldConfig("Rotation axes type", "Allows you to specify the rotation axes that are used when rotating entities (i.e. world axes, grid axes).", "General", false)]
        //private RotationAxesType        _rotationAxesType                   = defaultRotationAxesType;
        [SerializeField][UIFieldConfig("Keyboard X rotation step", "The amount of rotation applied around the X axis when rotating entities with the keyboard.", "Keyboard", true)][Min(1e-2f)]
        private float                   _keyboardXRotationStep              = defaultKeyboardXRotationStep;
        [SerializeField][UIFieldConfig("Keyboard Y rotation step", "The amount of rotation applied around the Y axis when rotating entities with the keyboard.")][Min(1e-2f)]
        private float                   _keyboardYRotationStep              = defaultKeyboardYRotationStep;
        [SerializeField][UIFieldConfig("Keyboard Z rotation step", "The amount of rotation applied around the Z axis when rotating entities with the keyboard.")][Min(1e-2f)]
        private float                   _keyboardZRotationStep              = defaultKeyboardZRotationStep;
        [SerializeField][UIFieldConfig("Offset sensitivity", "Mouse sensitivity when offsetting entities (e.g. when offsetting objects from a surface).", "Mouse", true)][Range(1e-2f, 10.0f)]
        private float                   _mouseOffsetSensitivity             = defaultMouseOffsetSensitivity;
        [SerializeField][UIFieldConfig("Rotation sensitivity", "Mouse sensitivity when rotating entities.")][Range(1e-2f, 10.0f)]
        private float                   _mouseRotationSensitivity           = defaultMouseRotationSensitivity;
        [SerializeField][UIFieldConfig("Scroll rotation step", "The amount of rotation applied when rotating entities with the mouse scroll wheel.")][Min(1e-2f)]
        private float                   _scrollRotationStep                 = defaultScrollRotationStep;
        [SerializeField][UIFieldConfig("Scale sensitivity", "Mouse sensitivity when scaling entities.")][Range(1e-2f, 10.0f)]
        private float                   _mouseScaleSensitivity              = defaultMouseScaleSensitivity;
        [SerializeField][UIFieldConfig("Log shortcuts", "If checked, shortcuts will be displayed on the screen when they are used. Useful when recording video tutorials.", "Shortcut Logging", true)]
        private bool                    _logShortcuts                       = defaultLogShortcuts;
        [SerializeField][UIFieldConfig("Log text color", "The text color used when drawing the shortcuts on the screen.")]
        private Color                   _shortcutLogTextColor               = defaultShortcutLogTextColor;
        [SerializeField][UIFieldConfig("Log font size", "The font size used when drawing the shortcuts on the screen.")][Min(10)]
        private int                     _shortcutLogFontSize                = defaultShortcutLogFontSize;

        //public RotationAxesType         rotationAxesType                    { get { return _rotationAxesType; } set { UndoEx.record(this); _rotationAxesType = value; EditorUtility.SetDirty(this); } }
        public float                    keyboardXRotationStep               { get { return _keyboardXRotationStep; } set { UndoEx.record(this); _keyboardXRotationStep = Mathf.Max(1e-2f, value); EditorUtility.SetDirty(this); } }
        public float                    keyboardYRotationStep               { get { return _keyboardYRotationStep; } set { UndoEx.record(this); _keyboardYRotationStep = Mathf.Max(1e-2f, value); EditorUtility.SetDirty(this); } }
        public float                    keyboardZRotationStep               { get { return _keyboardZRotationStep; } set { UndoEx.record(this); _keyboardZRotationStep = Mathf.Max(1e-2f, value); EditorUtility.SetDirty(this); } }
        public float                    mouseOffsetSensitivity              { get { return _mouseOffsetSensitivity; } set { UndoEx.record(this); _mouseOffsetSensitivity = Mathf.Clamp(value, 1e-2f, 10.0f); EditorUtility.SetDirty(this); } }
        public float                    mouseRotationSensitivity            { get { return _mouseRotationSensitivity; } set { UndoEx.record(this); _mouseRotationSensitivity = Mathf.Clamp(value, 1e-2f, 10.0f); EditorUtility.SetDirty(this); } }
        public float                    scrollRotationStep                  { get { return _scrollRotationStep; } set { UndoEx.record(this); _scrollRotationStep = Mathf.Max(1e-2f, value); EditorUtility.SetDirty(this); } }
        public float                    mouseScaleSensitivity               { get { return _mouseScaleSensitivity; } set { UndoEx.record(this); _mouseScaleSensitivity = Mathf.Clamp(value, 1e-2f, 10.0f); EditorUtility.SetDirty(this); } }
        public bool                     logShortcuts                        { get { return _logShortcuts; } set { UndoEx.record(this); _logShortcuts = value; EditorUtility.SetDirty(this); } }
        public Color                    shortcutLogTextColor                { get { return _shortcutLogTextColor; } set { UndoEx.record(this); _shortcutLogTextColor = value; EditorUtility.SetDirty(this); } }
        public int                      shortcutLogFontSize                 { get { return _shortcutLogFontSize; } set { UndoEx.record(this); _shortcutLogFontSize = Mathf.Clamp(value, 10, 50); EditorUtility.SetDirty(this); } }

        public static RotationAxesType  defaultRotationAxesType             { get { return RotationAxesType.Grid; } }
        public static float             defaultMouseOffsetSensitivity       { get { return 0.05f; } }
        public static float             defaultMouseRotationSensitivity     { get { return 1.0f; } }
        public static float             defaultMouseScaleSensitivity        { get { return 0.01f; } }
        public static float             defaultKeyboardXRotationStep        { get { return 90.0f; } }
        public static float             defaultKeyboardYRotationStep        { get { return 90.0f; } }
        public static float             defaultKeyboardZRotationStep        { get { return 90.0f; } }
        public static float             defaultScrollRotationStep           { get { return 90.0f; } }
        public static bool              defaultLogShortcuts                 { get { return false; } }
        public static Color             defaultShortcutLogTextColor         { get { return Color.green; } }
        public static int               defaultShortcutLogFontSize          { get { return 30; } }

        public override void useDefaults()
        {
            //rotationAxesType            = defaultRotationAxesType;
            keyboardXRotationStep       = defaultKeyboardXRotationStep;
            keyboardYRotationStep       = defaultKeyboardYRotationStep;
            keyboardZRotationStep       = defaultKeyboardZRotationStep;
            mouseOffsetSensitivity      = defaultMouseOffsetSensitivity;
            mouseRotationSensitivity    = defaultMouseRotationSensitivity;
            scrollRotationStep          = defaultScrollRotationStep;
            mouseScaleSensitivity       = defaultMouseScaleSensitivity;
            logShortcuts                = defaultLogShortcuts;
            shortcutLogTextColor        = defaultShortcutLogTextColor;
            shortcutLogFontSize         = defaultShortcutLogFontSize;

            EditorUtility.SetDirty(this);
        }

        public Vector3 getRotationAxis(int axisIndex)
        {
            if (axisIndex == 0) return PluginScene.instance.grid.right;
            else if (axisIndex == 1) return PluginScene.instance.grid.up;
            else return PluginScene.instance.grid.look;

/*
            if (_rotationAxesType == RotationAxesType.World)
            {
                if (axisIndex == 0) return Vector3.right;
                else if (axisIndex == 1) return Vector3.up;
                else return Vector3.forward;
            }
            else
            {
                if (axisIndex == 0) return PluginScene.instance.grid.right;
                else if (axisIndex == 1) return PluginScene.instance.grid.up;
                else return PluginScene.instance.grid.look;
            }*/
        }
    }

    class InputPrefsProvider : SettingsProvider
    {
        public InputPrefsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            UI.createPrefsTitleLabel("Input", rootElement);
            InputPrefs.instance.buildDefaultUI(rootElement, PluginSettingsUIBuildConfig.defaultConfig);

            const float labelWidth = 155.0f;
            rootElement.Query<Label>().ForEach(item => item.setChildLabelWidth(labelWidth));
        }

        [SettingsProvider]
        public static SettingsProvider create()
        {
            if (GSpawn.active == null) return null;
            return new InputPrefsProvider("Preferences/" + GSpawn.pluginName + "/Input", SettingsScope.User);
        }
    }
}
#endif