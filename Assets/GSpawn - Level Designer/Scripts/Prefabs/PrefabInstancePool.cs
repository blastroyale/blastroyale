#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    // Note: Needed because we can't use List<GameObject> directly as a value
    //       in the serializable dictionary defined below. It doesn't serialize properly.
    //       Could this be hiding another bug in other parts of the code?
    [Serializable]
    public class PrefabInstanceList
    {
        [SerializeField]
        public List<GameObject> list = new List<GameObject>();
    }

    [Serializable]
    public class PrefabAssetInstanceMap : SerializableDictionary<GameObject, PrefabInstanceList> { }

    public class PrefabInstancePool : ScriptableObject
    {
        [SerializeField]
        private PrefabAssetInstanceMap _prefabInstanceMap = new PrefabAssetInstanceMap();

        public void clear()
        {
            foreach (var pair in _prefabInstanceMap)
            {
                var prefabInstanceList = pair.Value;
                foreach (var prefabInstance in prefabInstanceList.list)
                {
                    if (prefabInstance != null)
                    {
                        GameObject.DestroyImmediate(prefabInstance);
                    }
                }
                prefabInstanceList.list.Clear();
            }

            _prefabInstanceMap.Clear();
        }

        public void onPrefabAssetWillBeDeleted(GameObject prefabAsset)
        {
            PrefabInstanceList prefabInstanceList;
            if (_prefabInstanceMap.TryGetValue(prefabAsset, out prefabInstanceList))
            {
                foreach (var prefabInstance in prefabInstanceList.list)
                {
                    if (prefabInstance != null)
                        GameObject.DestroyImmediate(prefabInstance);
                }
            }
        }

        public GameObject acquirePrefabInstance(GameObject prefabAsset, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var prefabInstance                  = acquirePrefabInstance(prefabAsset);
            prefabInstance.transform.position   = position;
            prefabInstance.transform.rotation   = rotation;
            prefabInstance.transform.localScale = scale;

            return prefabInstance;
        }

        public GameObject acquirePrefabInstance(GameObject prefabAsset)
        {
            PrefabInstanceList prefabInstanceList;
            if (!_prefabInstanceMap.TryGetValue(prefabAsset, out prefabInstanceList))
            {
                prefabInstanceList = new PrefabInstanceList();
                _prefabInstanceMap.Add(prefabAsset, prefabInstanceList);
            }

            GameObject prefabInstance;
            if (prefabInstanceList.list.Count == 0) prefabInstance = createPrefabInstance(prefabAsset);
            else
            {
                int last = prefabInstanceList.list.Count - 1;
                prefabInstance = prefabInstanceList.list[last];
                prefabInstanceList.list.RemoveAt(last);

                // Note: Could have been destroyed (e.g. spawn curve, switch to selection mode, delete curve objects).
                if (prefabInstance == null) prefabInstance = createPrefabInstance(prefabAsset);
            }

            prefabInstance.SetActive(true);
            prefabInstance.tag          = prefabAsset.tag;
            prefabInstance.hideFlags    = prefabInstance.hideFlags & (~HideFlags.HideInHierarchy);
            return prefabInstance;
        }

        public void releasePrefabInstance(GameObject prefabAsset, GameObject prefabInstance)
        {
            _prefabInstanceMap[prefabAsset].list.Add(prefabInstance);
            prefabInstance.makeEditorOnly();
            prefabInstance.hideFlags |= HideFlags.HideInHierarchy;
            prefabInstance.SetActive(false);
        }

        public void hidePrefabInstancesInInspector()
        {
            foreach (var pair in _prefabInstanceMap)
            {
                var instanceList = pair.Value;
                instanceList.list.RemoveAll(item => item == null);
                foreach (var prefabInstance in instanceList.list)
                {
                    if (!prefabInstance.activeSelf)
                    {
                        prefabInstance.hideFlags |= HideFlags.HideInHierarchy;
                    }
                }
            }
        }

        private GameObject createPrefabInstance(GameObject prefabAsset)
        {
            GameObject prefabInstance = prefabAsset.instantiatePrefab();
            prefabInstance.SetActive(false);
            prefabInstance.makeEditorOnly();
            prefabInstance.hideFlags |= HideFlags.HideInHierarchy;

            return prefabInstance;
        }

        private void OnDestroy()
        {
            clear();
        }
    }
}
#endif