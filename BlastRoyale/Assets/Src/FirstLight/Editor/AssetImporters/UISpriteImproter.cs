using UnityEditor;

namespace FirstLight.Editor.AssetImporters
{
	public class UISpriteImproter : AssetPostprocessor
	{
		public void OnPreprocessTexture()
		{
			if (!assetImporter.assetPath.Contains("Assets/Art/UI/Sprites")) return;
			var textureImporter  = (TextureImporter)assetImporter;
			textureImporter.textureType = TextureImporterType.Sprite;
		}
	}
}