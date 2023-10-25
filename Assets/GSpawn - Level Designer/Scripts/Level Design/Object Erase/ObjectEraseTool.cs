#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public enum ObjectEraseToolId
    {
        Cursor = 0,
        Brush2D,
        Brush3D
    }

    public abstract class ObjectEraseTool
    {
        [NonSerialized]
        private List<GameObject>            _gameObjectBuffer = new List<GameObject>();

        public abstract ObjectEraseToolId   toolId { get; }

        public void onSceneGUI()
        {
            doOnSceneGUI();
            draw();
        }

        protected abstract void doOnSceneGUI();
        protected abstract void draw();

        protected void eraseGameObject(GameObject gameObject)
        {
            // Note: Can be null when deleting objects in a list and the parent of a hierarchy
            //       is deleted before its children.
            if (gameObject == null) return;

            var outerPrefabInstance = gameObject.getOutermostPrefabInstanceRoot();
            if (outerPrefabInstance != null) PluginScene.instance.deleteObject(outerPrefabInstance);
            else PluginScene.instance.deleteObject(gameObject);
        }

        protected void eraseGameObjects(IEnumerable<GameObject> gameObjects)
        {
            GameObjectEx.getPrefabInstancesAndNonInstances(gameObjects, _gameObjectBuffer);
            foreach (var go in _gameObjectBuffer)
                eraseGameObject(go);
        }
    }
}
#endif