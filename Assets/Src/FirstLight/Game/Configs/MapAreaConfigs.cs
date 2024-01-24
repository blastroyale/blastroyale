using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using UnityEngine;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public class MapAreaConfig
	{
		public GameId MapID;
		[Required] public Texture2D AreaTexture;

		public List<MapAreaEntry> Areas;

		public string GetAreaName(Vector2 position)
		{
			var pixelX = Mathf.FloorToInt(AreaTexture.width * position.x);
			var pixelY = Mathf.FloorToInt(AreaTexture.height * position.y);
			
			// var pixelColor = (Color32) AreaTexture.GetPixel(pixelX, pixelY);
			var pixelColor = AreaTexture.GetPixels32()[pixelY * AreaTexture.width + pixelX];
			
			return Areas.FirstOrDefault(a => a.Color.CompareRGB(pixelColor))?.Name ?? string.Empty;
		}

		[Button]
		private void RefreshAreas()
		{
			var colors = new HashSet<Color32>();
			foreach (var c in AreaTexture.GetPixels32())
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

	[IgnoreServerSerialization]
	[CreateAssetMenu(fileName = "MapAreaConfigs", menuName = "ScriptableObjects/Configs/MapAreaConfigs")]
	public class MapAreaConfigs : ScriptableObject
	{
		[SerializeField] private List<MapAreaConfig> _configs = new ();

		public MapAreaConfig GetMapAreaConfig(GameId map)
		{
			return _configs.FirstOrDefault(c => c.MapID == map);
		}
	}
}