using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Infos;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct MapConfig
	{
		public int Id;
		public GameId Map;
		public GameMode GameMode;
		public int PlayersLimit;
		public int GameEndTarget;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="MapConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MapConfigs", menuName = "ScriptableObjects/Configs/MapConfigs")]
	public class MapConfigs : ScriptableObject, IConfigsContainer<MapConfig>
	{
		[SerializeField] private List<MapConfig> _configs = new List<MapConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<MapConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}