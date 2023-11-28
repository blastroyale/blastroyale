#if UNITY_EDITOR
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class GridViewItemPool<TItem, TItemData> : VisualElement
        where TItem : GridViewItem<TItemData>, new()
        where TItemData : IUIItemStateProvider
    {
        private IGridView       _gridView;
        private List<TItem>     _freeItems  = new List<TItem>();

        public static string    newItemTag  { get { return "GridViewItemPool.NewItem"; } }

        public GridViewItemPool(IGridView gridView)
        {
            _gridView = gridView;
        }

        public void releaseItem(TItem item)
        {
            _freeItems.Add(item);

            item.data = default(TItemData);
            item.setVisible(false);
            item.removeTag(newItemTag);
        }

        public TItem obtainItem(TItemData itemData)
        {
            TItem item = default(TItem);

            if (_freeItems.Count == 0)
            {
                item = new TItem();
                item.initialize(_gridView, itemData);
                item.buildUI();
                _freeItems.Add(item);
                item.addTag(newItemTag);
            }

            item = _freeItems[_freeItems.Count - 1];
            _freeItems.RemoveAt(_freeItems.Count - 1);

            item.data = itemData;
            item.setVisible(true);

            return item;
        }
    }
}
#endif