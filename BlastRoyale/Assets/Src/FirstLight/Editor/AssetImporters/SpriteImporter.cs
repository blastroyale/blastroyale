using UnityEditor;

namespace FirstLight.Editor.AssetImporters
{
	public class SpriteImporter : AssetPostprocessor
	{
		public void OnPreprocessTexture()
		{
			if (!assetImporter.assetPath.Contains("Assets/Art/Ui/Sprites")) return;
			
		}
	}
}