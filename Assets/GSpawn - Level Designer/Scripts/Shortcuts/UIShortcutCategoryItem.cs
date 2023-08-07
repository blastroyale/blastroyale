#if UNITY_EDITOR
namespace GSpawn
{
    public class UIShortcutCategoryItem : ListViewItem<ShortcutCategory>
    {
        public override string      displayName     { get { return data.categoryName; } }
        public override PluginGuid  guid            { get { return getItemId(data); } }

        public static PluginGuid getItemId(ShortcutCategory shortcutCategory) 
        { 
            return shortcutCategory.guid; 
        }

        protected override void onPostBuildUI()
        {
            style.height                        = 20.0f;
            _displayNameLabel.style.marginLeft  = UIValues.listItemLeftMargin;
        }
    }
}
#endif