#if UNITY_EDITOR
//#define PREFAB_LIB_NO_DUPLICATE_PREFABS

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public class PrefabLibHashSet : SerializableHashSet<PrefabLib> { }

    public class PrefabLib : ScriptableObject, IUIItemStateProvider
    {
        [Flags]
        public enum CloneFlags
        {
            None = 0,
            Parent = 1,
            All = Parent
        }

        [SerializeField]
        private PluginGuid              _guid                           = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private PrefabLib               _parentLib                      = null;
        [SerializeField]
        private List<PrefabLib>         _directChildren                 = new List<PrefabLib>();
        [SerializeField]
        private bool                    _prefabsVisibleInManager        = true;
        [SerializeField]
        private string                  _libName                        = string.Empty;
        [SerializeField]
        private PrefabLibFolderPath     _folderPath                     = new PrefabLibFolderPath();
        [SerializeField]
        private List<PluginPrefab>      _prefabs                        = new List<PluginPrefab>();
        [SerializeField]
        private bool                    _uiSelected                     = false;
        [NonSerialized]
        private CopyPasteMode           _uiCopyPasteMode                = CopyPasteMode.None;

        [NonSerialized]
        private List<PluginPrefab>      _pastePluginPrefabBuffer        = new List<PluginPrefab>();
        [NonSerialized]
        private List<PluginPrefab>      _pluginPrefabBuffer             = new List<PluginPrefab>();

        public PluginGuid               guid                            { get { return _guid; } }
        // Note: libName doesn't support Undo as it seems it is hard to sync the lib name with the name of the SO asset.
        public string                   libName                         { get { return _libName; } set { if (!string.IsNullOrEmpty(value)) { UndoEx.record(this); _libName = value; } } }
        public bool                     prefabsVisibleInManagerLocal    { get { return _prefabsVisibleInManager; } }
        public int                      numPrefabs                      { get { return _prefabs.Count; } }
        public bool                     empty                           { get { return _prefabs.Count == 0; } }
        public string                   folderPath                      { get { return _folderPath.path; } set { _folderPath.set(value); } }
        public string                   reversedFolderPath              { get { return _folderPath.reversedPath; } }
        public int                      numDirectChildren               { get { return _directChildren.Count; } }
        public bool                     hasChildren                     { get { return _directChildren.Count != 0; } }
        public PrefabLib                parentLib
        {
            get { return _parentLib; }
            set
            {
                if (value == _parentLib || value == this) return;
                if (_parentLib != null)
                {
                    UndoEx.record(_parentLib);
                    _parentLib._directChildren.Remove(this);
                }

                UndoEx.record(this);
                _parentLib = value;
                if (_parentLib != null)
                {
                    UndoEx.record(_parentLib);
                    _parentLib._directChildren.Add(this);
                }
            }
        }
        public bool                     uiSelected                      { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode            uiCopyPasteMode                 { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }

        public static int movePrefabs(List<PluginPrefab> prefabs, List<PrefabLib> srcLibs, PrefabLib destLib, string progressTitle)
        {
            int numMoved        = 0;
            int numSrcLibs      = srcLibs.Count;

            PluginProgressDialog.begin(progressTitle);
            for (int libIndex = 0; libIndex < numSrcLibs; ++libIndex)
            {
                PluginProgressDialog.updateItemProgress(srcLibs[libIndex].libName, (libIndex + 1) / (float)numSrcLibs);
                numMoved += srcLibs[libIndex].movePrefabs(prefabs, destLib);
            }
            PluginProgressDialog.end();

            return numMoved;
        }

        public static void resetPrefabPreviews(List<PrefabLib> libs)
        {
            PluginProgressDialog.begin("Refreshing Prefab Previews");
            for (int libIndex = 0; libIndex < libs.Count; ++libIndex)
            {
                var lib = libs[libIndex];
                PluginProgressDialog.updateItemProgress(lib.libName, (libIndex + 1) / (float)libs.Count);

                lib.resetPrefabPreviews();
            }

            PluginProgressDialog.end();
        }

        public static void deletePrefabsFromLibs(List<PluginPrefab> prefabs, string progressTitle)
        {
            int numPrefabs = prefabs.Count;
            PluginProgressDialog.begin(progressTitle);
            for (int prefabIndex = 0; prefabIndex < numPrefabs; ++prefabIndex)
            {
                PluginPrefab prefab = prefabs[prefabIndex];
                PluginProgressDialog.updateItemProgress(prefab.prefabAsset.name, (prefabIndex + 1) / (float)numPrefabs);
                PrefabLib lib = PrefabLibProfileDb.instance.activeProfile.findOwnerLibOfPrefab(prefab);
                if (lib != null) lib.deletePrefab(prefab);
            }
            PluginProgressDialog.end();
        }

        public static void paste(List<PluginPrefab> prefabs, List<PrefabLib> destLibs, CopyPasteMode copyPasteMode)
        {
            foreach (var destLib in destLibs)
                destLib.paste(prefabs);

            if (copyPasteMode == CopyPasteMode.Cut) deletePrefabsFromLibs(prefabs, "Removing Prefabs");
        }

        public static void paste(List<PluginPrefab> prefabs, PrefabLib destLib, CopyPasteMode copyPasteMode)
        {
            destLib.paste(prefabs);

            if (copyPasteMode == CopyPasteMode.Cut) deletePrefabsFromLibs(prefabs, "Removing Prefabs");
        }

        public static void paste(List<PrefabLib> sourceLibs, List<PrefabLib> destLibs, CopyPasteMode copyPasteMode)
        {
            foreach (var destLib in destLibs)
                destLib.paste(sourceLibs);

            if (copyPasteMode == CopyPasteMode.Cut)
            {
                var destLibsSet = new HashSet<PrefabLib>(destLibs);
                PluginProgressDialog.begin("Deleting Original Prefabs");
                for (int libIndex = 0; libIndex < sourceLibs.Count; ++libIndex)
                {
                    var srcLib = sourceLibs[libIndex];
                    if (destLibsSet.Contains(srcLib)) continue;

                    PluginProgressDialog.updateItemProgress(srcLib.libName, (libIndex + 1) / (float)sourceLibs.Count);
                    srcLib.deleteAllPrefabs();
                }
                PluginProgressDialog.end();
            }
        }

        public static void getPrefabs(List<PrefabLib> libs, List<PluginPrefab> prefabs)
        {
            prefabs.Clear();
            foreach(var lib in libs)
                lib.appendPrefabs(prefabs);
        }

        public static void getPrefabNames(List<PrefabLib> libs, List<string> prefabNames)
        {
            prefabNames.Clear();
            foreach (var lib in libs)
                lib.getPrefabNames(prefabNames, true);
        }

        public static void appendAllChildren(List<PrefabLib> libs)
        {
            var allChildren     = new List<PrefabLib>();
            var iterLibs        = new List<PrefabLib>(libs);
            foreach(var lib in iterLibs)
            {
                lib.allChildrenDFS(allChildren);
                allChildren.RemoveAll(item => iterLibs.Contains(item));
                libs.AddRange(allChildren);
            }
        }

        public static void getLibsInHierarchy(PrefabLib parentLib, List<PrefabLib> allLibs)
        {
            allLibs.Clear();
            allLibs.Add(parentLib);
            parentLib.appendAllChildrenDFS(allLibs);
        }

        public static void getLibsInHierarchies(List<PrefabLib> parentLibs, List<PrefabLib> allLibs)
        {
            allLibs.Clear();
            foreach(var parentLib in parentLibs)
            {
                allLibs.Add(parentLib);
                parentLib.appendAllChildrenDFS(allLibs);
            }
        }

        public static void getParentLibs(List<PrefabLib> libs, List<PrefabLib> parentLibs)
        {
            parentLibs.Clear();

            foreach (var lib in libs)
            {
                bool foundParent = false;
                if (lib.parentLib == null)
                {
                    parentLibs.Add(lib);
                    continue;
                }

                foreach (var otherLib in libs)
                {
                    if (otherLib == lib) continue;

                    if (lib.isChildOf(otherLib))
                    {
                        foundParent = true;
                        break;
                    }
                }

                if (!foundParent) parentLibs.Add(lib);
            }
        }

        public static int calcNumPrefabsInLibs(List<PrefabLib> libs)
        {
            int numPrefabs = 0;
            foreach (var lib in libs)
                numPrefabs += lib.numPrefabs;

            return numPrefabs;
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            var prefabsToRemove = _prefabs.FindAll(item => item.prefabAsset == prefabAsset);
            foreach (var prefab in prefabsToRemove)
            {
                _prefabs.Remove(prefab);
                AssetDbEx.removeObjectFromAsset(prefab, this);
                DestroyImmediate(prefab);
            }
        }

        public int movePrefabs(List<PluginPrefab> prefabs, PrefabLib destLib)
        {
            if (this == destLib) return 0;

            int numMoved = 0;
            foreach(var prefab in prefabs)
            {
                if (containsPrefab(prefab))
                {
                    UndoEx.record(this);
                    _prefabs.Remove(prefab);
                    AssetDbEx.removeObjectFromAsset(prefab, this);
                    EditorUtility.SetDirty(this);

                    UndoEx.record(destLib);
                    destLib._prefabs.Add(prefab);
                    AssetDbEx.addObjectToAsset(prefab, destLib);

                    ++numMoved;
                }
            }

            return numMoved;
        }

        public void setPrefabsObjectGroup(ObjectGroup objectGroup)
        {
            foreach (var prefab in _prefabs)
                prefab.objectGroup = objectGroup;
        }

        public void performPrefabAction(Action<PluginPrefab> prefabAction)
        {
            foreach (var prefab in _prefabs)
                prefabAction(prefab);
        }

        public void setPrefabsVisibleInManagerLocal(bool visible)
        {
            if (visible == _prefabsVisibleInManager) return;

            UndoEx.record(this);
            _prefabsVisibleInManager = visible;
        }

        public bool prefabsVisibleInManagerGlobal()
        {
            PrefabLib parent = parentLib;
            while (parent != null)
            {
                if (!parent._prefabsVisibleInManager) return false;
                parent = parent.parentLib;
            }

            return true;
        }

        public void resetPrefabPreviews()
        {
            foreach (var prefab in _prefabs)
                prefab.resetPreview();
        }

        public void regeneratePrefabPreviews()
        {
            int numPrefabs = _prefabs.Count;
            PluginProgressDialog.begin("Regenerating Prefab Previews");
            for (int prefabIndex = 0; prefabIndex < numPrefabs; ++prefabIndex)
            {
                var prefab = _prefabs[prefabIndex];
                PluginProgressDialog.updateItemProgress(prefab.prefabAsset.name, (prefabIndex + 1) / (float)numPrefabs);
                prefab.regeneratePreview();
            }

            PluginProgressDialog.end();
        }

        public void paste(List<PrefabLib> srcLibs)
        {
            getPrefabs(srcLibs, _pastePluginPrefabBuffer);
            createPrefabs(_pastePluginPrefabBuffer, "Pasting Libraries");
        }

        public void paste(List<PluginPrefab> prefabs)
        {
            createPrefabs(prefabs, "Pasting Prefabs");
        }

        public PluginPrefab createPrefab(GameObject prefabAsset)
        {
            #if PREFAB_LIB_NO_DUPLICATE_PREFABS
            if (!containsPrefab(prefabAsset))
            #endif
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled = false;

                UndoEx.record(this);
                var pluginPrefab            = UndoEx.createScriptableObject<PluginPrefab>();
                pluginPrefab.prefabAsset    = prefabAsset;
                AssetDbEx.addObjectToAsset(pluginPrefab, this);

                _prefabs.Add(pluginPrefab);

                UndoEx.restoreEnabledState();
                return pluginPrefab;
            }

            #if PREFAB_LIB_NO_DUPLICATE_PREFABS
            return null;
            #endif
        }

        public void createPrefabs(List<PluginPrefab> prefabs, string progressTitle)
        {
            _pluginPrefabBuffer.Clear();
            PluginProgressDialog.begin(progressTitle);

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            UndoEx.record(this);
            for (int prefabIndex = 0; prefabIndex < prefabs.Count; ++prefabIndex)
            {
                var prefabAsset = prefabs[prefabIndex].prefabAsset;
                PluginProgressDialog.updateItemProgress(prefabAsset.name, (prefabIndex + 1) / (float)prefabs.Count);

                #if PREFAB_LIB_NO_DUPLICATE_PREFABS
                if (!containsPrefab(prefabAsset))
                #endif
                {
                    var pluginPrefab            = UndoEx.createScriptableObject<PluginPrefab>();
                    pluginPrefab.prefabAsset    = prefabAsset;
                    pluginPrefab.objectGroup    = prefabs[prefabIndex].objectGroup;

                    AssetDbEx.addObjectToAsset(pluginPrefab, this);
                    _pluginPrefabBuffer.Add(pluginPrefab);
                }
            }

            UndoEx.record(this);
            foreach (var prefab in _pluginPrefabBuffer)
                _prefabs.Add(prefab);

            EditorUtility.SetDirty(this);
            PluginProgressDialog.end();

            UndoEx.restoreEnabledState();
        }

        public void createPrefabs(List<PluginPrefab> prefabs, List<PluginPrefab> createdPrefabs, bool append, string progressTitle)
        {
            _pluginPrefabBuffer.Clear();
            PluginProgressDialog.begin(progressTitle);
            if (!append) createdPrefabs.Clear();

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            UndoEx.record(this);
            for (int prefabIndex = 0; prefabIndex < prefabs.Count; ++prefabIndex)
            {
                var prefabAsset = prefabs[prefabIndex].prefabAsset;
                PluginProgressDialog.updateItemProgress(prefabAsset.name, (prefabIndex + 1) / (float)prefabs.Count);

                #if PREFAB_LIB_NO_DUPLICATE_PREFABS
                if (!containsPrefab(prefabAsset))
                #endif
                {
                    var pluginPrefab            = UndoEx.createScriptableObject<PluginPrefab>();
                    pluginPrefab.prefabAsset    = prefabAsset;
                    pluginPrefab.objectGroup    = prefabs[prefabIndex].objectGroup;

                    AssetDbEx.addObjectToAsset(pluginPrefab, this);
                    _pluginPrefabBuffer.Add(pluginPrefab);
                    createdPrefabs.Add(pluginPrefab);
                }
            }

            UndoEx.record(this);
            foreach (var prefab in _pluginPrefabBuffer)
                _prefabs.Add(prefab);

            EditorUtility.SetDirty(this);
            PluginProgressDialog.end();

            UndoEx.restoreEnabledState();
        }

        public void createPrefabs(List<GameObject> prefabAssets, List<PluginPrefab> createdPrefabs, bool append)
        {
            _pluginPrefabBuffer.Clear();
            if (!append) createdPrefabs.Clear();

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            UndoEx.record(this);
            foreach (var prefabAsset in prefabAssets)
            {
                #if PREFAB_LIB_NO_DUPLICATE_PREFABS
                if (!containsPrefab(prefabAsset))
                #endif
                {
                    var pluginPrefab = UndoEx.createScriptableObject<PluginPrefab>();
                    pluginPrefab.prefabAsset = prefabAsset;

                    AssetDbEx.addObjectToAsset(pluginPrefab, this);
                    _pluginPrefabBuffer.Add(pluginPrefab);
                    createdPrefabs.Add(pluginPrefab);
                }
            }

            UndoEx.record(this);
            foreach (var prefab in _pluginPrefabBuffer)
                _prefabs.Add(prefab);

            EditorUtility.SetDirty(this);

            UndoEx.restoreEnabledState();
        }

        public void createPrefabs(List<GameObject> prefabAssets)
        {
            _pluginPrefabBuffer.Clear();

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            UndoEx.record(this);
            foreach (var prefabAsset in prefabAssets)
            {
                #if PREFAB_LIB_NO_DUPLICATE_PREFABS
                if (!containsPrefab(prefabAsset))
                #endif
                {
                    var pluginPrefab = UndoEx.createScriptableObject<PluginPrefab>();
                    pluginPrefab.prefabAsset = prefabAsset;

                    AssetDbEx.addObjectToAsset(pluginPrefab, this);
                    _pluginPrefabBuffer.Add(pluginPrefab);
                }
            }

            UndoEx.record(this);
            foreach (var prefab in _pluginPrefabBuffer)
                _prefabs.Add(prefab);

            EditorUtility.SetDirty(this);
            UndoEx.restoreEnabledState();
        }

        public bool isHierarchyEmpty()
        {
            if (!empty) return false;
            if (!hasChildren && empty) return true;
            if (hasChildren && allChildrenEmpty()) return true;

            return false;
        }

        public bool allChildrenEmpty()
        {
            if (!hasChildren) return true;

            var allChildren = new List<PrefabLib>();
            allChildrenDFS(allChildren);

            foreach (var child in allChildren)
                if (!child.empty) return false;

            return true;
        }

        public bool containsPrefab(GameObject prefabAsset)
        {
            foreach(var prefab in _prefabs)
            {
                if (prefab.prefabAsset == prefabAsset) return true;
            }

            return false;
        }

        public bool containsPrefab(PluginPrefab prefab)
        {
            return _prefabs.Contains(prefab);
        }

        public bool containsAnyPrefabAsset(List<GameObject> prefabAssets)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefabAssets.Contains(prefab.prefabAsset)) return true;
            }

            return false;
        }

        public bool containsAnyPrefab(List<PluginPrefab> pluginPrefabs)
        {
            foreach (var prefab in _prefabs)
            {
                if (pluginPrefabs.Contains(prefab)) return true;
            }

            return false;
        }

        public void deletePrefab(PluginPrefab prefab)
        {
            if (prefab != null)
            {
                if (containsPrefab(prefab))
                {
                    UndoEx.record(this);
                    _prefabs.Remove(prefab);
                    UndoEx.destroyObjectImmediate(prefab);
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void deletePrefabs(List<PluginPrefab> prefabs)
        {
            if (prefabs.Count != 0)
            {
                UndoEx.record(this);
                _pluginPrefabBuffer.Clear();

                foreach (var prefab in prefabs)
                {
                    if (containsPrefab(prefab))
                    {
                        _prefabs.Remove(prefab);
                        _pluginPrefabBuffer.Add(prefab);
                    }
                }

                foreach (var prefab in _pluginPrefabBuffer)
                    UndoEx.destroyObjectImmediate(prefab);

                EditorUtility.SetDirty(this);
            }
        }

        public void deleteAllPrefabs()
        {
            if (_prefabs.Count != 0)
            {
                UndoEx.record(this);
                _pluginPrefabBuffer.Clear();
                _pluginPrefabBuffer.AddRange(_prefabs);

                _prefabs.Clear();

                foreach (var prefab in _pluginPrefabBuffer)
                    UndoEx.destroyObjectImmediate(prefab);

                EditorUtility.SetDirty(this);
            }
        }

        public void setDirectChildIndex(PrefabLib childLib, int newIndex)
        {
            int currentIndex = _directChildren.IndexOf(childLib);
            if (currentIndex >= 0)
            {
                UndoEx.record(this);
                PrefabLib otherLib              = _directChildren[newIndex];
                _directChildren[newIndex]       = childLib;
                _directChildren[currentIndex]   = otherLib;
            }
        }

        public void getAllPrefabAssets(List<GameObject> prefabAssets, bool append)
        {
            if (!append) prefabAssets.Clear();

            int prefabCount = _prefabs.Count;
            for (int i = 0; i < prefabCount; ++i)
            {
                prefabAssets.Add(_prefabs[i].prefabAsset);
            }
        }

        public PluginPrefab getPrefab(int index)
        {
            return _prefabs[index];
        }

        public PluginPrefab getPrefab(GameObject prefabAsset)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefabAsset == prefab.prefabAsset) return prefab;

                // Why was it like this before?? Seems utterly wrong.
                // Note: Need to check by name. There can be more than one GameObject instance
                //       referencing the same prefab asset.
                //if (prefab.prefabAsset.name == prefabAsset.name) return prefab;
            }

            return null;
        }

        public PluginPrefab getPrefab(PluginGuid guid)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab.guid == guid) return prefab;
            }

            return null;
        }

        public PrefabLib getDirectChild(int index)
        {
            return _directChildren[index];
        }

        public void getDirectChildren(List<PrefabLib> childLibs)
        {
            childLibs.Clear();
            if (numDirectChildren != 0)
                childLibs.AddRange(_directChildren);
        }

        public void getPrefabs(List<PluginPrefab> prefabs)
        {
            prefabs.Clear();
            prefabs.AddRange(_prefabs);
        }

        public void appendPrefabs(List<PluginPrefab> prefabs)
        {
            prefabs.AddRange(_prefabs);
        }

        public void getPrefabNames(List<string> prefabNames, bool append)
        {
            if (!append) prefabNames.Clear();
            foreach (var prefab in _prefabs)
                prefabNames.Add(prefab.prefabAsset.name);
        }

        public void appendPrefabIds(List<PluginGuid> ids)
        {
            foreach (var prefab in _prefabs)
                ids.Add(prefab.guid);
        }

        public int deleteNullPrefabs()
        {
            var nullPrefabs = _prefabs.FindAll(item => item.prefabAsset == null);
            foreach (var nullPrefab in nullPrefabs)
            {
                _prefabs.Remove(nullPrefab);
                AssetDbEx.removeObjectFromAsset(nullPrefab, this);
                DestroyImmediate(nullPrefab);
            }

            return nullPrefabs.Count;
        }

        public bool isChildOf(PrefabLib lib)
        {
            if (parentLib == null) return false;
            if (parentLib == lib) return true;

            var parent = parentLib.parentLib;
            while (parent != null && parent != lib)
                parent = parent.parentLib;

            return parent == lib;
        }

        public void allChildrenDFS(List<PrefabLib> allChildren)
        {
            allChildren.Clear();
            allChildrenRecurseDFS(this, allChildren);
        }

        public void appendAllChildrenDFS(List<PrefabLib> allChildren)
        {
            allChildrenRecurseDFS(this, allChildren);
        }

        private void allChildrenRecurseDFS(PrefabLib parentLib, List<PrefabLib> allChildren)
        {
            foreach (var child in parentLib._directChildren)
            {
                allChildren.Add(child);
                allChildrenRecurseDFS(child, allChildren);
            }
        }

        private void OnDestroy()
        {
            deleteAllPrefabs();
        }
    }
}
#endif