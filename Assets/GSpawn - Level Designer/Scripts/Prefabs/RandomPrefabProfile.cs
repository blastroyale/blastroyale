#if UNITY_EDITOR
//#define RANDOM_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class RandomPrefabProfile : Profile
    {
        private SerializedObject        _serializedObject;
        [SerializeField]
        private List<RandomPrefab>      _prefabs            = new List<RandomPrefab>();

        [NonSerialized]
        private bool                                        _probabilityTableDirty  = true;
        [NonSerialized]
        private CumulativeProbabilityTable<RandomPrefab>    _probabilityTable       = new CumulativeProbabilityTable<RandomPrefab>();
        [NonSerialized]
        private CumulativeProbabilityTable<RandomPrefab>    _tagProbabilityTable    = new CumulativeProbabilityTable<RandomPrefab>();
        [NonSerialized]
        private List<RandomPrefab>                          _randomPrefabBuffer     = new List<RandomPrefab>();

        public int                      numPrefabs          { get { return _prefabs.Count; } }

        public SerializedObject serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }

        public int getNumUsedPrefabs()
        {
            int numUsed = 0;
            foreach (var prefab in _prefabs)
            {
                if (prefab.used) ++numUsed;
            }

            return numUsed;
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

        public void onPrefabsUsedStateChanged()
        {
            _probabilityTableDirty = true;
        }

        public void onPrefabsProbabilityChanged()
        {
            _probabilityTableDirty = true;
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            var prefabsToRemove = _prefabs.FindAll(item => item.prefabAsset == prefabAsset);
            foreach (var randomPrefab in prefabsToRemove)
            {
                _prefabs.Remove(randomPrefab);
                AssetDbEx.removeObjectFromAsset(randomPrefab, this);
                DestroyImmediate(randomPrefab);
            }

            _probabilityTableDirty = true;
        }

        public RandomPrefab pickPrefab()
        {
            if (_probabilityTableDirty)
            {
                _probabilityTable.clear();
                foreach (var prefab in _prefabs)
                {
                    if (prefab.used)
                        _probabilityTable.addEntityAndRefresh(prefab, prefab.probability);
                }

                _probabilityTableDirty = false;
            }

            return _probabilityTable.pickEntity();
        }

        public RandomPrefab pickPrefabByTag(string lowerCaseTag)
        {
            _tagProbabilityTable.clear();
            foreach (var prefab in _prefabs)
            {
                if (prefab.used && prefab.prefabAsset.tag.ToLower() == lowerCaseTag)
                    _tagProbabilityTable.addEntity(prefab, prefab.probability);
            }
            _tagProbabilityTable.refresh();

            return _tagProbabilityTable.pickEntity();
        }

        public RandomPrefab createPrefab(PluginPrefab pluginPrefab)
        {
            #if RANDOM_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
            if (!containsPrefab(pluginPrefab))
            #endif
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled = false;

                UndoEx.record(this);
                var randomPrefab            = UndoEx.createScriptableObject<RandomPrefab>();
                randomPrefab.pluginPrefab   = pluginPrefab;
                randomPrefab.name           = randomPrefab.pluginPrefab.prefabAsset.name;

                _prefabs.Add(randomPrefab);
                AssetDbEx.addObjectToAsset(randomPrefab, this);
                _probabilityTableDirty      = true;

                EditorUtility.SetDirty(this);
                UndoEx.restoreEnabledState();

                return randomPrefab;
            }

            #if RANDOM_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
            return null;
            #endif
        }

        public void createPrefabs(List<PluginPrefab> pluginPrefabs, List<RandomPrefab> createdPrefabs, bool appendCreated, string progressTitle)
        {
            _randomPrefabBuffer.Clear();
            PluginProgressDialog.begin(progressTitle);
            if (!appendCreated) createdPrefabs.Clear();

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            for (int prefabIndex = 0; prefabIndex < pluginPrefabs.Count; ++prefabIndex)
            {
                var pluginPrefab = pluginPrefabs[prefabIndex];
                PluginProgressDialog.updateItemProgress(pluginPrefab.prefabAsset.name, (prefabIndex + 1) / (float)pluginPrefabs.Count);

                #if RANDOM_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
                if (!containsPrefab(pluginPrefab))
                #endif
                {
                    var randomPrefab            = UndoEx.createScriptableObject<RandomPrefab>();
                    randomPrefab.pluginPrefab   = pluginPrefab;
                    randomPrefab.name           = randomPrefab.pluginPrefab.prefabAsset.name;

                    AssetDbEx.addObjectToAsset(randomPrefab, this);
                    createdPrefabs.Add(randomPrefab);
                    _randomPrefabBuffer.Add(randomPrefab);
                }
            }

            UndoEx.record(this);
            foreach (var randomPrefab in _randomPrefabBuffer)
                _prefabs.Add(randomPrefab);

            EditorUtility.SetDirty(this);
            _probabilityTableDirty = true;

            PluginProgressDialog.end();

            UndoEx.restoreEnabledState();
        }

        public void deletePrefab(RandomPrefab prefab)
        {
            if (prefab != null)
            {
                if (containsPrefab(prefab))
                {
                    UndoEx.record(this);
                    _prefabs.Remove(prefab);
                    UndoEx.destroyObjectImmediate(prefab);
                    _probabilityTableDirty = true;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void deletePrefabs(List<RandomPrefab> prefabs)
        {
            if (prefabs.Count != 0)
            {
                UndoEx.record(this);
                _randomPrefabBuffer.Clear();
                foreach (var prefab in prefabs)
                {
                    if (containsPrefab(prefab))
                    {
                        _prefabs.Remove(prefab);
                        _randomPrefabBuffer.Add(prefab);
                    }
                }

                foreach (var prefab in _randomPrefabBuffer)
                    UndoEx.destroyObjectImmediate(prefab);

                _probabilityTableDirty = true;
                EditorUtility.SetDirty(this);
            }
        }

        public void deleteAllPrefabs()
        {
            if (_prefabs.Count != 0)
            {
                UndoEx.record(this);

                _randomPrefabBuffer.Clear();
                _randomPrefabBuffer.AddRange(_prefabs);
                _prefabs.Clear();

                foreach (var prefab in _randomPrefabBuffer)
                    UndoEx.destroyObjectImmediate(prefab);

                _probabilityTableDirty = true;
                EditorUtility.SetDirty(this);
            }
        }

        public bool containsPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var randPrefab in _prefabs)
            {
                if (randPrefab.pluginPrefab == pluginPrefab) return true;
            }

            return false;
        }

        public bool containsPrefab(RandomPrefab randomPrefab)
        {
            return _prefabs.Contains(randomPrefab);
        }

        public bool containsPrefabAsset(GameObject prefabAsset)
        {
            foreach (var p in _prefabs)
            {
                if (p.prefabAsset == prefabAsset) return true;
            }

            return false;
        }

        public RandomPrefab getPrefab(int index)
        {
            return _prefabs[index];
        }

        public RandomPrefab getPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab.pluginPrefab == pluginPrefab) return prefab;
            }

            return null;
        }

        public void getPrefabs(List<RandomPrefab> randomPrefabs)
        {
            randomPrefabs.Clear();
            randomPrefabs.AddRange(_prefabs);
        }

        public void getPrefabs(List<PluginPrefab> pluginPrefabs, List<RandomPrefab> randomPrefabs, bool append)
        {
            if (!append) randomPrefabs.Clear();
            foreach (var pluginPrefab in pluginPrefabs)
            {
                RandomPrefab randomPrefab = getPrefab(pluginPrefab);
                if (randomPrefab != null) randomPrefabs.Add(randomPrefab);
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

        private void OnEnable()
        {
            _probabilityTableDirty = true;
            Undo.undoRedoPerformed += onUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
        }

        private void OnDestroy()
        {
            deleteAllPrefabs();
        }
    }
}
#endif