#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class UIPluginObjectLayerItem : ListViewItem<PluginObjectLayer>
    {
        private Button              _erasableToggle;
        private Button              _sphericalMeshToggle;
        private Button              _terrainMeshToggle;

        public override PluginGuid  guid                    { get { return getItemId(data); } }
        public override string      displayName 
        {
            get
            {
                var pluginLayer = PluginObjectLayerDb.instance.getLayer(data.layerIndex);
                return pluginLayer.hasName ? pluginLayer.layerName : "[Unnamed]"; ;
            } 
        }

        public static PluginGuid getItemId(PluginObjectLayer objectLayer) 
        { 
            return objectLayer.guid; 
        }

        protected override void onRefreshUI()
        {
            applyVariableState();
        }

        protected override void onBuildUIBeforeDisplayName()
        {
            var label = new Label();
            Add(label);
            label.style.width = 60.0f;

            if (LayerEx.isBuiltinLayer(data.layerIndex))
            {
                label.text = "Builtin";
                label.tooltip = "Builtin layer " + data.layerIndex;
                label.SetEnabled(false);
            }
            else
            {
                label.text = "User";
                label.tooltip = "User layer " + data.layerIndex;
            }

            var indexLabel = new Label(data.layerIndex.ToString() + ": ");
            indexLabel.style.width = 20.0f;
            Add(indexLabel);
        }

        protected override void onBuildUIAfterDisplayName()
        {
            _displayNameLabel.style.flexGrow        = 1.0f;
            _renameField.style.flexGrow             = 1.0f;

            const float buttonRightMargin           = -2.0f;
            _erasableToggle                         = UI.createIconButton(TexturePool.instance.eraser, UIValues.smallIconSize, this);
            _erasableToggle.tooltip                 = "Toggle erasable.";
            _erasableToggle.style.marginRight       = buttonRightMargin;
            _erasableToggle.clicked                 += () => { data.isErasable = !data.isErasable; applyVariableState(); };

            _terrainMeshToggle                      = UI.createIconButton(TexturePool.instance.terrain, UIValues.smallIconSize, this);
            _terrainMeshToggle.tooltip              = "Toggle terrain mesh.";
            _terrainMeshToggle.style.marginRight    = buttonRightMargin;
            _terrainMeshToggle.clicked              += () => { data.isTerrainMesh = !data.isTerrainMesh; applyVariableState(); ObjectSelection.instance.refreshObjectSelectionUI(); };

            _sphericalMeshToggle                    = UI.createIconButton(TexturePool.instance.greenSphere, UIValues.smallIconSize, this);
            _sphericalMeshToggle.tooltip            = "Toggle spherical mesh.";
            _sphericalMeshToggle.clicked            += () => { data.isSphericalMesh = !data.isSphericalMesh; applyVariableState(); ObjectSelection.instance.refreshObjectSelectionUI(); };
            _sphericalMeshToggle.style.marginTop    = -0.5f;

            UI.createLineColumnSeparator(this);

            var actionButton                        = UI.createButton(TexturePool.instance.lightBulb, UI.ButtonStyle.Push, UIValues.smallButtonSize, this);
            actionButton.tooltip                    = "Activate layer.";
            actionButton.clicked                    += () => { PluginScene.instance.setLayerActive(data.layerIndex, true, true); };

            actionButton                            = UI.createButton(TexturePool.instance.lightBulbGray, UI.ButtonStyle.Push, UIValues.smallButtonSize, this);
            actionButton.tooltip                    = "Deactivate layer.";
            actionButton.clicked                    += () => { PluginScene.instance.setLayerActive(data.layerIndex, false, true); };

            actionButton                            = UI.createButton(TexturePool.instance.delete, UI.ButtonStyle.Push, UIValues.smallButtonSize, this);
            actionButton.tooltip                    = "Delete layer. Note: Tile rule prefab instances will not be deleted.";
            actionButton.clicked                    += () => { PluginScene.instance.deleteLayer(data.layerIndex); };

            applyVariableState();
        }

        protected override bool onCanRename()
        {
            return LayerEx.isUserLayer(data.layerIndex);
        }

        protected override void onEndRename(bool commit)
        {
            if (commit)
            {
                LayerEx.setLayerName(data.layerIndex, _renameField.text);
                applyVariableState();
            }
        }
        private void applyVariableState()
        {
            _displayNameLabel.text = displayName;
            if (data.hasName)
            {
                _displayNameLabel.style.color                   = UIValues.listItemTextColor.createNewAlpha(LayerEx.isBuiltinLayer(data.layerIndex) ? UIValues.disabledOpacity : 1.0f);
                _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
            else
            {
                _displayNameLabel.style.color                   = UIValues.importantInfoLabelColor.createNewAlpha(LayerEx.isBuiltinLayer(data.layerIndex) ? UIValues.disabledOpacity : 1.0f);
                _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            _erasableToggle.style.unityBackgroundImageTintColor         = data.isErasable ? Color.white : UIValues.disabledColor;
            _terrainMeshToggle.style.unityBackgroundImageTintColor      = data.isTerrainMesh ? Color.white : UIValues.disabledColor;
            _sphericalMeshToggle.style.unityBackgroundImageTintColor    = data.isSphericalMesh ? Color.white : UIValues.disabledColor;
        }
    }
}
#endif