#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    // Note: Until there's any update here:
    //       https://forum.unity.com/threads/scrollview-scrollto-doesnt-seem-to-work-in-runtime.1024249/
    public class ScrollToItem<TItem> : EditorUpdateAction
        where TItem : VisualElement
    {
        private ScrollView          _scrollView;
        private TItem               _item;
        private int                 _undoGroup;
        private ScriptableObject    _undoRecordObject;

        public Action<TItem>        onPostScroll { get; set; }

        public ScrollToItem(ScrollView scrollView, TItem item, int undoGroup, ScriptableObject undoRecordObject)
        {
            _scrollView         = scrollView;
            _item               = item;
            _undoGroup          = undoGroup;
            _undoRecordObject   = undoRecordObject;
        }

        public ScrollToItem(VisualElement parent, ScrollView scrollView, TItem item)
        {
            _scrollView         = scrollView;
            _item               = item;
            _undoGroup          = 0;
            _undoRecordObject   = null;
        }

        protected override void execute()
        {
            // Note: Make sure the item hasn't been removed in the mean time
            if (_item != null && _item.parent != null)
            {
                if (_undoRecordObject != null) UndoEx.record(_undoRecordObject);
                _scrollView.ScrollTo(_item);
                if (onPostScroll != null) onPostScroll(_item);
                if (_undoRecordObject != null) Undo.CollapseUndoOperations(_undoGroup);
            }
        }
    }
}
#endif