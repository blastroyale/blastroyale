#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GSpawn
{
    public static class PluginDragAndDropTitles
    {
        public static string treeViewItem { get { return GSpawn.pluginName + ".TreeViewItem"; } }
        public static string listViewItem { get { return GSpawn.pluginName + ".ListViewItem"; } }
        public static string gridViewItem { get { return GSpawn.pluginName + ".GridView"; } }
    }

    public static class PluginDragAndDrop
    {
        public delegate void    BeginHandler    ();
        public static event     BeginHandler    began;

        public delegate void    EndHandler      ();
        public static event     EndHandler      ended;

        private static string                   _title              = string.Empty;
        private static int                      _initiatorId;
        private static System.Object            _pluginData;

        public static System.Object             pluginData          { get { return _pluginData; } }
        public static string                    title               { get { return _title; } }
        public static bool                      initiatedByPlugin   { get { return (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) && (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0); } }
        public static string[]                  unityPaths          { get { return DragAndDrop.paths; } }
        public static UnityEngine.Object[]      unityObjects        { get { return DragAndDrop.objectReferences; } }
        public static DragAndDropVisualMode     visualMode          { get { return DragAndDrop.visualMode; } set { DragAndDrop.visualMode = value; } }
        public static int                       initiatorId         { get { return _initiatorId; } }

        public static void beginDrag(string title, int initiatorId, System.Object pluginData)
        {
            if (title != null) _title = title;
            else _title = string.Empty;

            _initiatorId    = initiatorId;
            _pluginData     = pluginData;
            if (began != null) began();

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.StartDrag(_title);
            DragAndDrop.objectReferences    = null;
            DragAndDrop.paths               = null;
        }

        public static void defaultUpdateVisualMode(EventType eventType)
        {
            if (eventType == EventType.DragUpdated) visualMode = DragAndDropVisualMode.Copy;
        }

        public static void endDrag()
        {
            _title = string.Empty;
            _pluginData = null;
            if (ended != null) ended();
        }

        public static bool isTitlePluginSpecific(string title)
        {
            return title == PluginDragAndDropTitles.treeViewItem ||
                title == PluginDragAndDropTitles.gridViewItem ||
                title == PluginDragAndDropTitles.listViewItem;
        }
    }
}
#endif