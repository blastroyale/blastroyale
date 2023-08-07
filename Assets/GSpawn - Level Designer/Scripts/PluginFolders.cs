#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GSpawn
{
    public static class PluginFolders
    {
        public static string data                                   { get { return FileSystem.findFolderPath("GSpawn - Level Designer") + "/Data"; } }
        public static string settings                               { get { return data + "/Settings"; } }
        public static string preferences                            { get { return data + "/Preferences"; } }
        public static string profiles                               { get { return data + "/Profiles"; } }
        public static string gridProfiles                           { get { return profiles + "/Grid"; } }
        public static string shortcutProfiles                       { get { return profiles + "/Shortcuts"; } }
        public static string randomPrefabProfiles                   { get { return profiles + "/Random Prefabs"; } }
        public static string intRangePrefabProfiles                 { get { return profiles + "/Int Range Prefabs"; } }
        public static string scatterBrushPrefabProfiles             { get { return profiles + "/Scatter Brush Prefabs"; } }
        public static string tileRuleProfiles                       { get { return profiles + "/Tile Rules"; } }
        public static string modularWallPrefabProfiles              { get { return profiles + "/Modular Walls"; } }
        public static string curvePrefabProfiles                    { get { return profiles + "/Curve Prefabs"; } }
        public static string prefabLibProfiles                      { get { return profiles + "/Prefab Libs"; ; } }
        public static string pluginObjectLayers                     { get { return data + "/Plugin Object Layers"; } }
        public static string objectGroups                           { get { return data + "/Object Groups"; } }
        public static string intPatternProfiles                     { get { return profiles + "/Int Patterns"; } }
        public static string segmentsObjectSpawnSettingsProfiles    { get { return profiles + "/Segments Object Spawn"; } }
        public static string boxObjectSpawnSettingsProfiles         { get { return profiles + "/Box Object Spawn"; } }
        public static string curveObjectSpawnSettingsProfiles       { get { return profiles + "/Curve Object Spawn"; } }
        public static string pluginInternal                         { get { return data + "/Internal"; } }

        public static bool validateFolderPathForClientUsage(string path)
        {
            if (path == data || isChildOfDataFolder(path))
            {
                Debug.LogWarning("The Data folder or children in the Data folder can not be used for this purpose.");
                return false;
            }

            return true;
        }

        public static bool isDataFolderValid()
        {
            if (!FileSystem.folderExists(data)) return false;
            else
            {
                if (!FileSystem.folderExists(settings)) return false;
                if (!FileSystem.folderExists(preferences)) return false;
                if (!FileSystem.folderExists(profiles)) return false;
                if (!FileSystem.folderExists(gridProfiles)) return false;
                if (!FileSystem.folderExists(shortcutProfiles)) return false;
                if (!FileSystem.folderExists(randomPrefabProfiles)) return false;
                if (!FileSystem.folderExists(intRangePrefabProfiles)) return false;
                if (!FileSystem.folderExists(scatterBrushPrefabProfiles)) return false;
                if (!FileSystem.folderExists(tileRuleProfiles)) return false;
                if (!FileSystem.folderExists(modularWallPrefabProfiles)) return false;
                if (!FileSystem.folderExists(curvePrefabProfiles)) return false;
                if (!FileSystem.folderExists(prefabLibProfiles)) return false;
                if (!FileSystem.folderExists(pluginObjectLayers)) return false;
                if (!FileSystem.folderExists(objectGroups)) return false;
                if (!FileSystem.folderExists(intPatternProfiles)) return false;
                if (!FileSystem.folderExists(segmentsObjectSpawnSettingsProfiles)) return false;
                if (!FileSystem.folderExists(boxObjectSpawnSettingsProfiles)) return false;
                if (!FileSystem.folderExists(curveObjectSpawnSettingsProfiles)) return false;
                if (!FileSystem.folderExists(pluginInternal)) return false;
            }

            return true;
        }

        public static bool isChildOfDataFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.Length > data.Length && path.Contains(data);
        }

        public static void createDataFolderAndAssetsIfMissing()
        {
            PluginProgressDialog.begin(GSpawn.pluginName + " - Processing Data Folder & Assets");

            createSettingsFolderAndAssets();
            createPluginInternalFolderAndAssets();
            createPrefsFolderAndAssets();
            createGridProfilesFolderAndAssets();
            createShortcutProfilesFolderAndAssets();
            createRandomPrefabProfilesFolderAndAssets();
            createIntRangePrefabProfilesFolderAndAssets();
            createScatterBrushPrefabProfilesFolderAndAssets();
            createModularWallPrefabProfilesFolderAndAssets();
            createTileRuleProfilesFolderAndAssets();
            createCurvePrefabProfilesFolderAndAssets();
            createPrefabLibProfilesFolderAndAssets();
            createPluginObjectLayersFolderAndAssets();
            createObjectGroupsFolderAndAssets();
            createIntPatternProfilesFolderAndAssets();
            createSegmentsObjectSpawnSettingsProfilesFolderAndAssets();
            createBoxObjectSpawnSettingsProfilesFolderAndAssets();
            createCurveObjectSpawnSettingsProfilesFolderAndAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PluginProgressDialog.end();
        }

        private static void createPluginInternalFolderAndAssets()
        {
            if (FileSystem.folderExists(pluginInternal)) return;

            FileSystem.createFolder(pluginInternal);
            AssetDbEx.createScriptableObject_NoSave<PluginInstanceData>(pluginInternal, typeof(PluginInstanceData).Name);
            AssetDbEx.createScriptableObject_NoSave<PrefabDataDb>(pluginInternal, typeof(PrefabDataDb).Name);
            AssetDbEx.createScriptableObject_NoSave<PrefabDecorRuleDb>(pluginInternal, typeof(PrefabDecorRuleDb).Name);
        }

        private static void createSettingsFolderAndAssets()
        {
            if (FileSystem.folderExists(settings)) return;

            FileSystem.createFolder(settings);

            // Mesh combine
            AssetDbEx.createScriptableObject_NoSave<MeshCombineUI>(settings, typeof(MeshCombineUI).Name);
            AssetDbEx.createScriptableObject_NoSave<MeshCombineSettings>(settings, typeof(MeshCombineSettings).Name);

            // Modular Snap Spawn
            AssetDbEx.createScriptableObject_NoSave<ObjectModularSnapSettings>(settings, typeof(ModularSnapObjectSpawn).Name + "_" + typeof(ObjectModularSnapSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectMirrorGizmoSettings>(settings, typeof(ModularSnapObjectSpawn).Name + "_" + typeof(ObjectMirrorGizmoSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectSpawnGuideSettings>(settings, typeof(ModularSnapObjectSpawn).Name + "_" + typeof(ObjectSpawnGuideSettings).Name);

            // Modular Walls Spawn
            AssetDbEx.createScriptableObject_NoSave<ModularWallsObjectSpawnSettings>(settings, typeof(ModularWallsObjectSpawn).Name + "_" + typeof(ModularWallsObjectSpawnSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectModularSnapSettings>(settings, typeof(ModularWallsObjectSpawn).Name + "_" + typeof(ObjectModularSnapSettings).Name);

            // Segments Spawn
            AssetDbEx.createScriptableObject_NoSave<ObjectModularSnapSettings>(settings, typeof(SegmentsObjectSpawn).Name + "_" + typeof(ObjectModularSnapSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectMirrorGizmoSettings>(settings, typeof(SegmentsObjectSpawn).Name + "_" + typeof(ObjectMirrorGizmoSettings).Name);

            // Box Spawn
            AssetDbEx.createScriptableObject_NoSave<ObjectModularSnapSettings>(settings, typeof(BoxObjectSpawn).Name + "_" + typeof(ObjectModularSnapSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectMirrorGizmoSettings>(settings, typeof(BoxObjectSpawn).Name + "_" + typeof(ObjectMirrorGizmoSettings).Name);

            // Props Spawn
            AssetDbEx.createScriptableObject_NoSave<ObjectSurfaceSnapSettings>(settings, typeof(PropsObjectSpawn).Name + "_" + typeof(ObjectSurfaceSnapSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectDragSpawnSettings>(settings, typeof(PropsObjectSpawn).Name + "_" + typeof(ObjectDragSpawnSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<TerrainFlattenSettings>(settings, typeof(PropsObjectSpawn).Name + "_" + typeof(TerrainFlattenSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectSpawnGuideSettings>(settings, typeof(PropsObjectSpawn).Name + "_" + typeof(ObjectSpawnGuideSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectMirrorGizmoSettings>(settings, typeof(PropsObjectSpawn).Name + "_" + typeof(ObjectMirrorGizmoSettings).Name);

            // Scatter Brush Spawn
            AssetDbEx.createScriptableObject_NoSave<ScatterBrushObjectSpawnSettings>(settings, typeof(ScatterBrushObjectSpawnSettings).Name);

            // Tile Rule Spawn
            AssetDbEx.createScriptableObject_NoSave<TileRuleObjectSpawnSettings>(settings, typeof(TileRuleObjectSpawnSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<TileRuleGridSettings>(settings, typeof(TileRuleObjectSpawn).Name + "_" + typeof(TileRuleGridSettings).Name);

            // Physics Spawn
            AssetDbEx.createScriptableObject_NoSave<PhysicsObjectSpawnSettings>(settings, typeof(PhysicsObjectSpawnSettings).Name);

            // Object Selection
            AssetDbEx.createScriptableObject_NoSave<ObjectSelectionSettings>(settings, typeof(ObjectSelectionSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectSelectionGrowSettings>(settings, typeof(ObjectSelectionGrowSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectProjectionSettings>(settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectProjectionSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectVertexSnapSettings>(settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectVertexSnapSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectBoxSnapSettings>(settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectBoxSnapSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectSurfaceSnapSettings>(settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectSurfaceSnapSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectModularSnapSettings>(settings, typeof(ObjectSelection).Name + "_" + typeof(ObjectModularSnapSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectMirrorGizmoSettings>(settings, typeof(ObjectSelectionGizmos).Name + "_" + typeof(ObjectMirrorGizmoSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectExtrudeGizmoSettings>(settings, typeof(ObjectSelectionGizmos).Name + "_" + typeof(ObjectExtrudeGizmoSettings).Name);

            // Object Erase
            AssetDbEx.createScriptableObject_NoSave<ObjectEraseCursorSettings>(settings, typeof(ObjectEraseCursorSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectEraseBrush2DSettings>(settings, typeof(ObjectEraseBrush2DSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectEraseBrush3DSettings>(settings, typeof(ObjectEraseBrush3DSettings).Name);
        }

        private static void createPrefsFolderAndAssets()
        {
            if (FileSystem.folderExists(preferences)) return;

            FileSystem.createFolder(preferences);
            AssetDbEx.createScriptableObject_NoSave<GizmoPrefs>(preferences, typeof(GizmoPrefs).Name);
            AssetDbEx.createScriptableObject_NoSave<GridPrefs>(preferences, typeof(GridPrefs).Name);
            AssetDbEx.createScriptableObject_NoSave<InputPrefs>(preferences, typeof(InputPrefs).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectErasePrefs>(preferences, typeof(ObjectErasePrefs).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectPrefs>(preferences, typeof(ObjectPrefs).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectSelectionPrefs>(preferences, typeof(ObjectSelectionPrefs).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectSpawnPrefs>(preferences, typeof(ObjectSpawnPrefs).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectTransformSessionPrefs>(preferences, typeof(ObjectTransformSessionPrefs).Name);
        }

        private static void createGridProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(gridProfiles)) return;

            FileSystem.createFolder(gridProfiles);
            AssetDbEx.createScriptableObject_NoSave<GridSettingsProfileDb>(gridProfiles, typeof(GridSettingsProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<GridSettingsProfileDbUI>(gridProfiles, typeof(GridSettingsProfileDbUI).Name);
        }

        private static void createShortcutProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(shortcutProfiles)) return;

            FileSystem.createFolder(shortcutProfiles);
            AssetDbEx.createScriptableObject_NoSave<ShortcutProfileDb>(shortcutProfiles, typeof(ShortcutProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<ShortcutProfileDbUI>(shortcutProfiles, typeof(ShortcutProfileDbUI).Name);
        }

        private static void createRandomPrefabProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(randomPrefabProfiles)) return;

            FileSystem.createFolder(randomPrefabProfiles);
            AssetDbEx.createScriptableObject_NoSave<RandomPrefabProfileDb>(randomPrefabProfiles, typeof(RandomPrefabProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<RandomPrefabProfileDbUI>(randomPrefabProfiles, typeof(RandomPrefabProfileDbUI).Name);
        }

        private static void createIntRangePrefabProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(intRangePrefabProfiles)) return;

            FileSystem.createFolder(intRangePrefabProfiles);
            AssetDbEx.createScriptableObject_NoSave<IntRangePrefabProfileDb>(intRangePrefabProfiles, typeof(IntRangePrefabProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<IntRangePrefabProfileDbUI>(intRangePrefabProfiles, typeof(IntRangePrefabProfileDbUI).Name);
        }

        private static void createScatterBrushPrefabProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(scatterBrushPrefabProfiles)) return;

            FileSystem.createFolder(scatterBrushPrefabProfiles);
            AssetDbEx.createScriptableObject_NoSave<ScatterBrushPrefabProfileDb>(scatterBrushPrefabProfiles, typeof(ScatterBrushPrefabProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<ScatterBrushPrefabProfileDbUI>(scatterBrushPrefabProfiles, typeof(ScatterBrushPrefabProfileDbUI).Name);
        }    

        private static void createModularWallPrefabProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(modularWallPrefabProfiles)) return;

            FileSystem.createFolder(modularWallPrefabProfiles);
            AssetDbEx.createScriptableObject_NoSave<ModularWallPrefabProfileDb>(modularWallPrefabProfiles, typeof(ModularWallPrefabProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<ModularWallPrefabProfileDbUI>(modularWallPrefabProfiles, typeof(ModularWallPrefabProfileDbUI).Name);
        }

        private static void createTileRuleProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(tileRuleProfiles)) return;

            FileSystem.createFolder(tileRuleProfiles);
            AssetDbEx.createScriptableObject_NoSave<TileRuleProfileDb>(tileRuleProfiles, typeof(TileRuleProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<TileRuleProfileDbUI>(tileRuleProfiles, typeof(TileRuleProfileDbUI).Name);
        }

        private static void createCurvePrefabProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(curvePrefabProfiles)) return;

            FileSystem.createFolder(curvePrefabProfiles);
            AssetDbEx.createScriptableObject_NoSave<CurvePrefabProfileDb>(curvePrefabProfiles, typeof(CurvePrefabProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<CurvePrefabProfileDbUI>(curvePrefabProfiles, typeof(CurvePrefabProfileDbUI).Name);
        }

        private static void createPrefabLibProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(prefabLibProfiles)) return;

            FileSystem.createFolder(prefabLibProfiles);
            AssetDbEx.createScriptableObject_NoSave<PrefabLibProfileDb>(prefabLibProfiles, typeof(PrefabLibProfileDb).Name);

            // Note: In this order.
            AssetDbEx.createScriptableObject_NoSave<PluginPrefabManagerUI>(prefabLibProfiles, typeof(PluginPrefabManagerUI).Name);
            AssetDbEx.createScriptableObject_NoSave<PrefabLibProfileDbUI>(prefabLibProfiles, typeof(PrefabLibProfileDbUI).Name);

            AssetDbEx.createScriptableObject_NoSave<PrefabFromSelectedObjectsCreationSettingsUI>(prefabLibProfiles, typeof(PrefabFromSelectedObjectsCreationSettingsUI).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectGroupsFromPrefabLibsCreationSettingsUI>(prefabLibProfiles, typeof(ObjectGroupsFromPrefabLibsCreationSettingsUI).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectGroupsFromPrefabLibsCreationSettings>(prefabLibProfiles, typeof(ObjectGroupsFromPrefabLibsCreationSettings).Name);
            AssetDbEx.createScriptableObject_NoSave<PrefabFromSelectedObjectsCreationSettings>(prefabLibProfiles, typeof(PrefabFromSelectedObjectsCreationSettings).Name);
        }

        private static void createPluginObjectLayersFolderAndAssets()
        {
            if (FileSystem.folderExists(pluginObjectLayers)) return;

            FileSystem.createFolder(pluginObjectLayers);
            AssetDbEx.createScriptableObject_NoSave<PluginObjectLayerDb>(pluginObjectLayers, typeof(PluginObjectLayerDb).Name);
            AssetDbEx.createScriptableObject_NoSave<PluginObjectLayerDbUI>(pluginObjectLayers, typeof(PluginObjectLayerDbUI).Name);
        }

        private static void createObjectGroupsFolderAndAssets()
        {
            if (FileSystem.folderExists(objectGroups)) return;

            FileSystem.createFolder(objectGroups);
            AssetDbEx.createScriptableObject_NoSave<ObjectGroupDb>(objectGroups, typeof(ObjectGroupDb).Name);
            AssetDbEx.createScriptableObject_NoSave<ObjectGroupDbUI>(objectGroups, typeof(ObjectGroupDbUI).Name);
            AssetDbEx.createScriptableObject_NoSave<PrefabsFromObjectGroupsCreationSettingsUI>(objectGroups, typeof(PrefabsFromObjectGroupsCreationSettingsUI).Name);
            AssetDbEx.createScriptableObject_NoSave<PrefabsFromObjectGroupsCreationSettings>(objectGroups, typeof(PrefabsFromObjectGroupsCreationSettings).Name);
        }

        private static void createIntPatternProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(intPatternProfiles)) return;

            FileSystem.createFolder(intPatternProfiles);
            AssetDbEx.createScriptableObject_NoSave<IntPatternDb>(intPatternProfiles, typeof(IntPatternDb).Name);
            AssetDbEx.createScriptableObject_NoSave<IntPatternDbUI>(intPatternProfiles, typeof(IntPatternDbUI).Name);
        }

        private static void createSegmentsObjectSpawnSettingsProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(segmentsObjectSpawnSettingsProfiles)) return;

            FileSystem.createFolder(segmentsObjectSpawnSettingsProfiles);
            AssetDbEx.createScriptableObject_NoSave<SegmentsObjectSpawnSettingsProfileDb>(segmentsObjectSpawnSettingsProfiles, typeof(SegmentsObjectSpawnSettingsProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<SegmentsObjectSpawnSettingsProfileDbUI>(segmentsObjectSpawnSettingsProfiles, typeof(SegmentsObjectSpawnSettingsProfileDbUI).Name);
        }

        private static void createBoxObjectSpawnSettingsProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(boxObjectSpawnSettingsProfiles)) return;

            FileSystem.createFolder(boxObjectSpawnSettingsProfiles);
            AssetDbEx.createScriptableObject_NoSave<BoxObjectSpawnSettingsProfileDb>(boxObjectSpawnSettingsProfiles, typeof(BoxObjectSpawnSettingsProfileDb).Name);
            AssetDbEx.createScriptableObject_NoSave<BoxObjectSpawnSettingsProfileDbUI>(boxObjectSpawnSettingsProfiles, typeof(BoxObjectSpawnSettingsProfileDbUI).Name);
        }

        private static void createCurveObjectSpawnSettingsProfilesFolderAndAssets()
        {
            if (FileSystem.folderExists(curveObjectSpawnSettingsProfiles)) return;

            FileSystem.createFolder(curveObjectSpawnSettingsProfiles);
            AssetDbEx.createScriptableObject_NoSave<CurveObjectSpawnSettingsProfileDb>(curveObjectSpawnSettingsProfiles, typeof(CurveObjectSpawnSettingsProfileDb).Name);
        }
    }
}
#endif