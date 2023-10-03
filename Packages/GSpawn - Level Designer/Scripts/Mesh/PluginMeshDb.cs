#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class PluginMeshDb : Singleton<PluginMeshDb>
    {
        private Dictionary<Mesh, PluginMesh> _meshMap = new Dictionary<Mesh, PluginMesh>();

        public void colectPluginMeshes(List<GameObject> gameObjects, List<PluginMesh> pluginMeshes)
        {
            pluginMeshes.Clear();
            foreach (var gameObject in gameObjects)
            {
                var mesh = gameObject.getMesh();
                if (mesh != null)
                {
                    var pluginMesh = this.getPluginMesh(mesh);
                    if (pluginMesh != null) pluginMeshes.Add(pluginMesh);
                }
            }
        }

        public PluginMesh getPluginMesh(Mesh unityMesh)
        {
            if (unityMesh == null) return null;

            if (_meshMap.ContainsKey(unityMesh)) return _meshMap[unityMesh];
            else
            {
                PluginMesh pluginMesh = new PluginMesh(unityMesh);
                _meshMap.Add(unityMesh, pluginMesh);
                return pluginMesh;
            }
        }

        public void onMeshAssetWillBeDeleted(Mesh meshAsset)
        {
            if (_meshMap.ContainsKey(meshAsset)) _meshMap.Remove(meshAsset);
        }

        public void handleNullRefs()
        {
            var newMap = new Dictionary<Mesh, PluginMesh>();
            foreach (var pair in _meshMap)
                if (pair.Key != null) newMap.Add(pair.Key, pair.Value);

            _meshMap.Clear();
            _meshMap = newMap;
        }
    }
}
#endif