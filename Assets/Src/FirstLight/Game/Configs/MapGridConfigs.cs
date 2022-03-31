using System;
using System.Collections.Generic;
using UnityEngine;
using I2.Loc;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct MapGridConfig
	{
		public string AreaName;
		public int X;
		public int Y;

		/// <summary>
		/// Checks if is a valid Grid position to click
		/// </summary>
		public bool IsValid => !string.IsNullOrWhiteSpace(AreaName);
	}

	[Serializable]
	public struct MapGridRowConfig
	{
		public List<MapGridConfig> Row;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="MapConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MapGridConfigs", menuName = "ScriptableObjects/Configs/MapGridConfigs")]
	public class MapGridConfigs : ScriptableObject
	{
		[SerializeField] private List<MapGridRowConfig> _configs = new List<MapGridRowConfig>();

		/// <summary>
		/// Set's the <see cref="MapGridConfig"/> <paramref name="data"/>
		/// </summary>
		public void SetData(List<MapGridRowConfig> data)
		{
			_configs = data;
		}
		/// <summary>
		/// Requests the map grid size (Column/Rows Count)
		/// </summary>
		/// <param name="getMaxIndicesSizeInstead">If true, will return Count-1 instead, for use with indices.</param>
		public Vector2Int GetSize(bool getMaxIndicesSizeInstead = false)
		{
			int x = _configs[0].Row.Count;
			int y = _configs.Count;

			if (getMaxIndicesSizeInstead)
			{
				x -= 1;
				y -= 1;
			}
			
			return new Vector2Int(x, y);
		}

		/// <summary>
		/// Requests the <see cref="MapGridConfig"/> on the given data
		/// </summary>
		public MapGridConfig GetConfig(int x, int y)
		{
			return _configs[y].Row[x];
		}
		
		/// <summary>
		/// Requests the area name translated
		/// </summary>
		public string GetTranslation(string areaName)
		{
			return LocalizationManager.GetTranslation($"{nameof(ScriptTerms.MapDropPoints)}/{areaName}");
		}
	}
}