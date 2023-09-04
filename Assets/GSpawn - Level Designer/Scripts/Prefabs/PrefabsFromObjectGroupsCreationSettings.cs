#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class PrefabsFromObjectGroupsCreationSettings : PluginSettings<PrefabsFromObjectGroupsCreationSettings>
    {
        [SerializeField]
        private string      _destinationFolder          = defaultDestinationFolder;

        public string       destinationFolder 
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

        public static string defaultDestinationFolder { get { return string.Empty; } }

        public override void useDefaults()
        {
            destinationFolder = defaultDestinationFolder;
            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {            
            var destFolderTextField = UI.createTextField("_destinationFolder", serializedObject, "Destination folder", "The folder in which the prefab assets will be created. You can drag and drop a folder into this field.", parent);
            destFolderTextField.registerDragAndDropCallback(() =>
            {
                if (!PluginDragAndDrop.initiatedByPlugin && PluginDragAndDrop.unityPaths.Length != 0 &&
                PluginFolders.validateFolderPathForClientUsage(PluginDragAndDrop.unityPaths[0]))
                {
                    destFolderTextField.value = PluginDragAndDrop.unityPaths[0];
                }
            }, DragAndDropVisualMode.Generic);
        }
    }
}
#endif