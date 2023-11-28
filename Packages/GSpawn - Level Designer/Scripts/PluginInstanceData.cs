#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class PluginInstanceData : ScriptableObject
    {
        private static PluginInstanceData   _instance = null;

        private GSpawn                      _activePlugin;
        private bool                        _pluginInstancesCollected   = false;
        private List<GSpawn>                _plugins                    = new List<GSpawn>();

        public GSpawn activePlugin
        {
            get
            {
                if (!_pluginInstancesCollected) getPluginInstances();

                if (Selection.activeGameObject != null)
                {
                    var plugin = Selection.activeGameObject.getPlugin();
                    if (plugin != null) _activePlugin = plugin;
                }

                if (_activePlugin == null && _plugins.Count != 0)
                    _activePlugin = _plugins[0];

                return _activePlugin;
            }
        }
        public int numPlugins { get { return _plugins.Count; } }

        public static PluginInstanceData instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!FileSystem.folderExists(PluginFolders.pluginInternal)) return null;
                    _instance = AssetDbEx.loadScriptableObject<PluginInstanceData>(PluginFolders.pluginInternal);
                }
                return _instance;
            }
        }

        public bool isPlugin(GameObject gameObject)
        {
            foreach (var plugin in _plugins)
                if (plugin.gameObject == gameObject) return true;

            return false;
        }

        public void add(GSpawn plugin)
        {
            if (!_plugins.Contains(plugin))
                _plugins.Add(plugin);
        }

        public void remove(GSpawn plugin)
        {
            _plugins.Remove(plugin);
            if (plugin == _activePlugin) _activePlugin = null;
        }

        private void getPluginInstances()
        {
            _plugins.Clear();
            _plugins.AddRange(FindObjectsOfType<GSpawn>());
            _pluginInstancesCollected = true;
        }
    }
}
#endif