#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class GridView<TItem, TItemData> : VisualElement, IGridView
        where TItem : GridViewItem<TItemData>, new()
        where TItemData : IUIItemStateProvider
    {
        public delegate void    SelectionChangedHandler             (GridView<TItem, TItemData> gridView);
        public delegate void    SelectedItemsWillBeDeletedHandler   (GridView<TItem, TItemData> gridView, List<PluginGuid> itemIds);
        public delegate void    PerformPasteHandler                 (GridView<TItem, TItemData> gridView, List<PluginGuid> srcItemIds, List<PluginGuid> destItemIds, CopyPasteMode copyPasteMode);

        public event            SelectionChangedHandler            selectionChanged;
        public event            SelectedItemsWillBeDeletedHandler  selectedItemsWillBeDeleted;
        public event            PerformPasteHandler                paste;

        private class CopyOperation
        {
            public CopyPasteMode    copyPasteMode   = CopyPasteMode.Copy;
            public List<TItem>      sourceItems     = new List<TItem>();

            public bool isActive                    { get { return sourceItems.Count != 0; } }
        }

        public class DragAndDropData
        {
            private List<TItem>     _items          = new List<TItem>();

            public int              numItems        { get { return _items.Count; } }

            public DragAndDropData(GridView<TItem, TItemData> gridView)
            {
                gridView.getVisibleSelectedItems(_items);
            }

            public TItem getItem(int index)
            {
                return _items[index];
            }
        }

        private GridViewState                       _state                      = null;

        [NonSerialized]
        private Dictionary<PluginGuid, TItem>       _items                      = new Dictionary<PluginGuid, TItem>();
        [NonSerialized]
        private GridViewItemPool<TItem, TItemData>  _itemPool;

        [NonSerialized]
        private CopyOperation                       _copyOp                     = new CopyOperation();

        [NonSerialized]
        private TItem                               _selectRangeBegin           = null;
        [NonSerialized]
        private TItem                               _selectRangeEnd             = null;

        [NonSerialized]
        private ScrollView                          _scrollView;
        [NonSerialized]
        private VisualElement                       _itemContainer;
        private bool                                _canMultiSelect             = true;

        private float                               _itemMarginLeft             = 1.0f;
        private float                               _itemMarginTop              = 1.0f;
        private Vector2                             _imageSize                  = new Vector2(128.0f, 128.0f);

        private bool                                hasSelectRange              { get { return _selectRangeBegin != null && _selectRangeEnd != null; } }

        public int                                  numSelectedItems            { get { return _state.selectedItems.Count; } }
        public int                                  dragAndDropInitiatorId      { get { return GetHashCode(); } }
        public int                                  copyPasteInitiatorId        { get { return GetHashCode(); } }
        public System.Object                        dragAndDropData             { get { return new DragAndDropData(this); } }
        public bool                                 canCopyPaste                { get; set; }
        public bool                                 canCutPaste                 { get; set; }
        public bool                                 canDelete                   { get; set; }
        public bool                                 canMultiSelect              { get { return _canMultiSelect; } set { _canMultiSelect = value; } }

        public GridView(GridViewState state, VisualElement parent)
        {
            _itemPool               = new GridViewItemPool<TItem, TItemData>(this);

            _state                  = state;
            focusable               = true;
            style.flexGrow          = 1.0f;
            parent.Add(this);

            _scrollView             = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.focusable   = true;
            _scrollView.contentContainer.style.flexWrap         = Wrap.NoWrap;
            _scrollView.contentContainer.style.flexDirection    = FlexDirection.Column;
            _scrollView.contentContainer.style.flexGrow         = 1.0f;
            _scrollView.RegisterCallback<KeyDownEvent>(onKeyDown);
            _scrollView.verticalScroller.valueChanged           += p => 
            {
                _state.storeScrollData(_scrollView);
            };
            Add(_scrollView);

            _itemContainer                      = new VisualElement();
            _itemContainer.style.flexWrap       = Wrap.Wrap;
            _itemContainer.style.flexDirection  = FlexDirection.Row;
            _scrollView.Add(_itemContainer);

            RegisterCallback<DragUpdatedEvent>((p) => { PluginDragAndDrop.visualMode = DragAndDropVisualMode.Copy; });
        }

        public void setItemCopyPasteMode(PluginGuid itemId, CopyPasteMode copyPasteMode)
        {
            _items[itemId].setCopyPasteMode(copyPasteMode);
        }

        public void setItemsCopyPasteMode(IEnumerable<PluginGuid> itemIds, CopyPasteMode copyPasteMode)
        {
            foreach (PluginGuid id in itemIds)
                setItemCopyPasteMode(id, copyPasteMode);
        }

        public void setAllItemsCopyPasteMode(CopyPasteMode copyPasteMode)
        {
            foreach (var pair in _items)
                setItemCopyPasteMode(pair.Key, copyPasteMode);
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

        public int deleteItems(Predicate<TItemData> removalCondition)
        {
            int numRemoved  = 0;
            var items       = new List<TItem>(_items.Values);
            foreach(var item in items)
            {
                if (removalCondition(item.data))
                {
                    ++numRemoved;
                    doDeleteItem(item.guid);
                }
            }

            return numRemoved;
        }

        public void setImageSize(Vector2 size)
        {
            foreach(var pair in _items)
                pair.Value.imageSize = size;

            _imageSize = size;
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

        public void setItemSelected(PluginGuid itemId, bool selected, bool notify)
        {
            bool alreadySelected = isItemSelected(itemId);
            if (selected && alreadySelected) return;
            if (!selected && !alreadySelected) return;

            UndoEx.record(_state);
            setItemSelected(_items[itemId], selected, notify);
        }

        public void setAllItemsVisible(bool visible)
        {
            foreach (var pair in _items)
                pair.Value.setVisible(visible);
        }

        public void setItemsVisible(List<PluginGuid> itemIds, bool visible)
        {
            foreach (var itemId in itemIds)
                _items[itemId].setVisible(visible);  
        }

        public void setItemVisible(PluginGuid itemId, bool visible)
        {
            _items[itemId].setVisible(visible);
        }

        public void getSelectedItemData(List<TItemData> itemData)
        {
            itemData.Clear();
            foreach (var id in _state.selectedItems)
                itemData.Add(_items[id].data);
        }

        public void getSelectedItemData(List<TItemData> itemData, bool onlyVisible)
        {
            if (onlyVisible) getVisibleSelectedItemData(itemData);
            else getSelectedItemData(itemData);
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

        public void getVisibleItemData(List<TItemData> itemData)
        {
            itemData.Clear();
            foreach (var pair in _items)
            {
                var item = pair.Value;
                if (item.itemVisible) itemData.Add(item.data);
            }
        }

        public void refreshUI()
        {
            if (GSpawn.active == null) return;

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

        public void refreshSelectedItemsUI()
        {
            foreach (var id in _state.selectedItems)
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

        public void filterItems(Func<TItemData, bool> filter)
        {
            foreach (var pair in _items)
                pair.Value.setVisible(filter(pair.Value.data));
        }

        public bool isItemSelected(PluginGuid itemId)
        {
            return _state.selectedItems.Contains(itemId);
        }

        public void onBeginBuild()
        {
            foreach (var pair in _items)
                _itemPool.releaseItem(pair.Value);

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
                _selectRangeBegin           = null;
                _selectRangeEnd             = null;
            }

            _state.applyScrollState(_scrollView);
        }

        public PluginGuid addItem(TItemData itemData, bool visible)
        {
            var item = _itemPool.obtainItem(itemData);
            _items.Add(item.guid, item);
            _itemContainer.Add(item);

            item.imageSize          = _imageSize;
            item.style.marginLeft   = _itemMarginLeft;
            item.style.marginTop    = _itemMarginTop;

            //item.buildUI();
            if (item.hasTag(GridViewItemPool<TItem, TItemData>.newItemTag)) registerItemEventHandlers(item);
            if (!visible) item.setVisible(false);

            setItemSelected(item.guid, itemData.uiSelected, false);
            item.setCopyPasteMode(itemData.uiCopyPasteMode);
            item.refreshUI();

            return item.guid;
        }

        private void onItemClickedDown(TItem item, MouseDownEvent e)
        {
            if (e.button != (int)MouseButton.LeftMouse || e.altKey) return;

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
            if (_copyOp.isActive)
            {
                if (FixedShortcuts.cancelAction(e))
                {
                    UICopyPaste.cancel();
                    return;
                }
            }

            if (FixedShortcuts.ui_DeleteSelected(e))    deleteSelected();
            else if (FixedShortcuts.ui_SelectUp(e))     selectUp(e);
            else if (FixedShortcuts.ui_SelectDown(e))   selectDown(e);
            else if (FixedShortcuts.ui_SelectLeft(e))   selectLeft(e);
            else if (FixedShortcuts.ui_SelectRight(e))  selectRight(e);
            else if (FixedShortcuts.ui_Copy(e))         copySelected(CopyPasteMode.Copy);
            else if (FixedShortcuts.ui_Cut(e))          copySelected(CopyPasteMode.Cut);
            else if (FixedShortcuts.ui_Paste(e))        pasteSelected();
            else if (FixedShortcuts.ui_SelectAll(e))    selectAll();
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

            if (paste != null) paste(this, srcItemIds, destItemIds, _copyOp.copyPasteMode);
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
            if (selectedItemsWillBeDeleted != null) selectedItemsWillBeDeleted(this, selectedIds);

            UndoEx.record(_state);

            _state.selectedItems.Clear();
            foreach (var selectedId in selectedIds)
                doDeleteItem(selectedId);
        }

        private void selectAll()
        {
            if (!canMultiSelect) return;
            UndoEx.record(_state);

            clearSelection(false);
            setSelectRange((TItem)_itemContainer[0], (TItem)_itemContainer[_itemContainer.childCount - 1]);
            setItemsSelected(0, _itemContainer.childCount - 1, true);
        }

        private void selectUp(KeyDownEvent e)
        {
            if (hasSelectRange)
            {
                if (FixedShortcuts.ui_EnableDirectionalSelectAppend(e) && canMultiSelect)
                {
                    int itemPitch   = getNumItemsPerRow();
                    int endIndex    = _itemContainer.IndexOf(_selectRangeEnd);
                    int beginIndex  = _itemContainer.IndexOf(_selectRangeBegin);
                    int nextIndex   = Mathf.Max(0, endIndex - itemPitch);

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

                        nextIndex -= itemPitch;
                        nextIndex = Mathf.Max(0, nextIndex);
                    }

                    _scrollView.ScrollTo(_selectRangeEnd);
                }
                else
                {
                    int itemPitch = getNumItemsPerRow();
                    int nextIndex = Mathf.Max(0, _itemContainer.IndexOf(_selectRangeEnd) - itemPitch);
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

                        nextIndex -= itemPitch;
                        nextIndex = Mathf.Max(0, nextIndex);
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
                    int itemPitch   = getNumItemsPerRow();
                    int endIndex    = _itemContainer.IndexOf(_selectRangeEnd);
                    int beginIndex  = _itemContainer.IndexOf(_selectRangeBegin);
                    int nextIndex   = Mathf.Min(_itemContainer.childCount - 1, endIndex + itemPitch);

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

                        nextIndex += itemPitch;
                        nextIndex = Mathf.Min(_itemContainer.childCount - 1, nextIndex);
                    }

                    _scrollView.ScrollTo(_selectRangeEnd);
                }
                else
                {
                    int itemPitch = getNumItemsPerRow();
                    int nextIndex = Mathf.Min(_itemContainer.childCount - 1, _itemContainer.IndexOf(_selectRangeEnd) + itemPitch);
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

                        nextIndex += itemPitch;
                        nextIndex = Mathf.Min(_itemContainer.childCount - 1, nextIndex);
                    }

                    _scrollView.ScrollTo(_selectRangeEnd);
                    if (selectionChanged != null) selectionChanged(this);
                }
            }
        }

        private void selectLeft(KeyDownEvent e)
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

        private void selectRight(KeyDownEvent e)
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

        private void setItemSelected(TItem item, bool selected, bool notify)
        {
            if (selected) _state.selectedItems.Add(item.guid);
            else _state.selectedItems.Remove(item.guid);
            item.setSelected(selected);

            if (notify && selectionChanged != null) selectionChanged(this);
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
            item.RegisterCallback<MouseDownEvent>((p) =>
            { onItemClickedDown(item, p); });
            item.RegisterCallback<MouseUpEvent>((p) =>
            { onItemClickedUp(item, p); });
        }

        private void unregisterItemEventHandlers(TItem item)
        {
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

        private float getItemAreaWidth()
        {
            return _scrollView.worldBound.width - _scrollView.verticalScroller.worldBound.width;
        }

        private int getNumItemsPerRow()
        {
            return Mathf.Max(1, (int)(getItemAreaWidth() / (_imageSize.x + _itemMarginLeft)));
        }
    }
}
#endif