#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace GSpawn
{
    public class ListView<TItem, TItemData> : VisualElement, IListView
        where TItem : ListViewItem<TItemData>, new()
        where TItemData : IUIItemStateProvider
    {
        public delegate void    CanDeleteSelectedItemsHandler       (ListView<TItem, TItemData> listView, List<PluginGuid> itemIds, YesNoAnswer answer);
        public delegate void    SelectedItemsWillBeDeletedHandler   (ListView<TItem, TItemData> listView, List<PluginGuid> itemIds);
        public delegate void    SelectionChangedHandler             (ListView<TItem, TItemData> listView);
        public delegate void    SelectionDeletedHandler             (ListView<TItem, TItemData> listView);
        public delegate void    PerformPasteHandler                 (ListView<TItem, TItemData> listView, List<PluginGuid> srcItemIds, List<PluginGuid> destItemIds, CopyPasteMode copyPasteMode);

        public event            CanDeleteSelectedItemsHandler      canDeleteSelectedItems;
        public event            SelectedItemsWillBeDeletedHandler  selectedItemsWillBeDeleted;
        public event            SelectionDeletedHandler            selectionDeleted;
        public event            SelectionChangedHandler            selectionChanged;
        public event            PerformPasteHandler                doPaste;

        public class DragAndDropData
        {
            private List<TItem> _items      = new List<TItem>();

            public int          numItems    { get { return _items.Count; } }

            public DragAndDropData(ListView<TItem, TItemData> listView)
            {
                listView.getVisibleSelectedItems(_items);
            }

            public TItem getItem(int index)
            {
                return _items[index];
            }
        }

        private class CopyOperation
        {
            public CopyPasteMode    copyPasteMode   = CopyPasteMode.Copy;
            public List<TItem>      sourceItems     = new List<TItem>();

            public bool             isActive        { get { return sourceItems.Count != 0; } }
        }

        [NonSerialized]
        private CopyOperation           _copyOp                     = new CopyOperation();

        [NonSerialized]
        private TItem                   _dragBeginItem              = null;
        [NonSerialized]
        private TItem                   _dropDestination            = null;
        [NonSerialized]
        private TItem                   _renameItem                 = null;

        private ListViewState           _state                      = null;

        [NonSerialized]
        private Dictionary<PluginGuid, TItem>       _items          = new Dictionary<PluginGuid, TItem>();
        [NonSerialized]
        private ListViewItemPool<TItem, TItemData>  _itemPool;

        [NonSerialized]
        private TItem                   _selectRangeBegin           = null;
        [NonSerialized]
        private TItem                   _selectRangeEnd             = null;

        [NonSerialized]
        private ScrollView              _scrollView;
        [NonSerialized]
        private VisualElement           _itemContainer;

        private bool                    hasSelectRange              { get { return _selectRangeBegin != null && _selectRangeEnd != null; } }

        public bool                     canDragAndDrop              { get; set; }
        public bool                     canRenameItems              { get; set; }
        public bool                     canMultiSelect              { get; set; }
        public bool                     canDelete                   { get; set; }
        public bool                     canDuplicate                { get; set; }
        public bool                     canCopyPaste                { get; set; }
        public bool                     canCutPaste                 { get; set; }
        public bool                     draggingItems               { get { return _dragBeginItem != null; } }
        public int                      numItems                    { get { return _items.Count; } }
        public int                      numSelectedItems            { get { return _state.selectedItems.Count; } }
        public bool                     renamingItem                { get { return _renameItem != null; } }
        public TItemData                dropDestinationData         { get { return _dropDestination != null ? _dropDestination.data : default(TItemData); } }
        public int                      dragAndDropInitiatorId      { get { return GetHashCode(); } }
        public int                      copyPasteInitiatorId        { get { return GetHashCode(); } }
        public System.Object            dragAndDropData             { get { return new DragAndDropData(this); } }
        public Func<PluginGuid, bool>   canDeleteItem               { get; set; }

        public ListView(ListViewState state, VisualElement parent)
        {
            _itemPool       = new ListViewItemPool<TItem, TItemData>(this);

            _state          = state;
            focusable       = true;
            style.flexGrow  = 1.0f;
            parent.Add(this);

            _scrollView                     = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.focusable           = true;
            _scrollView.style.flexGrow      = 1.0f;
            _scrollView.contentContainer.style.flexWrap         = Wrap.NoWrap;
            _scrollView.contentContainer.style.flexDirection    = FlexDirection.Column;
            _scrollView.contentContainer.style.flexGrow         = 1.0f;
            _scrollView.RegisterCallback<KeyDownEvent>(onKeyDown);
            _scrollView.verticalScroller.valueChanged += p =>
            {
                _state.storeScrollData(_scrollView);
            };

            RegisterCallback<DragUpdatedEvent>((p) => { PluginDragAndDrop.visualMode = DragAndDropVisualMode.Copy; });
            RegisterCallback<DragPerformEvent>(onDragPerform);
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

        public void getItems(List<TItem> items)
        {
            items.Clear();
            foreach (var pair in _items)
                items.Add(pair.Value);
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

        public int getNumVisibleSelectedItems()
        {
            int numItems = _state.selectedItems.Count;
            if (numItems == 0) return 0;

            foreach (var id in _state.selectedItems)
            {
                var item = _items[id];
                if (item.itemVisible) --numItems;
            }

            return numItems;
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

            item.style.borderBottomColor    = Color.black.createNewAlpha(0.15f);
            item.style.borderBottomWidth    = 1.0f;

            if (item.hasTag(ListViewItemPool<TItem, TItemData>.newItemTag)) registerItemEventHandlers(item);
            if (!visible) item.setVisible(false);

            setItemSelected(item.guid, itemData.uiSelected, false);
            item.setCopyPasteMode(itemData.uiCopyPasteMode);
            item.refreshUI();

            return item.guid;
        }

        public void deleteSelectedItems()
        {
            deleteSelected();
        }

        public void deleteItem(PluginGuid itemId)
        {
            doDeleteItem(itemId);
        }

        public void deleteItems(List<PluginGuid> itemsIds)
        {
            foreach (var id in itemsIds)
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

        public bool anySelectedItemsBeingRenamed()
        {
            foreach (var id in _state.selectedItems)
            {
                var item = _items[id];
                if (item.renaming) return true;
            }

            return false;
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

        private void onItemClickedDown(TItem item, MouseDownEvent e)
        {
            if (e.button != (int)MouseButton.LeftMouse || e.altKey) return;
            if (renamingItem && item != _renameItem) _renameItem.endRename(true);

            UndoEx.record(_state);
            if (((!e.ctrlKey && !e.shiftKey) || !canMultiSelect) && !isItemSelected(item.guid))
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

            if (FixedShortcuts.ui_DeleteSelected(e))            deleteSelected();
            else if (FixedShortcuts.ui_DuplicateSelected(e))    duplicateSelected();
            else if (FixedShortcuts.ui_SelectUp(e))             selectUp(e);
            else if (FixedShortcuts.ui_SelectDown(e))           selectDown(e);
            else if (FixedShortcuts.ui_SelectLeft(e))           selectUp(e);
            else if (FixedShortcuts.ui_SelectRight(e))          selectDown(e);
            else if (FixedShortcuts.ui_SelectAll(e))            selectAll();
            else if (FixedShortcuts.ui_Copy(e))                 copySelected(CopyPasteMode.Copy);
            else if (FixedShortcuts.ui_Cut(e))                  copySelected(CopyPasteMode.Cut);
            else if (FixedShortcuts.ui_Paste(e))                pasteSelected();
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
            if (doPaste == null || !_copyOp.isActive) return;
            if (_copyOp.copyPasteMode == CopyPasteMode.Copy && !canCopyPaste) return;
            if (_copyOp.copyPasteMode == CopyPasteMode.Cut && !canCutPaste) return;

            var destItemIds = new List<PluginGuid>();
            getSelectedItemIds(destItemIds);
            if (destItemIds.Count == 0) return;

            var srcItemIds = new List<PluginGuid>();
            getItemIds(_copyOp.sourceItems, srcItemIds);

            doPaste(this, srcItemIds, destItemIds, _copyOp.copyPasteMode);
            cancelCopyPaste();

            foreach (var itemId in _state.selectedItems)
                _items[itemId].refreshUI();

            foreach (var itemId in srcItemIds)
                _items[itemId].refreshUI();
        }

        private void cancelCopyPaste()
        {
            foreach (var item in _copyOp.sourceItems)
                item.setCopyPasteMode(CopyPasteMode.None);

            _copyOp.sourceItems.Clear();
        }

        private void deleteSelected()
        {
            if (numSelectedItems == 0 || !canDelete) return;

            var selectedIds = new List<PluginGuid>();
            getSelectedItemIds(selectedIds);

            if (canDeleteSelectedItems != null)
            {
                YesNoAnswer answer = new YesNoAnswer();
                canDeleteSelectedItems(this, selectedIds, answer);
                if (answer.hasNo) return;
            }

            if (selectedItemsWillBeDeleted != null) selectedItemsWillBeDeleted(this, selectedIds);

            UndoEx.record(_state);

            _state.selectedItems.Clear();
            if (canDeleteItem != null)
            {
                int numDeleted = 0;
                foreach (var selectedId in selectedIds)
                {
                    if (canDeleteItem(selectedId))
                    {
                        doDeleteItem(selectedId);
                        ++numDeleted;
                    }
                }

                if (numDeleted != 0 && selectionDeleted != null) selectionDeleted(this);
            }
            else
            {
                foreach (var selectedId in selectedIds)
                    doDeleteItem(selectedId);

                if (selectionDeleted != null) selectionDeleted(this);
            }
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
            if (!canMultiSelect) return;
            UndoEx.record(_state);

            clearSelection(false);
            setSelectRange((TItem)_itemContainer[0], (TItem)_itemContainer[_itemContainer.childCount - 1]);
            setItemsSelected(0, _itemContainer.childCount - 1, true);
        }

        private void duplicateSelected()
        {
            if (numSelectedItems == 0 || !canDuplicate) return;

            var allSelectedItems = new List<TItem>();
            getSelectedItems(allSelectedItems);

            PluginProgressDialog.begin("Duplicating Selected Items");
            List<TItem> clonedParents = new List<TItem>();
            for (int index = 0; index < allSelectedItems.Count; ++index)
            {
                var item        = allSelectedItems[index];
                var clonedData  = item.cloneData();
                float progress  = (index + 1) / (float)allSelectedItems.Count;
                PluginProgressDialog.updateItemProgress(item.displayName, progress);

                PluginGuid newItemId = addItem(clonedData, true);
                var clonedItem  = _items[newItemId];
                clonedParents.Add(clonedItem);
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

        private void selectUp(KeyDownEvent e)
        {
            if (hasSelectRange)
            {
                if (FixedShortcuts.ui_EnableDirectionalSelectAppend(e) && canMultiSelect)
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
                if (FixedShortcuts.ui_EnableDirectionalSelectAppend(e) && canMultiSelect)
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
            var item = _items[itemId];
            unregisterItemEventHandlers(item);

            _itemContainer.Remove(item);
            _items.Remove(itemId);
            _state.selectedItems.Remove(itemId);

            if (itemId == _state.selectRangeBeginId ||
                itemId == _state.selectRangeEndId)
            {
                _state.hasSelectRange   = false;
                _selectRangeBegin       = null;
                _selectRangeEnd         = null;
            }

            _itemPool.releaseItem(item);
        }

        private void registerItemEventHandlers(TItem item)
        {
            item.dragBegin      += onItemDragBegin;
            item.dragEnter      += onItemDragEnter;
            item.dragLeave      += onItemDragLeave;
            item.dragPerform    += onItemDragPerform;
            item.renameBegin    += onItemRenameBegin;
            item.renameEnd      += onItemRenameEnd;

            item.RegisterCallback<MouseDownEvent>((p) =>
            { onItemClickedDown(item, p); });
            item.RegisterCallback<MouseUpEvent>((p) =>
            { onItemClickedUp(item, p); });
        }

        private void unregisterItemEventHandlers(TItem item)
        {
        }

        private void onItemDragBegin(ListViewItem<TItemData> item)
        {
            _dragBeginItem = (TItem)item;
        }

        private void onItemDragEnter(ListViewItem<TItemData> item)
        {
            _dropDestination = (TItem)item;
        }

        private void onItemDragLeave(ListViewItem<TItemData> item)
        {
            _dropDestination = null;
        }

        private void onItemRenameBegin(ListViewItem<TItemData> item)
        {
            if (renamingItem) _renameItem.endRename(true);
            _renameItem = (TItem)item;
        }

        private void onItemRenameEnd(ListViewItem<TItemData> item, bool commit)
        {
            _renameItem = null;
        }

        private void onItemDragPerform()
        {
            _dragBeginItem = null;
        }

        private void onDragPerform(DragPerformEvent e)
        {
            _dragBeginItem = null;
        }
    }
}
#endif