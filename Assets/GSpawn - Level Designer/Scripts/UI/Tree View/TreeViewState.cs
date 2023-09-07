#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class TreeViewState : ScriptableObject
    {
        [SerializeField]
        public PluginGuidHashSet    selectedItems           = new PluginGuidHashSet();
        [SerializeField]
        public PluginGuidHashSet    collapsedItems          = new PluginGuidHashSet();
        [SerializeField]
        public PluginGuid           selectRangeBeginId;
        [SerializeField]
        public PluginGuid           selectRangeEndId;
        [SerializeField]
        public bool                 hasSelectRange;
        [SerializeField]
        public Vector2              scrollOffset;
        [SerializeField]
        public float                vertScrollHighValue;
        [SerializeField]
        public float                vertScrollLowValue;

        public void storeScrollData(ScrollView scrollView)
        {
            scrollOffset            = scrollView.scrollOffset;
            vertScrollHighValue     = scrollView.verticalScroller.highValue;
            vertScrollLowValue      = scrollView.verticalScroller.lowValue;
        }

        public void applyScrollState(ScrollView scrollView)
        {
            // Save this here before changing high and low values. Changing
            // those values will call the value changed handler and that
            // will in turn call storeScrollData.
            Vector2 scrOffset                       = scrollOffset;  
            scrollView.verticalScroller.lowValue    = vertScrollLowValue;
            scrollView.verticalScroller.highValue   = vertScrollHighValue;
            scrollView.scrollOffset                 = scrOffset;

            // Restore ...
            scrollOffset                            = scrOffset;
        }
    }
}
#endif