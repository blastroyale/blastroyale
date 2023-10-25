#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using UnityEditor;

namespace GSpawn
{
    public class UIPluginPrefabItemData : IUIItemStateProvider
    {
        public PluginPrefab         prefab;
        public PrefabLib            prefabLib;

        public bool                 uiSelected          { get { return prefab.uiSelected; } set { prefab.uiSelected = value; } }
        public CopyPasteMode        uiCopyPasteMode     { get { return prefab.uiCopyPasteMode; } set { prefab.uiCopyPasteMode = value; } }
        public PluginGuid           guid                { get { return prefab.guid; } }
    }

    public class UIPluginPrefabItem : GridViewItem<UIPluginPrefabItemData>
    {
        private PrefabPreviewUI     _prefabPreviewUI;
        private Button              _objectGroupIconBtn;
        private VisualElement       _previewImage;
        private Label               _objectGroupLabel;
        private Vector2             _imageSize;

        public override string      displayName         { get { return data.prefab.prefabAsset.name; } }
        public override PluginGuid  guid                { get { return getItemId(data.prefab); } }
        public override Vector2     imageSize 
        { 
            get { return _imageSize; }
            set 
            { 
                _imageSize                      = value; 
                _previewImage.style.width       = value.x; 
                _previewImage.style.height      = value.y;
                style.maxWidth                  = _imageSize.x;
                applyObjectGroupControlStates(); 
            }
        }

        public static PluginGuid getItemId(PluginPrefab prefab) 
        { 
            return prefab.guid; 
        }

        public static void getItemIds(List<PluginPrefab> prefabs, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var prefab in prefabs)
                ids.Add(getItemId(prefab));
        }

        public override bool canBeCopyPasteSource() 
        { 
            return true; 
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onBuildUI()
        {
            _prefabPreviewUI                    = new PrefabPreviewUI();
            _prefabPreviewUI.initialize(this, _imageSize);
            _prefabPreviewUI.onRotatePreview    = (p) => { data.prefab.rotatePreview(p.mouseDelta); };
            _previewImage                       = _prefabPreviewUI.previewImage;

            _displayNameLabel                   = _prefabPreviewUI.prefabNameLabel;
            _displayNameLabel.style.color       = getDisplayNameLabelColor();

            addDragAndDropInitiator(_previewImage);

            var toolbar                         = new Toolbar();
            toolbar.style.flexWrap              = Wrap.NoWrap;
            toolbar.style.marginRight           = -1.0f;          // Note: Needed to actually clamp to the preview edge. Otherwise the preview background shows through.
            Add(toolbar);

            _objectGroupIconBtn                 = UI.createIconButton(TexturePool.instance.objectGroup, UIValues.smallIconSize, toolbar);
            _objectGroupIconBtn.style.marginTop = 2.0f;

            _objectGroupLabel                   = new Label();
            _objectGroupLabel.style.marginTop   = 2.0f;
            _objectGroupLabel.style.marginLeft  = 2.0f;
            toolbar.style.overflow              = Overflow.Hidden;       // Note: To clip the object group label.
            toolbar.Add(_objectGroupLabel);

            var rowSeparator                    = UI.createRowSeparator(_previewImage);
            rowSeparator.style.flexGrow         = 1.0f;
            rowSeparator.SetEnabled(false);         // Note: Disable to allow the preview image to respond to drag events.

            // Note: It seems that calling 'SetEnabled' above doesn't fix anything. So just make sure
            //       to add the row separator (which now covers the entire preview) to the list of
            //       drag and drop initiators.
            addDragAndDropInitiator(rowSeparator);

            /*
            _cutIcon = new VisualElement();
            _cutIcon.style.setBackgroundImage(TexturePool.instance.scissors, true);
            _cutIcon.setDisplayVisible(false);
            _previewImage.Add(_cutIcon);*/

            applyVariableStates();
            registerCallbacks();
        }

        private Color getDisplayNameLabelColor()
        {
            return UIValues.prefabPreviewNameLabelColor;
        }

        private void spawnPrefab()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            if (GSpawn.isActiveSelected)
            {
                if (toolId == LevelDesignToolId.ObjectSpawn)
                {
                    ObjectSpawn.instance.usePrefab(data.prefab);
                    SceneViewEx.focus();
                }
                else if (toolId == LevelDesignToolId.ObjectSelection)
                {
                    GameObject gameObject = data.prefab.spawn();
                    gameObject.placeInFrontOfCamera(PluginCamera.camera);
                    PluginScene.instance.grid.snapObjectAllAxes(gameObject);
                    ObjectSelection.instance.setSelectedObject(gameObject);

                    SceneViewEx.focus();
                }
            }
            else
            {
                GameObject gameObject = data.prefab.spawn();
                gameObject.placeInFrontOfCamera(PluginCamera.camera);
                PluginScene.instance.grid.snapObjectAllAxes(gameObject);
            }
        }

