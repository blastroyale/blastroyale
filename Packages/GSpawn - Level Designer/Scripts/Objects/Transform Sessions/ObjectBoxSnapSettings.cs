#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;

namespace GSpawn
{
    [Flags]
    public enum ObjectBoxSnapDestinationTypes
    {
        None = 0,
        Grid = 1,
        Mesh = 2,
        Sprite = 4,
        Terrain = 8,
        All = ~0
    }

    public class ObjectBoxSnapSettings : PluginSettings<ObjectBoxSnapSettings>
    {
        [SerializeField]
        private ObjectBoxSnapDestinationTypes       _destinationTypes           = defaultDestinationTypes;
        [SerializeField]
        private int                                 _destinationLayers          = defaultDestinationLayers;

        public ObjectBoxSnapDestinationTypes        destinationTypes            { get { return _destinationTypes; } set { _destinationTypes = value; EditorUtility.SetDirty(this); } }
        public bool                                 allowsGridDestination       { get { return (destinationTypes & ObjectBoxSnapDestinationTypes.Grid) != 0; } }
        public bool                                 allowsMeshDestination       { get { return (destinationTypes & ObjectBoxSnapDestinationTypes.Mesh) != 0; } }
        public bool                                 allowsSpriteDestination     { get { return (destinationTypes & ObjectBoxSnapDestinationTypes.Sprite) != 0; } }
        public bool                                 allowsTerrainDestination    { get { return (destinationTypes & ObjectBoxSnapDestinationTypes.Terrain) != 0; } }
        public bool                                 allowsObjectDestination     { get { return allowsMeshDestination || allowsSpriteDestination || allowsTerrainDestination; } }
        public int                                  destinationLayers           { get { return _destinationLayers; } set { _destinationLayers = value; EditorUtility.SetDirty(this); } }

        public static ObjectBoxSnapDestinationTypes defaultDestinationTypes     { get { return ObjectBoxSnapDestinationTypes.All; } }
        public static int                           defaultDestinationLayers    { get { return ~0; } }

        public override void useDefaults()
        {
            destinationTypes    = defaultDestinationTypes;
            destinationLayers   = defaultDestinationLayers;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            UI.createEnumFlagsField(typeof(ObjectBoxSnapDestinationTypes), "_destinationTypes", serializedObject, "Destination types",
                "Allows you to specify the types of entities that can be used as destination when snapping.", parent);
            UI.createLayerMaskField(destinationLayers, "_destinationLayers", serializedObject, "Destination layers",
                "Allows you to specify the layers that can be used as destination when snapping.", parent);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }
    }
}
#endif