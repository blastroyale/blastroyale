#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GSpawn
{
    public static class SceneViewEx
    {
        public static bool sceneViewHasFocus()
        {
            return SceneView.lastActiveSceneView != null && EditorWindow.focusedWindow == SceneView.lastActiveSceneView;
        }

        public static void frame(Bounds bounds, bool instant)
        {
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(bounds, instant);
        }

        public static void focus()
        {
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Focus();
        }

        public static bool containsCursor(this SceneView sceneView, Event e)
        {
            Vector2 mousePos = e.mousePosition;
            return sceneView.containsPoint(mousePos);
        }

        public static bool containsCursor(Event e)
        {
            Vector2 mousePos = e.mousePosition;
            return containsPoint(mousePos);
        }

        public static bool containsPoint(this SceneView sceneView, Vector2 point)
        {
            Camera camera = sceneView.camera;
            return camera.pixelRect.Contains(point);
        }

        public static bool containsPoint(Vector2 point)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return false;
            Camera camera = sceneView.camera;
            
            return camera.pixelRect.Contains(point);
        }
    }
}
#endif