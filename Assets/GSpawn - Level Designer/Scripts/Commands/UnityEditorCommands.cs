#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public static class UnityEditorCommands
    {
        private static List<GameObject>     _selectedEditorObjectsBackup    = new List<GameObject>();
        private static List<GameObject>     _duplicateParentsBuffer         = new List<GameObject>();
        private static List<GameObject>     _duplicatesBuffer               = new List<GameObject>();

        public static string                selectAllName                   { get { return "SelectAll"; } }
        public static string                duplicateName                   { get { return "Duplicate"; } }
        public static string                deleteName                      { get { return "Delete"; } }
        public static string                softDeleteName                  { get { return "SoftDelete"; } }
        public static string                frameSelectedName               { get { return "FrameSelected"; } }
        public static string                frameSelectedWithLockName       { get { return "FrameSelectedWithLock"; } }

        public static void softDelete(List<GameObject> gameObjects)
        {
            var lastSceneView = SceneView.lastActiveSceneView;
            if (lastSceneView == null) return;

            SelectionEx.getGameObjects(_selectedEditorObjectsBackup);
            SelectionEx.clearGameObjects();
            SelectionEx.appendGameObjects(gameObjects);

            lastSceneView.Focus();
            EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent(softDeleteName));

            SelectionEx.clearGameObjects();
            SelectionEx.appendGameObjects(_selectedEditorObjectsBackup);
        }

        public static void frameSelected(List<GameObject> gameObjects)
        {
            var lastSceneView = SceneView.lastActiveSceneView;
            if (lastSceneView == null) return;

            SelectionEx.getGameObjects(_selectedEditorObjectsBackup);
            SelectionEx.clearGameObjects();
            SelectionEx.appendGameObjects(gameObjects);

            lastSceneView.Focus();
            EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent(frameSelectedName));

            SelectionEx.clearGameObjects();
            SelectionEx.appendGameObjects(_selectedEditorObjectsBackup);
        }

        // Note: Call only from 'Repaint' events to correctly access the duplicate objects.
        public static void duplicate(List<GameObject> gameObjects, List<GameObject> duplicates)
        {
            duplicates.Clear();
            var lastSceneView = SceneView.lastActiveSceneView;
            if (lastSceneView != null)
            {
                SelectionEx.getGameObjects(_selectedEditorObjectsBackup);
                SelectionEx.clearGameObjects();
                SelectionEx.appendGameObjects(gameObjects);

                lastSceneView.Focus();
                EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent(duplicateName));

                // Note: This only works if the duplicate command is sent from a 'Repaint' event.
                //       Otherwise, the command doesn't replace the source parents with the duplicates
                //       and in that case, we can't access the duplicates.
                SelectionEx.getGameObjects(duplicates);
                GameObjectEx.getParents(duplicates, _duplicateParentsBuffer);
                foreach (var duplicate in _duplicateParentsBuffer)
                    duplicate.transform.SetSiblingIndex(0);

                SelectionEx.clearGameObjects();
                SelectionEx.appendGameObjects(_selectedEditorObjectsBackup);
            }
        }

        // Note: Call only from 'Repaint' events to correctly access the duplicate objects.
        // Note: It seems that KeyDown events are also OK.
        // Note: MouseDown events produce don't report the created objects correctly.
        public static void duplicate(List<GameObject> gameObjects)
        {
            var lastSceneView = SceneView.lastActiveSceneView;
            if (lastSceneView != null)
            {
                SelectionEx.getGameObjects(_selectedEditorObjectsBackup);
                SelectionEx.clearGameObjects();
                SelectionEx.appendGameObjects(gameObjects);

                lastSceneView.Focus();
                EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent(duplicateName));

                // Note: This only works if the duplicate command is sent from a 'Repaint' event.
                //       Otherwise, the command doesn't replace the source parents with the duplicates
                //       and in that case, we can't access the duplicates.
                SelectionEx.getGameObjects(_duplicatesBuffer);
                GameObjectEx.getParents(_duplicatesBuffer, _duplicateParentsBuffer);
                foreach (var duplicate in _duplicateParentsBuffer)
                    duplicate.transform.SetSiblingIndex(0);

                SelectionEx.clearGameObjects();
                SelectionEx.appendGameObjects(_selectedEditorObjectsBackup);
            }
        }

        // Note: Call only from 'Repaint' events to correctly access the duplicate objects.
        // Note: It seems that KeyDown events are also OK.
        // Note: MouseDown events produce don't report the created objects correctly.
        public static GameObject duplicate(GameObject gameObject)
        {
            var lastSceneView = SceneView.lastActiveSceneView;
            if (lastSceneView != null)
            {
                SelectionEx.getGameObjects(_selectedEditorObjectsBackup);
                SelectionEx.clearGameObjects();
                SelectionEx.appendGameObject(gameObject);

                lastSceneView.Focus();
                EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent(duplicateName));

                // Note: This only works if the duplicate command is sent from a 'Repaint' event.
                //       Otherwise, the command doesn't replace the source parents with the duplicates
                //       and in that case, we can't access the duplicates.
                SelectionEx.getGameObjects(_duplicatesBuffer);
                GameObjectEx.getParents(_duplicatesBuffer, _duplicateParentsBuffer);
                foreach (var duplicate in _duplicateParentsBuffer)
                    duplicate.transform.SetSiblingIndex(0);

                SelectionEx.clearGameObjects();
                SelectionEx.appendGameObjects(_selectedEditorObjectsBackup);

                return _duplicatesBuffer[0];
            }

            return null;
        }
    }
}
#endif