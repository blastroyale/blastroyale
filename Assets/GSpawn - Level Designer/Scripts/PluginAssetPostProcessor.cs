#if UNITY_EDITOR
using UnityEditor;

namespace GSpawn
{
    public class PluginAssetPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (IntPatternDb.exists)                            IntPatternDb.instance.onPostProcessAllAssets();
            if (GridSettingsProfileDb.exists)                   GridSettingsProfileDb.instance.onPostProcessAllAssets();
            if (SegmentsObjectSpawnSettingsProfileDb.exists)    SegmentsObjectSpawnSettingsProfileDb.instance.onPostProcessAllAssets();
            if (IntRangePrefabProfileDb.exists)                 IntRangePrefabProfileDb.instance.onPostProcessAllAssets();
            if (RandomPrefabProfileDb.exists)                   RandomPrefabProfileDb.instance.onPostProcessAllAssets();
            if (ScatterBrushPrefabProfileDb.exists)               ScatterBrushPrefabProfileDb.instance.onPostProcessAllAssets();
            if (CurvePrefabProfileDb.exists)                    CurvePrefabProfileDb.instance.onPostProcessAllAssets();
            if (PrefabLibProfileDb.exists)                      PrefabLibProfileDb.instance.onPostProcessAllAssets();
            if (ShortcutProfileDb.exists)                       ShortcutProfileDb.instance.onPostProcessAllAssets();
        }
    }
}
#endif