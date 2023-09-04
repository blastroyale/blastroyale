#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class PluginAssetModificationProcessor : AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            // If we have the plugin object in the scene, do not delete the data folder or 
            // anything that resides in it.
            if (assetPath == PluginFolders.data)
            {
                if (EditorUtility.DisplayDialog("Are you sure?", "You are about to delete the Data folder. Doing this will erase the plugin (GSpawn) objects "
                    + "from the scene in which they exist. This will happen the next time you load a scene. Would you like to continue?", "Ok", "Cancel"))
                {
                    var plugins = GameObject.FindObjectsOfType<GSpawn>();
                    foreach (var plugin in plugins)
                        GameObject.DestroyImmediate(plugin.gameObject);

                    onDataFolderWillBeDeleted();
                    return AssetDeleteResult.DidNotDelete;
/*
                    EditorUtility.DisplayDialog("Restriction", "Can't delete the Data folder while the plugin object is in the scene. " +
                        "Please delete the plugin object from the scene and then try again.", "Ok");*/
                    
                }
                else return AssetDeleteResult.FailedDelete;
            }

            // Check if the asset is a child of the data folder
            if (PluginFolders.isChildOfDataFolder(assetPath))
            {
                if (EditorUtility.DisplayDialog("Are you sure?", "You are about to delete the child folders in the Data folder. Doing this will erase the plugin (GSpawn) objects "
                    + "from the scene in which they exist. This will happen the next time you load a scene. Would you like to continue?", "Ok", "Cancel"))
                {
                    var plugins = GameObject.FindObjectsOfType<GSpawn>();
                    foreach (var plugin in plugins)
                        GameObject.DestroyImmediate(plugin.gameObject);

                    onDataFolderWillBeDeleted();
                    return AssetDeleteResult.DidNotDelete;                  
                }
                else return AssetDeleteResult.FailedDelete;
            }

            // Load the asset
            var assetToBeDeleted = AssetDatabase.LoadAssetAtPath(assetPath, typeof(SceneAsset));

            // Check if we are dealing with a PluginUI asset
            PluginUI pluginUI = assetToBeDeleted as PluginUI;
            if (pluginUI)
            {
                pluginUI.onPluginUIAssetWillBeDestroyed();
                return AssetDeleteResult.DidNotDelete;
            }

            // Check if we are dealing with a scene asset
            SceneAsset sceneAsset = assetToBeDeleted as SceneAsset;
            if (sceneAsset != null)
            {
                if (ObjectGroupDb.exists) ObjectGroupDb.instance.onSceneAssetWillBeDeleted(sceneAsset, assetPath);

                int numPrefabLibProfiles = PrefabLibProfileDb.instance.numProfiles;
                for (int i = 0; i < numPrefabLibProfiles; ++i)
                    PrefabLibProfileDb.instance.performPrefabAction((p) => { p.onSceneAssetWillBeDeleted(sceneAsset, assetPath); });

                return AssetDeleteResult.DidNotDelete;
            }

            // Check if we are dealing with a prefab asset
            assetToBeDeleted = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
            GameObject prefabAsset = assetToBeDeleted as GameObject;
            if (prefabAsset != null)
            {
                if (PrefabDataDb.exists)                    PrefabDataDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                if (RandomPrefabProfileDb.exists)           RandomPrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                if (IntRangePrefabProfileDb.exists)         IntRangePrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                if (PrefabLibProfileDb.exists)              PrefabLibProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                if (ScatterBrushPrefabProfileDb.exists)     ScatterBrushPrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                if (CurvePrefabProfileDb.exists)            CurvePrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                if (TileRuleProfileDb.exists)               TileRuleProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                if (ModularWallPrefabProfileDb.exists)      ModularWallPrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                if (PrefabDecorRuleDb.exists)               PrefabDecorRuleDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);

                onPrefabAssetWillBeDeleted(prefabAsset);

                return AssetDeleteResult.DidNotDelete;
            }
            else
            {
                // Maybe we are dealing with a folder that contains prefabs. Search for prefabs in that folder and 
                // let the databases know we are about to delete those prefabs.
                var allFolderPaths = FileSystem.findAllFolders(assetPath, true);
                foreach (var folderPath in allFolderPaths)
                {
                    var loadedPrefabs = AssetDbEx.loadPrefabs(folderPath, null);
                    if (loadedPrefabs.Count != 0)
                    {
                        foreach (var prefab in loadedPrefabs)
                        {
                            if (PrefabDataDb.exists)                    PrefabDataDb.instance.onPrefabAssetWillBeDeleted(prefab);
                            if (RandomPrefabProfileDb.exists)           RandomPrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefab);
                            if (IntRangePrefabProfileDb.exists)         IntRangePrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                            if (PrefabLibProfileDb.exists)              PrefabLibProfileDb.instance.onPrefabAssetWillBeDeleted(prefab);
                            if (ScatterBrushPrefabProfileDb.exists)     ScatterBrushPrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                            if (CurvePrefabProfileDb.exists)            CurvePrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                            if (TileRuleProfileDb.exists)               TileRuleProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                            if (ModularWallPrefabProfileDb.exists)      ModularWallPrefabProfileDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                            if (PrefabDecorRuleDb.exists)               PrefabDecorRuleDb.instance.onPrefabAssetWillBeDeleted(prefabAsset);
                        }

                        onPrefabAssetsWillBeDeleted(loadedPrefabs);
                    }
                }

                return AssetDeleteResult.DidNotDelete;
            }
        }

        private static void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            var pluginInstancePools = ScriptableObject.FindObjectsOfType<PrefabInstancePool>();
            foreach (var pool in pluginInstancePools)
                pool.onPrefabAssetWillBeDeleted(prefabAsset);
        }

        private static void onPrefabAssetsWillBeDeleted(List<GameObject> prefabAssets)
        {
            var pluginInstancePools = ScriptableObject.FindObjectsOfType<PrefabInstancePool>();
            foreach (var pool in pluginInstancePools)
            {
                foreach(var prefabAsset in prefabAssets)
                {
                    pool.onPrefabAssetWillBeDeleted(prefabAsset);
                }
            }
        }

        private static void onDataFolderWillBeDeleted()
        {
            var pluginUIs = AssetDbEx.loadAssetsInFolder<PluginUI>(PluginFolders.data);
            foreach (var ui in pluginUIs)
                ui.onPluginUIAssetWillBeDestroyed();
        }
    }
}
#endif
