#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class ObjectEvents
    {
        public static void onObjectsTransformed()
        {
            ObjectSelectionGizmos.instance.onTargetObjectTransformsChanged();
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection) PluginInspectorUI.instance.refresh();
        }

        public static void onObjectsTransformedByGizmo(ObjectTransformGizmo gizmo)
        {
            PluginInspectorUI.instance.refresh();
        }

        public static void onObjectsTransformedByUI()
        {
            ObjectSelection.instance.onObjectTransformsChanged();
            PluginInspectorUI.instance.refresh();
        }

        public static void onObjectsWillBeDestroyed(List<GameObject> gameObjects)
        {
            ObjectSelection.instance.onObjectsWillBeDestroyed(gameObjects, true);
            PluginScene.instance.onObjectsWillBeDestroyed(gameObjects);
        }

        public static void onObjectWillBeDestroyed(GameObject gameObject)
        {
            PluginScene.instance.onObjectWillBeDestroyed(gameObject);
        }

        public static void onObjectSpawned(GameObject gameObject)
        {
            PluginScene.instance.onObjectSpawned(gameObject);
        }

        public static void onObjectsSpawned(List<GameObject> gameObjects)
        {
            foreach (var go in gameObjects)
                PluginScene.instance.onObjectSpawned(go);
        }
    }
}
#endif