#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class UIScatterBrushPrefabItemData : IUIItemStateProvider
    {
        public ScatterBrushPrefab           brushPrefab;
        public ScatterBrushPrefabProfile    brushPrefabProfile;

        public bool                         uiSelected              { get { return brushPrefab.uiSelected; } set { brushPrefab.uiSelected = value; } }
        public CopyPasteMode                uiCopyPasteMode         { get { return brushPrefab.uiCopyPasteMode; } set { brushPrefab.uiCopyPasteMode = value; } }
        public PluginGuid                   guid                    { get { return brushPrefab.guid; } }
    }

    public class UIScatterBrushPrefabItem : GridViewItem<UIScatterBrushPrefabItemData>
    {
        private PrefabPreviewUI     _prefabPreviewUI;
        private VisualElement       _previewImage;
        private Vector2             _imageSize;

        public override string      displayName                 { get { return data.brushPrefab.prefabAsset.name; } }
        public override PluginGuid  guid                        { get { return getItemId(data.brushPrefab); } }

        public override Vector2     imageSize
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

        public static PluginGuid getItemId(ScatterBrushPrefab brushPrefab) 
        { 
            return brushPrefab.guid; 
        }

        public static void getItemIds(List<ScatterBrushPrefab> brushPrefab, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var prefab in brushPrefab)
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
            _prefabPreviewUI.onRotatePreview    = (p) => { data.brushPrefab.rotatePreview(p.mouseDelta); };
            _previewImage                       = _prefabPreviewUI.previewImage;

            _displayNameLabel                   = _prefabPreviewUI.prefabNameLabel;
            _displayNameLabel.style.color       = getDisplayNameLabelColor();

            applyVariableStates();
            registerCallbacks();
        }

        private Color getDisplayNameLabelColor()
        {
            if (!data.brushPrefab.used) return UIValues.unusedPrefabLabelColor;
            return UIValues.prefabPreviewNameLabelColor;
        }

        private void applyVariableStates()
        {
            _previewImage.style.backgroundImage     = data.brushPrefab.previewTexture;
            _previewImage.tooltip                   = displayName + "\nModel size: " + data.brushPrefab.pluginPrefab.modelSize.ToString("F3");
            _displayNameLabel.text                  = data.brushPrefab.prefabAsset.name;
            _displayNameLabel.style.color           = getDisplayNameLabelColor();
        }

        private void registerCallbacks()
        {
            _prefabPreviewUI.resetPreviewButton.clicked     += () => { data.brushPrefab.resetPreview(); };
            _prefabPreviewUI.pingButton.clicked             += () => { data.brushPrefab.prefabAsset.pingPrefabAsset(); };
        }
    }
}
#endif