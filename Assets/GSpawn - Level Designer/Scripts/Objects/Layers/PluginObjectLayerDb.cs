#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class PluginObjectLayerDb : ScriptableObject
    {
        private static PluginObjectLayerDb  _instance;

        [NonSerialized]
        private PluginObjectLayerDbUI       _ui;
        [SerializeField]
        private PluginObjectLayer[]         _layers = new PluginObjectLayer[LayerEx.getMaxLayer() + 1];

        public int                          numLayers { get { return _layers.Length; } }
        public PluginObjectLayerDbUI        ui
        {
            get
            {
                if (_ui == null)
                    _ui = AssetDbEx.loadScriptableObject<PluginObjectLayerDbUI>(PluginFolders.pluginObjectLayers);

                return _ui;
            }
        }

        public static PluginObjectLayerDb   instance
        {
            get
            {
                if (_instance == null) _instance = AssetDbEx.loadScriptableObject<PluginObjectLayerDb>(PluginFolders.pluginObjectLayers);
                return _instance;
            }
        }
        public static bool exists { get { return _instance != null; } }

        public bool isLayerTerrainMesh(int index)
        {
            return getLayer(index).isTerrainMesh;
        }

        public bool isLayerSphericalMesh(int index)
        {
            return getLayer(index).isSphericalMesh;
        }

        public PluginObjectLayer getLayer(int index)
        {
            // Note: Do this here. Seems like we can't call addObjectToAsset in OnEnable.
            if (_layers[index] == null)
            {
                _layers[index]              = ScriptableObject.CreateInstance<PluginObjectLayer>();
                _layers[index].layerIndex   = index;
                _layers[index].name         = "Layer_" + index.ToString();
                AssetDbEx.addObjectToAsset(_layers[index], this);
                EditorUtility.SetDirty(this);
            }

            return _layers[index];
        }

        public void getLayers(List<PluginObjectLayer> allLayers)
        {
            allLayers.Clear();

            int numLayers = _layers.Length;
            for (int i = 0; i < numLayers; ++i)
                allLayers.Add(getLayer(i));
        }

        public void getUnityLayers(List<PluginObjectLayer> pluginLayers, List<int> unityLayers)
        {
            unityLayers.Clear();
            foreach (var pluginLayer in pluginLayers)
                unityLayers.Add(pluginLayer.layerIndex);
        }

        private void OnDestroy()
        {
            for (int i = LayerEx.getMinlayer(); i < LayerEx.getMaxLayer(); ++i)
                ScriptableObjectEx.destroyImmediate(_layers[i]);

            ScriptableObjectEx.destroyImmediate(_ui);
        }
    }
}
#endif