#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class UIPrefabLibItem : TreeViewItem<PrefabLib>
    {
        private Button              _prefabVisToggle;
        private Label               _numPrefabsLabel;
        private VisualElement       _postDisplayNameContainer;

        public override string      displayName         { get { return PrefabLibProfileDb.instance.activeProfile.getLibDisplayName(data); } }
        public override PluginGuid  guid                { get { return getItemId(data); } }

        public static PluginGuid getItemId(PrefabLib prefabLib) 
        {
            return prefabLib.guid; 
        }

        public static void getItemIds(List<PrefabLib> prefabLibs, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var lib in prefabLibs)
                ids.Add(getItemId(lib));
        }

        public override PrefabLib cloneData()
        {
            return PrefabLibProfileDb.instance.activeProfile.cloneLib(data, PrefabLib.CloneFlags.None);
        }

        public override void setDataParent(PrefabLib parentData)
        {
            data.parentLib = parentData;
            applyPrefabVisibilityState();
        }

        public override void setIndexInDataParent(int indexInDataParent)
        {
            PrefabLib lib = data;
            if (lib.parentLib != null) lib.parentLib.setDirectChildIndex(lib, indexInDataParent);
        }

        public override bool canBeCopyPasteSource()
        {
            return !data.empty;
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onBuildUIBeforeDisplayName()
        {
            _prefabVisToggle                    = UI.createIconButton(getPrefabVisibilityIcon(), UIValues.smallToolbarButtonSize, this);
            _prefabVisToggle.style.alignSelf    = Align.Center;
            _prefabVisToggle.style.marginLeft   = 0.0f;
            _prefabVisToggle.tooltip            = "Toggle prefab visibility in prefab manager.";
            Add(_prefabVisToggle);
        }

        protected override void onBuildUIAfterDisplayName()
        {
            _displayNameLabel.tooltip                       = data.folderPath;
            _displayNameLabel.style.flexGrow                = 1.0f;
            _renameField.style.flexGrow                     = 1.0f;

            _postDisplayNameContainer                       = new VisualElement();
            _postDisplayNameContainer.style.flexGrow        = 1.0f;
            _postDisplayNameContainer.setDisplayVisible(false);
            Add(_postDisplayNameContainer);

            /*_cutIcon = new VisualElement();
            _cutIcon.style.setBackgroundImage(TexturePool.instance.scissors, true);
            _postDisplayNameContainer.Add(_cutIcon);*/

            _numPrefabsLabel                                = new Label();
            _numPrefabsLabel.setDisplayVisible(!data.empty);
            _numPrefabsLabel.text                           = data.numPrefabs.ToString();
            _numPrefabsLabel.style.color                    = UIValues.listItemTextColor;
            _numPrefabsLabel.style.unityFontStyleAndWeight  = FontStyle.Bold;
            _numPrefabsLabel.tooltip                        = "The number of prefabs stored in this library.";  
            Add(_numPrefabsLabel);

            applyVariableStates();
            registerCallbacks();
        }

        protected override void onCopyPasteModeChanged()
        {
            applyCopyPasteSourceState();
        }

        protected override void onBeginRename()
        {
            _renameField.SetValueWithoutNotify(data.libName);
        }

        protected override void onEndRename(bool commit)
        {
            if (commit)
            {
                PrefabLibProfileDb.instance.activeProfile.renameLib(data, _renameField.text);
            }        
        }

        private Texture2D getPrefabVisibilityIcon()
        {
            return TexturePool.instance.visible;
        }

        private void applyLibEmptyState()
        {
            if (data.empty)
            {
                _numPrefabsLabel.setDisplayVisible(false);
                _displayNameLabel.style.color                   = UIValues.importantInfoLabelColor;
                _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                _numPrefabsLabel.setDisplayVisible(true);
                _displayNameLabel.style.color                   = UIValues.listItemTextColor;
                _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
        }

        private void applyCopyPasteSourceState()
        {
            if (copyPasteMode != CopyPasteMode.None)
            {
                _displayNameLabel.style.color                   = UIValues.copySourceListItemTextColor;
                _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                if (copyPasteMode == CopyPasteMode.Cut)
                {
                    _postDisplayNameContainer.setDisplayVisible(true);
                    _displayNameLabel.style.flexGrow = 0.0f;
                }
            }
            else
            {
                applyLibEmptyState();
                _postDisplayNameContainer.setDisplayVisible(false);
                _displayNameLabel.style.flexGrow = 1.0f;
            }
        }

        private void applyPrefabVisibilityState()
        {
            _prefabVisToggle.style.setBackgroundImage(getPrefabVisibilityIcon(), true);

            if (!data.prefabsVisibleInManagerLocal || !data.prefabsVisibleInManagerGlobal()) style.opacity = UIValues.disabledOpacity;
            else style.opacity = 1.0f;
        }

        private void applyVariableStates()
        {
            _displayNameLabel.tooltip   = data.folderPath;
            _numPrefabsLabel.text       = data.numPrefabs.ToString();

            applyPrefabVisibilityState();
            applyCopyPasteSourceState();
            applyLibEmptyState();
        }

        private void registerCallbacks()
        {
            _prefabVisToggle.RegisterCallback<MouseUpEvent>((p) =>
            {
                if (!data.prefabsVisibleInManagerGlobal()) return;

                bool visible = !data.prefabsVisibleInManagerLocal;
                data.setPrefabsVisibleInManagerLocal(visible);
                applyPrefabVisibilityState();

                var libs = new List<PrefabLib>();
                PrefabLib.getLibsInHierarchy(data, libs);
                PluginPrefabManagerUI.instance.libsChangedPrefabVisibility(libs, visible);

                var allChildren = new List<TreeViewItem<PrefabLib>>();
                getAllChildrenBFS(allChildren);
                foreach (var child in allChildren)
                {
                    var libItem = (UIPrefabLibItem)child;
                    libItem.applyPrefabVisibilityState();
                }
            });
        }
    }
}
#endif