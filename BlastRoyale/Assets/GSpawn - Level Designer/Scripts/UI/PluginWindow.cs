#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace GSpawn
{
    public abstract class PluginWindow : EditorWindow
    {
        [NonSerialized]
        protected UIHeader      _header;

        [SerializeField]
        private bool            _needsRebuild               = true;
        [SerializeField]
        private GSpawn          _ownerPlugin;
        [SerializeField]
        protected bool          _requiresPluginInstance     = true;

        public static T show<T>(string title) where T : PluginWindow
        {
            T window = GetWindow<T>(title) as T;
            window.Show();

            return window;
        }

        public static T showUtility<T>(string title) where T : PluginWindow
        {
            T window = GetWindow<T>(true, title) as T;
            window.ShowUtility();

            return window;
        }

        public static T showModalUtility<T>(string title) where T : PluginWindow
        {
            T window = GetWindow<T>(true, title) as T;
            window.ShowModalUtility();

            return window;
        }

        public void setMinMaxSize(Vector2 minMaxSize)
        {
            this.minSize = minMaxSize;
            this.maxSize = minMaxSize;
        }

        public void setMinMaxSize(Vector2 minSize, Vector2 maxSize)
        {
            this.minSize = minSize;
            this.maxSize = maxSize;
        }

        public void setSize(Vector2 size)
        {
            position = new Rect(position.position, size);
        }

        public void centerOnScreen()
        {
            Vector2 size = position.size;
            position = new Rect(new Vector2((Screen.currentResolution.width - size.x) * 0.5f, (Screen.currentResolution.height - size.y) * 0.5f), size);
        }

        protected abstract void onBuildUI();
        protected virtual void onGUI() { }
        protected virtual void onProjectChanged() { }
        protected virtual void onEnabled() { }
        protected virtual void onDisabled() { }

        private void OnGUI()
        {
            updateOwnerPlugin();

            if (_requiresPluginInstance && _ownerPlugin == null) return;
            if (_needsRebuild)
            {
                rootVisualElement.Clear();
                onBuildUI();
                _needsRebuild = false;
            }

            onGUI();
        }

        private void Update()
        {
            if (_requiresPluginInstance && GSpawn.numPlugins == 0) Close();
        }

        private void OnEnable()
        {
            EditorApplication.modifierKeysChanged += Repaint;
            EditorApplication.projectChanged += onProjectChanged;
            _needsRebuild = true;
            onEnabled();
        }

        private void OnDisable()
        {
            onDisabled();
            EditorApplication.modifierKeysChanged -= Repaint;
            EditorApplication.projectChanged -= onProjectChanged;
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void updateOwnerPlugin()
        {
            if (_ownerPlugin == null || (GSpawn.active != null && _ownerPlugin != GSpawn.active))
            {
                rootVisualElement.Clear();
                _ownerPlugin = GSpawn.active;

                // Note: Needed when existing playmode.
                if (_ownerPlugin != null)
                    onBuildUI();
            }
        }
    }
}
#endif