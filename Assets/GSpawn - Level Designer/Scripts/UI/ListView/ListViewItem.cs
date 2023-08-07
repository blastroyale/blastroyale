#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public abstract class ListViewItem<TData> : VisualElement
        where TData : IUIItemStateProvider
    {
        public delegate void RenameBeginHandler(ListViewItem<TData> item);
        public delegate void RenameEndHandler(ListViewItem<TData> item, bool commit);
        public delegate void DragBeginHandler(ListViewItem<TData> item);
        public delegate void DragEnterHandler(ListViewItem<TData> item);
        public delegate void DragLeaveHandler(ListViewItem<TData> item);
        public delegate void DragPerformHandler();

        public event RenameBeginHandler     renameBegin;
        public event RenameEndHandler       renameEnd;
        public event DragBeginHandler       dragBegin;
        public event DragEnterHandler       dragEnter;
        public event DragLeaveHandler       dragLeave;
        public event DragPerformHandler     dragPerform;

        [Flags]
        private enum States
        {
            Normal = 0,
            Rename = 1
        }

        private States                  _states                 = States.Normal;
        protected Label                 _displayNameLabel;
        protected TextField             _renameField;

        private ItemDragState           _dragState              = ItemDragState.AtRest;
        private HashSet<VisualElement>  _dragInitiators         = new HashSet<VisualElement>();

        private bool                    _visible                = true;
        private bool                    _allowDelayedRename     = false;

        private IListView               _listView;
        private HashSet<string>         _tags                   = new HashSet<string>();

        public TData                    data                    { get; set; }
        public int                      numTags                 { get { return _tags.Count; } }
        public bool                     selected                { get { return data.uiSelected; } }
        public CopyPasteMode            copyPasteMode           { get { return data.uiCopyPasteMode; } }
        public bool                     renaming                { get { return (_states & States.Rename) != 0; } }
        public bool                     itemVisible             { get { return _visible; } }
        public string                   displayNameLabelText    { get { return _displayNameLabel != null ? _displayNameLabel.text : string.Empty; } }
        public abstract string          displayName             { get; }
        public abstract PluginGuid      guid                    { get; }

        public ListViewItem()
        {
            style.flexDirection = FlexDirection.Row;
            style.flexWrap      = Wrap.NoWrap;
            style.height        = 16.0f;

            _dragInitiators.Add(this);

            RegisterCallback<DragEnterEvent>(onDragEnter);
            RegisterCallback<DragLeaveEvent>(onDragLeave);
            RegisterCallback<DragPerformEvent>(onDragPerform);
            RegisterCallback<DragExitedEvent>(onDragExit);

            //RegisterCallback<MouseDownEvent>(onMouseDown);
            //RegisterCallback<MouseUpEvent>(onMouseUp);
            //RegisterCallback<MouseMoveEvent>(onMouseMove);
        }

        public void refreshUI()
        {
            _displayNameLabel.text = displayName;
            updateBkColor();
            onRefreshUI();
        }

        public void initialize(IListView listView, TData data)
        {
            if (_listView != null) return;

            _listView = listView;
            this.data = data;
        }

        public void onWillBeDetachedFromListView()
        {
            endRename(false);

            data = default(TData);
            setVisible(false);
        }

        public void addTag(string tag)
        {
            _tags.Add(tag);
        }

        public bool hasTag(string tag)
        {
            return _tags.Contains(tag);
        }

        public bool removeTag(string tag)
        {
            return _tags.Remove(tag);
        }

        public void setSelected(bool selected)
        {
            data.uiSelected = selected;
            updateBkColor();
            onSelectedStateChanged(selected);
        }

        public bool setCopyPasteMode(CopyPasteMode copyPasteMode)
        {
            if (copyPasteMode != CopyPasteMode.None && !canBeCopyPasteSource()) return false;

            data.uiCopyPasteMode = copyPasteMode;
            onCopyPasteModeChanged();
            return true;
        }

        public virtual TData cloneData() { return default(TData); }
        public virtual bool canBeCopyPasteSource() { return false; }

        public void beingDelayedRename()
        {
            // Note: When data == null, the item is pooled and not used by the list view.
            if (!_allowDelayedRename || data == null) return;

            if (_listView.canRenameItems && selected && _listView.numSelectedItems == 1)
                beginRename();

            _allowDelayedRename = false;
        }

        public void buildUI()
        {
            if (_displayNameLabel != null) return;

            onBuildUIBeforeDisplayName();

            _renameField = new TextField();
            _renameField.style.maxHeight = 20.0f;
            _renameField.setDisplayVisible(false);
            Add(_renameField);

            _renameField.RegisterCallback<KeyDownEvent>(p =>
            {
                if (p.keyCode == KeyCode.Return) endRename(true);
                else
                if (p.keyCode == KeyCode.Escape) endRename(false);
            });

            _displayNameLabel                   = new Label();
            _displayNameLabel.style.alignSelf   = Align.Center;
            _displayNameLabel.text              = displayName;
            _displayNameLabel.style.color       = UIValues.listItemTextColor;
            Add(_displayNameLabel);

/*
            _displayNameLabel.RegisterCallback<MouseDownEvent>(p =>
            {
                if (p.clickCount == 2)
                {
                    _allowDelayedRename = false;
                }
                else if (_listView.canRenameItems && selected && FixedShortcuts.ui_BeginItemRenameOnClick(p))
                {
                    _allowDelayedRename = true;
                    var action = new DelayedListViewItemRename<TData>(this);
                    action.delayMode = EditorUpdateActionDelayMode.Seconds;
                    action.executionTime = EditorApplication.timeSinceStartup + UIValues.itemClickRenameDelay;
                    GSpawn.active.registerEditorUpdateAction(action);
                }
            });*/
            _displayNameLabel.RegisterCallback<MouseUpEvent>(onMouseUp);
            _displayNameLabel.RegisterCallback<MouseMoveEvent>(onMouseMove);

            onBuildUIAfterDisplayName();
            onPostBuildUI();

            _dragInitiators.Add(_displayNameLabel);
            refreshUI();
        }

        public void setVisible(bool visible)
        {
            if (_visible == visible) return;

            _visible = visible;
            this.setDisplayVisible(itemVisible);
        }

        public void beginRename()
        {
            if (renaming || !onCanRename()) return;

            _states |= States.Rename;

            _renameField.setDisplayVisible(true);
            _displayNameLabel.setDisplayVisible(false);
            _renameField.focsuEx();
            _renameField.SelectAll();
            _renameField.SetValueWithoutNotify(displayName);

            onBeginRename();

            if (renameBegin != null) renameBegin(this);
        }

        public void endRename(bool commit)
        {
            if (!renaming) return;

            _states &= ~States.Rename;

            _renameField.setDisplayVisible(false);
            _displayNameLabel.setDisplayVisible(true);

            onEndRename(commit);
            _displayNameLabel.text = displayName;

            if (renameEnd != null) renameEnd(this, commit);
        }

        protected virtual bool onCanRename() { return true; }
        protected virtual void onRefreshUI() { }
        protected virtual void onBuildUIBeforeDisplayName() { }
        protected virtual void onBuildUIAfterDisplayName() { }
        protected virtual void onPostBuildUI() { }
        protected virtual void onBeginRename() { }
        protected virtual void onEndRename(bool commit) { }
        protected virtual void onSelectedStateChanged(bool selected) { }
        protected virtual void onCopyPasteModeChanged() { }

        protected void reparentMainControls(VisualElement parent)
        {
            if (parent == this) return;

            Remove(_renameField);
            Remove(_displayNameLabel);

            parent.Add(_renameField);
            parent.Add(_displayNameLabel);
        }

        protected VisualElement createRow()
        {
            var row = new VisualElement();
            Add(row);
            row.style.flexDirection = FlexDirection.Row;

            return row;
        }

        private void onMouseDown(MouseDownEvent e)
        {
            if (!_listView.canDragAndDrop) return;

            if (e.button == (int)MouseButton.LeftMouse &&
                _dragInitiators.Contains((VisualElement)e.target))
            {
                _dragState = ItemDragState.Ready;
            }
        }

        private void onMouseUp(MouseUpEvent e)
        {
            if (!_listView.canDragAndDrop) return;

            _dragState = ItemDragState.AtRest;
        }

        private void onMouseMove(MouseMoveEvent e)
        {
            if (!_listView.canDragAndDrop) return;

            if (_dragState == ItemDragState.Ready)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    _allowDelayedRename = false;
                    _dragState          = ItemDragState.Dragging;
                    PluginDragAndDrop.beginDrag(PluginDragAndDropTitles.listViewItem, _listView.dragAndDropInitiatorId, _listView.dragAndDropData);
                    if (dragBegin != null) dragBegin(this);
                }
            }
        }

        private void updateBkColor()
        {
            style.backgroundColor = selected ? UIValues.selectedListItemColor : UIValues.unselectedListItemColor;
        }

        private bool _calledOnDragEnter = false;
        private void onDragEnter(DragEnterEvent e)
        {
            _calledOnDragEnter      = true;
            _allowDelayedRename     = false;
            style.backgroundColor   = UIValues.dropDestinationItemColor;
            if (dragEnter != null) dragEnter(this);
        }

        private void onDragLeave(DragLeaveEvent e)
        {
            _calledOnDragEnter      = false;
            _allowDelayedRename     = false;
            _dragState              = ItemDragState.AtRest;
            updateBkColor();
            if (dragLeave != null) dragLeave(this);
        }

        private void onDragPerform(DragPerformEvent e)
        {
            if (!_calledOnDragEnter) return;
            _calledOnDragEnter      = false;
            _allowDelayedRename     = false;
            _dragState              = ItemDragState.AtRest;
            updateBkColor();
            if (dragPerform != null) dragPerform();
        }

        private void onDragExit(DragExitedEvent e)
        {
            _calledOnDragEnter      = false;
            _allowDelayedRename     = false;
            _dragState              = ItemDragState.AtRest;
            PluginDragAndDrop.endDrag();
        }
    }
}
#endif