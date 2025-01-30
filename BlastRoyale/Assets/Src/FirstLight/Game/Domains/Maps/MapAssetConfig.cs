using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.AssetImporter;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs
{
	[Serializable]
	[CreateAssetMenu(fileName = "MapConfig", menuName = "ScriptableObjects/Map/MapConfigEntry")]
	public class MapAssetConfig : ScriptableObject
	{
		public AssetReferenceScene Scene;
		public AssetReferenceT<MapAsset> QuantumMap;
		public AssetReferenceSprite MapPreview;
		public MapAreaConfig MapAreas;
	}

	[Serializable]
	public class MapAreaConfig
	{
		public AssetReferenceTexture2D AreaTextureRef;
		public List<MapAreaEntry> Areas;

		public string GetAreaName(Texture2D loadedtexture, Vector2 position)
		{
			var pixelX = Mathf.FloorToInt(loadedtexture.width * position.x);
			var pixelY = Mathf.FloorToInt(loadedtexture.height * position.y);

			// var pixelColor = (Color32) AreaTexture.GetPixel(pixelX, pixelY);
			var pixelColor = loadedtexture.GetPixels32()[pixelY * loadedtexture.width + pixelX];

			return Areas.FirstOrDefault(a => a.Color.CompareRGB(pixelColor))?.Name ?? string.Empty;
		}

		[Button]
		private void RefreshAreas()
		{
			var texture = AddressableUtils.LoadAddressableEditorTime(AreaTextureRef);
			var colors = new HashSet<Color32>();
			foreach (var c in texture.GetPixels32())
			{
				if (c is {r: 0, g: 0, b: 0}) continue; // Skip black
				colors.Add(c);
			}

			// Add new ones
			foreach (var c in colors)
			{
				if (Areas.Any(a => a.Color.CompareRGB(c))) continue;
				Areas.Add(new MapAreaEntry(c, string.Empty));
			}

			// Remove missing ones
			Areas.RemoveAll(a => !colors.Contains(a.Color));
		}
	}

	[Serializable]
	public class MapAreaEntry
	{
		public Color32 Color;
		public string Name;

		public MapAreaEntry(Color32 color, string name)
		{
			Color = color;
			Name = name;
		}
	}
}