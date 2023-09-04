#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class UIIntRangePrefabItemData : IUIItemStateProvider
    {
        public IntRangePrefab           intRangePrefab;
        public IntRangePrefabProfile    intRangePrefabProfile;

        public bool                     uiSelected          { get { return intRangePrefab.uiSelected; } set { intRangePrefab.uiSelected = value; } }
        public CopyPasteMode            uiCopyPasteMode     { get { return intRangePrefab.uiCopyPasteMode; } set { intRangePrefab.uiCopyPasteMode = value; } }
        public PluginGuid               guid                { get { return intRangePrefab.guid; } }
    }

    public class UIIntRangePrefabItem : GridViewItem<UIIntRangePrefabItemData>
    {
        private PrefabPreviewUI         _prefabPreviewUI;
        private VisualElement           _previewImage;
        private Vector2                 _imageSize;
        private Button                  _defaultPrefabButton;

        public override string          displayName         { get { return data.intRangePrefab.prefabAsset.name; } }
        public override PluginGuid      guid                { get { return getItemId(data.intRangePrefab); } }

        public override Vector2         imageSize
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

        public static PluginGuid getItemId(IntRangePrefab irPrefab) 
        {
            return irPrefab.guid;
        }

        public static void  getItemIds(List<IntRangePrefab> irPrefabs, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var prefab in irPrefabs)
                ids.Add(getItemId(prefab));
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onBuildUI()
        {
            _prefabPreviewUI = new PrefabPreviewUI();
            _prefabPreviewUI.initialize(this, _imageSize, () => 
            { 
                _defaultPrefabButton = UI.createToolbarButton(TexturePool.instance.defaultObject, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, _prefabPreviewUI.bottomToolbar);
                UI.useDefaultMargins(_defaultPrefabButton);
                _defaultPrefabButton.tooltip = "Set this as the default pick prefab.";
            });
            _prefabPreviewUI.onRotatePreview    = (p) => { data.intRangePrefab.rotatePreview(p.mouseDelta); };
            _previewImage                       = _prefabPreviewUI.previewImage;

            _displayNameLabel                   = _prefabPreviewUI.prefabNameLabel;
            _displayNameLabel.style.color       = getDisplayNameLabelColor();
            
            applyVariableStates();
            registerCallbacks();
        }

        private Color getDisplayNameLabelColor()
        {
            if (!data.intRangePrefab.used) return UIValues.unusedPrefabLabelColor;
            return UIValues.prefabPreviewNameLabelColor;
        }

        private void applyDefaultPrefabState()
        {
            if (!data.intRangePrefabProfile.isDefaultPickPrefab(data.intRangePrefab))
            {
                _defaultPrefabButton.style.unityBackgroundImageTintColor = UIValues.disabledColor;
            }
            else
            {
                _defaultPrefabButton.style.unityBackgroundImageTintColor = Color.white;
            }
        }

        private void applyVariableStates()
        {
            _previewImage.style.backgroundImage     = data.intRangePrefab.previewTexture;
            _previewImage.tooltip                   = displayName;
            _displayNameLabel.text                  = data.intRangePrefab.prefabAsset.name;
            _displayNameLabel.style.color           = getDisplayNameLabelColor();

            applyDefaultPrefabState();
        }

        private void registerCallbacks()
        {
            _prefabPreviewUI.resetPreviewButton.clicked += () => { data.intRangePrefab.resetPreview(); };
            _prefabPreviewUI.pingButton.clicked         += () => { data.intRangePrefab.prefabAsset.pingPrefabAsset(); };

            _defaultPrefabButton.RegisterCallback<MouseUpEvent>((p) =>
            {
                var currentDefaultPick = data.intRangePrefabProfile.defaultPickPrefab;
                if (currentDefaultPick != null)
                {
                    if (currentDefaultPick == data.intRangePrefab) data.intRangePrefabProfile.setDefaultPickPrefab(null);
                    else
                    {
                        data.intRangePrefabProfile.setDefaultPickPrefab(data.intRangePrefab);
                        IntRangePrefabProfileDbUI.instance.onIntRangePrefabNeedsUIRefresh(currentDefaultPick);
                    }
                }
                else data.intRangePrefabProfile.setDefaultPickPrefab(data.intRangePrefab);

                applyDefaultPrefabState();
            });
        }
    }
}
#endif