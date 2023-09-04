#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class PrefabLibProfile : Profile
    {
        [SerializeField]
        private List<PrefabLib>     _libs           = new List<PrefabLib>();
        [NonSerialized]
        private List<string>        _stringBuffer   = new List<string>();

        public int                  numLibs         { get { return _libs.Count; } }

        public string getLibDisplayName(PrefabLib lib)
        {
            return lib.libName;
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            foreach (var lib in _libs)
                lib.onPrefabAssetWillBeDeleted(prefabAsset);
        }

        public PrefabLib findOwnerLibOfPrefab(PluginPrefab prefab)
        {
            foreach (var lib in _libs)
            {
                if (lib.containsPrefab(prefab)) return lib;
            }

            return null;
        }

        public PrefabLib findOwnerLibOfPrefabAsset(GameObject prefabAsset)
        {
            foreach (var lib in _libs)
            {
                if (lib.containsPrefab(prefabAsset)) return lib;
            }

            return null;
        }

        public void findOwnerLibsOfPrefabAsset(GameObject prefabAsset, List<PrefabLib> libs)
        {
            libs.Clear();
            foreach (var lib in _libs)
            {
                if (lib.containsPrefab(prefabAsset)) libs.Add(lib);
            }
        }

        public void findOwnerLibsWithAnyPrefabAsset(List<GameObject> prefabAssets, List<PrefabLib> libs)
        {
            libs.Clear();
            foreach (var lib in _libs)
            {   
                if (lib.containsAnyPrefabAsset(prefabAssets)) libs.Add(lib);
            }
        }

        public void findOwnerLibsOfPrefabs(List<PluginPrefab> pluginPrefabs, List<PrefabLib> libs)
        {
            libs.Clear();
            foreach(var lib in _libs)
            {
                if (lib.containsAnyPrefab(pluginPrefabs)) libs.Add(lib);
            }
        }

        public void getAllPrefabAssets(List<GameObject> prefabAssets)
        {
            prefabAssets.Clear();
            int libCount = numLibs;
            for (int i = 0; i < libCount; ++i)
            {
                var lib = _libs[i];
                lib.getAllPrefabAssets(prefabAssets, true);
            }
        }

        public void getAllPrefabAssets(List<GameObject> prefabAssets, List<PrefabLib> libs)
        {
            libs.Clear();
            prefabAssets.Clear();
            int libCount = numLibs;
            for (int i = 0; i < libCount; ++i)
            {
                var lib         = _libs[i];
                int numPrefabs  = lib.numPrefabs;
                for (int j = 0; j < numPrefabs; ++j)
                {
                    prefabAssets.Add(lib.getPrefab(j).prefabAsset);
                    libs.Add(lib);
                }
            }
        }

        public PluginPrefab getPrefab(GameObject prefabAsset)
        {
            foreach (var lib in _libs)
            {
                PluginPrefab prefab = lib.getPrefab(prefabAsset);
                if (prefab != null) return prefab;
            }

            return null;
        }

        public PluginPrefab getPrefab(PluginGuid guid)
        {
            foreach (var lib in _libs)
            {
                PluginPrefab prefab = lib.getPrefab(guid);
                if (prefab != null) return prefab;
            }

            return null;
        }

        public void performPrefabAction(Action<PluginPrefab> prefabAction)
        {
            foreach (var lib in _libs)
                lib.performPrefabAction(prefabAction);
        }

        public PrefabLib createLib(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            getLibNames(_stringBuffer, null);
            //name = UniqueNameGen.generate(name, _stringBuffer);

            UndoEx.saveEnabledState();
            UndoEx.enabled  = false;

            var newLib      = UndoEx.createScriptableObject<PrefabLib>();
            newLib.libName  = name;
            newLib.name     = name;
            AssetDbEx.addObjectToAsset(newLib, this);

            UndoEx.record(this);
            _libs.Add(newLib);

            UndoEx.restoreEnabledState();

            return newLib;
        }

        public PrefabLib cloneLib(PrefabLib prefabLib, PrefabLib.CloneFlags cloneFlags)
        {
            if (containsLib(prefabLib))
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled = false;

                UndoEx.record(this);

                PrefabLib clonedLib = createLib(prefabLib.libName);
                if ((cloneFlags & PrefabLib.CloneFlags.Parent) != 0) clonedLib.parentLib = prefabLib.parentLib;

                int numPrefabs = prefabLib.numPrefabs;
                for (int i = 0; i < numPrefabs; ++i)
                    clonedLib.createPrefab(prefabLib.getPrefab(i).prefabAsset);

                UndoEx.restoreEnabledState();
                return clonedLib;
            }

            return null;
        }

        public void renameLib(PrefabLib lib, string newName)
        {
            if (!string.IsNullOrEmpty(newName) && containsLib(lib) && lib.libName != newName)
            {
                getLibNames(_stringBuffer, lib.libName);
                UndoEx.record(this);
                lib.libName = newName;// UniqueNameGen.generate(newName, _stringBuffer);
                lib.name = lib.libName;
            }
        }

        public bool containsLib(PrefabLib lib)
        {
            if (lib == null) return false;
            return _libs.Contains(lib);
        }

        public int deleteNullPrefabs()
        {
            int numRemoved = 0;
            foreach (var lib in _libs)
                numRemoved += lib.deleteNullPrefabs();

            return numRemoved;
        }

        public void deleteAllLibs()
        {
            if (numLibs != 0)
            {
                var parentLibs      = new List<PrefabLib>();
                PrefabLib.getParentLibs(_libs, parentLibs);

                var libsToRemove    = new List<PrefabLib>();
                var children        = new List<PrefabLib>();
                foreach (var parentLib in parentLibs)
                {
                    if (containsLib(parentLib))
                    {
                        libsToRemove.Add(parentLib);
                        parentLib.allChildrenDFS(children);

                        foreach (var child in children)
                            _libs.Remove(child);
                    }
                }

                // Note: We need to do this in steps in order to allow Undo/Redo to work correctly.
                foreach (var lib in libsToRemove)
                {
                    lib.parentLib = null;
                    lib.deleteAllPrefabs();     // Note: Remove prefabs here. Not in the lib's OnDestroy callback as it won't work with Undo/Redo.
                }

                UndoEx.record(this);
                foreach (var lib in libsToRemove)
                    _libs.Remove(lib);

                foreach (var lib in libsToRemove)
                    UndoEx.destroyObjectImmediate(lib);

                EditorUtility.SetDirty(this);
            }
        }

        public void deleteEmptyLibHierarchies()
        {
            for (int i = 0; i < _libs.Count;)
            {
                int oldNumLibs = numLibs;
                if (_libs[i].isHierarchyEmpty()) deleteLib(_libs[i]);
                if (oldNumLibs == numLibs) ++i;
            }         
        }

        public void deleteEmptyLibHierarchies(List<PrefabLib> rootLibs)
        {
            for (int i = 0; i < _libs.Count;)
            {
                int oldNumLibs = numLibs;
                foreach(var rootLib in rootLibs)
                {
                    if (_libs[i].isHierarchyEmpty() &&
                        _libs[i].isChildOf(rootLib))
                    {
                        deleteLib(_libs[i]);
                        break;
                    }
                }
                if (oldNumLibs == numLibs) ++i;
            }
        }

        public void deleteLib(PrefabLib lib)
        {
            if (containsLib(lib))
            {
                var libsToRemove = new List<PrefabLib>();
                libsToRemove.Add(lib);
                lib.appendAllChildrenDFS(libsToRemove);

                // Note: We need to do this in steps in order to allow Undo/Redo to work correctly.
                foreach (var l in libsToRemove)
                {
                    l.parentLib = null;
                    l.deleteAllPrefabs();     // Note: Remove prefabs here. Not in the lib's OnDestroy callback as it won't work with Undo/Redo.
                }

                UndoEx.record(this);
                foreach (var l in libsToRemove)
                    _libs.Remove(l);

                foreach (var l in libsToRemove)
                    UndoEx.destroyObjectImmediate(l);

                EditorUtility.SetDirty(this);
            }
        }

        public void deleteLibs(List<PrefabLib> libs)
        {
            if (libs.Count != 0)
            {
                var parentLibs      = new List<PrefabLib>();
                PrefabLib.getParentLibs(libs, parentLibs);

                var libsToRemove    = new List<PrefabLib>();
                var children        = new List<PrefabLib>();
                foreach (var parentLib in parentLibs)
                {
                    if (containsLib(parentLib))
                    {                    
                        libsToRemove.Add(parentLib);
                        parentLib.allChildrenDFS(children);

                        foreach (var child in children)
                            libsToRemove.Add(child);
                    }
                }

                // Note: We need to do this in steps in order to allow Undo/Redo to work correctly.
                foreach (var lib in libsToRemove)
                {
                    lib.parentLib = null;
                    lib.deleteAllPrefabs();     // Note: Remove prefabs here. Not in the lib's OnDestroy callback as it won't work with Undo/Redo.
                }

                UndoEx.record(this);
                foreach (var lib in libsToRemove)
                    _libs.Remove(lib);

                foreach (var lib in libsToRemove)
                    UndoEx.destroyObjectImmediate(lib);

                EditorUtility.SetDirty(this);
            }
        }

        public PrefabLib getLib(int index)
        {
            return _libs[index];
        }

        public void getRootLibs(List<PrefabLib> libs)
        {
            libs.Clear();
            foreach (var lib in _libs)
                if (lib.parentLib == null) libs.Add(lib);
        }

        public void getLibs(List<PrefabLib> libs)
        {
            libs.Clear();
            libs.AddRange(_libs);
        }

        public void getLibNames(List<string> libNames, string ignoredName)
        {
            libNames.Clear();
            foreach (var lib in _libs)
            {
                if (lib.libName != ignoredName)
                    libNames.Add(lib.libName);
            }
        }

        public void getLibDisplayNames(List<string> libNames)
        {
            libNames.Clear();
            foreach (var lib in _libs)
                libNames.Add(getLibDisplayName(lib));
        }

        public void getSelectedLibs(List<PrefabLib> libs)
        {
            libs.Clear();
            foreach (var lib in _libs)
            {
                if (lib.uiSelected) libs.Add(lib);
            }
        }

        public void regeneratePrefabPreviews()
        {
            foreach (var lib in _libs)
            {
                lib.regeneratePrefabPreviews();
            }
        }

        private void OnEnable()
        {
            deleteNullPrefabs();
        }

        private void OnDestroy()
        {
            deleteAllLibs();
        }
    }
}
#endif