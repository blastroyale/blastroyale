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
			TextureImporter importer = assetImporter as TextureImporter;
			if (importer == null) return;
			if (ApplySkinMipMaps(importer)) return;
			if (!assetImporter.assetPath.Contains("Assets/")) return;


			if (importer.mipmapEnabled)
			{
				importer.mipmapEnabled = false;
				Debug.LogWarning($"Asset {assetImporter.assetPath} was imported using MipMaps which we dont need - disabling it ");
			}
		}


		private bool ApplySkinMipMaps(TextureImporter importer)
		{
			if (importer.textureType == TextureImporterType.Sprite) return false;
			if (!assetImporter.assetPath.StartsWith("Assets/AddressableResources/Collections/CharacterSkins")) return false;
			importer.mipmapEnabled = true;
			importer.streamingMipmaps = true;
			importer.ignoreMipmapLimit = false;
			return true;

		}
	}
}