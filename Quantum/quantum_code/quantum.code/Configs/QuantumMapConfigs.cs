using System;
using System.Collections.Generic;

namespace Quantum
{
	[Serializable]
	public partial class QuantumMapConfig
	{
		public GameId Map;
		public int MaxPlayers;
		public bool IsTestMap;
		public bool IsCustomOnly;
		public float DropSelectionSize;
		public float MinimapCameraSize;
		public short LootingVersion;
		public bool IsLegacyMap;
	}
	
	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumMapConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumMapConfigs
	{
		public List<QuantumMapConfig> QuantumConfigs = new List<QuantumMapConfig>();
		
		private IDictionary<string, QuantumMapConfig> _dictionary;

		private object _lock = new object();
		
		/// <summary>
		/// Requests the <see cref="QuantumMapConfig"/> from it's <paramref name="id"/>
		/// </summary>
		public QuantumMapConfig GetConfig(string id)
		{
			if (_dictionary == null)
			{
				lock (_lock)
				{
					var dict = new Dictionary<string, QuantumMapConfig>();
					foreach (var config in QuantumConfigs)
					{
						dict.Add(config.Map.ToString(), config);
					}
					_dictionary = dict;
				}
			}
			return _dictionary[id];
		}
	}
}