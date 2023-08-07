#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;

namespace GSpawn
{
    [Flags]
    public enum ObjectVertexSnapDestinationTypes
    {
        None        = 0,
        Grid        = 1,
        Mesh        = 2,
        Sprite      = 4,
        Terrain     = 8,
        All         = ~0
    }

    public class ObjectVertexSnapSettings : PluginSettings<ObjectVertexSnapSettings>
    {
        [SerializeField]
        private ObjectVertexSnapDestinationTypes        _destinationTypes           = defaultDestinationTypes;
        [SerializeField]
        private int                                     _destinationLayers          = defaultDestinationLayers;

        public ObjectVertexSnapDestinationTypes         destinationTypes            { get { return _destinationTypes; } set { _destinationTypes = value; EditorUtility.SetDirty(this); } }
        public bool                                     allowsGridDestination       { get { return (destinationTypes & ObjectVertexSnapDestinationTypes.Grid) != 0; } }
        public bool                                     allowsMeshDestination       { get { return (destinationTypes & ObjectVertexSnapDestinationTypes.Mesh) != 0; } }
        public bool                                     allowsSpriteDestination     { get { return (destinationTypes & ObjectVertexSnapDestinationTypes.Sprite) != 0; } }
        public bool                                     allowsTerrainDestination    { get { return (destinationTypes & ObjectVertexSnapDestinationTypes.Terrain) != 0; } }
        public bool                                     allowsObjectDestination     { get { return allowsMeshDestination || allowsSpriteDestination || allowsTerrainDestination; } }
        public int                                      destinationLayers           { get { return _destinationLayers; } set { _destinationLayers = value; EditorUtility.SetDirty(this); } }

        public static ObjectVertexSnapDestinationTypes  defaultDestinationTypes     { get { return ObjectVertexSnapDestinationTypes.All; } }
        public static int                               defaultDestinationLayers    { get { return ~0; } }

        public override void useDefaults()
        {
            destinationTypes    = defaultDestinationTypes;
            destinationLayers   = defaultDestinationLayers;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            UI.createEnumFlagsField(typeof(ObjectVertexSnapDestinationTypes), "_destinationTypes", serializedObject, "Destination types",
                "Allows you to specify the types of entities that can be used as destination when snapping.", parent);
            UI.createLayerMaskField(destinationLayers, "_destinationLayers", serializedObject, "Destination layers", 
                "Allows you to specify the layers that can be used as destination when snapping.", parent);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }
    }
}
#endif