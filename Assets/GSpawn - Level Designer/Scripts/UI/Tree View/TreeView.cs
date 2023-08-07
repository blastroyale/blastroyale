#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace GSpawn
{
    public class TreeView<TItem, TItemData> : VisualElement, ITreeView
        where TItem : TreeViewItem<TItemData>, new()
        where TItemData : IUIItemStateProvider
    {
        public delegate void    SelectedItemsWillBeDeletedHandler   (TreeView<TItem, TItemData> treeView, List<PluginGuid> selectedParentIds, List<PluginGuid> allSelectedItemIds, List<PluginGuid> allItemIds);
        public delegate void    ItemsDetachedFromParentHandler      (TreeView<TItem, TItemData> treeView, List<PluginGuid> itemIds);
        public delegate void    SelectionChangedHandler             (TreeView<TItem, TItemData> treeView);
        public delegate void    PerformPasteHandler                 (TreeView<TItem, TItemData> treeView, List<PluginGuid> srcItemIds, List<PluginGuid> destItemIds, CopyPasteMode copyPasteMode);

        public event            SelectedItemsWillBeDeletedHandler   selectedItemsWillBeDeleted;
        public event            ItemsDetachedFromParentHandler      itemsDetachedFromParent;
        public event            SelectionChangedHandler             selectionChanged;
        public event            PerformPasteHandler                 paste;

        public class DragAndDropData
        {
            private List<TItem> _items      = new List<TItem>();

            public int          numItems    { get { return _items.Count; } }

            public DragAndDropData(TreeView<TItem, TItemData> treeView)
            {
                treeView.getVisibleSelectedItems(_items);
            }

            public TItem getItem(int index)
            {
                return _items[index];
            }
        }

        private class CopyOperation
        {
            public CopyPasteMode    copyPasteMode   = CopyPasteMode.None;
            public List<TItem>      sourceItems     = new List<TItem>();

            public bool             isActive        { get { return sourceItems.Count != 0; } }
        }

        [NonSerialized]
        private List<PluginGuid>                _itemGuidBuffer         = new List<PluginGuid>();
        [NonSerialized]
        private List<TreeViewItem<TItemData>>   _itemBuffer             = new List<TreeViewItem<TItemData>>();

        [NonSerialized]
        private CopyOperation                   _copyOp                 = new CopyOperation();

        [NonSerialized]
        private TItem                           _dragBeginItem          = null;
        [NonSerialized]
        private TItem                           _dropDestination        = null;
        [NonSerialized]
        private TItem                           _renameItem             = null;

        private Func<TItemData>                 _createItemDataFunc;
        private TreeViewState                   _state                  = null;

        [NonSerialized]
        private Dictionary<PluginGuid, TItem>       _items              = new Dictionary<PluginGuid, TItem>();
        [NonSerialized]
        private TreeViewItemPool<TItem, TItemData> _itemPool;

        [NonSerialized]
        private TItem                           _selectRangeBegin       = null;
        [NonSerialized]
        private TItem                           _selectRangeEnd         = null;

        [NonSerialized]
        private ScrollView                      _scrollView;
        [NonSerialized]
        private VisualElement                   _itemContainer;

        private bool                            hasSelectRange          { get { return _selectRangeBegin != null && _selectRangeEnd != null; } }

        public bool                             draggingItems           { get { return _dragBeginItem != null; } }
        public int                              numSelectedItems        { get { return _state.selectedItems.Count; } }
        public bool                             renamingItem            { get { return _renameItem != null; } }
        public Func<TItemData>                  createItemDataFunc      { set { _createItemDataFunc = value; } }
        public TItemData                        dropDestinationData     { get { return _dropDestination != null ? _dropDestination.data : default(TItemData); } }
        public int                              dragAndDropInitiatorId  { get { return GetHashCode(); } }
        public int                              copyPasteInitiatorId    { get { return GetHashCode(); } }
        public System.Object                    dragAndDropData         { get { return new DragAndDropData(this); } }
        public bool                             canCopyPaste            { get; set; }
        public bool                             canCutPaste             { get; set; }
        public bool                             canDuplicate            { get; set; }

        public TreeView(TreeViewState state, VisualElement parent)
        {
            _itemPool       = new TreeViewItemPool<TItem, TItemData>(this);

            _state          = state;
            focusable       = true;
            style.flexGrow  = 1.0f;
            parent.Add(this);
      
            _scrollView                 = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.focusable       = true;
            _scrollView.style.flexGrow  = 1.0f;
            _scrollView.contentContainer.style.flexWrap         = Wrap.NoWrap;
            _scrollView.contentContainer.style.flexDirection    = FlexDirection.Column;
            _scrollView.contentContainer.style.flexGrow         = 1.0f;
            _scrollView.RegisterCallback<KeyDownEvent>(onKeyDown);     
            _scrollView.verticalScroller.valueChanged += p => 
            {
                _state.storeScrollData(_scrollView);
            };
     
            RegisterCallback<DragUpdatedEvent>((p) => { PluginDragAndDrop.visualMode = DragAndDropVisualMode.Copy; });
            RegisterCallback<DragExitedEvent>(p => { _dropDestination = null; });

            _itemContainer = _scrollView;
            Add(_scrollView);
        }

        public bool containsItem(PluginGuid itemId)
        {
            return _items.ContainsKey(itemId);
        }

        public void filterMissingItems(List<PluginGuid> itemIds)
        {
            itemIds.RemoveAll(itemId => !containsItem(itemId));
        }

        public void scheduleScrollToItem(PluginGuid itemId)
        {
            GSpawn.active.registerEditorUpdateAction(new ScrollToItem<TItem>(_scrollView, _items[itemId], Undo.GetCurrentGroup(), _state));
        }

        public void scheduleScrollToItems(List<PluginGuid> itemIds)
        {
            var items = new List<TItem>();
            idsToItems(itemIds, items, false);
            GSpawn.active.registerEditorUpdateAction(new ScrollToItem<TItem>(_scrollView, _itemContainer.findChildItemWithGreatestIndex(items), Undo.GetCurrentGroup(), _state));
        }

        public void expandSelected()
        {
            if (numSelectedItems == 0) return;

            UndoEx.record(_state);
            foreach (var id in _state.selectedItems)
            {
                var item = _items[id];
                if (!item.expanded) item.toggleExpandedState(true);
            }
        }

        public void expandUpwards(List<PluginGuid> itemIds)
        {
            foreach (var itemId in itemIds)
            {
                var item    = _items[itemId];
                var parent  = item.parentItem;
                while (parent != null)
                {
                    parent.setExpanded(true, true);
                    parent = parent.parentItem;
                }
            }
        }

        public void collapseSelected()
        {
            if (numSelectedItems == 0) return;

            UndoEx.record(_state);
            foreach (var id in _state.selectedItems)
            {
                var item = _items[id];
                if (item.expanded) item.toggleExpandedState(true);
            }
        }

        public void detachSelectedFromParents()
        {
            if (numSelectedItems == 0) return;

            var itemIds = new List<PluginGuid>(_state.selectedItems.hashSet);
            var itemsToDetach = new List<TItem>();
            idsToItems(itemIds, itemsToDetach, true);
            if (itemsToDetach.Count == 0) return;

            foreach (var item in itemsToDetach)
                detachItemFromParent(item);

            GSpawn.active.registerEditorUpdateAction(new ScrollToItem<TItem>(_scrollView, _scrollView.findChildItemWithSmallestIndex(itemsToDetach), Undo.GetCurrentGroup(), _state));

            if (itemsDetachedFromParent != null)
                itemsDetachedFromParent(this, itemIds);
        }

        public void setItemSelected(PluginGuid itemId, bool selected, bool notify)
        {
            bool alreadySelected = isItemSelected(itemId);
            if (selected && alreadySelected) return;
            if (!selected && !alreadySelected) return;

            UndoEx.record(_state);
            setItemSelected(_items[itemId], selected, notify);
        }

        public void setAllItemsSelected(bool selected, bool onlyVisible, bool notify)
        {
            UndoEx.record(_state);
            if (onlyVisible)
            {
                foreach (var pair in _items)
                    if (pair.Value.itemVisible) setItemSelected(pair.Value, selected, notify);
            }
            else
            {
                foreach (var pair in _items)
                    setItemSelected(pair.Value, selected, notify);
            }
        }

        public void setItemsSelected(IEnumerable<PluginGuid> itemIds, bool selected, bool onlyVisible, bool notify)
        {
            UndoEx.record(_state);
            if (onlyVisible)
            {
                foreach (var id in itemIds)
                {
                    var item = _items[id];
                    if (item.itemVisible) setItemSelected(item, selected, notify);
                }
            }
            else
            {
                foreach (var id in itemIds)
                    setItemSelected(_items[id], selected, notify);
            }
        }

        public void filterItems(Func<TItemData, bool> filter)
        {
            foreach (var pair in _items)
                pair.Value.setVisible(filter(pair.Value.data));
        }

        public void setAllItemsVisible(bool visible)
        {
            foreach (var pair in _items)
                pair.Value.setVisible(visible);
        }

        public void refreshUI()
        {
            foreach (var pair in _items)
                pair.Value.refreshUI();
        }

        public void refreshItemUI(PluginGuid itemId)
        {
            _items[itemId].refreshUI();
        }

        public void refreshItemsUI(List<PluginGuid> itemIds)
        {
            foreach (var id in itemIds)
                _items[id].refreshUI();
        }

        public TItemData getItemData(PluginGuid itemId)
        {
            return _items[itemId].data;
        }

        public void getItemData(IEnumerable<PluginGuid> itemIds, List<TItemData> itemData)
        {
            itemData.Clear();
            foreach (var id in itemIds)
                itemData.Add(_items[id].data);
        }

        public void getItemData(List<TItemData> itemData)
        {
            itemData.Clear();
            foreach (var pair in _items)
                itemData.Add(pair.Value.data);
        }

        public void getSelectedItemData(List<TItemData> itemData)
        {
            itemData.Clear();
            foreach (var id in _state.selectedItems)
                itemData.Add(_items[id].data);
        }

        public void getVisibleSelectedItemData(List<TItemData> itemData)
        {
            itemData.Clear();
            foreach (var id in _state.selectedItems)
            {
                var item = _items[id];
                if (item.itemVisible) itemData.Add(item.data);
            }
        }

        public bool isItemSelected(PluginGuid itemId)
        {
            return _state.selectedItems.Contains(itemId);
        }

        public void onBeginBuild()
        {
            _items.Clear();
            _itemContainer.Clear();
            _copyOp.sourceItems.Clear();
        }

        public void onEndBuild()
        {
            _state.selectedItems.RemoveWhere(itemId => !containsItem(itemId));

            foreach (var pair in _items)
            {
                var item = pair.Value;

                if (_state.selectedItems.Contains(pair.Key))
                    setItemSelected(item, true, false);

                if (item.expanded && _state.collapsedItems.Contains(pair.Key))
                    item.toggleExpandedState(false);
            }
       
            if (_state.hasSelectRange)
            {
                if (containsItem(_state.selectRangeBeginId)) _selectRangeBegin = _items[_state.selectRangeBeginId];
                else _selectRangeBegin = null;

                if (containsItem(_state.selectRangeEndId)) _selectRangeEnd = _items[_state.selectRangeEndId];
                else _selectRangeEnd = null;

                if (_selectRangeBegin == null || _selectRangeEnd == null)
                {
                    _state.hasSelectRange   = false;
                    _selectRangeBegin       = null;
                    _selectRangeEnd         = null;
                }
            }
            else
            {
                _selectRangeBegin   = null;
                _selectRangeEnd     = null;
            }

            _state.applyScrollState(_scrollView);
        }

        public PluginGuid addItem(TItemData itemData, bool visible)
        {
            var item = _itemPool.obtainItem(itemData);
            _items.Add(item.guid, item);
            _itemContainer.Add(item);

            if (item.hasTag(TreeViewItemPool<TItem, TItemData>.newItemTag)) registerItemEventHandlers(item);
            if (!visible) item.setVisible(false);

            setItemSelected(item.guid, itemData.uiSelected, false);
            item.setCopyPasteMode(itemData.uiCopyPasteMode);
            item.refreshUI();

            return item.guid;
        }

        public PluginGuid addItem(TItemData itemData, PluginGuid parentId, bool visible)
        {
            PluginGuid itemId = addItem(itemData, visible);
            setItemParent(_items[itemId], _items[parentId]);

            return itemId;
        }

        public void deleteItem(PluginGuid itemId)
        {
            doDeleteItem(itemId);
        }

        public void deleteItems(List<PluginGuid> itemsIds)
        {
            var parentIds = new List<PluginGuid>();
            getParentIds(itemsIds, parentIds);

            foreach (var id in parentIds)
                doDeleteItem(id);
        }

        public int deleteItems(Predicate<TItemData> removalCondition)
        {
            int numRemoved = 0;
            var items = new List<TItem>(_items.Values);
            foreach (var item in items)
            {
                if (removalCondition(item.data))
                {
                    ++numRemoved;
                    doDeleteItem(item.guid);
                }
            }

            return numRemoved;
        }

        public void detachItemFromParent(PluginGuid itemId)
        {
            detachItemFromParent(_items[itemId]);
        }

        public void setItemParent(PluginGuid itemId, PluginGuid parentItemId)
        {
            setItemParent(_items[itemId], _items[parentItemId]);
        }

        public bool anySelectedItemsBeingRenamed()
        {
            foreach (var id in _state.selectedItems)
            {
                var item = _items[id];
                if (item.renaming) return true;
            }

            return false;
        }

        private void setItemParent(TItem item, TItem parent)
        {
            if (parent == null)
            {
                detachItemFromParent(item);
                return;
            }
     
            if (item == parent || item.parentItem == parent || parent.isChildOf(item)) return;

            var parentHierarchy = new List<TreeViewItem<TItemData>>();
            parent.getAllChildrenAndSelfDFS(parentHierarchy);

            var itemHierarchy = new List<TreeViewItem<TItemData>>();
            item.getAllChildrenAndSelfBFS(itemHierarchy);
            var insertAfter = parentHierarchy[parentHierarchy.Count - 1];
          
            while (itemHierarchy.Count != 0)
            {
                itemHierarchy[0].PlaceInFront(insertAfter);
                insertAfter = (TItem)itemHierarchy[0];
                itemHierarchy.RemoveAt(0);
            }

            item.parentItem = parent;
            item.setDataParent(parent.data);
        }

        private void detachItemFromParent(TItem item)
        {
            var parent = item.parentItem;
            if (parent == null) return;
        
            item.parentItem = null;
            item.setDataParent(default(TItemData));

            var itemHierarchy = new List<TreeViewItem<TItemData>>();
            item.getAllChildrenAndSelfBFS(itemHierarchy);
            var insertAfter = _itemContainer[_itemContainer.childCount - 1];

            while (itemHierarchy.Count != 0)
            {
                itemHierarchy[0].PlaceInFront(insertAfter);
                insertAfter = (TItem)itemHierarchy[0];
                itemHierarchy.RemoveAt(0);
            }
        }

        private void idsToItems(List<PluginGuid> itemIds, List<TItem> items, bool onlyVisible)
        {
            if (onlyVisible)
            {
                items.Clear();
                foreach (var id in itemIds)
                {
                    var item = _items[id];
                    if (!item.itemVisible) continue;
                    items.Add(_items[id]);
                }
            }
            else
            {
                items.Clear();
                foreach (var id in itemIds)
                    items.Add(_items[id]);
            }
        }

        private void onItemExpandedStateChanged(TreeViewItem<TItemData> item, bool expanded)
        {
            if (!expanded) _state.collapsedItems.Add(item.guid);
            else _state.collapsedItems.Remove(item.guid);
        }

        private void onItemClickedDown(TItem item, MouseDownEvent e)
        {
            if (e.button != (int)MouseButton.LeftMouse || e.altKey) return;
            if (renamingItem && item != _renameItem) _renameItem.endRename(true);

            UndoEx.record(_state);
            if (!e.ctrlKey && !e.shiftKey && !isItemSelected(item.guid))
            {
                clearSelection(false);
                setItemSelected(item, true, true);
                setSelectRange(item, item);

                _scrollView.ScrollTo(item);
            }
            else
            if (e.ctrlKey && !e.shiftKey)
            {
                bool selected = !isItemSelected(item.guid);
                setItemSelected(item, selected, true);
                setSelectRange(item, item);

                if (selected) _scrollView.ScrollTo(item);
            }
            else
            if (!e.ctrlKey && e.shiftKey && numSelectedItems != 0)
            {
                clearSelection(false);
                setSelectRange(_selectRangeBegin, item);
                setItemsSelected(_itemContainer.IndexOf(_selectRangeBegin), _itemContainer.IndexOf(_selectRangeEnd), true);

                _scrollView.ScrollTo(_selectRangeEnd);
            }

            if (numSelectedItems == 0) setSelectRange(null, null);
        }

        private void onItemClickedUp(TItem item, MouseUpEvent e)
        {
            if (e.button != (int)MouseButton.LeftMouse) return;

            UndoEx.record(_state);
            if (!e.ctrlKey && !e.shiftKey && numSelectedItems > 1)
            {
                clearSelection(false);
                setItemSelected(item, true, true);
                setSelectRange(item, item);

                _scrollView.ScrollTo(item);
            }

            if (numSelectedItems == 0) setSelectRange(null, null);
        }

        private void getParents(List<TItem> items, List<TItem> parents)
        {
            parents.Clear();

            foreach (var item in items)
            {
                bool foundParent = false;
                if (item.parentItem == null)
                {
                    parents.Add(item);
                    continue;
                }

                // Note: Is this second loop still needed?
                foreach (var otherItem in items)
                {
                    if (otherItem == item) continue;

                    if (item.isChildOf(otherItem))
                    {
                        foundParent = true;
                        break;
                    }
                }

                if (!foundParent) parents.Add(item);
            }
        }

        private void getParentIds(List<PluginGuid> itemIds, List<PluginGuid> parentIds)
        {
            parentIds.Clear();

            foreach(var id in itemIds)
            {
                bool foundParent = false;
                var item = _items[id];
                if (item.parentItem == null)
                {
                    parentIds.Add(id);
                    continue;
                }

                // Note: Is this second loop still needed?
                foreach (var otherId in itemIds)
                {
                    if (otherId == id) continue;

                    var otherItem = _items[otherId];
                    if (item.isChildOf(otherItem))
                    {
                        foundParent = true;
                        break;
                    }
                }

                if (!foundParent) parentIds.Add(id);
            }
        }

        private void onKeyDown(KeyDownEvent e)
        {
            if (renamingItem)
            {
                if (e.keyCode == KeyCode.Return) _renameItem.endRename(true);
                else if (FixedShortcuts.cancelAction(e)) _renameItem.endRename(false);
                return;
            }

            if (_copyOp.isActive)
            {
                if (FixedShortcuts.cancelAction(e))
                {
                    UICopyPaste.cancel();
                    return;
                }
            }

            if (FixedShortcuts.ui_DeleteSelected(e))                    deleteSelected();
            else if (FixedShortcuts.ui_DuplicateSelected(e))            duplicateSelected();
            else if (FixedShortcuts.ui_SelectUp(e))                     selectUp(e);
            else if (FixedShortcuts.ui_SelectDown(e))                   selectDown(e);
            else if (FixedShortcuts.ui_SelectLeft(e))                   selectLeft();
            else if (FixedShortcuts.ui_SelectRight(e))                  selectRight();
            else if (FixedShortcuts.ui_SelectAll(e))                    selectAll();
            else if (FixedShortcuts.ui_DetachSelectedFromParents(e))    detachSelectedFromParents();
            else if (FixedShortcuts.ui_ExpandSelected(e))               expandSelected();
            else if (FixedShortcuts.ui_CollapseSelected(e))             collapseSelected();
            else if (FixedShortcuts.ui_Copy(e))                         copySelected(CopyPasteMode.Copy);
            else if (FixedShortcuts.ui_Cut(e))                          copySelected(CopyPasteMode.Cut);
            else if (FixedShortcuts.ui_Paste(e))                        pasteSelected();
            else if (FixedShortcuts.ui_CreateChildrenForSelected(e))    createChildItemForSelected();
            else if (FixedShortcuts.ui_BeginRename(e))
            {
                if (numSelectedItems == 1 && !anySelectedItemsBeingRenamed())
                {
                    foreach (var id in _state.selectedItems)
                    {
                        var item = _items[id];
                        item.beginRename();
                        break;
                    }
                }
            }
        }

        private void copySelected(CopyPasteMode copyPasteMode)
        {
            if (numSelectedItems == 0) return;
            if (copyPasteMode == CopyPasteMode.Copy && !canCopyPaste) return;
            if (copyPasteMode == CopyPasteMode.Cut && !canCutPaste) return;

            cancelCopyPaste();
            UICopyPaste.begin(copyPasteMode, copyPasteInitiatorId, pasteSelected, cancelCopyPaste);

            _copyOp.copyPasteMode = copyPasteMode;
            foreach (var selectedId in _state.selectedItems)
            {
                var item = _items[selectedId];
                if (item.setCopyPasteMode(copyPasteMode))
                    _copyOp.sourceItems.Add(item);
            }
        }

        private void pasteSelected()
        {
            if (paste == null || !_copyOp.isActive) return;
            if (_copyOp.copyPasteMode == CopyPasteMode.Copy && !canCopyPaste) return;
            if (_copyOp.copyPasteMode == CopyPasteMode.Cut && !canCutPaste) return;
          
            var destItemIds = new List<PluginGuid>();
            getSelectedItemIds(destItemIds);
            if (destItemIds.Count == 0) return;

            var srcItemIds = new List<PluginGuid>();
            getItemIds(_copyOp.sourceItems, srcItemIds);

            paste(this, srcItemIds, destItemIds, _copyOp.copyPasteMode);
            cancelCopyPaste();

            foreach (var itemId in _state.selectedItems)
                _items[itemId].refreshUI();

            foreach (var itemId in srcItemIds)
                _items[itemId].refreshUI();
        }

        private void createChildItemForSelected()
        {
            if (numSelectedItems != 1 || _createItemDataFunc == null) return;

            var selectedItem = new List<TItem>();
            getSelectedItems(selectedItem);
            if (!selectedItem[0].itemVisible) return;

            UndoEx.record(_state);
            setItemSelected(selectedItem[0], false, false);

            PluginGuid itemId = addItem(_createItemDataFunc(), selectedItem[0].guid, true);
            var item = _items[itemId];

            UndoEx.record(_state);
            setItemSelected(item, true, true);
            setSelectRange(item, item);
            setSelectRange(item, item);

            GSpawn.active.registerEditorUpdateAction(new ScrollToItem<TItem>(_scrollView, item, Undo.GetCurrentGroup(), _state));
        }

        private void cancelCopyPaste()
        {
            foreach (var item in _copyOp.sourceItems)
                item.setCopyPasteMode(CopyPasteMode.None);

            _copyOp.sourceItems.Clear();
        }

        private void deleteSelected()
        {
            if (numSelectedItems == 0) return;

            var parentIds = new List<PluginGuid>();
            getSelectedParentIds(parentIds);
            var selectedIds = new List<PluginGuid>();
            getSelectedItemIds(selectedIds);

            _itemGuidBuffer.Clear();
            foreach(var parentId in parentIds)
            {
                var item = _items[parentId];
                item.getAllChildrenAndSelfDFS(_itemBuffer);
                storeItemIds(_itemBuffer, _itemGuidBuffer);
            }

            if (selectedItemsWillBeDeleted != null) selectedItemsWillBeDeleted(this, parentIds, selectedIds, _itemGuidBuffer);

            UndoEx.record(_state);

            _state.selectedItems.Clear();
            foreach (var selectedId in parentIds)
                doDeleteItem(selectedId);
        }

        private void clearSelection(bool notify)
        {
            foreach (var pair in _items)
                setItemSelected(pair.Value, false, false);

            if (notify && selectionChanged != null) selectionChanged(this);
        }

        private void setItemsSelected(int beginIndex, int endIndex, bool selected)
        {
            int dir     = (int)Mathf.Sign(endIndex - beginIndex);
            int stop    = endIndex + dir;
            if (dir == 0)
            {
                dir = 1;
                stop = endIndex + 1;
            }

            for (int itemIndex = beginIndex; itemIndex != stop; itemIndex += dir)
                setItemSelected((TItem)_itemContainer[itemIndex], selected, false);

            if (selectionChanged != null) selectionChanged(this);
        }

        private void setItemsSelected(List<TItem> items, bool selected, bool notify)
        {
            foreach (var item in items)
                setItemSelected(item, selected, false);

            if (notify && selectionChanged != null) selectionChanged(this);
        }

        private void setItemSelected(TItem item, bool selected, bool notify)
        {
            if (selected) _state.selectedItems.Add(item.guid);
            else _state.selectedItems.Remove(item.guid);
            item.setSelected(selected);

            if (notify && selectionChanged != null) selectionChanged(this);
        }

        private void selectAll()
        {
            UndoEx.record(_state);

            clearSelection(false);
            setSelectRange((TItem)_itemContainer[0], (TItem)_itemContainer[_itemContainer.childCount - 1]);
            setItemsSelected(0, _itemContainer.childCount - 1, true);
        }

        private void duplicateSelected()
        {
            if (numSelectedItems == 0 || !canDuplicate) return;

            var parentIds = new List<PluginGuid>();
            getSelectedParentIds(parentIds);        

            TItem clonedItem    = null;
            List<TItem> parents = new List<TItem>();
            idsToItems(parentIds, parents, true);

            PluginProgressDialog.begin("Duplicating Selected Items");
            List<TItem> clonedParents = new List<TItem>();
            for (int index = 0; index < parents.Count; ++index)
            {
                var item        = parents[index];
                var clonedData  = item.cloneData();
                float progress  = (index + 1) / (float)parentIds.Count;

                PluginProgressDialog.updateItemProgress(item.displayName, progress);

                if (item.parentItem != null)
                {
                    PluginGuid newItemId = addItem(clonedData, item.parentItem.guid, true);
                    clonedItem = _items[newItemId];
                    clonedItem.setDataParent(item.parentItem.data);
                    clonedParents.Add(clonedItem);
                }
                else
                {
                    PluginGuid newItemId = addItem(clonedData, true);
                    clonedItem = _items[newItemId];
                    clonedParents.Add(clonedItem);
                }

                duplicateItemRecurse(item, clonedItem, progress);
            }

            PluginProgressDialog.end();

            if (clonedParents.Count != 0)
            {
                var scrollToItem = new ScrollToItem<TItem>(_scrollView, _scrollView.findChildItemWithSmallestIndex(clonedParents), Undo.GetCurrentGroup(), _state);
                scrollToItem.onPostScroll = item =>
                {
                    clearSelection(false);
                    setSelectRange(item, item);
                    setItemsSelected(clonedParents, true, true);
                };
                GSpawn.active.registerEditorUpdateAction(scrollToItem);
                parent.schedule.Execute(() => { _scrollView.Focus(); });
            }
        }

        private void duplicateItemRecurse(TItem parent, TItem clonedParent, float progress)
        {
            var childItems = new List<TreeViewItem<TItemData>>();
            parent.getDirectChildren(childItems);
            foreach (var child in childItems)
            {
                var clonedData = child.cloneData();
                PluginGuid newItemId = addItem(clonedData, clonedParent.guid, true);
                var clonedChild = _items[newItemId];
                clonedChild.setDataParent(clonedParent.data);

                PluginProgressDialog.updateItemProgress(clonedChild.displayName, progress);
                duplicateItemRecurse((TItem)child, clonedChild, progress);
            }
        }

        private void selectLeft()
        {
            if (_selectRangeBegin != null)
            {
                if (_selectRangeBegin.expanded && _selectRangeBegin.numDirectChildren != 0) _selectRangeBegin.toggleExpandedState(true);
                else
                {
                    var nextEpandedParent = _selectRangeBegin.findFirstExpandedParent();
                    if (nextEpandedParent != null)
                    {
                        UndoEx.record(_state);

                        setItemSelected(_selectRangeBegin, false, false);
                        setItemSelected((TItem)nextEpandedParent, true, true);
                        setSelectRange((TItem)nextEpandedParent, (TItem)nextEpandedParent);
                    }

                    _scrollView.ScrollTo(_selectRangeBegin);
                }
            }
        }

        private void selectRight()
        {
            if (_selectRangeBegin != null)
            {
                if (!_selectRangeBegin.expanded) _selectRangeBegin.toggleExpandedState(true);
                else
                {
                    var nextChildParent = _selectRangeBegin.findFirstDirectChildParent();
                    if (nextChildParent != null)
                    {
                        UndoEx.record(_state);

                        setItemSelected(_selectRangeBegin, false, false);
                        setItemSelected((TItem)nextChildParent, true, true);
                        setSelectRange((TItem)nextChildParent, (TItem)nextChildParent);
                    }

                    _scrollView.ScrollTo(_selectRangeEnd);
                }
            }
        }

        private void selectUp(KeyDownEvent e)
        {
            if (hasSelectRange)
            {
                if (FixedShortcuts.ui_EnableDirectionalSelectAppend(e))
                {
                    int endIndex    = _itemContainer.IndexOf(_selectRangeEnd);
                    int beginIndex  = _itemContainer.IndexOf(_selectRangeBegin);
                    int nextIndex   = endIndex - 1;

                    while (nextIndex >= 0)
                    {
                        TItem nextItem = (TItem)_itemContainer[nextIndex];
                        if (nextItem.itemVisible)
                        {
                            UndoEx.record(_state);

                            clearSelection(false);
                            setSelectRange(_selectRangeBegin, nextItem);
                            setItemsSelected(beginIndex, nextIndex, true);
                            break;
                        }

                        --nextIndex;
                    }

                    _scrollView.ScrollTo(_selectRangeEnd);
                }
                else
                {
                    int nextIndex = _itemContainer.IndexOf(_selectRangeEnd) - 1;
                    while (nextIndex >= 0)
                    {
                        TItem nextItem = (TItem)_itemContainer[nextIndex];
                        if (nextItem.itemVisible)
                        {
                            UndoEx.record(_state);

                            clearSelection(false);
                            setSelectRange(nextItem, nextItem);
                            setItemSelected(_selectRangeBegin, true, false);
                            break;
                        }

                        --nextIndex;
                    }

                    _scrollView.ScrollTo(_selectRangeEnd);
                    if (selectionChanged != null) selectionChanged(this);
                }
            }
        }

        private void selectDown(KeyDownEvent e)
        {
            if (hasSelectRange)
            {
                if (FixedShortcuts.ui_EnableDirectionalSelectAppend(e))
                {
                    int endIndex    = _itemContainer.IndexOf(_selectRangeEnd);
                    int beginIndex  = _itemContainer.IndexOf(_selectRangeBegin);
                    int nextIndex   = endIndex + 1;

                    while (nextIndex < _itemContainer.childCount)
                    {
                        TItem nextItem = (TItem)_itemContainer[nextIndex];
                        if (nextItem.itemVisible)
                        {
                            UndoEx.record(_state);
    
                            clearSelection(false);
                            setSelectRange(_selectRangeBegin, nextItem);
                            setItemsSelected(beginIndex, nextIndex, true);
                            break;
                        }

                        ++nextIndex;
                    }

                    _scrollView.ScrollTo(_selectRangeEnd);
                }
                else
                {
                    int nextIndex = _itemContainer.IndexOf(_selectRangeEnd) + 1;
                    while (nextIndex < _itemContainer.childCount)
                    {
                        TItem nextItem = (TItem)_itemContainer[nextIndex];
                        if (nextItem.itemVisible)
                        {
                            UndoEx.record(_state);

                            clearSelection(false);
                            setSelectRange(nextItem, nextItem);
                            setItemSelected(_selectRangeBegin, true, false);
                            break;
                        }

                        ++nextIndex;
                    }

                    _scrollView.ScrollTo(_selectRangeEnd);
                    if (selectionChanged != null) selectionChanged(this);
                }
            }
        }

        private void getSelectedParentIds(List<PluginGuid> parentIds)
        {
            parentIds.Clear();
            var selectedIds = new List<PluginGuid>(_state.selectedItems.hashSet);
            getParentIds(selectedIds, parentIds);
        }

        private void getSelectedItemIds(List<PluginGuid> ids)
        {
            ids.Clear();
            ids.AddRange(_state.selectedItems.hashSet);
        }

        private void getItemIds(List<TItem> items, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var item in items)
                ids.Add(item.guid);
        }

        private void storeItemIds(List<TreeViewItem<TItemData>> items, List<PluginGuid> ids)
        {
            foreach (var item in items)
                ids.Add(item.guid);
        }

        private void setSelectRange(TItem begin, TItem end)
        {
            _selectRangeBegin = begin;
            if (_selectRangeBegin != null) _state.selectRangeBeginId = _selectRangeBegin.guid;

            _selectRangeEnd = end;
            if (_selectRangeEnd != null) _state.selectRangeEndId = _selectRangeEnd.guid;

            _state.hasSelectRange = _selectRangeBegin != null && _selectRangeEnd != null;
        }

        private void getSelectedItems(List<TItem> selectedItems)
        {
            selectedItems.Clear();
            foreach (var selectedId in _state.selectedItems)
                selectedItems.Add(_items[selectedId]);
        }

        private void getVisibleSelectedItems(List<TItem> selectedItems)
        {
            selectedItems.Clear();
            foreach (var selectedId in _state.selectedItems)
            {
                var item = _items[selectedId];
                if (item.itemVisible) selectedItems.Add(item);
            }
        }

        private void doDeleteItem(PluginGuid itemId)
        {
            var childrenAndSelf = new List<TreeViewItem<TItemData>>();
            _items[itemId].getAllChildrenAndSelfDFS(childrenAndSelf);
            foreach(var item in childrenAndSelf)
            {
                unregisterItemEventHandlers((TItem)item);

                _items.Remove(item.guid);
                _itemContainer.Remove(item);
                _state.selectedItems.Remove(itemId);

                if (item.guid == _state.selectRangeBeginId ||
                    item.guid == _state.selectRangeEndId)
                {
                    _state.hasSelectRange   = false;
                    _selectRangeBegin       = null;
                    _selectRangeEnd         = null;
                }

                _itemPool.releaseItem((TItem)item);
            }
        }

        private void registerItemEventHandlers(TItem item)
        {
            item.dragBegin              += onItemDragBegin;
            item.dragEnter              += onItemDragEnter;
            item.dragLeave              += onItemDragLeave;
            item.dragPerform            += onItemDragPerform;
            item.renameBegin            += onItemRenameBegin;
            item.renameEnd              += onItemRenameEnd;
            item.expandedStateChanged   += onItemExpandedStateChanged;

            item.RegisterCallback<MouseDownEvent>((p) =>
            { onItemClickedDown(item, p); });
            item.RegisterCallback<MouseUpEvent>((p) =>
            { onItemClickedUp(item, p); });
        }

        private void unregisterItemEventHandlers(TItem item)
        {
        }

        private void onItemDragBegin(TreeViewItem<TItemData> item)
        {
            _dragBeginItem = (TItem)item;
        }

        private void onItemDragEnter(TreeViewItem<TItemData> item)
        {
            _dropDestination = (TItem)item;
        }

        private void onItemDragLeave(TreeViewItem<TItemData> item)
        {
            _dropDestination = null;
        }

        private void onItemRenameBegin(TreeViewItem<TItemData> item)
        {
            if (renamingItem) _renameItem.endRename(true);
            _renameItem = (TItem)item;
        }

        private void onItemRenameEnd(TreeViewItem<TItemData> item, bool commit)
        {
            _renameItem = null;
        }

        private void onItemDragPerform(DragPerformEvent e)
        {
            // Note: Only proceed if the drag was initiated by a tree view item.
            //       External drag events must be ignored.
            if (_dragBeginItem != null) dropSelectedOnDropDestination(e);
            _dragBeginItem = null;
        }

        private void dropSelectedOnDropDestination(DragPerformEvent e)
        {
            var dropppedItems = new List<TItem>();
            getSelectedItems(dropppedItems);
            if (dropppedItems.Count == 0) return;
            
            if (FixedShortcuts.ui_TreeViewPlaceItemsAbove(e))
            {
                // Note: It is OK for this to be null. In that case, the items
                //       will be detached from their parent and placed above
                //       the drop destination.
                var dropDestinationParent = (TItem)_dropDestination.parentItem;

                var parentBuffer = new List<TItem>();
                getParents(dropppedItems, parentBuffer);
                dropppedItems.Clear();
                dropppedItems.AddRange(parentBuffer);

                // Sort the parents by the index in which they appear in their parent container.
                // This step is needed for the next 'foreach' loop when calling 'PlaceBehind'.
                // It helps us preserve the order in which they appear in the list.
                dropppedItems.Sort((TItem i0, TItem i1) =>
                {
                    int index0 = i0.parent.IndexOf(i0);
                    int index1 = i1.parent.IndexOf(i1);
                    return index0.CompareTo(index1);
                });

                var childBuffer = new List<TreeViewItem<TItemData>>();
                foreach (var item in dropppedItems)
                {
                    setItemParent(item, dropDestinationParent);
                    item.PlaceBehind(_dropDestination);

                    // Note: Because all items share the same parent (i.e. _itemContainer),
                    // we need to also move the children.
                    item.getAllChildrenBFS(childBuffer);
                    var insertAfter = item;
                    foreach (var child in childBuffer)
                    {
                        child.PlaceInFront(insertAfter);
                        insertAfter = (TItem)child;
                    }
                }

                // Note: Only rearrange children in the data model if we have a parent.
                //       This means that if we place items at the root (where they don't
                //       have a parent), their placement in the tree will be reset each
                //       time the tree is rebuilt because the data model has not been
                //       updated. This is OK. Unity seems to handle game objects the same
                //       way in the hierarchy view.
                if (dropDestinationParent != null)
                {
                    int parentIndex = dropDestinationParent.parent.IndexOf(dropDestinationParent);
                    foreach (var item in dropppedItems)
                    {
                        int relativeIndex = item.parent.IndexOf(item) - (parentIndex + 1);
                        item.setIndexInDataParent(relativeIndex);
                    }
                }
            }
            else
            {
                foreach (var item in dropppedItems)
                    setItemParent(item, _dropDestination);
            }

            var scrollToAction = new ScrollToItem<TItem>(_scrollView, _scrollView.findChildItemWithSmallestIndex(dropppedItems), Undo.GetCurrentGroup(), _state);
            GSpawn.active.registerEditorUpdateAction(scrollToAction);
        }
    }
}
#endif