#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public static class PrefabFactory
    {
        private static List<GameObject> _objectBuffer = new List<GameObject>();

        public static bool create(List<ObjectGroup> rootObjectGroups, PrefabsFromObjectGroupsCreationSettings settings, List<GameObject> createdPrefabAssets)
        {
            if (createdPrefabAssets != null)
                createdPrefabAssets.Clear();

            string errorTitle = "Prefab Creation Failed";
            if (string.IsNullOrEmpty(settings.destinationFolder))
            {
                EditorUtility.DisplayDialog(errorTitle, "Invalid prefab destination folder.", "Ok");
                return false;
            }

            if (!FileSystem.folderExists(settings.destinationFolder))
            {
                EditorUtility.DisplayDialog(errorTitle, "The specified prefab destination folder does not exist.", "Ok");
                return false;
            }

            PluginProgressDialog.begin("Creating Prefabs");
            PluginProgressDialog.updateProgress("Creating prefabs from object groups...", 0.0f);
            for (int rootIndex = 0; rootIndex < rootObjectGroups.Count; ++rootIndex)
            {
                var rootGroup           = rootObjectGroups[rootIndex];
                GameObject rootClone    = cloneObjectGroup(rootGroup.gameObject);
                cloneObjectGroupChildrenRecurse(rootClone, rootGroup.gameObject);

                PluginProgressDialog.updateProgress(rootGroup.gameObject.name, (rootIndex + 1) / (float)rootObjectGroups.Count);

                string prefabPath       = settings.destinationFolder + "/" + rootClone.name + ".prefab";
                GameObject prefab       = AssetDbEx.loadPrefab(prefabPath);
                if (prefab != null)
                {
                    if (!handlePrefabAlreadyExists(prefabPath, prefab, rootClone, true)) continue;
                }

                GameObject prefabAsset  = tryCreatePrefab(prefabPath, rootClone, true);
                if (prefabAsset != null)
                {
                    if (createdPrefabAssets != null) createdPrefabAssets.Add(prefabAsset);
                }
            }

            PluginProgressDialog.updateProgress("Done.", 1.0f);
            PluginProgressDialog.end();

            return true;
        }

        public static GameObject create(List<GameObject> parents, PrefabFromSelectedObjectsCreationSettings settings)
        {
            string errorTitle = "Prefab Creation Failed";
            if (string.IsNullOrEmpty(settings.prefabName))
            {
                EditorUtility.DisplayDialog(errorTitle, "No prefab name was specified.", "Ok");
                return null;
            }
            
            if (string.IsNullOrEmpty(settings.destinationFolder))
            {
                EditorUtility.DisplayDialog(errorTitle, "Invalid prefab destination folder.", "Ok");
                return null;
            }

            if (!FileSystem.folderExists(settings.destinationFolder))
            {                
                EditorUtility.DisplayDialog(errorTitle, "The specified prefab destination folder does not exist.", "Ok");
                return null;
            }

            string prefabPath = settings.destinationFolder + "/" + settings.prefabName + ".prefab";
            GameObject prefab = AssetDbEx.loadPrefab(prefabPath);
            if (prefab != null)
            {
                if (!handlePrefabAlreadyExists(prefabPath, prefab, null, false)) return null;
            }

            _objectBuffer.Clear();
            bool searchForPivot = !string.IsNullOrEmpty(settings.pivotObjectName);

            PluginProgressDialog.begin("Creating Prefab");
            PluginProgressDialog.updateProgress("Cloning objects...", 0.0f);
            List<GameObject> clonedParents  = new List<GameObject>();
            GameObject pivotObject          = null;
            foreach (GameObject parent in parents)
            {
                GameObject parentClone              = GameObject.Instantiate(parent);
                parentClone.name                    = parent.name;
                parentClone.transform.localScale    = parent.transform.lossyScale;
                clonedParents.Add(parentClone);

                if (searchForPivot && pivotObject == null)
                {
                    parent.getAllChildrenAndSelf(false, false, _objectBuffer);
                    foreach (var c in _objectBuffer)
                    {
                        if (c.name == settings.pivotObjectName)
                        {
                            pivotObject = c;
                            break;
                        }
                    }
                }
            }

            PluginProgressDialog.updateProgress("Calculating positions...", 0.33f);
            AABB pivotAABB = pivotObject != null ?
                ObjectBounds.calcWorldAABB(pivotObject, ObjectBounds.QueryConfig.defaultConfig) : 
                ObjectBounds.calcHierarchiesWorldAABB(parents, ObjectBounds.QueryConfig.defaultConfig);

            GameObject root = new GameObject(settings.prefabName);
            if (settings.pivot == PrefabCreationPivot.Center || 
               (pivotObject == null && settings.pivot == PrefabCreationPivot.FromPivotObject))  root.transform.position = pivotAABB.center;
            else if (settings.pivot == PrefabCreationPivot.CenterBottom)    root.transform.position = Box3D.calcFaceCenter(pivotAABB.center, pivotAABB.size, Quaternion.identity, Box3DFace.Bottom);
            else if (settings.pivot == PrefabCreationPivot.CenterTop)       root.transform.position = Box3D.calcFaceCenter(pivotAABB.center, pivotAABB.size, Quaternion.identity, Box3DFace.Top);
            else if (settings.pivot == PrefabCreationPivot.CenterBack)      root.transform.position = Box3D.calcFaceCenter(pivotAABB.center, pivotAABB.size, Quaternion.identity, Box3DFace.Back);
            else if (settings.pivot == PrefabCreationPivot.CenterFront)     root.transform.position = Box3D.calcFaceCenter(pivotAABB.center, pivotAABB.size, Quaternion.identity, Box3DFace.Front);
            else if (settings.pivot == PrefabCreationPivot.CenterLeft)      root.transform.position = Box3D.calcFaceCenter(pivotAABB.center, pivotAABB.size, Quaternion.identity, Box3DFace.Left);
            else if (settings.pivot == PrefabCreationPivot.CenterRight)     root.transform.position = Box3D.calcFaceCenter(pivotAABB.center, pivotAABB.size, Quaternion.identity, Box3DFace.Right);
            else if (settings.pivot == PrefabCreationPivot.FromPivotObject) root.transform.position = pivotObject.transform.position;
            else if (settings.pivot == PrefabCreationPivot.TileRule)
            {
                if (pivotObject == null)
                {
                    // Pick the parent with the largest volume. This will be treated as the 
                    // main object (e.g. a platform/floor or something like that). The rest 
                    // of the objects are assumed to be decorations.
                    AABB mainAABB       = AABB.getInvalid();
                    float largestVolume = float.MinValue;
                    foreach (GameObject parent in parents)
                    {
                        AABB parentAABB = ObjectBounds.calcWorldAABB(parent, ObjectBounds.QueryConfig.defaultConfig);
                        if (!parentAABB.isValid) continue;

                        if (parentAABB.volume > largestVolume)
                        {
                            largestVolume = parentAABB.volume;
                            mainAABB = parentAABB;
                        }
                    }

                    if (mainAABB.isValid)
                    {
                        Vector3 aabbCenter = mainAABB.center;
                        root.transform.position = new Vector3(aabbCenter.x, mainAABB.min.y, aabbCenter.z);
                    }
                    else root.transform.position = parents[0].transform.position;
                }
                else root.transform.position = new Vector3(pivotAABB.center.x, pivotAABB.min.y, pivotAABB.center.z);
            }

            foreach (var clonedParent in clonedParents)
                clonedParent.transform.parent = root.transform;

            root.transform.position = Vector3.zero;

            PluginProgressDialog.updateProgress("Creating prefab asset...", 0.66f);
            GameObject prefabAsset = tryCreatePrefab(prefabPath, root, true);

            PluginProgressDialog.updateProgress("Done.", 1.0f);
            PluginProgressDialog.end();

            return prefabAsset;
        }

        private static GameObject cloneObjectGroup(GameObject objectGroup)
        {
            GameObject clone            = new GameObject(objectGroup.name);
            clone.layer                 = objectGroup.layer;
            clone.tag                   = objectGroup.tag;
            clone.transform.position    = objectGroup.transform.position;
            clone.transform.rotation    = objectGroup.transform.rotation;
            clone.transform.localScale  = objectGroup.transform.localScale;

            return clone;
        }

        private static void cloneObjectGroupChildrenRecurse(GameObject clonedParentGroup, GameObject originalParentGroup)
        {
            int numChildren = originalParentGroup.transform.childCount;
            for (int i = 0; i < numChildren; ++i)
            {
                GameObject child = originalParentGroup.transform.GetChild(i).gameObject;
                if (ObjectGroupDb.instance.isObjectGroup(child))
                {
                    var clonedChild = cloneObjectGroup(child);
                    clonedChild.transform.SetParent(clonedParentGroup.transform);
                    cloneObjectGroupChildrenRecurse(clonedChild, child);
                }
            }
        }

        private static GameObject tryCreatePrefab(string prefabPath, GameObject sourceObject, bool destroySource)
        {
            GameObject prefabAsset = null;
            try
            {
                prefabAsset = sourceObject.saveAsPrefabAsset(prefabPath);
                if (destroySource) GameObject.DestroyImmediate(sourceObject);
            }
            catch
            {
                if (destroySource) GameObject.DestroyImmediate(sourceObject);
            }

            return prefabAsset;
        }

        private static bool handlePrefabAlreadyExists(string prefabPath, GameObject prefabAsset, GameObject sourceObject, bool destroySource)
        {
            if (!EditorUtility.DisplayDialog("Overwrite Prefab?", "A prefab with the same name (" + prefabAsset.name + ") already exists " + 
                "in the specified folder. Would you like to overwrite it?", "Yes", "No"))
            {
                if (destroySource && sourceObject != null) GameObject.DestroyImmediate(sourceObject);
                return false;
            }

            prefabAsset.disconnectPrefabInstances();
            AssetDatabase.DeleteAsset(prefabPath);
            return true;
        }
    }
}
#endif