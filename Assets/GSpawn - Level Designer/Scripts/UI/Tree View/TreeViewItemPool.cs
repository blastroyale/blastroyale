#if UNITY_EDITOR
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class TreeViewItemPool<TItem, TItemData> : VisualElement
        where TItem : TreeViewItem<TItemData>, new()
        where TItemData : IUIItemStateProvider
    {
        private ITreeView       _treeView;
        private List<TItem>     _freeItems  = new List<TItem>();

        public static string    newItemTag  { get { return "TreeViewItemPool.NewItem"; } }

        public TreeViewItemPool(ITreeView treeView)
        {
            _treeView = treeView;
        }

        public void releaseItem(TItem item)
        {
            _freeItems.Add(item);

            item.onWillBeDetachedFromTreeView();
            item.removeTag(newItemTag);
        }

        public TItem obtainItem(TItemData itemData)
        {
            TItem item = default(TItem);

            if (_freeItems.Count == 0)
            {
                item = new TItem();
                item.initialize(_treeView, itemData);
                item.buildUI();
                _freeItems.Add(item);
                item.addTag(newItemTag);
            }

            item = _freeItems[_freeItems.Count - 1];
            _freeItems.RemoveAt(_freeItems.Count - 1);

            item.parentItem = null;
            item.data       = itemData;
            item.setVisible(true);
            item.setExpanded(true, false);

            return item;
        }
    }
}
#endif