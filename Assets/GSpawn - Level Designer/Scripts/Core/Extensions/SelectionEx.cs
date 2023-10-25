#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public static class SelectionEx
    {
        public static void clear()
        {
            Selection.objects = new UnityEngine.Object[0];
        }

        public static bool containsObjectsOfType<T>() where T : Component
        {
            var selectedObjects = new List<GameObject>();
            getGameObjects(selectedObjects);

            foreach (var go in selectedObjects)
                if (go.GetComponent<T>() != null) return true;

            return false;
        }

        public static void clearGameObjects()
        {
            var selectedObjects = new List<UnityEngine.Object>(Selection.objects);
            selectedObjects.RemoveAll(item => (item as GameObject) != null && (item as GameObject).isSceneObject());
            Selection.objects = selectedObjects.ToArray();
        }

        public static void getGameObjects(List<GameObject> gameObjects)
        {
            var transforms = Selection.GetTransforms(SelectionMode.ExcludePrefab);
            GameObjectEx.getGameObjects(transforms, gameObjects);
        }

        public static void appendGameObject(GameObject gameObject)
        {
            var selectedObjects = new List<UnityEngine.Object>(Selection.objects);
            selectedObjects.Add(gameObject as UnityEngine.Object);
            Selection.objects = selectedObjects.ToArray();
        }

        public static void appendGameObjects(List<GameObject> gameObjects)
        {
            var selectedObjects = new List<UnityEngine.Object>(Selection.objects);
            foreach(var gameObject in gameObjects)
                selectedObjects.Add(gameObject as UnityEngine.Object);

            Selection.objects = selectedObjects.ToArray();
        }

        public static void removeGameObject(GameObject gameObject)
        {
            var selectedObjects = new List<UnityEngine.Object>(Selection.objects);
            selectedObjects.Remove(gameObject);
            Selection.objects = selectedObjects.ToArray();
        }

        public static void removeGameObjects(List<GameObject> gameObjects)
        {
            var selectedObjects = new List<UnityEngine.Object>(Selection.objects);
            selectedObjects.RemoveAll(item => (item as GameObject) != null && gameObjects.Contains(item as GameObject));
            Selection.objects = selectedObjects.ToArray();
        }
    }
}
#endif