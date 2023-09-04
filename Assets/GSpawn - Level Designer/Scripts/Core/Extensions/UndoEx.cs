#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public static class UndoEx
    {
        private static bool             _isEnabled          = true;
        private static Stack<bool>      _enabledStateStack  = new Stack<bool>();

        public static bool              enabled             { get { return _isEnabled; } set { _isEnabled = value; } }

        public static void saveEnabledState()
        {
            _enabledStateStack.Push(enabled);
        }

        public static void restoreEnabledState()
        {
            if (_enabledStateStack.Count != 0) _isEnabled = _enabledStateStack.Pop();
        }

        public static void record(Object recordObject)
        {
            if (!enabled) return;
            Undo.RecordObject(recordObject, GSpawn.pluginName);
        }

        public static void record(Object recordObject, string name)
        {
            if (!enabled) return;
            Undo.RecordObject(recordObject, name);
        }

        public static T addComponent<T>(GameObject gameObject) where T : Component
        {
            if (enabled) return Undo.AddComponent<T>(gameObject);
            else return gameObject.AddComponent<T>();
        }

        public static T createScriptableObject<T>() where T : ScriptableObject
        {
            if (enabled)
            {
                var scriptableObject = ScriptableObject.CreateInstance<T>();
                Undo.RegisterCreatedObjectUndo(scriptableObject, GSpawn.pluginName);

                return scriptableObject;
            }
            else return ScriptableObject.CreateInstance<T>();
        }

        public static T cloneScriptableObject<T>(T source) where T : ScriptableObject
        {
            if (enabled)
            {
                var clone = ScriptableObject.Instantiate(source);
                Undo.RegisterCreatedObjectUndo(clone, GSpawn.pluginName);

                return clone;
            }
            else return ScriptableObject.Instantiate(source);
        }

        public static void recordGameObject(GameObject gameObject)
        {
            if (!enabled) return;

            record(gameObject);
        }

        public static void recordGameObjects(IEnumerable<GameObject> gameObjects)
        {
            if (!enabled) return;

            foreach (var gameObject in gameObjects)
                record(gameObject);
        }

        public static void setTransformParent(Transform transform, Transform newParentTransform)
        {
            if (enabled) Undo.SetTransformParent(transform, newParentTransform, GSpawn.pluginName);
            else transform.parent = newParentTransform;
        }

        public static void setObjectsTransformParent(IEnumerable<GameObject> gameObjects, Transform newParentTransform)
        {
            if (enabled)
            {
                foreach (var gameObject in gameObjects)
                    Undo.SetTransformParent(gameObject.transform, newParentTransform, GSpawn.pluginName);
            }
            else
            {
                foreach (var gameObject in gameObjects)
                    gameObject.transform.parent = newParentTransform;
            }
        }

        public static void registerCompleteObjectUndo(UnityEngine.Object obj)
        {
            if (!enabled) return;

            Undo.RegisterCompleteObjectUndo(obj, GSpawn.pluginName);
        }

        public static void registerChildrenOrderUndo(UnityEngine.Object obj)
        {
            if (!enabled) return;

            Undo.RegisterChildrenOrderUndo(obj, GSpawn.pluginName);
        }

        public static void recordGameObjectTransforms(IEnumerable<GameObject> gameObjects)
        {
            if (!enabled) return;

            foreach (var gameObject in gameObjects)
                record(gameObject.transform);
        }

        public static void recordTransform(Transform transform)
        {
            if (!enabled) return;
            record(transform);
        }

        public static void recordTransforms(IEnumerable<Transform> transforms)
        {
            if (!enabled) return;

            foreach (var transform in transforms)
                record(transform);
        }

        public static void registerCreatedObject<T>(T createdObject) where T : UnityEngine.Object
        {
            if (!enabled) return;
            Undo.RegisterCreatedObjectUndo(createdObject, GSpawn.pluginName);
        }

        public static void registerCreatedObject<T>(T createdObject, string name) where T : UnityEngine.Object
        {
            if (!enabled) return;
            Undo.RegisterCreatedObjectUndo(createdObject, name);
        }

        public static void destroyObjectImmediate(Object unityObject)
        {
            if (enabled) Undo.DestroyObjectImmediate(unityObject);
            else Object.DestroyImmediate(unityObject, true);
        }

        public static void destroyGameObjectsImmediate(List<GameObject> gameObjects)
        {
            if (enabled)
            {
                foreach (var gameObj in gameObjects)
                {
                    if (gameObj != null)
                        Undo.DestroyObjectImmediate(gameObj);
                }
            }
            else
            {
                foreach (var gameObj in gameObjects)
                {
                    if (gameObj != null)
                        GameObject.DestroyImmediate(gameObj);
                }
            }
        }

        public static void destroyGameObjectImmediate(GameObject gameObject)
        {
            if (enabled) Undo.DestroyObjectImmediate(gameObject);
            else GameObject.DestroyImmediate(gameObject);
        }
    }
}
#endif