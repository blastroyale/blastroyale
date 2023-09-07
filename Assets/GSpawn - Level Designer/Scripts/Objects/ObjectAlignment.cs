#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class ObjectAlignment
    {
        private static List<GameObject>     _parentObjectBuffer     = new List<GameObject>();
        private static List<Transform>      _transformBuffer        = new List<Transform>();

        public static void alignObjects(IEnumerable<GameObject> gameObjects, int axisIndex, bool allowUndoRedo)
        {
            GameObjectEx.getParents(gameObjects, _parentObjectBuffer);
            TransformEx.getTransforms(gameObjects, _transformBuffer);

            float avgPos = 0.0f;
            foreach (var transform in _transformBuffer)
                avgPos += transform.position[axisIndex];

            if (allowUndoRedo) UndoEx.recordTransforms(_transformBuffer);
            avgPos /= (float)_transformBuffer.Count;
            foreach (var transform in _transformBuffer)
            {
                Vector3 newPos      = transform.position;
                newPos[axisIndex]   = avgPos;
                transform.position  = newPos;
            }
        }
    }
}
#endif