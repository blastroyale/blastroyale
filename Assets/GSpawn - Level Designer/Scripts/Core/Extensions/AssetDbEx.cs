#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public static class AssetDbEx
    {
        public enum AssetType
        {
            Prefab = 1,
            Object
        }

        public static void createAssetFolder(string folderPath)
        {
            FileSystem.createFolder(folderPath);
            AssetDatabase.Refresh();
        }

        public static bool prefabExists(string prefabPath)
        {
            return AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) != null;
        }

        public static void findAllSceneAssetsInProject(List<SceneAsset> sceneAssets)
        {
            sceneAssets.Clear();

            string[] sceneAssetGUIDS    = AssetDatabase.FindAssets("t:scene");
            var numScenes               = sceneAssetGUIDS.Length;

            for (int i = 0; i < numScenes; ++i)
            {
                var sceneAssetPath  = AssetDatabase.GUIDToAssetPath(sceneAssetGUIDS[i]);
                var sceneAsset      = AssetDatabase.LoadAssetAtPath(sceneAssetPath, typeof(SceneAsset)) as SceneAsset;

                sceneAssets.Add(sceneAsset);
            }
        }

        public static GameObject loadPrefab(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
            if (prefab != null) return prefab as GameObject;

            return null;
        }

        public static List<GameObject> loadPrefabs(string folderPath, Action<GameObject, float> onPrefabLoaded)
        {
            if (string.IsNullOrEmpty(folderPath)) return new List<GameObject>();

            List<string> assetPaths = FileSystem.findAllFilesInFolder(folderPath);
            return findPrefabsInAssets(assetPaths, onPrefabLoaded);
        }

        public static bool isPrefab(string assetPath)
        {
            string prefabExtension = ".prefab";
            if (assetPath.Length <= prefabExtension.Length) return false;
            else return assetPath.EndsWith(prefabExtension);
        }

        public static string getAssetPath(UnityEngine.Object asset)
        {
            return AssetDatabase.GetAssetPath(asset);
        }

        public static void deleteAsset(UnityEngine.Object asset)
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset));
        }

        public static void saveScriptableObject(ScriptableObject scriptableObject, string assetPath)
        {
            AssetDatabase.CreateAsset(scriptableObject, assetPath);
            AssetDatabase.SaveAssets();
        }

        public static void addObjectToAsset(UnityEngine.Object objectToAdd, UnityEngine.Object assetObject)
        {
            AssetDatabase.AddObjectToAsset(objectToAdd, assetObject);;
        }

        public static void addObjectToAsset(UnityEngine.Object objectToAdd, UnityEngine.Object assetObject, bool saveAssets)
        {
            AssetDatabase.AddObjectToAsset(objectToAdd, assetObject);
            if (saveAssets) AssetDatabase.SaveAssets();
        }

        public static void removeObjectFromAsset(UnityEngine.Object objectToRemove, UnityEngine.Object assetObject)
        {
            if (objectToRemove != null) 
            {
                AssetDatabase.RemoveObjectFromAsset(objectToRemove);
                EditorUtility.SetDirty(assetObject);
            }
        }

        public static T loadScriptableObject<T>(string folderPath) where T : ScriptableObject
        {
            var assets = loadAssetsInFolder<T>(folderPath);
            if (assets.Count == 0)
            {
                Debug.LogError("No ScriptableObject assets of type '" + typeof(T).Name + "' exist.");
                return null;
            }
            if (assets.Count > 1)
            {
                Debug.LogWarning("More than one instance of ScriptableObject assets of type '" + typeof(T).Name + "' exist.");
                return assets[0];
            }

            return assets[0];
        }

        public static T loadScriptableObject<T>(string folderPath, string soName) where T : ScriptableObject
        {
            var assets = loadAssetsInFolder<T>(folderPath);
            if (assets.Count == 0)
            {
                Debug.LogError("No ScriptableObject assets of type '" + typeof(T).Name + "' exist.");
                return null;
            }

            foreach (var soAsset in assets)
            {
                if (soAsset.name == soName) return soAsset;
            }

            Debug.LogError("No ScriptableObject assets of type '" + typeof(T).Name + "' with name '" + soName + "' exist.");
            return null;
        }

        public static T loadScriptableObjectOrCreate<T>(string folderPath, string soName) where T : ScriptableObject
        {
            var assets = loadAssetsInFolder<T>(folderPath);
            if (assets.Count >= 1)
            {
                foreach(var soAsset in assets)
                {
                    if (soAsset.name == soName) return soAsset;
                }
            }

            var asset = ScriptableObject.CreateInstance<T>();
            saveScriptableObject(asset, folderPath + "/" + soName + ".asset");
            return asset;
        }

        public static T createScriptableObject_NoSave<T>(string folderPath, string soName) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, folderPath + "/" + soName + ".asset");
            return asset;
        }

        public static T loadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        public static List<T> loadAssetsInFolder<T>(string folderPath) where T : UnityEngine.Object
        {
            var assets = loadAssetsInFolder(folderPath, AssetType.Object);
            var finalAssetList = new List<T>();
            foreach (var asset in assets)
            {
                T derivedAsset = asset as T;
                if (derivedAsset != null) finalAssetList.Add(derivedAsset);
            }

            return finalAssetList;
        }

        public static List<UnityEngine.Object> loadAssetsInFolder(string folderPath, AssetType assetType)
        {
            string assetTypeFilter = "t:";
            if (assetType == AssetType.Prefab) assetTypeFilter += "GameObject";
            else if (assetType == AssetType.Object) assetTypeFilter += "Object";

            var assetGUIDs = AssetDatabase.FindAssets(assetTypeFilter, new string[] { folderPath });
            return convertGUIDsToAssets(assetGUIDs);
        }

        public static List<UnityEngine.Object> convertGUIDsToAssets(string[] assetGUIDs)
        {
            var assets = new List<UnityEngine.Object>();
            foreach (var guid in assetGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));

                if (asset != null) assets.Add(asset);
            }

            return assets;
        }

        public static List<GameObject> convertAssetsToPrefabs(List<UnityEngine.Object> assets)
        {
            if (assets == null || assets.Count == 0) return new List<GameObject>();

            var prefabs = new List<GameObject>(assets.Count);
            foreach (var asset in assets)
            {
                GameObject prefab = asset as GameObject;
                if (prefab != null) prefabs.Add(prefab);
            }

            return prefabs;
        }

        public static List<GameObject> findPrefabsInAssets(List<string> assetPaths, Action<GameObject, float> onPrefabLoaded)
        {
            var prefabs = new List<GameObject>();
            for (int pathIndex = 0; pathIndex < assetPaths.Count; ++pathIndex)
            {
                string assetPath = assetPaths[pathIndex];
                if (isPrefab(assetPath))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                    if (prefab != null)
                    {
                        prefabs.Add(prefab);
                        if (onPrefabLoaded != null) onPrefabLoaded(prefab, (pathIndex + 1) / (float)assetPaths.Count);
                    }
                }
            }

            return prefabs;
        }
    }
}
#endif