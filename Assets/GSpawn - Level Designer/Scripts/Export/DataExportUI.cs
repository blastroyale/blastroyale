#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

namespace GSpawn
{
    public class DataExportUI : PluginUI
    {
        public static DataExportUI instance { get { return GSpawn.active.dataExportUI; } }

        protected override void onBuild()
        {
            contentContainer.style.marginLeft   = UIValues.settingsMarginLeft;
            contentContainer.style.marginTop    = UIValues.settingsMarginTop;
            contentContainer.style.marginRight  = UIValues.settingsMarginRight;
            contentContainer.style.marginBottom = UIValues.settingsMarginBottom;
            contentContainer.style.flexGrow     = 1.0f;
            DataExportSettings.instance.buildUI(contentContainer);

            UI.createRowSeparator(contentContainer).style.flexGrow = 1.0f;

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            contentContainer.Add(buttonRow);

            var checkAllBtn = new Button();
            buttonRow.Add(checkAllBtn);
            checkAllBtn.text            = "Check all";
            checkAllBtn.tooltip         = "Check all data items for export.";
            checkAllBtn.style.width     = UIValues.useDefaultsButtonWidth * 0.75f;
            checkAllBtn.clicked += () => { DataExportSettings.instance.setExportAll(true); };

            var uncheckAllBtn = new Button();
            buttonRow.Add(uncheckAllBtn);
            uncheckAllBtn.text          = "Uncheck all";
            uncheckAllBtn.tooltip       = "Uncheck all data items.";
            uncheckAllBtn.style.width   = checkAllBtn.style.width;
            uncheckAllBtn.clicked += () => { DataExportSettings.instance.setExportAll(false); };

            UI.createColumnSeparator(buttonRow).style.flexGrow = 1.0f;

            var exportBtn = new Button();
            buttonRow.Add(exportBtn);
            exportBtn.text              = "Export";
            exportBtn.tooltip           = "Export plugin data.";
            exportBtn.style.width       = UIValues.useDefaultsButtonWidth;
            exportBtn.style.alignSelf   = Align.FlexEnd;
            exportBtn.clicked += () => 
            {
                string filePath = EditorUtility.SaveFilePanel("Select Export Data File", "", GSpawn.pluginName, "unitypackage");
                var dataExportSettings = DataExportSettings.instance;
                List<string> assetPaths = new List<string>();

                if (dataExportSettings.exportPrefs)
                {
                    assetPaths.Add(AssetDbEx.getAssetPath(GizmoPrefs.instance));
                    assetPaths.Add(AssetDbEx.getAssetPath(GridPrefs.instance));
                    assetPaths.Add(AssetDbEx.getAssetPath(InputPrefs.instance));
                    assetPaths.Add(AssetDbEx.getAssetPath(ObjectErasePrefs.instance));
                    assetPaths.Add(AssetDbEx.getAssetPath(ObjectPrefs.instance));
                    assetPaths.Add(AssetDbEx.getAssetPath(ObjectSelectionPrefs.instance));
                    assetPaths.Add(AssetDbEx.getAssetPath(ObjectSpawnPrefs.instance));
                    assetPaths.Add(AssetDbEx.getAssetPath(ObjectTransformSessionPrefs.instance));
                }

                if (dataExportSettings.exportGeneralSettings)
                {
                    collectScriptableObjectAssetPaths(PluginFolders.settings, assetPaths);
                }

                /*if (dataExportSettings.exportGridProfiles)
                {
                    assetPaths.Add(AssetDbEx.getAssetPath(GridSettingsProfileDb.instance));
                    collectProfilePaths<GridSettingsProfileDb, GridSettingsProfile>(GridSettingsProfileDb.instance, assetPaths);
                    collectScriptableObjectAssetPaths<PluginUI>(PluginFolders.gridProfiles, assetPaths);
                }*/

                if (dataExportSettings.exportShortcutProfiles)
                {
                    assetPaths.Add(AssetDbEx.getAssetPath(ShortcutProfileDb.instance));
                    collectProfilePaths<ShortcutProfileDb, ShortcutProfile>(ShortcutProfileDb.instance, assetPaths);
                    collectScriptableObjectAssetPaths<PluginUI>(PluginFolders.shortcutProfiles, assetPaths);
                }

                if (dataExportSettings.exportIntPatterns)
                {
                    collectScriptableObjectAssetPaths(PluginFolders.intPatternProfiles, assetPaths);
                    collectScriptableObjectAssetPaths<PluginUI>(PluginFolders.intPatternProfiles, assetPaths);
                }

                /*if (dataExportSettings.exportSegmentsObjectSpawnSettingsProfiles)
                {
                    assetPaths.Add(AssetDbEx.getAssetPath(SegmentsObjectSpawnSettingsProfileDb.instance));
                    collectProfilePaths<SegmentsObjectSpawnSettingsProfileDb, SegmentsObjectSpawnSettingsProfile>(SegmentsObjectSpawnSettingsProfileDb.instance, assetPaths);
                    collectScriptableObjectAssetPaths<PluginUI>(PluginFolders.segmentsObjectSpawnSettingsProfiles, assetPaths);
                }

                if (dataExportSettings.exportBoxObjectSpawnSettingsProfiles)
                {
                    assetPaths.Add(AssetDbEx.getAssetPath(BoxObjectSpawnSettingsProfileDb.instance));
                    collectProfilePaths<BoxObjectSpawnSettingsProfileDb, BoxObjectSpawnSettingsProfile>(BoxObjectSpawnSettingsProfileDb.instance, assetPaths);
                    collectScriptableObjectAssetPaths<PluginUI>(PluginFolders.boxObjectSpawnSettingsProfiles, assetPaths);
                }

                if (DataExportSettings.defaultExportCurveObjectSpawnSettingsProfile)
                {
                    assetPaths.Add(AssetDbEx.getAssetPath(CurveObjectSpawnSettingsProfileDb.instance));
                    collectProfilePaths<CurveObjectSpawnSettingsProfileDb, CurveObjectSpawnSettingsProfile>(CurveObjectSpawnSettingsProfileDb.instance, assetPaths);
                    collectScriptableObjectAssetPaths<PluginUI>(PluginFolders.curveObjectSpawnSettingsProfiles, assetPaths);
                }*/

                if (assetPaths.Count == 0)
                {
                    EditorUtility.DisplayDialog("Export Info", "Nothing was exported.", "Ok");
                    return;
                }
                else
                {
                    AssetDatabase.ExportPackage(assetPaths.ToArray(), filePath);
                    EditorUtility.DisplayDialog("Export Info", "Data exported successfully.", "Ok");
                }
            };
        }

        protected override void onRefresh()
        {
        }

        private void collectScriptableObjectAssetPaths(string folderPath, List<string> paths)
        {
            var settingsAssets = AssetDbEx.loadAssetsInFolder<ScriptableObject>(folderPath);
            foreach (var asset in settingsAssets)
                paths.Add(AssetDbEx.getAssetPath(asset));
        }

        private void collectScriptableObjectAssetPaths<T>(string folderPath, List<string> paths) where T : ScriptableObject
        {
            var settingsAssets = AssetDbEx.loadAssetsInFolder<T>(folderPath);
            foreach (var asset in settingsAssets)
                paths.Add(AssetDbEx.getAssetPath(asset));
        }

        private void collectProfilePaths<TProfileDb, TProfile>(TProfileDb profileDb, List<string> paths)
            where TProfileDb : ProfileDb<TProfile>
            where TProfile : Profile
        {
            int numProfiles = profileDb.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
            {
                paths.Add(AssetDbEx.getAssetPath(profileDb.getProfile(i)));
            }
        }
    }
}
#endif
