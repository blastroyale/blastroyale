#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectScaleStartDataMap
    {
        private Dictionary<GameObject, ObjectScaleStartData>    _map    = new Dictionary<GameObject, ObjectScaleStartData>();

        public bool                                             empty   { get { return _map.Count == 0; } }

        public ObjectScaleStartData getData(GameObject gameObject)
        {
            return _map[gameObject];
        }

        public Vector3 getLocalScale(GameObject gameObject)
        {
            return _map[gameObject].localScale;
        }

        public void get(IEnumerable<GameObject> gameObjects, Vector3 scalePivot)
        {
            _map.Clear();
            foreach (var go in gameObjects)
                _map.Add(go, new ObjectScaleStartData() { localScale = go.transform.localScale, pivotToPosition = (go.transform.position - scalePivot) });
        }

        public void clear()
        {
            _map.Clear();
        }
    }
}
#endif