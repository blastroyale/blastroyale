#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;

namespace GSpawn
{
    public enum MeshCombinePivot
    {
        Center = 0,
        TopCenter,
        BottomCenter,
        LeftCenter,
        RightCenter,
        FrontCenter,
        BackCenter
    }

    public enum MeshCombineIndexFormat
    {
        UInt16 = 0,
        UInt32
    }

    public class MeshCombineSettings : PluginSettings<MeshCombineSettings>
    {
        private static MeshCombineSettings      _instance;

        [NonSerialized]
        private MeshCombineUI                   _ui;

        [SerializeField]
        private bool                            _combineStaticMeshes                = defaultCombineStaticMeshes;
        [SerializeField]
        private bool                            _combineDynamicMeshes               = defaultCombineDynamicMeshes;
        [SerializeField]
        private bool                            _combineLODs                        = defaultCombineLODs;
        [SerializeField]
        private int                             _lodIndex                           = defaultLODIndex;
        [SerializeField]
        private bool                            _combineAsStatic                    = defaultCombineAsStatic;
        [SerializeField]
        private MeshCombinePivot                _combinedMeshPivot                  = defaultCombinedMeshPivot;
        [SerializeField]
        private MeshCombineIndexFormat          _combinedIndexFormat                = defaultCombinedIndexFormat;
        [SerializeField]
        private bool                            _ignoreMultiLevelHierarchies        = defaultIgnoreMultiLevelHierarchies;
        [SerializeField]
        private bool                            _generateLightmapUVs                = defaultGenerateLightmapUVs;
        [SerializeField]
        private bool                            _combinedMeshesAreReadable          = defaultCombinedMeshesAreReadable;
        [SerializeField]
        private bool                            _disableSourceRenderers             = defaultDisableSourceRenderers;
        [SerializeField]    
        private string                          _combinedMeshObjectBaseName         = defaultCombinedMeshObjectBaseName;
        [SerializeField]
        private string                          _combinedMeshBaseName               = defaultCombinedMeshBaseName;
        [SerializeField]
        private string                          _combinedMeshFolder                 = defaultCombinedMeshFolder;

        public bool                             combineStaticMeshes                 { get { return _combineStaticMeshes; } set { UndoEx.record(this); _combineStaticMeshes = value; EditorUtility.SetDirty(this); } }
        public bool                             combineDynamicMeshes                { get { return _combineDynamicMeshes; } set { UndoEx.record(this); _combineDynamicMeshes = value; EditorUtility.SetDirty(this); } }
        public bool                             combineLODs                         { get { return _combineLODs; } set { UndoEx.record(this); _combineLODs = value; EditorUtility.SetDirty(this); } }
        public int                              lodIndex                            { get { return _lodIndex; } set { UndoEx.record(this); _lodIndex = Math.Clamp(value, 0, 7); EditorUtility.SetDirty(this); } }
        public bool                             combineAsStatic                     { get { return _combineAsStatic; } set { UndoEx.record(this); _combineAsStatic = value; EditorUtility.SetDirty(this); } }
        public MeshCombinePivot                 combinedMeshPivot                   { get { return _combinedMeshPivot; } set { UndoEx.record(this); _combinedMeshPivot = value; EditorUtility.SetDirty(this); } }
        public MeshCombineIndexFormat           combinedIndexFormat                 { get { return _combinedIndexFormat; } set { UndoEx.record(this); _combinedIndexFormat = value; EditorUtility.SetDirty(this); } }
        public bool                             ignoreMultiLevelHierarchies         { get { return _ignoreMultiLevelHierarchies; } set { UndoEx.record(this); _ignoreMultiLevelHierarchies = value; EditorUtility.SetDirty(this); } }
        public bool                             generateLightmapUVs                 { get { return _generateLightmapUVs; } set { UndoEx.record(this); _generateLightmapUVs = value; EditorUtility.SetDirty(this); } }
        public bool                             combinedMeshesAreReadable           { get { return _combinedMeshesAreReadable; } set { UndoEx.record(this); _combinedMeshesAreReadable = value; EditorUtility.SetDirty(this); } }
        public bool                             disableSourceRenderers              { get { return _disableSourceRenderers; } set { UndoEx.record(this); _disableSourceRenderers = value; EditorUtility.SetDirty(this); } }
        public string                           combinedMeshObjectBaseName
        {
            get { return _combinedMeshObjectBaseName; }
            set
            {
                UndoEx.record(this);
                _combinedMeshObjectBaseName = value;
                EditorUtility.SetDirty(this);
            }
        }
        public string                           combinedMeshBaseName 
        { 
            get { return _combinedMeshBaseName; } 
            set 
            { 
                UndoEx.record(this); 
                _combinedMeshBaseName = string.IsNullOrEmpty(value) ? defaultCombinedMeshBaseName : value;
                EditorUtility.SetDirty(this); 
            } 
        }
        public string                           combinedMeshFolder 
        { 
            get { return _combinedMeshFolder; } 
            set 
            {
                if (!PluginFolders.validateFolderPathForClientUsage(value)) return;

                UndoEx.record(this); 
                _combinedMeshFolder = string.IsNullOrEmpty(value) ? defaultCombinedMeshFolder : value;
                EditorUtility.SetDirty(this); 
            } 
        }
        public MeshCombineUI                    ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<MeshCombineUI>(PluginFolders.settings);