        private void applyObjectGroupControlStates()
        {
            if (data.prefab.hasObjectGroup)
            {
                _objectGroupLabel.text                          = data.prefab.objectGroup.gameObject.name;
                _objectGroupLabel.tooltip                       = "The prefab is linked to the '" + _objectGroupLabel.text + "' object group.";
                _objectGroupLabel.style.color                   = UIValues.listItemTextColor;
                _objectGroupLabel.style.unityFontStyleAndWeight = FontStyle.Normal;

                _objectGroupIconBtn.style.backgroundImage       = TexturePool.instance.objectGroupDelete;
                _objectGroupIconBtn.tooltip                     = "Break link to the '" + _objectGroupLabel.text + "' object group.";
            }
            else
            {
                _objectGroupLabel.text                          = "n/a";
                _objectGroupLabel.tooltip                       = "No object group is linked to this prefab.";
                _objectGroupLabel.style.color                   = UIValues.importantInfoLabelColor;
                _objectGroupLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                _objectGroupIconBtn.style.backgroundImage       = TexturePool.instance.objectGroup;
                _objectGroupIconBtn.tooltip                     = "No object group is linked to this prefab.";
            }
        }

        protected override void applyCopyPasteModeStates()
        {
            //_cutIcon.setDisplayVisible(false);
            if (copyPasteMode == CopyPasteMode.None)        _displayNameLabel.style.color = getDisplayNameLabelColor();
            else if (copyPasteMode == CopyPasteMode.Copy)   _displayNameLabel.style.color = UIValues.copySourceListItemTextColor;
            else if (copyPasteMode == CopyPasteMode.Cut)
            {
                _displayNameLabel.style.color = UIValues.copySourceListItemTextColor;
                //_cutIcon.setDisplayVisible(true);
            }
        }

        private void applyVariableStates()
        {
            _previewImage.style.backgroundImage     = data.prefab.previewTexture;
            _previewImage.tooltip                   = displayName + "\nModel size: " + data.prefab.modelSize.ToString("F3");
            _displayNameLabel.text                  = data.prefab.prefabAsset.name;

            applyObjectGroupControlStates();
            applyCopyPasteModeStates();
        }

        private void registerCallbacks()
        {
            _previewImage.RegisterCallback<MouseDownEvent>((p) =>
            {
                if (FixedShortcuts.ui_SpawnPrefabOnDblClick(p))
                {
                    spawnPrefab();
                    SceneView.lastActiveSceneView.Focus();
                }
                else if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                {
                    if (FixedShortcuts.ui_ReplaceSelectionOnClick(p))
                    {
                        ObjectSelection.instance.replaceSelection(data.prefab);
                        SceneViewEx.focus();
                    }
                    else if (FixedShortcuts.ui_selection_ReplaceWithSelectedPrefabsInManager(p))
                    {
                        var visibleSelectedPrefabs = new List<PluginPrefab>();
                        PluginPrefabManagerUI.instance.getVisibleSelectedPrefabs(visibleSelectedPrefabs);
                        ObjectSelection.instance.replaceSelection(visibleSelectedPrefabs);
                        SceneViewEx.focus();
                    }
                }
            });

            _prefabPreviewUI.resetPreviewButton.clicked += () => { data.prefab.resetPreview(); };
            _prefabPreviewUI.pingButton.clicked         += () => { data.prefab.prefabAsset.pingPrefabAsset(); };

            _objectGroupIconBtn.clicked += () =>
            {
                if (data.prefab.objectGroup != null)
                {
                    data.prefab.objectGroup = null;
                    applyObjectGroupControlStates();
                }
            };
        }
    }
}
#endif