#if UNITY_EDITOR
//#define CURVE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace GSpawn
{
    public class CurvePrefabProfile : Profile
    {
        private SerializedObject                        _serializedObject;
        [SerializeField]
        private List<CurvePrefab>                       _prefabs                = new List<CurvePrefab>();
        [NonSerialized]
        private bool                                    _probabilityTableDirty  = true;
        [NonSerialized]
        private CumulativeProbabilityTable<CurvePrefab> _probabilityTable       = new CumulativeProbabilityTable<CurvePrefab>();
        [NonSerialized]
        private List<CurvePrefab>                       _curvePrefabBuffer      = new List<CurvePrefab>();

        public int                                      numPrefabs              { get { return _prefabs.Count; } }

        public SerializedObject serializedObject
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

        public CurvePrefab pickPrefab()
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
            foreach (var curvePrefab in prefabsToRemove)
            {
                _prefabs.Remove(curvePrefab);
                AssetDbEx.removeObjectFromAsset(curvePrefab, this);
                DestroyImmediate(curvePrefab);
            }

            _probabilityTableDirty = true;
        }

        public CurvePrefab createPrefab(PluginPrefab pluginPrefab)
        {
            #if CURVE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
            if (!containsPrefab(pluginPrefab))
            #endif
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled              = false;

                UndoEx.record(this);
                var curvePrefab             = UndoEx.createScriptableObject<CurvePrefab>();
                curvePrefab.pluginPrefab    = pluginPrefab;
                curvePrefab.name            = curvePrefab.pluginPrefab.prefabAsset.name;

                _prefabs.Add(curvePrefab);
                AssetDbEx.addObjectToAsset(curvePrefab, this);

                EditorUtility.SetDirty(this);
                _probabilityTableDirty      = true;

                UndoEx.restoreEnabledState();

                return curvePrefab;
            }

            #if CURVE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
            return null;
            #endif
        }

        public void createPrefabs(List<PluginPrefab> pluginPrefabs, List<CurvePrefab> createdPrefabs, bool appendCreated, string progressTitle)
        {
            _curvePrefabBuffer.Clear();
            PluginProgressDialog.begin(progressTitle);
            if (!appendCreated) createdPrefabs.Clear();

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            for (int prefabIndex = 0; prefabIndex < pluginPrefabs.Count; ++prefabIndex)
            {
                var pluginPrefab = pluginPrefabs[prefabIndex];
                PluginProgressDialog.updateItemProgress(pluginPrefab.prefabAsset.name, (prefabIndex + 1) / (float)pluginPrefabs.Count);

                #if CURVE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
                if (!containsPrefab(pluginPrefab))
                #endif
                {
                    var curvePrefab             = UndoEx.createScriptableObject<CurvePrefab>();
                    curvePrefab.pluginPrefab    = pluginPrefab;
                    curvePrefab.name            = curvePrefab.pluginPrefab.prefabAsset.name;

                    AssetDbEx.addObjectToAsset(curvePrefab, this);
                    createdPrefabs.Add(curvePrefab);
                    _curvePrefabBuffer.Add(curvePrefab);
                }
            }

            UndoEx.record(this);
            foreach (var curvePrefab in _curvePrefabBuffer)
                _prefabs.Add(curvePrefab);

            EditorUtility.SetDirty(this);
            _probabilityTableDirty = true;
            PluginProgressDialog.end();

            UndoEx.restoreEnabledState();
        }

        public void deletePrefab(CurvePrefab curvePrefab)
        {
            if (curvePrefab != null)
            {
                if (containsPrefab(curvePrefab))
                {
                    UndoEx.record(this);
                    _prefabs.Remove(curvePrefab);
                    UndoEx.destroyObjectImmediate(curvePrefab);

                    EditorUtility.SetDirty(this);
                    _probabilityTableDirty = true;
                }
            }
        }

        public void deletePrefabs(List<CurvePrefab> curvePrefabs)
        {
            if (curvePrefabs.Count != 0)
            {
                UndoEx.record(this);
                _curvePrefabBuffer.Clear();
                foreach (var prefab in curvePrefabs)
                {
                    if (containsPrefab(prefab))
                    {
                        _prefabs.Remove(prefab);
                        _curvePrefabBuffer.Add(prefab);
                    }
                }

                foreach (var prefab in _curvePrefabBuffer)
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

                _curvePrefabBuffer.Clear();
                _curvePrefabBuffer.AddRange(_prefabs);
                _prefabs.Clear();

                foreach (var prefab in _curvePrefabBuffer)
                    UndoEx.destroyObjectImmediate(prefab);

                EditorUtility.SetDirty(this);
                _probabilityTableDirty = true;
            }
        }

        public bool containsPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var curvePrefab in _prefabs)
            {
                if (curvePrefab.pluginPrefab == pluginPrefab) return true;
            }

            return false;
        }

        public bool containsPrefab(CurvePrefab curvePrefab)
        {
            return _prefabs.Contains(curvePrefab);
        }

        public CurvePrefab getPrefab(int index)
        {
            return _prefabs[index];
        }

        public CurvePrefab getPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab.pluginPrefab == pluginPrefab) return prefab;
            }

            return null;
        }

        public void getPrefabs(List<CurvePrefab> curvePrefabs)
        {
            curvePrefabs.Clear();
            curvePrefabs.AddRange(_prefabs);
        }

        public void getPrefabs(List<PluginPrefab> pluginPrefabs, List<CurvePrefab> curvePrefabs, bool append)
        {
            if (!append) curvePrefabs.Clear();
            foreach (var pluginPrefab in pluginPrefabs)
            {
                CurvePrefab curvePrefab = getPrefab(pluginPrefab);
                if (curvePrefab != null) curvePrefabs.Add(curvePrefab);
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