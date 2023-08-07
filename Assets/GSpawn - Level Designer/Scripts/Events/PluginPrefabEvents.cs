#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public static class PluginPrefabEvents
    {
        public static void onPrefabsWillBeRemoved(List<PluginPrefab> pluginPrefabs)
        {
            RandomPrefabProfileDb.instance.deletePrefabs(pluginPrefabs);
            IntRangePrefabProfileDb.instance.deletePrefabs(pluginPrefabs);
            ScatterBrushPrefabProfileDb.instance.deletePrefabs(pluginPrefabs);
            CurvePrefabProfileDb.instance.deletePrefabs(pluginPrefabs);
            TileRuleProfileDb.instance.deletePrefabs(pluginPrefabs);
            ModularWallPrefabProfileDb.instance.deletePrefabs(pluginPrefabs);
        }

        public static void onPrefabChangedName(PluginPrefab prefab)
        {
            RandomPrefabProfileDbUI.instance.refresh();
            IntRangePrefabProfileDbUI.instance.refresh();
            ScatterBrushPrefabProfileDbUI.instance.refresh();
            CurvePrefabProfileDbUI.instance.refresh();
            TileRuleProfileDbUI.instance.refresh();
            PluginPrefabManagerUI.instance.refresh();
            ModularWallPrefabProfileDbUI.instance.refresh();
        }
    }
}
#endif