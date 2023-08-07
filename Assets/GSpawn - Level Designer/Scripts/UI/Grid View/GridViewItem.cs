#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public abstract class GridViewItem<TData> : VisualElement
        where TData : IUIItemStateProvider
    {
        public delegate void DragBeginHandler(GridViewItem<TData> item);
        public delegate void DragEnterHandler(GridViewItem<TData> item);
        public delegate void DragLeaveHandler(GridViewItem<TData> item);
        public delegate void DragPerformHandler();

        public DragBeginHandler         dragBegin;
        public DragEnterHandler         dragEnter;
        public DragLeaveHandler         dragLeave;
        public DragPerformHandler       dragPerform;

        protected Label                 _displayNameLabel;
        private bool                    _visible = true;

        private IGridView               _gridView;
        private ItemDragState           _dragState = ItemDragState.AtRest;
        private HashSet<VisualElement>  _dragInitiators = new HashSet<VisualElement>();

        private HashSet<string>         _tags = new HashSet<string>();

        public TData                    data            { get; set; }
        public bool                     itemVisible     { get { return _visible; } }
        public int                      numTags         { get { return _tags.Count; } }
        public abstract string          displayName     { get; }
        public abstract PluginGuid      guid            { get; }
        public abstract Vector2         imageSize       { get; set; }
        public bool                     selected        { get { return data.uiSelected; } }
        public CopyPasteMode            copyPasteMode   { get { return data.uiCopyPasteMode; } }

        public GridViewItem()
        {
            style.flexDirection = FlexDirection.Column;
            style.flexWrap      = Wrap.NoWrap;
            style.setBorderWidth(1.0f);
            style.setBorderColor(Color.black);

            _dragInitiators.Add(this);

            RegisterCallback<DragEnterEvent>(onDragEnter);
            RegisterCallback<DragLeaveEvent>(onDragLeave);
            RegisterCallback<DragPerformEvent>(onDragPerform);
            RegisterCallback<DragExitedEvent>(onDragExit);

            RegisterCallback<MouseDownEvent>(onMouseDown);
            RegisterCallback<MouseUpEvent>(onMouseUp);
            RegisterCallback<MouseMoveEvent>(onMouseMove);
        }

        public void initialize(IGridView gridView, TData data)
        {
            if (_gridView != null) return;

            _gridView = gridView;
            this.data = data;
        }

        public void buildUI()
        {
            onBuildUI();
            refreshUI();
        }

        public void refreshUI()
        {
            if (GSpawn.active == null) return;

            _displayNameLabel.text = displayName;
            updateBkColor();
            onRefreshUI();
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
            applyCopyPasteModeStates();
            return true;
        }

        public void setVisible(bool visible)
        {
            if (_visible == visible) return;

            _visible = visible;
            this.setDisplayVisible(itemVisible);
        }

        public virtual bool canBeCopyPasteSource() { return false; }
        protected virtual void onRefreshUI() { }
        protected abstract void onBuildUI();
        protected virtual void onSelectedStateChanged(bool selected) { }
        protected virtual void applyCopyPasteModeStates() { }

        protected void addDragAndDropInitiator(VisualElement initiator)
        {
            _dragInitiators.Add(initiator);
        }

        private void updateBkColor()
        {
            style.backgroundColor = selected ? UIValues.selectedListItemColor : UIValues.unselectedListItemColor;
        }

        private void onMouseDown(MouseDownEvent e)
        {
            if (e.button == (int)MouseButton.LeftMouse &&
                _dragInitiators.Contains((VisualElement)e.target))
            {
                _dragState = ItemDragState.Ready;
            }
        }

        private void onMouseUp(MouseUpEvent e)
        {
            _dragState = ItemDragState.AtRest;
        }

        private void onMouseMove(MouseMoveEvent e)
        {
            if (_dragState == ItemDragState.Ready)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    PluginDragAndDrop.beginDrag(PluginDragAndDropTitles.gridViewItem, _gridView.dragAndDropInitiatorId, _gridView.dragAndDropData);
                    if (dragBegin != null) dragBegin(this);
                }

                _dragState = ItemDragState.Dragging;
            }
        }

        private bool _calledOnDragEnter = false;
        private void onDragEnter(DragEnterEvent e)
        {
            _calledOnDragEnter      = true;
            if (dragEnter != null) dragEnter(this);
        }

        private void onDragLeave(DragLeaveEvent e)
        {
            _calledOnDragEnter      = false;
            updateBkColor();
            if (dragLeave != null) dragLeave(this);
        }

        private void onDragPerform(DragPerformEvent e)
        {
            if (!_calledOnDragEnter) return;
            _calledOnDragEnter      = false;
            updateBkColor();
            if (dragPerform != null) dragPerform();
        }

        private void onDragExit(DragExitedEvent e)
        {
            _calledOnDragEnter      = false;
            PluginDragAndDrop.endDrag();
        }
    }
}
#endif