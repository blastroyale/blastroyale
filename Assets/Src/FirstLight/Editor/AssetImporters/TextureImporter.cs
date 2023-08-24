using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FirstLight.Editor.AssetImporters
{
	public class FLGTextureImporter : AssetPostprocessor
	{
		public void OnPreprocessTexture()
		{
			if (!assetImporter.assetPath.Contains("Assets/")) return;
			
			TextureImporter importer = assetImporter as TextureImporter;
			if (importer == null) return;
			if (importer.mipmapEnabled)
			{
				importer.mipmapEnabled = false;
				Debug.LogWarning($"Asset {assetImporter.assetPath} was imported using MipMaps which we dont need - disabling it ");
			}
		}
	}
}