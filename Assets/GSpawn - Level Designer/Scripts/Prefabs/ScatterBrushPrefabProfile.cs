#if UNITY_EDITOR
//#define SCATTER_BRUSH_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ScatterBrushPrefabProfile : Profile
    {
        private SerializedObject                                    _serializedObject;
        [SerializeField]
        private List<ScatterBrushPrefab>                            _prefabs                        = new List<ScatterBrushPrefab>();

        [NonSerialized]
        private bool                                                _probabilityTableDirty          = true;
        [NonSerialized]
        private CumulativeProbabilityTable<ScatterBrushPrefab>      _probabilityTable               = new CumulativeProbabilityTable<ScatterBrushPrefab>();
        [NonSerialized]
        private List<ScatterBrushPrefab>                            _scatterBrushPrefabBuffer       = new List<ScatterBrushPrefab>();

        public int                                                  numPrefabs                      { get { return _prefabs.Count; } }

        public SerializedObject                                     serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }

        public bool isAnyPrefabUsed()
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab.used) return true;
            }

            return false;
        }

        public void onPrefabsSpawnChanceChanged()
        {
            _probabilityTableDirty = true;
        }

        public void onPrefabsUsedStateChanged()
        {
            _probabilityTableDirty = true;
        }

        public ScatterBrushPrefab pickPrefab()
        {
            if (_probabilityTableDirty)
            {
                _probabilityTable.clear();
                foreach (var prefab in _prefabs)
                {
                    if (prefab.used)
                        _probabilityTable.addEntityAndRefresh(prefab, prefab.spawnChance);
                }

                _probabilityTableDirty = false;
            }

            return _probabilityTable.pickEntity();
        }

        public void resetPrefabPreviews()
        {
            int numPrefabs = _prefabs.Count;
            PluginProgressDialog.begin("Refreshing Prefab Previews");
            for (int prefabIndex = 0; prefabIndex < numPrefabs; ++prefabIndex)
            {
                var prefab = _prefabs[prefabIndex];
                PluginProgressDialog.updateItemProgress(prefab.prefabAsset.name, (prefabIndex + 1) / (float)numPrefabs);
                prefab.resetPreview();
            }

            PluginProgressDialog.end();
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

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            var prefabsToRemove = _prefabs.FindAll(item => item.prefabAsset == prefabAsset);
            foreach (var brushPrefab in prefabsToRemove)
            {
                _prefabs.Remove(brushPrefab);
                AssetDbEx.removeObjectFromAsset(brushPrefab, this);
                DestroyImmediate(brushPrefab);
            }

            _probabilityTableDirty = true;
        }

        public ScatterBrushPrefab createPrefab(PluginPrefab pluginPrefab)
        {
            #if SCATTER_BRUSH_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
            if (!containsPrefab(pluginPrefab))
            #endif
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled = false;

                UndoEx.record(this);
                var brushPrefab             = UndoEx.createScriptableObject<ScatterBrushPrefab>();
                brushPrefab.pluginPrefab    = pluginPrefab;
                brushPrefab.name            = brushPrefab.pluginPrefab.prefabAsset.name;

                _prefabs.Add(brushPrefab);
                AssetDbEx.addObjectToAsset(brushPrefab, this);

                EditorUtility.SetDirty(this);
                _probabilityTableDirty = true;

                UndoEx.restoreEnabledState();

                return brushPrefab;
            }

            #if SCATTER_BRUSH_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
            return null;
            #endif
        }

        public void createPrefabs(List<PluginPrefab> pluginPrefabs, List<ScatterBrushPrefab> createdPrefabs, bool appendCreated, string progressTitle)
        {
            _scatterBrushPrefabBuffer.Clear();
            PluginProgressDialog.begin(progressTitle);
            if (!appendCreated) createdPrefabs.Clear();

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            for (int prefabIndex = 0; prefabIndex < pluginPrefabs.Count; ++prefabIndex)
            {
                var pluginPrefab = pluginPrefabs[prefabIndex];
                PluginProgressDialog.updateItemProgress(pluginPrefab.prefabAsset.name, (prefabIndex + 1) / (float)pluginPrefabs.Count);

                #if SCATTER_BRUSH_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
                if (!containsPrefab(pluginPrefab))
                #endif
                {
                    var brushPrefab             = UndoEx.createScriptableObject<ScatterBrushPrefab>();
                    brushPrefab.pluginPrefab    = pluginPrefab;
                    brushPrefab.name            = brushPrefab.pluginPrefab.prefabAsset.name;

                    AssetDbEx.addObjectToAsset(brushPrefab, this);
                    createdPrefabs.Add(brushPrefab);
                    _scatterBrushPrefabBuffer.Add(brushPrefab);
                }
            }

            UndoEx.record(this);
            foreach (var brushPrefab in _scatterBrushPrefabBuffer)
                _prefabs.Add(brushPrefab);

            EditorUtility.SetDirty(this);
            _probabilityTableDirty = true;
            PluginProgressDialog.end();

            UndoEx.restoreEnabledState();
        }

        public void deletePrefab(ScatterBrushPrefab prefab)
        {
            if (prefab != null)
            {
                if (containsPrefab(prefab))
                {
                    UndoEx.record(this);
                    _prefabs.Remove(prefab);
                    UndoEx.destroyObjectImmediate(prefab);

                    EditorUtility.SetDirty(this);
                    _probabilityTableDirty = true;
                }
            }
        }

        public void deletePrefabs(List<ScatterBrushPrefab> prefabs)
        {
            if (prefabs.Count != 0)
            {
                UndoEx.record(this);
                _scatterBrushPrefabBuffer.Clear();
                foreach (var prefab in prefabs)
                {
                    if (containsPrefab(prefab))
                    {
                        _prefabs.Remove(prefab);
                        _scatterBrushPrefabBuffer.Add(prefab);
                    }
                }

                foreach (var prefab in _scatterBrushPrefabBuffer)
                    UndoEx.destroyObjectImmediate(prefab);

                EditorUtility.SetDirty(this);
                _probabilityTableDirty = true;
            }
        }

        public void deleteAllPrefabs()
        {
            if (_prefabs.Count != 0)
            {
                UndoEx.record(this);

                _scatterBrushPrefabBuffer.Clear();
                _scatterBrushPrefabBuffer.AddRange(_prefabs);
                _prefabs.Clear();

                foreach (var prefab in _scatterBrushPrefabBuffer)
                    UndoEx.destroyObjectImmediate(prefab);

                EditorUtility.SetDirty(this);
                _probabilityTableDirty = true;
            }
        }

        public bool containsPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var brushPrefab in _prefabs)
            {
                if (brushPrefab.pluginPrefab == pluginPrefab) return true;
            }

            return false;
        }

        public bool containsPrefab(ScatterBrushPrefab brushPrefab)
        {
            return _prefabs.Contains(brushPrefab);
        }

        public ScatterBrushPrefab getPrefab(int index)
        {
            return _prefabs[index];
        }

        public ScatterBrushPrefab getPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab.pluginPrefab == pluginPrefab) return prefab;
            }

            return null;
        }

        public void getPrefabs(List<ScatterBrushPrefab> brushPrefabs)
        {
            brushPrefabs.Clear();
            brushPrefabs.AddRange(_prefabs);
        }

        public void getPrefabs(List<PluginPrefab> pluginPrefabs, List<ScatterBrushPrefab> brushPrefabs, bool append)
        {
            if (!append) brushPrefabs.Clear();
            foreach (var pluginPrefab in pluginPrefabs)
            {
                ScatterBrushPrefab brushPrefab = getPrefab(pluginPrefab);
                if (brushPrefab != null) brushPrefabs.Add(brushPrefab);
            }
        }

        public void getPrefabNames(List<string> names)
        {
            names.Clear();
            foreach (var prefab in _prefabs)
                names.Add(prefab.prefabAsset.name);
        }

        public int deleteNullPrefabs()
        {
            var nullPrefabs = _prefabs.FindAll(item => item.pluginPrefab == null || item.prefabAsset == null);
            foreach (var nullPrefab in nullPrefabs)
            {
                _prefabs.Remove(nullPrefab);
                AssetDbEx.removeObjectFromAsset(nullPrefab, this);
                DestroyImmediate(nullPrefab);
            }
            _probabilityTableDirty = true;

            return nullPrefabs.Count;
        }

        private void onUndoRedo()
        {
            _probabilityTableDirty = true;
        }

        private void OnDestroy()
        {
            deleteAllPrefabs();
        }

        private void OnEnable()
        {
            _probabilityTableDirty = true;
            Undo.undoRedoPerformed += onUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
        }
    }
}
#endif