                return _ui;
            }
        }

        public static MeshCombineSettings       instance
        {
            get
            {
                if (_instance == null) _instance = AssetDbEx.loadScriptableObject<MeshCombineSettings>(PluginFolders.settings);
                return _instance;
            }
        }

        public static bool                      defaultCombineStaticMeshes              { get { return true; } }
        public static bool                      defaultCombineDynamicMeshes             { get { return false; } }
        public static bool                      defaultCombineLODs                      { get { return false; } }
        public static int                       defaultLODIndex                         { get { return 0; } }
        public static bool                      defaultCombineAsStatic                  { get { return true; } }
        public static MeshCombinePivot          defaultCombinedMeshPivot                { get { return MeshCombinePivot.Center; } }
        public static MeshCombineIndexFormat    defaultCombinedIndexFormat              { get { return MeshCombineIndexFormat.UInt32; } }
        public static bool                      defaultIgnoreMultiLevelHierarchies      { get { return false; } }
        public static bool                      defaultGenerateLightmapUVs              { get { return false; } }
        public static bool                      defaultCombinedMeshesAreReadable        { get { return false; } }
        public static bool                      defaultDisableSourceRenderers           { get { return true; } }
        public static string                    defaultCombinedMeshObjectBaseName       { get { return GSpawn.pluginName + "_Combined_"; } }
        public static string                    defaultCombinedMeshBaseName             { get { return GSpawn.pluginName + "_CombinedMesh_"; } }
        public static string                    defaultCombinedMeshFolder               { get { return "Assets/GSpawn Combined Meshes"; } }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 220.0f;

            var toggleCtrl = UI.createToggle("_combineStaticMeshes", serializedObject, "Combine static meshes", "If this is checked, meshes belonging " + 
                "to static objects will participate in the combine process.", parent);
            toggleCtrl.setChildLabelWidth(labelWidth);

            toggleCtrl = UI.createToggle("_combineDynamicMeshes", serializedObject, "Combine dynamic meshes", "If this is checked, meshes belonging " +
                "to dynamic objects will participate in the combine process.", parent);
            toggleCtrl.setChildLabelWidth(labelWidth);

            toggleCtrl = UI.createToggle("_combineLODs", serializedObject, "Combine LODs", "If this is checked, meshes belonging " +
                "to LOD groups will participate in the combine process.", parent);
            toggleCtrl.setChildLabelWidth(labelWidth);

            var lodIndexField = UI.createIntegerField("_lodIndex", serializedObject, "LOD index", "If LOD combine is turned on, this represents the " + 
                "index of the LOD whose mesh will be used in the combine process.", 0, 7, parent);
            lodIndexField.setChildLabelWidth(labelWidth);
            lodIndexField.setDisplayVisible(combineLODs);

            toggleCtrl.RegisterValueChangedCallback(p =>
            {
                lodIndexField.setDisplayVisible(combineLODs);
            });

            toggleCtrl = UI.createToggle("_combineAsStatic", serializedObject, "Combine as static", "If this is checked, meshes that result " +
                "from the combine process will be marked as static.", parent);
            toggleCtrl.setChildLabelWidth(labelWidth);

            var combMeshPivotField = UI.createEnumField(typeof(MeshCombinePivot), "_combinedMeshPivot", serializedObject,
                "Combined mesh pivot", "The pivot that will be calculated for all combined meshes.", parent);
            combMeshPivotField.setChildLabelWidth(labelWidth);

            var combIndexFormatField = UI.createEnumField(typeof(MeshCombineIndexFormat), "_combinedIndexFormat", serializedObject,
                "Combined index format", "", parent);
            combIndexFormatField.setChildLabelWidth(labelWidth);

            UI.createRowSeparator(parent);

            toggleCtrl = UI.createToggle("_ignoreMultiLevelHierarchies", serializedObject, "Ignore multi-level hierarchies", "If this is checked, objects that " +
                "belong to multi-level hierarchies will be ignored and their meshes will not participate in the combine process.", parent);
            toggleCtrl.setChildLabelWidth(labelWidth);

            toggleCtrl = UI.createToggle("_generateLightmapUVs", serializedObject, "Generate lightmap UVs", "If this is checked, lightmap UVs will be generated for all combined meshes.", parent);
            toggleCtrl.setChildLabelWidth(labelWidth);

            toggleCtrl = UI.createToggle("_combinedMeshesAreReadable", serializedObject, "Combined meshes are readable", "If this is checked, the combined meshes will maintain a system memory copy " + 
                "of the vertex data. If you don't need to read the mesh data from script, you should leave this unchecked to free up system memory.", parent);
            toggleCtrl.setChildLabelWidth(labelWidth);

            UI.createRowSeparator(parent);

            toggleCtrl = UI.createToggle("_disableSourceRenderers", serializedObject, "Disable source renderers", "If checked, the plugin will disable the renderers " +
                "in the source objects after the mesh combine processed is finished.", parent);
            toggleCtrl.setChildLabelWidth(labelWidth);

            UI.createRowSeparator(parent);

            var combMeshObjectBaseNameField = UI.createTextField("_combinedMeshObjectBaseName", serializedObject, "Combined mesh object base name", 
                "For each mesh that is created during the combine process, the plugin will create a scene object which references that mesh. " + 
                "This field holds the base name of all objects that are created.", parent);
            combMeshObjectBaseNameField.setChildLabelWidth(labelWidth);

            var combMeshBaseNameField = UI.createTextField("_combinedMeshBaseName", serializedObject, "Combined mesh base name", "Combined meshes are saved as assets. When a mesh " + 
                "asset is about to be saved, its name will be set to the value of this field succeeded by a string that uniquely identifies the mesh.", parent);
            combMeshBaseNameField.setChildLabelWidth(labelWidth);
            combMeshBaseNameField.RegisterValueChangedCallback(p => 
            {
                if (string.IsNullOrEmpty(p.newValue))
                {
                    combMeshBaseNameField.value = defaultCombinedMeshBaseName;
                }
            });

            var combMeshFolderField = UI.createTextField("_combinedMeshFolder", serializedObject, "Combined mesh folder", "Combined mesh assets are saved in this folder. If the folder " + 
                "doesn't exist it will automatically be created.", parent);
            combMeshFolderField.setChildLabelWidth(labelWidth);
            combMeshFolderField.registerDragAndDropCallback(() =>
            {
                if (!PluginDragAndDrop.initiatedByPlugin && PluginDragAndDrop.unityPaths.Length != 0 &&
                PluginFolders.validateFolderPathForClientUsage(PluginDragAndDrop.unityPaths[0]))
                {
                    combMeshFolderField.value = PluginDragAndDrop.unityPaths[0];
                }
            }, DragAndDropVisualMode.Generic);
            combMeshFolderField.RegisterValueChangedCallback(p =>
            {
                if (string.IsNullOrEmpty(p.newValue))
                {
                    combMeshFolderField.value = defaultCombinedMeshFolder;
                }
            });

            UI.createUseDefaultsButton(() => { useDefaults(); }, parent);
        }

        public override void useDefaults()
        {
            combineStaticMeshes         = defaultCombineStaticMeshes;
            combineDynamicMeshes        = defaultCombineDynamicMeshes;
            combineLODs                 = defaultCombineLODs;
            lodIndex                    = defaultLODIndex;
            combineAsStatic             = defaultCombineAsStatic;
            combinedMeshPivot           = defaultCombinedMeshPivot;
            combinedIndexFormat         = defaultCombinedIndexFormat;
            ignoreMultiLevelHierarchies = defaultIgnoreMultiLevelHierarchies;
            generateLightmapUVs         = defaultGenerateLightmapUVs;
            combinedMeshesAreReadable   = defaultCombinedMeshesAreReadable;
            disableSourceRenderers      = defaultDisableSourceRenderers;
            combinedMeshObjectBaseName  = defaultCombinedMeshObjectBaseName;
            combinedMeshBaseName        = defaultCombinedMeshBaseName;
            combinedMeshFolder          = defaultCombinedMeshFolder;

            EditorUtility.SetDirty(this);
        }

        private void OnDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_ui);
        }
    }
}
#endif