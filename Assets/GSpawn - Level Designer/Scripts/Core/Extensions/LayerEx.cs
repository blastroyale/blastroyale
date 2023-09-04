#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public static class LayerEx
    {
        private static SerializedObject     _tagManagerInstance = null;

        public static SerializedObject      tagManager
        {
            get
            {
                if (_tagManagerInstance == null) _tagManagerInstance = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                return _tagManagerInstance;
            }
        }

        // Based on the implementation details discussed here: https://forum.unity.com/threads/adding-layer-by-script.41970/
        public static void setLayerName(int layer, string name)
        {
            if (name == null || tagManager == null || LayerEx.isBuiltinLayer(layer)) return;

            var layers = tagManager.FindProperty("layers");
            if (layers == null) return;

            var layerProperty = layers.GetArrayElementAtIndex(layer);
            if (layerProperty.stringValue != name) layerProperty.stringValue = name;
            tagManager.ApplyModifiedProperties();
        }

        public static bool isLayerVisible(int layer)
        {
            return (Tools.visibleLayers & (1 << layer)) != 0;
        }

        public static bool isLayerHidden(int layer)
        {
            return (Tools.visibleLayers & (1 << layer)) == 0;
        }

        public static bool isPickingEnabled(int layer)
        {
            return (Tools.lockedLayers & (1 << layer)) == 0;
        }

        public static bool isPickingDisabled(int layer)
        {
            return (Tools.lockedLayers & (1 << layer)) != 0;
        }

        public static int getMinlayer()
        {
            return 0;
        }

        public static int getMaxLayer()
        {
            return 31;
        }

        public static bool isBitSet(int layerBits, int layer)
        {
            return (layerBits & (1 << layer)) != 0;
        }

        public static int setBit(int layerBits, int layer)
        {
            return layerBits | (1 << layer);
        }

        public static int clearBit(int layerBits, int layer)
        {
            return layerBits & (~(1 << layer));
        }

        public static bool isLayerValid(int layer)
        {
            return layer >= getMinlayer() && layer <= getMaxLayer();
        }

        public static void getLayerNames(List<string> layerNames)
        {
            layerNames.Clear();
            for (int layerIndex = 0; layerIndex <= 31; ++layerIndex)
            {
                string layerName = LayerMask.LayerToName(layerIndex);
                if (!string.IsNullOrEmpty(layerName)) layerNames.Add(layerName);
            }
        }

        public static bool isBuiltinLayer(int layer)
        {
            return !isUserLayer(layer);
        }

        public static bool isUserLayer(int layer)
        {
            return layer >= 6 || layer == 3;
        }
    }
}
#endif