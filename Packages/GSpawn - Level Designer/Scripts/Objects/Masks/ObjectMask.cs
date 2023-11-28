#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class ObjectMask : ScriptableObject
    {
        [NonSerialized]
        private List<ObjectMaskEntry>   _entryBuffer        = new List<ObjectMaskEntry>();

        [SerializeField]
        private ListViewState                                       _maskViewState;
        private ListView<UIObjectMaskEntryItem, ObjectMaskEntry>    _maskView;

        [SerializeField]
        private List<ObjectMaskEntry>   _entries            = new List<ObjectMaskEntry>();
        [SerializeField]
        private GameObjectHashSet       _maskedObjects      = new GameObjectHashSet();

        public int                                                  numObjects      { get { return _entries.Count; } }
        public ListView<UIObjectMaskEntryItem, ObjectMaskEntry>     maskView        { get { return _maskView; } }

        public void refreshMaskView()
        {
            if (_maskView != null)
            {
                _maskView.onBeginBuild();
                foreach(var entry in _entries)
                {
                    _maskView.addItem(entry, true);
                }
                _maskView.onEndBuild();
            }
        }

        public void maskObject(GameObject gameObject)
        {
            if (gameObject == null || isObjectMasked(gameObject) || !gameObject.isSceneObject()) return;

            UndoEx.record(this);
            ObjectMaskEntry maskEntry   = UndoEx.createScriptableObject<ObjectMaskEntry>();
            maskEntry.gameObject        = gameObject;
            _entries.Add(maskEntry);
            _maskedObjects.Add(gameObject);

            refreshMaskView();
        }

        public void maskObjects(IEnumerable<GameObject> gameObjects)
        {
            UndoEx.record(this);
            foreach (var gameObject in gameObjects)
            {
                if (gameObject != null && !isObjectMasked(gameObject) && gameObject.isSceneObject())
                {
                    ObjectMaskEntry maskEntry   = UndoEx.createScriptableObject<ObjectMaskEntry>();
                    maskEntry.gameObject        = gameObject;
                    _entries.Add(maskEntry);
                    _maskedObjects.Add(gameObject);
                }
            }

            refreshMaskView();
        }

        public void unmaskObjects(IEnumerable<GameObject> gameObjects)
        {
            if (_entries.Count != 0)
            {
                UndoEx.record(this);
                if (_maskViewState != null) UndoEx.record(_maskViewState);
                var removedEntries = new List<ObjectMaskEntry>(_entries);

                foreach (var gameObject in gameObjects)
                {
                    if (isObjectMasked(gameObject))
                    {
                        var entry = findObjectMaskEntry(gameObject);
                        if (entry != null)
                        {
                            _entries.Remove(entry);
                            _maskedObjects.Remove(gameObject);
                            if (_maskView != null) _maskView.deleteItem(UIObjectMaskEntryItem.getItemId(entry));
                        }
                    }
                }

                foreach (var entry in removedEntries)
                    UndoEx.destroyObjectImmediate(entry);
            }
        }

        public void unmaskAllObjects()
        {
            if (_entries.Count != 0)
            {
                UndoEx.record(this);
                var removedEntries = new List<ObjectMaskEntry>(_entries);

                _entries.Clear();
                _maskedObjects.Clear();
                if (_maskViewState != null)
                {
                    UndoEx.record(_maskViewState);
                    _maskViewState.clearSelectionInfo();
                }

                foreach (var entry in removedEntries)
                    UndoEx.destroyObjectImmediate(entry);
            }

            refreshMaskView();
        }

        public ObjectMaskEntry findObjectMaskEntry(GameObject gameObject)
        {
            foreach (var entry in _entries)
                if (entry.gameObject == gameObject) return entry;

            return null;
        }

        public bool isObjectMasked(GameObject gameObject)
        {
            return _maskedObjects.Contains(gameObject);
        }

        public void buildUI(VisualElement parent, string sectionLabel)
        {
            if (!string.IsNullOrEmpty(sectionLabel)) UI.createSectionLabel(sectionLabel, parent);
            if (_maskViewState == null) _maskViewState = ScriptableObject.CreateInstance<ListViewState>();

            _maskView                   = new ListView<UIObjectMaskEntryItem, ObjectMaskEntry>(_maskViewState, parent);
            _maskView.style.height      = 100.0f;
            _maskView.style.setBorderWidth(1.0f);
            _maskView.style.setBorderColor(UIValues.listViewBorderColor);
            _maskView.style.marginTop   = 3.0f;

            _maskView.canDelete         = true;
            _maskView.canMultiSelect    = true;

            _maskView.selectedItemsWillBeDeleted += onSelectedMaskEntryItemsWillBeDeleted;

            _maskView.RegisterCallback<DragPerformEvent>(p => 
            {
                if (!PluginDragAndDrop.initiatedByPlugin)
                {
                    var dragObjects = PluginDragAndDrop.unityObjects;
                    foreach(var dragObject in dragObjects)
                    {
                        GameObject gameObject = dragObject as GameObject;
                        if (gameObject != null) maskObject(gameObject);
                    }
                }
            });

            var btn         = new Button();
            parent.Add(btn);
            btn.text        = "Mask selected";
            btn.tooltip     = "Add the currently selected objects to the erase mask.";
            btn.style.width = UIValues.useDefaultsButtonWidth;
            btn.clicked     += () => { ObjectErase.instance.eraseMask.maskObjects(ObjectSelection.instance.objectCollection); };

            refreshMaskView();
        }

        private void removeEntries(List<ObjectMaskEntry> entries)
        {
            UndoEx.record(this);
            foreach (var entry in entries)
            {
                _entries.Remove(entry);
                _maskedObjects.Remove(entry.gameObject);
            }

            foreach (var entry in entries)
                UndoEx.destroyObjectImmediate(entry);
        }

        private void onSelectedMaskEntryItemsWillBeDeleted(ListView<UIObjectMaskEntryItem, ObjectMaskEntry> listView, List<PluginGuid> itemIds)
        {
            _maskView.getItemData(itemIds, _entryBuffer);
            removeEntries(_entryBuffer);
            _entryBuffer.Clear();
        }

        private void onUndoRedo()
        {
            refreshMaskView();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += onUndoRedo;
            EditorApplication.hierarchyChanged += onHierarchyChanged;

            // Note: Just to be sure.
            _entries.RemoveAll(item => item.gameObject == null);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
            EditorApplication.hierarchyChanged -= onHierarchyChanged;
        }

        private void OnDestroy()
        {
            unmaskAllObjects();

            if (_maskView != null) _maskView.selectedItemsWillBeDeleted -= onSelectedMaskEntryItemsWillBeDeleted;
            ScriptableObjectEx.destroyImmediate(_maskViewState);
        }

        private void onHierarchyChanged()
        {
            _entries.RemoveAll(item => item.gameObject == null);
            _maskedObjects.RemoveWhere(item => item == null);
            refreshMaskView();
        }
    }
}
#endif