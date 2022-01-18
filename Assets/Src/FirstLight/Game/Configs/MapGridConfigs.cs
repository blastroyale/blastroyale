using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct MapGridConfig
	{
		public string Value;
		public int X;
		public int Y;
		public bool IsValid;
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
		/// Requests the map grid size
		/// </summary>
		public Pair<int, int> GetSize()
		{
			return new Pair<int, int>(_configs[0].Row.Count, _configs.Count);
		}

		/// <summary>
		/// Requests the <see cref="MapGridConfig"/> on the given data
		/// </summary>
		public MapGridConfig GetConfig(int x, int y)
		{
			return _configs[y].Row[x];
		}
	}
}