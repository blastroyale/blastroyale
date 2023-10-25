#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class UIModularWallPrefabItemData : IUIItemStateProvider
    {
        public ModularWallPrefab        mdWallPrefab;
        public ModularWallPrefabProfile mdWallPrefabProfile;

        public bool                     uiSelected      { get { return mdWallPrefab.uiSelected; } set { mdWallPrefab.uiSelected = value; } }
        public CopyPasteMode            uiCopyPasteMode { get { return mdWallPrefab.uiCopyPasteMode; } set { mdWallPrefab.uiCopyPasteMode = value; } }
        public PluginGuid               guid            { get { return mdWallPrefab.guid; } }
    }

    public class UIModularWallPrefabItem : GridViewItem<UIModularWallPrefabItemData>
    {
        private PrefabPreviewUI     _prefabPreviewUI;
        private VisualElement       _previewImage;
        private Vector2             _imageSize;

        public override string      displayName { get { return data.mdWallPrefab.prefabAsset.name; } }
        public override PluginGuid  guid        { get { return getItemId(data.mdWallPrefab); } }

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

        public static PluginGuid getItemId(ModularWallPrefab mdWallPrefab)
        {
            return mdWallPrefab.guid;
        }

        public static void getItemIds(List<ModularWallPrefab> mdWallPrefabs, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var prefab in mdWallPrefabs)
                ids.Add(getItemId(prefab));
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onBuildUI()
        {
            _prefabPreviewUI = new PrefabPreviewUI();
            _prefabPreviewUI.initialize(this, _imageSize);
            _prefabPreviewUI.onRotatePreview = (p) => { data.mdWallPrefab.rotatePreview(p.mouseDelta); };
            _previewImage = _prefabPreviewUI.previewImage;

            _displayNameLabel               = _prefabPreviewUI.prefabNameLabel;
            _displayNameLabel.style.color   = getDisplayNameLabelColor();

            applyVariableStates();
            registerCallbacks();
        }

        private Color getDisplayNameLabelColor()
        {
            if (!data.mdWallPrefab.used) return UIValues.unusedPrefabLabelColor;
            return UIValues.prefabPreviewNameLabelColor;
        }

        private void applyVariableStates()
        {
            _previewImage.style.backgroundImage = data.mdWallPrefab.previewTexture;
            _previewImage.tooltip               = displayName + "\nModel size: " + data.mdWallPrefab.pluginPrefab.modelSize.ToString("F3");
            _displayNameLabel.text              = data.mdWallPrefab.prefabAsset.name;
            _displayNameLabel.style.color       = getDisplayNameLabelColor();
        }

        private void registerCallbacks()
        {
            _prefabPreviewUI.resetPreviewButton.clicked += () => { data.mdWallPrefab.resetPreview(); };
            _prefabPreviewUI.pingButton.clicked         += () => { data.mdWallPrefab.prefabAsset.pingPrefabAsset(); };
        }
    }
}
#endif