using System;
using System.Linq;
using FirstLight.Editor.EditorTools;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.AssetImporters
{
	public class UISpriteImporter : AssetPostprocessor
	{
		private const string SPRITES_PATH = "Assets/Art/UI/Sprites";

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			var deletedSprite = deletedAssets.Any(deleted => deleted.StartsWith(SPRITES_PATH));
			if (deletedSprite)
			{
				EditorApplication.delayCall += EditorShortcuts.GenerateSpriteUss;
			}
		}

		private void OnPreprocessAsset()
		{
			if (!assetImporter.assetPath.Contains(SPRITES_PATH)) return;

			var svg = assetImporter as SVGImporter;
			if (svg == null) return;
			svg.SvgType = SVGType.UIToolkit;
		}

		public void OnPreprocessTexture()
		{
			if (!assetImporter.assetPath.Contains(SPRITES_PATH)) return;
			var textureImporter = (TextureImporter) assetImporter;
			textureImporter.textureType = TextureImporterType.Sprite;
		}

		private void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
		{
			if (!assetImporter.assetPath.Contains(SPRITES_PATH)) return;
			if (!assetImporter.importSettingsMissing) return; // No meta file, meaning its a new file
			EditorApplication.delayCall += EditorShortcuts.GenerateSpriteUss;
		}
	}
}