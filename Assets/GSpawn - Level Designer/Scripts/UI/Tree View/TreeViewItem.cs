#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public abstract class TreeViewItem<TData> : VisualElement
        where TData : IUIItemStateProvider
    {
        public delegate void ExpandedStateChangedHandler(TreeViewItem<TData> item, bool expanded);
        public delegate void RenameBeginHandler(TreeViewItem<TData> item);
        public delegate void RenameEndHandler(TreeViewItem<TData> item, bool commit);
        public delegate void DragBeginHandler(TreeViewItem<TData> item);
        public delegate void DragEnterHandler(TreeViewItem<TData> item);
        public delegate void DragLeaveHandler(TreeViewItem<TData> item);
        public delegate void DragPerformHandler(DragPerformEvent e);

        public ExpandedStateChangedHandler  expandedStateChanged;
        public RenameBeginHandler           renameBegin;
        public RenameEndHandler             renameEnd;
        public DragBeginHandler             dragBegin;
        public DragEnterHandler             dragEnter;
        public DragLeaveHandler             dragLeave;
        public DragPerformHandler           dragPerform;

        [Flags]
        private enum States
        {
            Normal = 0,
            Rename = 1
        }

        private States                      _states                 = States.Normal;
        protected Button                    _expandedStateBtn;
        protected Label                     _displayNameLabel;
        protected TextField                 _renameField;

        private ItemDragState               _dragState              = ItemDragState.AtRest;
        private HashSet<VisualElement>      _dragInitiators         = new HashSet<VisualElement>();

        private bool                        _visible                = true;
        private bool                        _visibleInHierarchy     = true;
        private bool                        _allowDelayedRename     = false;

        private ITreeView                   _treeView;
        private HashSet<string>             _tags                   = new HashSet<string>();

        private int                         _depth                  = 0;
        private bool                        _expanded               = true;
        private TreeViewItem<TData>         _parent;
        private List<TreeViewItem<TData>>   _directChildren         = new List<TreeViewItem<TData>>();

        public TData                        data                    { get; set; }
        public int                          depth                   { get { return _depth; } }
        public bool                         expanded                { get { return _expanded; } }
        public int                          numTags                 { get { return _tags.Count; } }
        public bool                         selected                { get { return data.uiSelected; } }
        public CopyPasteMode                copyPasteMode           { get { return data.uiCopyPasteMode; } }
        public int                          numDirectChildren       { get { return _directChildren.Count; } }
        public bool                         renaming                { get { return (_states & States.Rename) != 0; } }
        public bool                         itemVisible             { get { return _visible && _visibleInHierarchy; } }
        public TreeViewItem<TData>          parentItem
        {
            get { return _parent; }
            set
            {
                if (_parent == value || value == this) return;
                if (_parent != null)
                {
                    _parent._directChildren.Remove(this);
                    _parent.updateArrowButton();
                }

                _parent = value;
                if (_parent != null)
                {
                    if (!_parent.expanded) _parent.toggleExpandedState(true);

                    _parent._directChildren.Add(this);
                    setDepth(_parent._depth + 1);
                    _parent.updateArrowButton();
                    updateChildrenDepth(this);

                    _visibleInHierarchy = true;
                    this.setDisplayVisible(itemVisible);
                }
                else
                {
                    _visibleInHierarchy = true;
                    this.setDisplayVisible(itemVisible);

                    setDepth(0);
                    updateChildrenDepth(this);
                }
            }
        }
        public abstract string      displayName     { get; }
        public abstract PluginGuid  guid            { get; }

        public TreeViewItem()
        {
            style.flexDirection = FlexDirection.Row;
            style.flexWrap      = Wrap.NoWrap;
            style.height        = 16.0f;

            _dragInitiators.Add(this);

            RegisterCallback<DragEnterEvent>(onDragEnter);
            RegisterCallback<DragLeaveEvent>(onDragLeave);
            RegisterCallback<DragPerformEvent>(onDragPerform);
            RegisterCallback<DragExitedEvent>(onDragExit);

            RegisterCallback<MouseDownEvent>(onMouseDown);
            RegisterCallback<MouseUpEvent>(onMouseUp);
            RegisterCallback<MouseMoveEvent>(onMouseMove);
        }

        public void beingDelayedRename()
        {
            // Note: When data == null, the item is pooled and not used by the tree view.
            if (!_allowDelayedRename || data == null) return;

            if (selected && _treeView.numSelectedItems == 1)
                beginRename();

            _allowDelayedRename = false;
        }

        public void refreshUI()
        {
            _displayNameLabel.text = displayName;
            updateBkColor();
            onRefreshUI();
        }

        public void initialize(ITreeView treeView, TData data)
        {
            if (_treeView != null) return;

            _treeView = treeView;
            this.data = data;
        }

        public void onWillBeDetachedFromTreeView()
        {
            endRename(false);

            data        = default(TData);
            parentItem  = null;

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
        public abstract void setDataParent(TData parentData);
        public abstract void setIndexInDataParent(int indexInDataParent);
        public virtual bool canBeCopyPasteSource() { return false; }

        public void buildUI()
        {
            if (_displayNameLabel != null) return;

            _expandedStateBtn = new Button();
            _expandedStateBtn.style.backgroundColor = UIValues.listItemSeparatorColor;
            _expandedStateBtn.style.setBorderRadius(0.0f);
            _expandedStateBtn.style.setBorderWidth(0.0f);
            _expandedStateBtn.style.alignSelf = Align.Center;
            _expandedStateBtn.RegisterCallback<MouseUpEvent>((p) => 
            { 
                toggleExpandedState(true);

                // Note: Curiously, it seems that this is not necessary. But it seems right for it to be here.
                p.StopPropagation(); 
            });
            Add(_expandedStateBtn);

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
                else if (selected && FixedShortcuts.ui_BeginItemRenameOnClick(p))
                {
                    _allowDelayedRename = true;
                    var action = new DelayedTreeViewItemRename<TData>(this);
                    action.delayMode = EditorUpdateActionDelayMode.Seconds;
                    action.executionTime = EditorApplication.timeSinceStartup + UIValues.itemClickRenameDelay;
                    GSpawn.active.registerEditorUpdateAction(action);
                }
            });*/
            _displayNameLabel.RegisterCallback<MouseUpEvent>(onMouseUp);
            _displayNameLabel.RegisterCallback<MouseMoveEvent>(onMouseMove);

            onBuildUIAfterDisplayName();

            _dragInitiators.Add(_displayNameLabel);
            refreshUI();
        }

        public TreeViewItem<TData> findFirstDirectChildParent()
        {
            foreach (var child in _directChildren)
                if (child.numDirectChildren != 0) return child;

            return null;
        }

        public TreeViewItem<TData> findFirstExpandedParent()
        {
            if (parentItem == null) return null;
            if (parentItem.expanded) return parentItem;

            var parent = parentItem.parentItem;
            while (parent != null && !parent.expanded)
                parent = parent.parentItem;

            return parent != null ? parent : null;
        }

        public void getDirectChildren(List<TreeViewItem<TData>> children)
        {
            children.Clear();
            if (numDirectChildren != 0)
                children.AddRange(_directChildren);
        }

        public void getAllChildrenDFS(List<TreeViewItem<TData>> allChildren)
        {
            allChildren.Clear();
            getAllChildrenRecurseDFS(this, allChildren);
        }

        public void getAllChildrenAndSelfDFS(List<TreeViewItem<TData>> allChildren)
        {
            allChildren.Clear();
            allChildren.Add(this);
            getAllChildrenRecurseDFS(this, allChildren);
        }

        public void getAllChildrenBFS(List<TreeViewItem<TData>> allChildren)
        {
            allChildren.Clear();
            getAllChildrenRecurseBFS(this, allChildren);
        }

        public void getAllChildrenAndSelfBFS(List<TreeViewItem<TData>> allChildren)
        {
            allChildren.Clear();
            allChildren.Add(this);
            getAllChildrenRecurseBFS(this, allChildren);
        }

        public void setExpanded(bool expanded, bool notify)
        {
            if (numDirectChildren == 0) return;

            if (expanded == _expanded) return;

            toggleExpandedState(notify);
        }

        public void toggleExpandedState(bool notify)
        {
            if (numDirectChildren == 0) return;

            _expanded = !_expanded;
            _expandedStateBtn.style.backgroundImage = _expanded ? TexturePool.instance.itemArrowDown : TexturePool.instance.itemArrowRight;

            foreach (var child in _directChildren)
            {
                child.onParentExpandedStateChanged(_expanded);
            }

            if (notify && expandedStateChanged != null)
                expandedStateChanged(this, _expanded);
        }

        public bool isChildOf(TreeViewItem<TData> item)
        {
            if (parentItem == null) return false;
            if (parentItem == item) return true;

            var parent = parentItem.parentItem;
            while (parent != null && parent != item)
                parent = parent.parentItem;

            return parent == item;
        }

        public void setVisible(bool visible)
        {
            if (_visible == visible) return;

            _visible = visible;
            _visible = visible;
            this.setDisplayVisible(itemVisible);
        }

        public void beginRename()
        {
            if (renaming) return;

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

        protected abstract void onRefreshUI();
        protected abstract void onBuildUIBeforeDisplayName();
        protected abstract void onBuildUIAfterDisplayName();
        protected virtual void onBeginRename() { }
        protected virtual void onEndRename(bool commit) { }
        protected virtual void onSelectedStateChanged(bool selected) { }
        protected virtual void onCopyPasteModeChanged() { }

        private void onParentExpandedStateChanged(bool parentExpanded)
        {
            _visibleInHierarchy = (parentExpanded && parentItem.expanded);

            this.setDisplayVisible(itemVisible);
            foreach (var child in _directChildren)
                child.onParentExpandedStateChanged(_visibleInHierarchy);
        }

        private void getAllChildrenRecurseDFS(TreeViewItem<TData> parentItem, List<TreeViewItem<TData>> allChildren)
        {
            foreach (var child in parentItem._directChildren)
            {
                allChildren.Add(child);
                getAllChildrenRecurseDFS(child, allChildren);
            }
        }

        private void getAllChildrenRecurseBFS(TreeViewItem<TData> parentItem, List<TreeViewItem<TData>> allChildren)
        {
            foreach (var child in parentItem._directChildren)
                allChildren.Add(child);

            foreach(var child in parentItem._directChildren)
                getAllChildrenRecurseBFS(child, allChildren);
        }

        private void updateArrowButton()
        {
            Texture2D buttonTexture                         = expanded ? TexturePool.instance.itemArrowDown : TexturePool.instance.itemArrowRight;
            _expandedStateBtn.style.width                   = buttonTexture.width;
            _expandedStateBtn.style.height                  = buttonTexture.height;
            _expandedStateBtn.style.backgroundImage         = buttonTexture;
            _expandedStateBtn.visible                       = numDirectChildren != 0;
            _expandedStateBtn.style.marginRight             = 0.0f;
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
                    _allowDelayedRename = false;
                    _dragState          = ItemDragState.Dragging;
                    PluginDragAndDrop.beginDrag(PluginDragAndDropTitles.treeViewItem, _treeView.dragAndDropInitiatorId, _treeView.dragAndDropData);
                    if (dragBegin != null) dragBegin(this);
                }
            }
        }

        private void updateChildrenDepth(TreeViewItem<TData> parent)
        {
            foreach(var child in parent._directChildren)
            {
                child.setDepth(parent.depth + 1);
                updateChildrenDepth(child);
            }    
        }

        private void setDepth(int newDepth)
        {
            _depth = newDepth;
            style.paddingLeft = _depth * TexturePool.instance.itemArrowRight.width;
        }

        private void updateBkColor()
        {
            style.backgroundColor = selected ? UIValues.selectedListItemColor : UIValues.unselectedListItemColor;
        }

        // Note: Necessary because it seems that sometimes 'onDragPerform' is being called
        //       without calling 'onDragEnter'. Could be a bug someplace else, but it's
        //       quite tricky to track it down.
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
            _allowDelayedRename     = false;
            _dragState              = ItemDragState.AtRest;
            updateBkColor();
            if (dragPerform != null) dragPerform(e);
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