#if UNITY_EDITOR
//#define INT_RANGE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class IntRangePrefabProfile : Profile
    {
        private SerializedObject        _serializedObject;
        [SerializeField]
        private List<IntRangePrefab>    _prefabs                    = new List<IntRangePrefab>();
        [SerializeField]
        private IntRangePrefab          _defaultPickPrefab          = null;
        [NonSerialized]
        private List<IntRangePrefab>    _irPrefabBuffer             = new List<IntRangePrefab>();

        public int                      numPrefabs                  { get { return _prefabs.Count; } }
        public IntRangePrefab           defaultPickPrefab           { get { return _defaultPickPrefab; } }

        public SerializedObject         serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
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
            foreach (var randomPrefab in prefabsToRemove)
            {
                _prefabs.Remove(randomPrefab);
                AssetDbEx.removeObjectFromAsset(randomPrefab, this);
                DestroyImmediate(randomPrefab);
            }
        }

        public void setDefaultPickPrefab(IntRangePrefab irPrefab)
        {
            if (irPrefab == null)
            {
                UndoEx.record(this);
                _defaultPickPrefab = null;
                EditorUtility.SetDirty(this);
            }
            else
            if (containsPrefab(irPrefab))
            {
                UndoEx.record(this);
                _defaultPickPrefab = irPrefab;
                EditorUtility.SetDirty(this);
            }
        }

        public bool isDefaultPickPrefab(IntRangePrefab irPrefab)
        {
            return irPrefab == _defaultPickPrefab;
        }

        public IntRangePrefab pickPrefab(int val)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab.used && prefab.isValueInRange(val)) return prefab;
            }

            return _defaultPickPrefab;
        }

        public IntRangePrefab createPrefab(PluginPrefab pluginPrefab)
        {
            #if INT_RANGE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
            if (!containsPrefab(pluginPrefab))
            #endif
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled = false;

                UndoEx.record(this);
                var irPrefab            = UndoEx.createScriptableObject<IntRangePrefab>();
                irPrefab.pluginPrefab   = pluginPrefab;
                irPrefab.name           = irPrefab.pluginPrefab.prefabAsset.name;

                _prefabs.Add(irPrefab);
                AssetDbEx.addObjectToAsset(irPrefab, this);
                EditorUtility.SetDirty(this);

                UndoEx.restoreEnabledState();
                return irPrefab;
            }

            #if INT_RANGE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
            return null;
            #endif
        }

        public void createPrefabs(List<PluginPrefab> pluginPrefabs, List<IntRangePrefab> createdPrefabs, bool appendCreated, string progressTitle)
        {
            _irPrefabBuffer.Clear();
            PluginProgressDialog.begin(progressTitle);
            if (!appendCreated) createdPrefabs.Clear();

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            for (int prefabIndex = 0; prefabIndex < pluginPrefabs.Count; ++prefabIndex)
            {
                var pluginPrefab = pluginPrefabs[prefabIndex];
                PluginProgressDialog.updateItemProgress(pluginPrefab.prefabAsset.name, (prefabIndex + 1) / (float)pluginPrefabs.Count);

                #if INT_RANGE_PREFAB_PROFILE_NO_DUPLICATE_PREFABS
                if (!containsPrefab(pluginPrefab))
                #endif
                {
                    var irPrefab            = UndoEx.createScriptableObject<IntRangePrefab>();
                    irPrefab.pluginPrefab   = pluginPrefab;
                    irPrefab.name           = irPrefab.pluginPrefab.prefabAsset.name;

                    AssetDbEx.addObjectToAsset(irPrefab, this);
                    createdPrefabs.Add(irPrefab);
                    _prefabs.Add(irPrefab);
                }
            }

            UndoEx.record(this);
            foreach (var irPrefab in _irPrefabBuffer)
                _prefabs.Add(irPrefab);

            EditorUtility.SetDirty(this);
            PluginProgressDialog.end();

            UndoEx.restoreEnabledState();
        }

        public void deletePrefab(IntRangePrefab prefab)
        {
            if (prefab != null)
            {
                if (containsPrefab(prefab))
                {
                    UndoEx.record(this);
                    _prefabs.Remove(prefab);
                    if (prefab == _defaultPickPrefab) _defaultPickPrefab = null;
                    UndoEx.destroyObjectImmediate(prefab);
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void deletePrefabs(List<IntRangePrefab> prefabs)
        {
            if (prefabs.Count != 0)
            {
                UndoEx.record(this);
                _irPrefabBuffer.Clear();
                foreach (var prefab in prefabs)
                {
                    if (containsPrefab(prefab))
                    {
                        _prefabs.Remove(prefab);
                        if (prefab == _defaultPickPrefab) _defaultPickPrefab = null;
                        _irPrefabBuffer.Add(prefab);
                    }
                }

                foreach (var irPrefab in _irPrefabBuffer)
                    UndoEx.destroyObjectImmediate(irPrefab);

                EditorUtility.SetDirty(this);
            }
        }

        public void deleteAllPrefabs()
        {
            if (_prefabs.Count != 0)
            {
                UndoEx.record(this);

                _irPrefabBuffer.Clear();
                _irPrefabBuffer.AddRange(_prefabs);
                _prefabs.Clear();
                _defaultPickPrefab = null;

                foreach (var irPrefab in _irPrefabBuffer)
                    UndoEx.destroyObjectImmediate(irPrefab);

                EditorUtility.SetDirty(this);
            }
        }

        public bool containsPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var irPrefab in _prefabs)
            {
                if (irPrefab.pluginPrefab == pluginPrefab) return true;
            }

            return false;
        }

        public bool containsPrefab(IntRangePrefab irPrefab)
        {
            return _prefabs.Contains(irPrefab);
        }

        public IntRangePrefab getPrefab(PluginPrefab pluginPrefab)
        {
            foreach (var prefab in _prefabs)
            {
                if (prefab.pluginPrefab == pluginPrefab) return prefab;
            }

            return null;
        }

        public void getPrefabs(List<IntRangePrefab> irPrefabs)
        {
            irPrefabs.Clear();
            irPrefabs.AddRange(_prefabs);
        }

        public void getPrefabs(List<PluginPrefab> pluginPrefabs, List<IntRangePrefab> irPrefabs, bool append)
        {
            if (!append) irPrefabs.Clear();
            foreach (var pluginPrefab in pluginPrefabs)
            {
                IntRangePrefab irPrefab = getPrefab(pluginPrefab);
                if (irPrefab != null) irPrefabs.Add(irPrefab);
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

            return nullPrefabs.Count;
        }

        private void onUndoRedo()
        {
        }

        private void OnEnable()
        {
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