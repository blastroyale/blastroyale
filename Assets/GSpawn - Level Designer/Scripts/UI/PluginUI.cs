#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public abstract class PluginUI : ScriptableObject
    {
        // Note: Must be non-serialized. Otherwise, it appears to serialize it.
        [NonSerialized]
        private bool                _ready = false;
        private VisualElement       _rootElement;
        private VisualElement       _contentContainer;
        private PluginWindow        _targetWindow;
        private Editor              _targetEditor;

        [NonSerialized]
        private SerializedObject    _serializedObject;

        protected VisualElement     rootElement         { get { return _rootElement; } }
        protected VisualElement     contentContainer    { get { return _contentContainer; } }

        public SerializedObject     serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }
        public bool                 ready               { get { return _ready; } }
        public PluginWindow         targetWindow        { get { return _targetWindow; } }
        public Editor               targetEditor        { get { return _targetEditor; } }
        public bool                 uiVisible           { get { return _contentContainer.isDisplayVisible(); } }
        public bool                 uiVisibleAndReady   { get { return ready && uiVisible; } }
        public bool                 isEnabledSelf       { get { return _contentContainer != null && _contentContainer.enabledSelf; } }

        public void setVisible(bool visible)
        {
            if (_contentContainer == null) return;

            _contentContainer.setDisplayVisible(visible);
            if (uiVisibleAndReady) refresh();
        }

        public void setEnabled(bool enabled)
        {
            if (_contentContainer == null) return;
            _contentContainer.SetEnabled(enabled);
        }

        public void refresh() 
        {
            if (uiVisibleAndReady) onRefresh();
        }

        public void build(VisualElement rootElement, PluginWindow targetWindow)
        {
            _targetWindow       = targetWindow;
            _rootElement        = rootElement;
            _contentContainer   = new VisualElement();
            _contentContainer.setDisplayVisible(true);
            _rootElement.Add(_contentContainer);
            onBuild();
            _ready = true;
        }

        public void build(VisualElement rootElement, Editor targetEditor)
        {
            _targetEditor       = targetEditor;
            _rootElement        = rootElement;
            _contentContainer   = new VisualElement();
            _contentContainer.setDisplayVisible(true);
            _rootElement.Add(_contentContainer);
            onBuild();
            _ready = true;
        }

        public virtual void onGUI() { }

        public void onPluginUIAssetWillBeDestroyed()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
            onDestroy();
        }

        protected abstract void onBuild();
        protected abstract void onRefresh();
        protected virtual void onEnabled() { }
        protected virtual void onDisabled() { }
        protected virtual void onDestroy() { }
        protected virtual void onUndoRedo() { }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += onUndoRedo;
            onEnabled();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
            onDisabled();
        }

        private void OnDestroy()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
            onDestroy();
        }
    }
}
#endif