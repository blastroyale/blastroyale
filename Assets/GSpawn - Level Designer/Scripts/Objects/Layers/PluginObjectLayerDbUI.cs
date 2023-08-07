#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace GSpawn
{
    public class PluginObjectLayerDbUI : PluginUI
    {
        private List<PluginObjectLayer>                                 _pluginLayerBuffer  = new List<PluginObjectLayer>();
        private List<int>                                               _unityLayerBuffer   = new List<int>();

        [SerializeField]
        private ListViewState                                           _layerViewState;
        private ListView<UIPluginObjectLayerItem, PluginObjectLayer>    _layerView;

        public static PluginObjectLayerDbUI instance { get { return PluginObjectLayerDb.instance.ui; } }

        protected override void onRefresh()
        {
            refreshLayerView();
        }

        protected override void onBuild()
        {
            createTopToolbar();
            createLayerView();
            refreshLayerView();
        }

        protected override void onEnabled()
        {
            if (_layerViewState == null)
            {
                _layerViewState         = ScriptableObject.CreateInstance<ListViewState>();
                _layerViewState.name    = GetType().Name + "_LayerViewState";
                AssetDbEx.addObjectToAsset(_layerViewState, PluginObjectLayerDb.instance);
            }
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_layerViewState);
        }

        protected override void onUndoRedo()
        {
            if (_layerView != null)
                refreshLayerView();
        }

        private void createTopToolbar()
        {
            var toolbar                 = new Toolbar();
            toolbar.style.flexShrink    = 0.0f;
            contentContainer.Add(toolbar);

            var button      = UI.createToolbarButton(TexturePool.instance.lightBulb, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(button);
            button.tooltip  = "Activate selected layers.";
            button.clicked  += () => { selectedLayers(_unityLayerBuffer); PluginScene.instance.setLayersActive(_unityLayerBuffer, true, true); };

            button          = UI.createToolbarButton(TexturePool.instance.lightBulbGray, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(button);
            button.tooltip  = "Deactivate selected layers.";
            button.clicked  += () => { selectedLayers(_unityLayerBuffer); PluginScene.instance.setLayersActive(_unityLayerBuffer, false, true); };

            button          = UI.createToolbarButton(TexturePool.instance.delete, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            UI.useDefaultMargins(button);
            button.tooltip  = "Delete selected layers. Note: Tile rule prefab instances will not be deleted.";
            button.clicked  += () => { selectedLayers(_unityLayerBuffer); PluginScene.instance.deleteLayers(_unityLayerBuffer); };

            UI.createFlexGrow(toolbar);
            button          = UI.createToolbarButton(TexturePool.instance.refresh, UI.ButtonStyle.Push, UIValues.smallToolbarButtonSize, toolbar);
            button.tooltip  = "Refresh.";
            button.style.marginTop = 2.0f;
            button.clicked  += () => { refresh(); };
        }

        private void createLayerView()
        {
            _layerView                      = new ListView<UIPluginObjectLayerItem, PluginObjectLayer>(_layerViewState, contentContainer);
            contentContainer.style.flexGrow = 1.0f; // Note: Must be here. Otherwise, the list view scroll bar shows up unnecessarily.
            _layerView.canRenameItems       = true;
            _layerView.canMultiSelect       = true;
        }

        private void refreshLayerView()
        {
            _layerView.onBeginBuild();
            var layers = new List<PluginObjectLayer>();
            PluginObjectLayerDb.instance.getLayers(layers);

            foreach (var l in layers)
                _layerView.addItem(l, true);

            _layerView.onEndBuild();
        }

        private void selectedLayers(List<int> layers)
        {
            _layerView.getSelectedItemData(_pluginLayerBuffer);
            PluginObjectLayerDb.instance.getUnityLayers(_pluginLayerBuffer, layers);
        }
    }
}
#endif