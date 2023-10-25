#if UNITY_EDITOR
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public static class VisualElementEx
    {
        public static void setChildLabelWidth(this VisualElement elem, float width)
        {
            elem.Q<Label>().style.width = width;
        }

        public static void setDisplayVisible(this VisualElement elem, bool visible)
        {
            elem.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static bool isDisplayVisible(this VisualElement elem)
        {
            return elem.style.display == DisplayStyle.Flex;
        }

        public static bool isDisplayHidden(this VisualElement elem)
        {
            return elem.style.display == DisplayStyle.None;
        }

        public static void setChildDisplayVisible(this VisualElement parent, string childName, bool visible)
        {
            var child = parent.Q<VisualElement>(childName);
            if (child != null) child.setDisplayVisible(visible);
        }

        public static void setChildrenDisplayVisible(this VisualElement parent, string childrenName, bool visible)
        {
            int numChildren = parent.childCount;
            for (int i = 0; i < numChildren; ++i)
            {
                var child = parent.ElementAt(i);
                if (child.name == childrenName) child.setDisplayVisible(visible);
            }
        }

        public static void removeChildren(this VisualElement parent, List<string> childNames)
        {
            foreach(var childName in childNames)
            {
                var child = parent.Q<VisualElement>(childName);
                if (child != null) parent.Remove(child);
            }
        }

        public static void removeAllChildrenExcept(this VisualElement parent, VisualElement exceptionChild)
        {
            int exceptionChildIndex = parent.IndexOf(exceptionChild);
            if (exceptionChildIndex == -1) parent.Clear();
            else
            {
                // Remove the children which appear before the exception child
                int numToRemove = exceptionChildIndex;
                for (int i = 0; i < numToRemove; ++i)
                    parent.RemoveAt(0);

                // Remove the children which appear after the exception child
                numToRemove = parent.childCount - 1;
                for (int i = 0; i < numToRemove; ++i)
                    parent.RemoveAt(1);
            }
        }

        public static void setAllChildrenEnabledExcept(this VisualElement parent, bool enabled, VisualElement exceptionChild)
        {
            for (int i = 0; i < parent.childCount; ++i)
            {
                var child = parent.ElementAt(i);
                if (child != exceptionChild) child.SetEnabled(enabled);
            }
        }

        public static T findChildItemWithSmallestIndex<T>(this VisualElement parent, List<T> items) where T : VisualElement
        {
            if (items.Count == 0) return null;

            HashSet<T> itemsSet = new HashSet<T>(items);
            int numChildren = parent.childCount;
            for (int childIndex = 0; childIndex < numChildren; ++childIndex)
            {
                var child = (T)parent.ElementAt(childIndex);
                if (itemsSet.Contains(child)) return child;
            }

            return null;
        }

        public static T findChildItemWithGreatestIndex<T>(this VisualElement parent, List<T> items) where T : VisualElement
        {
            if (items.Count == 0) return null;

            HashSet<T> itemsSet = new HashSet<T>(items);
            int numChildren = parent.childCount;
            for (int childIndex = numChildren - 1; childIndex >= 0; --childIndex)
            {
                var child = (T)parent.ElementAt(childIndex);
                if (itemsSet.Contains(child)) return child;
            }

            return null;
        }

        public static void setChildrenMarginLeft(this VisualElement parent, float margin)
        {
            var children = parent.Children();
            foreach (var child in children)
                child.style.marginLeft = margin;
        }

        public static void setFieldLabelWidth(this VisualElement parent, float labelWidth)
        {
            parent.Query<Label>().ForEach(label => label.style.width = labelWidth);
        }

        public static TUIElement findDirectChild<TUIElement>(this VisualElement parent, string name) where TUIElement : VisualElement
        {
            var children = parent.Children();
            foreach (var child in children)
                if (child.name == name) return child as TUIElement;

            return null;
        }

        public static TUIElement findChild<TUIElement>(this VisualElement parent, string name) where TUIElement : VisualElement
        {
            var children = parent.Children();
            foreach (var child in children)
            {
                if (child.name == name) return child as TUIElement;
                var elem = child.findChild<TUIElement>(name);
                if (elem != null) return elem;
            }

            return null;
        }
    }
}
#endif