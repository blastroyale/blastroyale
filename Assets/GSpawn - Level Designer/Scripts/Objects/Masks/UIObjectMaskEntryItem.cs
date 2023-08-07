#if UNITY_EDITOR
namespace GSpawn
{
    public class UIObjectMaskEntryItem : ListViewItem<ObjectMaskEntry>
    {
        public override string      displayName     { get { return data.gameObject.name; } }
        public override PluginGuid  guid            { get { return getItemId(data); } }

        public static PluginGuid getItemId(ObjectMaskEntry maskEntry)
        { 
            return maskEntry.guid;
        }

        protected override void onBuildUIBeforeDisplayName()
        {
            //UI.createIcon(TexturePool.instance.scenePicking_notPickable, this);
        }
    }
}
#endif