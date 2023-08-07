#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class UITileRulePrefabItemData : IUIItemStateProvider
    {
        public TileRulePrefab   tileRulePrefab;
        public TileRuleProfile  tileRuleProfile;

        public bool             uiSelected              { get { return tileRulePrefab.uiSelected; } set { tileRulePrefab.uiSelected = value; } }
        public CopyPasteMode    uiCopyPasteMode         { get { return tileRulePrefab.uiCopyPasteMode; } set { tileRulePrefab.uiCopyPasteMode = value; } }
        public PluginGuid       guid                    { get { return tileRulePrefab.guid; } }
    }

    public class UITileRulePrefabItem : GridViewItem<UITileRulePrefabItemData>
    {
        private PrefabPreviewUI     _prefabPreviewUI;
        private VisualElement       _previewImage;
        private Vector2             _imageSize;

        public override string      displayName { get { return data.tileRulePrefab.prefabAsset.name; } }
        public override PluginGuid  guid { get { return getItemId(data.tileRulePrefab); } }

        public override Vector2 imageSize
        {
            get { return _imageSize; }
            set
            {
                _imageSize                  = value;
                _previewImage.style.width   = value.x;
                _previewImage.style.height  = value.y;
                style.maxWidth              = _imageSize.x;
            }
        }

        public static PluginGuid getItemId(TileRulePrefab tileRulePrefab)
        {
            return tileRulePrefab.guid;
        }

        public static void getItemIds(List<TileRulePrefab> tileRulePrefabs, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var prefab in tileRulePrefabs)
                ids.Add(getItemId(prefab));
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onBuildUI()
        {
            _prefabPreviewUI                    = new PrefabPreviewUI();
            _prefabPreviewUI.initialize(this, _imageSize);
            _prefabPreviewUI.onRotatePreview    = (p) => { data.tileRulePrefab.rotatePreview(p.mouseDelta); };
            _previewImage                       = _prefabPreviewUI.previewImage;

            _displayNameLabel                   = _prefabPreviewUI.prefabNameLabel;
            _displayNameLabel.style.color       = getDisplayNameLabelColor();

            applyVariableStates();
            registerCallbacks();
        }

        private Color getDisplayNameLabelColor()
        {
            if (!data.tileRulePrefab.used) return UIValues.unusedPrefabLabelColor;
            return UIValues.prefabPreviewNameLabelColor;
        }

        private void applyVariableStates()
        {
            _previewImage.style.backgroundImage     = data.tileRulePrefab.previewTexture;
            _previewImage.tooltip                   = displayName + "\nModel size: " + data.tileRulePrefab.pluginPrefab.modelSize.ToString("F3");
            _displayNameLabel.text                  = data.tileRulePrefab.prefabAsset.name;
            _displayNameLabel.style.color           = getDisplayNameLabelColor();
        }

        private void registerCallbacks()
        {
            _prefabPreviewUI.resetPreviewButton.clicked     += () => { data.tileRulePrefab.resetPreview(); };
            _prefabPreviewUI.pingButton.clicked             += () => { data.tileRulePrefab.prefabAsset.pingPrefabAsset(); };
        }
    }
}
#endif