#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectErasePrefs : Prefs<ObjectErasePrefs>
    {
        [SerializeField][UIFieldConfig("Border color", "The color used to draw the border of the 2D erase brush.", "2D Brush", false)]
        private Color       _brush2DBorderColor             = defaultBrush2DBorderColor;
        [SerializeField][UIFieldConfig("Fill color", "The color used to fill the 2D erase brush.")]
        private Color       _brush2DFillColor               = defaultBrush2DFillColor;
        [SerializeField][UIFieldConfig("Border color", "The color used to draw the border of the 3D erase brush.", "3D Brush", true)]
        private Color       _brush3DBorderColor             = defaultBrush3DBorderColor;
        [SerializeField][UIFieldConfig("Height indicator color", "The erase height indicator color.")]
        private Color       _brush3DHeightIndicatorColor    = defaultBrush3DHeightIndicator;

        public Color        brush2DBorderColor              { get { return _brush2DBorderColor; } set { UndoEx.record(this); _brush2DBorderColor = value; EditorUtility.SetDirty(this); } }
        public Color        brush2DFillColor                { get { return _brush2DFillColor; } set { UndoEx.record(this); _brush2DFillColor = value; EditorUtility.SetDirty(this); } }
        public Color        brush3DBorderColor              { get { return _brush3DBorderColor; } set { UndoEx.record(this); _brush3DBorderColor = value; EditorUtility.SetDirty(this); } }     
        public Color        brush3DHeightIndicatorColor     { get { return _brush3DHeightIndicatorColor; } set { UndoEx.record(this); _brush3DHeightIndicatorColor = value; EditorUtility.SetDirty(this); } }

        public static Color defaultBrush2DBorderColor       { get { return Color.black; } }
        public static Color defaultBrush2DFillColor         { get { return Color.red.createNewAlpha(0.2f); } }
        public static Color defaultBrush3DBorderColor       { get { return Color.red; } }
        public static Color defaultBrush3DHeightIndicator   { get { return Color.red; } }

        public override void useDefaults()
        {
            brush2DBorderColor              = defaultBrush2DBorderColor;
            brush2DFillColor                = defaultBrush2DFillColor;
            brush3DBorderColor              = defaultBrush3DBorderColor;
            brush3DHeightIndicatorColor     = defaultBrush3DHeightIndicator;

            EditorUtility.SetDirty(this);
        }
    }

    class ObjectErasePrefsPrefsProvider : SettingsProvider
    {
        public ObjectErasePrefsPrefsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            UI.createPrefsTitleLabel("Object Erase", rootElement);
            ObjectErasePrefs.instance.buildDefaultUI(rootElement, PluginSettingsUIBuildConfig.defaultConfig);

            const float labelWidth = 140.0f;
            rootElement.Query<Label>().ForEach(item => item.setChildLabelWidth(labelWidth));
        }

        [SettingsProvider]
        public static SettingsProvider create()
        {
            if (GSpawn.active == null) return null;
            return new ObjectErasePrefsPrefsProvider("Preferences/" + GSpawn.pluginName + "/Object Erase", SettingsScope.User);
        }
    }
}
#endif