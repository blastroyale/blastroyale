using UnityEditor;

namespace FirstLight.Editor.AssetImporters
{
	public class MeshImporter : AssetPostprocessor
	{
		public void OnPreprocessTexture()
		{
			if (!assetImporter.assetPath.Contains("Assets/")) return;
			
			ModelImporter importer = assetImporter as ModelImporter;
			if (importer == null) return;

			if (importer.meshOptimizationFlags != MeshOptimizationFlags.Everything || !importer.generateSecondaryUV)
			{
				importer.meshOptimizationFlags = MeshOptimizationFlags.Everything;
				importer.generateSecondaryUV = true;
				importer.SaveAndReimport();
			}
		}
	}
}