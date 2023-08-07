#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class UIObjectGroupItem : TreeViewItem<ObjectGroup>
    {
        private Button              _highlightGroupButton;
        private Button              _makeDefaultGroupButton;

        public override string      displayName     { get { return data.gameObject.name; } }
        public override PluginGuid  guid            { get { return getItemId(data); } }

        public static PluginGuid getItemId(ObjectGroup objectGroup) 
        {
            return objectGroup.guid;
        }

        public static void getItemIds(List<ObjectGroup> objectGroups, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var objectGroup in objectGroups)
                ids.Add(getItemId(objectGroup));
        }

        public override ObjectGroup cloneData()
        {
            return ObjectGroupDb.instance.cloneObjectGroup(data);
        }

        public override void setDataParent(ObjectGroup parentData)
        {
            data.setParentObjectGroup(parentData);
        }

        public override void setIndexInDataParent(int indexInDataParent)
        {
            var group = data;
            if (group.parentGroup != null) group.parentGroup.setDirectObjectGroupChildIndex(group, indexInDataParent);
        }

        protected override void onBeginRename()
        {
            _renameField.SetValueWithoutNotify(displayName);
        }

        protected override void onEndRename(bool commit)
        {
            if (commit)
            {
                ObjectGroupDb.instance.renameObjectGroup(data, _renameField.text);
            }
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onBuildUIBeforeDisplayName()
        {
            _makeDefaultGroupButton                     = UI.createIconButton(TexturePool.instance.defaultObjectGroup, UIValues.smallIconSize, this);
            _makeDefaultGroupButton.tooltip             = "Mark as default object group.";
            _makeDefaultGroupButton.style.marginLeft    = 0.0f;
        }

        protected override void onBuildUIAfterDisplayName()
        {
            _displayNameLabel.style.flexGrow    = 1.0f;
            _renameField.style.flexGrow         = 1.0f;

            var button                          = UI.createButton(TexturePool.instance.lightBulb, UI.ButtonStyle.Push, UIValues.smallButtonSize, this);
            button.tooltip                      = "Activate group children (action filters apply).";
            button.style.marginRight            = -3.0f;
            button.clicked                      += () => { PluginScene.instance.setObjectGroupChildrenActive(data, true, true); };

            button                              = UI.createButton(TexturePool.instance.lightBulbGray, UI.ButtonStyle.Push, UIValues.smallButtonSize, this);
            button.tooltip                      = "Deactivate group children (action filters apply).";
            button.style.marginRight            = -3.0f;
            button.clicked                      += () => { PluginScene.instance.setObjectGroupChildrenActive(data, false, true); };

             _highlightGroupButton              = UI.createButton(TexturePool.instance.ping, UI.ButtonStyle.Push, UIValues.smallButtonSize, this);
            _highlightGroupButton.tooltip       = "Ping object in hierarchy view.";
            _highlightGroupButton.style.marginRight = -3.0f;
            _highlightGroupButton.clicked       += () => { EditorGUIUtility.PingObject(data.gameObject); };

            button                              = UI.createButton(TexturePool.instance.clear, UI.ButtonStyle.Push, UIValues.smallButtonSize, this);
            button.tooltip                      = "Delete immediate children. Note: Only deletes the children that are not object groups.";
            button.clicked                      += () => { data.destroyImmediateNonGroupChildren(); };

            applyVariableStates();
            registerCallbacks();
        }

        private void applyDefaultGroupState()
        {
            if (ObjectGroupDb.instance.defaultObjectGroup != data)
            {
                _displayNameLabel.style.color                                   = UIValues.listItemTextColor;
                _displayNameLabel.style.unityFontStyleAndWeight                 = FontStyle.Normal;
                _makeDefaultGroupButton.style.unityBackgroundImageTintColor     = UIValues.disabledColor;
            }
            else
            {
                _displayNameLabel.style.color                                   = UIValues.importantInfoLabelColor;
                _displayNameLabel.style.unityFontStyleAndWeight                 = FontStyle.Bold;
                _makeDefaultGroupButton.style.unityBackgroundImageTintColor     = Color.white;
            }
        }

        private void applyVariableStates()
        {
            _displayNameLabel.text          = displayName;
            _displayNameLabel.style.opacity = data.gameObject.activeInHierarchy ? 1.0f : UIValues.disabledOpacity;
            applyDefaultGroupState();
        }

        private void registerCallbacks()
        {
            _makeDefaultGroupButton.clicked += () =>
            {
                var currentDefaultGroup = ObjectGroupDb.instance.defaultObjectGroup;
                if (currentDefaultGroup != null)
                {
                    if (currentDefaultGroup == data)
                    {
                        ObjectGroupDb.instance.setDefaultObjectGroup(null);
                        applyDefaultGroupState();
                    }
                    else
                    {
                        ObjectGroupDb.instance.setDefaultObjectGroup(data);
                        applyDefaultGroupState();
                        ObjectGroupDbUI.instance.onObjectGroupNeedsUIRefresh(currentDefaultGroup);
                    }
                }
                else
                {
                    ObjectGroupDb.instance.setDefaultObjectGroup(data);
                    applyDefaultGroupState();
                }
            };
        }
    }
}
#endif