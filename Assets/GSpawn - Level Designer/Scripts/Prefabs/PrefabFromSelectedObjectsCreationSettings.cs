#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public enum PrefabCreationPivot
    {
        Center = 0,
        CenterLeft,
        CenterRight,
        CenterTop,
        CenterBottom,
        CenterFront,
        CenterBack,
        FromPivotObject,
        TileRule
    }

    public class PrefabFromSelectedObjectsCreationSettings : PluginSettings<PrefabFromSelectedObjectsCreationSettings>
    {
        [SerializeField]
        private string                      _prefabName                 = defaultPrefabName;
        [SerializeField]
        private string                      _destinationFolder          = defaultDestinationFolder;
        [SerializeField]
        private PrefabCreationPivot         _pivot                      = defaultPivot;
        [SerializeField]
        private string                      _pivotObjectName            = defaultPivotObjectName;

        public string                       prefabName                  { get { return _prefabName; } set { UndoEx.record(this); if (value != null) _prefabName = value; EditorUtility.SetDirty(this); } }
        public string                       destinationFolder 
        { 
            get { return _destinationFolder; } 
            set 
            {
                if (!PluginFolders.validateFolderPathForClientUsage(value)) return;

                UndoEx.record(this); 
                if (value != null) _destinationFolder = value; 
                EditorUtility.SetDirty(this); 
            } 
        }
        public PrefabCreationPivot          pivot                       { get { return _pivot; } set { UndoEx.record(this); _pivot = value; EditorUtility.SetDirty(this); } }
        public string                       pivotObjectName             { get { return _pivotObjectName; } set { UndoEx.record(this); _pivotObjectName = value; EditorUtility.SetDirty(this); } }

        public static string                defaultPrefabName           { get { return string.Empty; } }
        public static string                defaultDestinationFolder    { get { return string.Empty; } }
        public static PrefabCreationPivot   defaultPivot                { get { return PrefabCreationPivot.CenterBottom; } }
        public static string                defaultPivotObjectName      { get { return string.Empty; } }

        public override void useDefaults()
        {
            prefabName          = defaultPrefabName;
            destinationFolder   = defaultDestinationFolder;
            pivot               = defaultPivot;
            pivotObjectName     = defaultPivotObjectName;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            UI.createTextField("_prefabName", serializedObject, "Prefab name", "The name of the prefab asset.", parent);
            var destFolderTextField = UI.createTextField("_destinationFolder", serializedObject, "Destination folder", "The folder in which the prefab asset will be created. You can drag and drop a folder into this field.", parent);
            destFolderTextField.registerDragAndDropCallback(()=>
            {
                if (!PluginDragAndDrop.initiatedByPlugin && PluginDragAndDrop.unityPaths.Length != 0 && 
                PluginFolders.validateFolderPathForClientUsage(PluginDragAndDrop.unityPaths[0]))
                {
                    destFolderTextField.value = PluginDragAndDrop.unityPaths[0];
                }
            }, DragAndDropVisualMode.Generic);

            UI.createEnumField(typeof(PrefabCreationPivot), "_pivot", serializedObject, "Pivot", "The prefab pivot. All objects that are part of the prefab will be made children " + 
                "of a single parent object. The pivot controls the position of the parent in relation to its children.", parent);

            UI.createTextField("_pivotObjectName", serializedObject, "Pivot object name", 
                "If this field is empty, or if an object with this name can not be found in the object selection, " + 
                "the plugin will ignore it and use the bounding volume of all selected objects to calculate the pivot. Otherwise, " + 
                "the plugin will use this object's bounding volume when calculating the pivot.", parent);
        }
    }
}
#endif