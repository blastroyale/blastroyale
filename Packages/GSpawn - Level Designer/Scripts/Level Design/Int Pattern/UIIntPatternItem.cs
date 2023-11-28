#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace GSpawn
{
    public class UIIntPatternItem : ListViewItem<IntPattern>
    {
        public override PluginGuid  guid        { get { return getItemId(data); } }
        public override string      displayName { get { return data.patternName; } }

        public static PluginGuid getItemId(IntPattern patern) 
        { 
            return patern.guid;
        }

        public static void getItemIds(List<IntPattern> patterns, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var pattern in patterns)
                ids.Add(getItemId(pattern));
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onBuildUIBeforeDisplayName()
        {
        }

        protected override void onBuildUIAfterDisplayName()
        {
            _displayNameLabel.style.flexGrow    = 1.0f;
            _displayNameLabel.style.marginLeft  = 5.0f;
            _renameField.style.flexGrow         = 1.0f;

            applyVariableStates();
        }

        protected override void onBeginRename()
        {
            _renameField.SetValueWithoutNotify(displayName);
        }

        protected override void onEndRename(bool commit)
        {
            if (commit)
            {
                IntPatternDb.instance.renamePattern(data, _renameField.text);
            }
        }

        private void applyVariableStates()
        {
            _displayNameLabel.text                          = displayName;
            _displayNameLabel.style.color                   = data == IntPatternDb.instance.defaultPattern ? UIValues.importantInfoLabelColor : UIValues.listItemTextColor;
            _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
        }
    }
}
#endif