#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class PrefabDragAndDrop
    {
        private List<PrefabLib>         _droppedRootLibs    = new List<PrefabLib>();
        private List<PluginPrefab>      _droppedPrefabs     = new List<PluginPrefab>();
        private bool                    _anythingDropped;

        public int                      numDroppedLibs      { get { return _droppedRootLibs.Count; } }
        public int                      numDroppedPrefabs   { get { return _droppedPrefabs.Count; } }
        public bool                     anythingDropped     { get { return _anythingDropped; } }

        public void getDroppedRootLibs(List<PrefabLib> droppedLibs)
        {
            droppedLibs.Clear();
            droppedLibs.AddRange(_droppedRootLibs);
        }

        public void getDroppedPrefabs(List<PluginPrefab> droppedPrefabs)
        {
            droppedPrefabs.Clear();
            droppedPrefabs.AddRange(_droppedPrefabs);
        }

        public void dropFoldersInLibDb()
        {
            // Note: Disable Undo/Redo. Causes issues if we Undo/Redo after drop and then switch to playmode.
            UndoEx.saveEnabledState();
            UndoEx.enabled      = false;
            _anythingDropped    = false;
            _droppedRootLibs.Clear();

            createLibsFromFolders();
            UndoEx.restoreEnabledState();
        }

        public void dropPrefabsInLibs(List<PrefabLib> destLibs)
        {
            // Note: Disable Undo/Redo. Causes issues if we Undo/Redo after drop and then switch to playmode.
            UndoEx.saveEnabledState();
            UndoEx.enabled      = false;
            _anythingDropped    = false;
            _droppedPrefabs.Clear();

            PluginProgressDialog.begin("Dropping Items");

            var processedPaths  = new HashSet<string>();
            var dropPaths       = PluginDragAndDrop.unityPaths;
            for (int pathIndex  = 0; pathIndex < dropPaths.Length; ++pathIndex)
            {
                var path            = dropPaths[pathIndex];
                var allFolderPaths  = FileSystem.findAllFolders(path, true);
                foreach(var folderPath in allFolderPaths)
                {
                    if (processedPaths.Contains(folderPath)) continue;

                    var prefabs = AssetDbEx.loadPrefabs(folderPath, onPrefabLoaded);
                    processedPaths.Add(folderPath);

                    foreach (var lib in destLibs)
                        lib.createPrefabs(prefabs, _droppedPrefabs, true);
                }
            }

            var droppedObjects          = PluginDragAndDrop.unityObjects;
            for (int dropObjectIndex    = 0; dropObjectIndex < droppedObjects.Length; ++dropObjectIndex)
            {
                var dropObject          = droppedObjects[dropObjectIndex];
                GameObject prefabAsset  = dropObject as GameObject;
                if (prefabAsset != null && !prefabAsset.isSceneObject())
                {
                    PluginProgressDialog.updateItemProgress(prefabAsset.name, (dropObjectIndex + 1) / (float)droppedObjects.Length);
                    foreach (var lib in destLibs)
                    {
                        PluginPrefab prefab = lib.createPrefab(prefabAsset);
                        if (prefab != null) _droppedPrefabs.Add(prefab);
                    }
                }
            }

            PluginProgressDialog.end();
            _anythingDropped = _droppedPrefabs.Count != 0;
            UndoEx.restoreEnabledState();
        }

        private void createLibsFromFolders()
        {
            PluginProgressDialog.begin("Dropping Items");

            List<string> rootFolderPaths = FileSystem.filterRootFolders(PluginDragAndDrop.unityPaths);
            if (rootFolderPaths.Count == 0) return;
      
            for (int rootFolderIndex = 0; rootFolderIndex < rootFolderPaths.Count; ++rootFolderIndex)
            {
                var rootFolder      = rootFolderPaths[rootFolderIndex];
                var prefabs         = AssetDbEx.loadPrefabs(rootFolder, onPrefabLoaded);

                string folderName   = FileSystem.findLastFolderNameInPath(rootFolder);
                var rootLib         = PrefabLibProfileDb.instance.activeProfile.createLib(folderName);
                _droppedRootLibs.Add(rootLib);

                rootLib.folderPath  = rootFolder;
                rootLib.createPrefabs(prefabs);

                createLibsFromFoldersRecurse(rootFolder, rootLib);
            }

            PluginProgressDialog.updateProgress("Removing empty libs...", 1.0f);
            PrefabLibProfileDb.instance.activeProfile.deleteEmptyLibHierarchies(_droppedRootLibs);
            _droppedRootLibs.RemoveAll(item => item == null);
            PluginProgressDialog.end();

            _anythingDropped = _droppedRootLibs.Count != 0;
        }

        private void createLibsFromFoldersRecurse(string parentFolder, PrefabLib parentLib)
        {
            List<string> childFolders = FileSystem.findImmediateChildFolders(parentFolder);
            foreach(var childFolder in childFolders)
            {
                var prefabs         = AssetDbEx.loadPrefabs(childFolder, onPrefabLoaded);
                string folderName   = FileSystem.findLastFolderNameInPath(childFolder);
                var childLib        = PrefabLibProfileDb.instance.activeProfile.createLib(folderName);
                childLib.parentLib  = parentLib;
                childLib.folderPath = childFolder;             

                childLib.createPrefabs(prefabs);
                createLibsFromFoldersRecurse(childFolder, childLib);
            }
        }

        private void onPrefabLoaded(GameObject prefabAsset, float progress)
        {
            PluginProgressDialog.updateItemProgress(prefabAsset.name, progress);
        }
    }
}
#endif