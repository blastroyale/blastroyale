#if UNITY_EDITOR
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class ListViewItemPool<TItem, TItemData> : VisualElement
        where TItem : ListViewItem<TItemData>, new()
        where TItemData : IUIItemStateProvider
    {
        private IListView       _listView;
        private List<TItem>     _freeItems  = new List<TItem>();

        public static string    newItemTag  { get { return "ListViewItemPool.NewItem"; } }

        public ListViewItemPool(IListView listView)
        {
            _listView = listView;
        }

        public void releaseItem(TItem item)
        {
            _freeItems.Add(item);

            item.onWillBeDetachedFromListView();
            item.removeTag(newItemTag);
        }

        public TItem obtainItem(TItemData itemData)
        {
            TItem item = default(TItem);

            if (_freeItems.Count == 0)
            {
                item = new TItem();
                item.initialize(_listView, itemData);
                item.buildUI();
                _freeItems.Add(item);
                item.addTag(newItemTag);
            }

            item = _freeItems[_freeItems.Count - 1];
            _freeItems.RemoveAt(_freeItems.Count - 1);

            item.data = itemData;
            item.setVisible(true);
            item.refreshUI();

            return item;
        }
    }
}
#endif