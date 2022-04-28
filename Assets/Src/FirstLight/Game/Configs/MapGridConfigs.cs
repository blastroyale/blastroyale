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
		public Vector2Int GetSize()
		{
			return new Vector2Int(_configs[0].Row.Count, _configs.Count);
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