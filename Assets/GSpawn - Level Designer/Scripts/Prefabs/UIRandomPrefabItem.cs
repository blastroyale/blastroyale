#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class UIRandomPrefabItemData : IUIItemStateProvider
    {
        public RandomPrefab             randomPrefab;
        public RandomPrefabProfile      randomPrefabProfile;

        public bool                     uiSelected          { get { return randomPrefab.uiSelected; } set { randomPrefab.uiSelected = value; } }
        public CopyPasteMode            uiCopyPasteMode     { get { return randomPrefab.uiCopyPasteMode; } set { randomPrefab.uiCopyPasteMode = value; } }
        public PluginGuid               guid                { get { return randomPrefab.guid; } }
    }

    public class UIRandomPrefabItem : GridViewItem<UIRandomPrefabItemData>
    {
        private PrefabPreviewUI         _prefabPreviewUI;
        private VisualElement           _previewImage;
        private Vector2                 _imageSize;

        public override string          displayName         { get { return data.randomPrefab.prefabAsset.name; } }
        public override PluginGuid      guid                { get { return getItemId(data.randomPrefab); } }

        public override Vector2         imageSize
        {
            get { return _imageSize; }
            set
            {
                _imageSize                      = value;
                _previewImage.style.width       = value.x;
                _previewImage.style.height      = value.y;
                style.maxWidth                  = _imageSize.x;
            }
        }

        public static PluginGuid getItemId(RandomPrefab randomPrefab) 
        { 
            return randomPrefab.guid; 
        }

        public static void getItemIds(List<RandomPrefab> randomPrefabs, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var prefab in randomPrefabs)
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
            _prefabPreviewUI.onRotatePreview    = (p) => { data.randomPrefab.rotatePreview(p.mouseDelta); };
            _previewImage                       = _prefabPreviewUI.previewImage;

            _displayNameLabel                   = _prefabPreviewUI.prefabNameLabel;
            _displayNameLabel.style.color       = getDisplayNameLabelColor();

            applyVariableStates();
            registerCallbacks();
        }

        private Color getDisplayNameLabelColor()
        {
            if (!data.randomPrefab.used) return UIValues.unusedPrefabLabelColor;
            return UIValues.prefabPreviewNameLabelColor;
        }

        private void applyVariableStates()
        {
            _previewImage.style.backgroundImage     = data.randomPrefab.previewTexture;
            _previewImage.tooltip                   = displayName;
            _displayNameLabel.text                  = data.randomPrefab.prefabAsset.name;
            _displayNameLabel.style.color           = getDisplayNameLabelColor();
        }

        private void registerCallbacks()
        {
            _prefabPreviewUI.resetPreviewButton.clicked     += () => { data.randomPrefab.resetPreview(); };
            _prefabPreviewUI.pingButton.clicked             += () => { data.randomPrefab.prefabAsset.pingPrefabAsset(); };
        }
    }
}
#endif