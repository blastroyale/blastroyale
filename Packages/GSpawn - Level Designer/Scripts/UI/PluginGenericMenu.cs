#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GSpawn
{
    public enum GenericMenuItemCategory
    {
        VisiblePrefabs              = 0,
    }

    public enum GenericMenuItemId
    {
        SelectAll                   = 0,
        DeselectAll,
        HighlightSelectedInManager
    }

    public class PluginGenericMenu
    {
        private GenericMenu _menu = new GenericMenu();

        public void addItem(GenericMenuItemCategory category, GenericMenuItemId id, bool enabled, GenericMenu.MenuFunction action)
        {
            string itemText = getItemCategoryText(category) + getItemIdText(id);

            if (enabled) _menu.AddItem(new GUIContent(itemText), false, action);
            else _menu.AddDisabledItem(new GUIContent(itemText));
        }

        public void showAsContext()
        {
            _menu.ShowAsContext();
        }

        private static string getItemCategoryText(GenericMenuItemCategory category)
        {
            if (category == GenericMenuItemCategory.VisiblePrefabs) return "Prefabs (Visible)/";
            return string.Empty;
        }

        private static string getItemIdText(GenericMenuItemId id)
        {
            switch (id) 
            {
                case GenericMenuItemId.SelectAll:

                    return "Select All";

                case GenericMenuItemId.DeselectAll:

                    return "Deselect All";

                case GenericMenuItemId.HighlightSelectedInManager:

                    return "Highlight Selected in Manager";

                default:

                    return string.Empty;
            }
        }
    }
}
#endif