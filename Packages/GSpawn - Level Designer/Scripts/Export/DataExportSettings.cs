#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class DataExportSettings : PluginSettings<DataExportSettings>
    {
        [SerializeField]
        private bool        _exportPrefs                                = defaultExportPrefs;
        [SerializeField]
        private bool        _exportGeneralSettings                      = defaultExportGeneralSettings;
        [SerializeField]
        private bool        _exportShortcutProfiles                     = defaultExportShortcutProfiles;
        [SerializeField]
        private bool        _exportIntPatterns                          = defaultExportIntPatterns;

        public bool exportPrefs                                         { get { return _exportPrefs; } set { UndoEx.record(this); _exportPrefs = value; EditorUtility.SetDirty(this); } }
        public bool exportGeneralSettings                               { get { return _exportGeneralSettings; } set { UndoEx.record(this); _exportGeneralSettings = value; EditorUtility.SetDirty(this); } }
        public bool exportShortcutProfiles                              { get { return _exportShortcutProfiles; } set { UndoEx.record(this); _exportShortcutProfiles = value; EditorUtility.SetDirty(this); } }
        public bool exportIntPatterns                                   { get { return _exportIntPatterns; } set { UndoEx.record(this); _exportIntPatterns = value; EditorUtility.SetDirty(this); } }

        public static DataExportSettings instance                               { get { return GSpawn.active.dataExportSettings; } }

        public static bool defaultExportPrefs                                   { get { return true; } }
        public static bool defaultExportGeneralSettings                         { get { return true; } }
        public static bool defaultExportShortcutProfiles                        { get { return true; } }
        public static bool defaultExportIntPatterns                             { get { return true; } }

        public void setExportAll(bool export)
        {
            exportPrefs                                 = export;
            exportGeneralSettings                       = export;
            exportShortcutProfiles                      = export;
            exportIntPatterns                           = export;
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 250.0f;

            var ctrl = UI.createToggle("_exportPrefs", serializedObject, "Export preferences", "Export preferences.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_exportGeneralSettings", serializedObject, "Export general settings", "Export general settings (e.g. modular snap settings, props spawn settings, selection settings etc).", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_exportShortcutProfiles", serializedObject, "Export shortcut profiles", "Export shortcut profiles.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            ctrl = UI.createToggle("_exportIntPatterns", serializedObject, "Export integer patterns", "Export integer patterns.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public override void useDefaults()
        {
            exportPrefs                                 = defaultExportPrefs;
            exportGeneralSettings                       = defaultExportGeneralSettings;
            exportShortcutProfiles                      = defaultExportShortcutProfiles;
            exportIntPatterns                           = defaultExportIntPatterns;

            EditorUtility.SetDirty(this);
        }
    }
}
#endif