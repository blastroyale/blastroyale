#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class UICurvePrefabItemData : IUIItemStateProvider
    {
        public CurvePrefab          curvePrefab;
        public CurvePrefabProfile   curvePrefabProfile;

        public bool                 uiSelected          { get { return curvePrefab.uiSelected; } set { curvePrefab.uiSelected = value; } }
        public CopyPasteMode        uiCopyPasteMode     { get { return curvePrefab.uiCopyPasteMode; } set { curvePrefab.uiCopyPasteMode = value; } }
        public PluginGuid           guid                { get { return curvePrefab.guid; } }
    }

    public class UICurvePrefabItem : GridViewItem<UICurvePrefabItemData>
    {
        private PrefabPreviewUI     _prefabPreviewUI;
        private VisualElement       _previewImage;
        private Vector2             _imageSize;

        public override string      displayName         { get { return data.curvePrefab.prefabAsset.name; } }
        public override PluginGuid  guid                { get { return getItemId(data.curvePrefab); } }

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

        public static PluginGuid getItemId(CurvePrefab curvePrefab) 
        { 
            return curvePrefab.guid;
        }

        public static void getItemIds(List<CurvePrefab> curvePrefabs, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var prefab in curvePrefabs)
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
            _prefabPreviewUI.onRotatePreview    = (p) => { data.curvePrefab.rotatePreview(p.mouseDelta); };
            _previewImage                       = _prefabPreviewUI.previewImage;

            _displayNameLabel                   = _prefabPreviewUI.prefabNameLabel;
            _displayNameLabel.style.color       = getDisplayNameLabelColor();

            applyVariableStates();
            registerCallbacks();
        }

        private Color getDisplayNameLabelColor()
        {
            if (!data.curvePrefab.used) return UIValues.unusedPrefabLabelColor;
            return UIValues.prefabPreviewNameLabelColor;
        }

        private void applyVariableStates()
        {
            _previewImage.style.backgroundImage     = data.curvePrefab.previewTexture;
            _previewImage.tooltip                   = displayName;
            _displayNameLabel.text                  = data.curvePrefab.prefabAsset.name;
            _displayNameLabel.style.color           = getDisplayNameLabelColor();
        }

        private void registerCallbacks()
        {
            _prefabPreviewUI.resetPreviewButton.clicked     += () => { data.curvePrefab.resetPreview(); };
            _prefabPreviewUI.pingButton.clicked             += () => { data.curvePrefab.prefabAsset.pingPrefabAsset(); };
        }
    }
}
#endif