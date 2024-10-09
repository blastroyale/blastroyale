using System;
using System.Collections.Generic;
using Photon.Deterministic;

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

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			_dictionary = new Dictionary<string, QuantumMapConfig>();
			for (var i = 0; i < QuantumConfigs.Count; i++)
			{
				_dictionary.Add(QuantumConfigs[i].Map.ToString(), QuantumConfigs[i]);
			}
		}

		
		/// <summary>
		/// Requests the <see cref="QuantumMapConfig"/> from it's <paramref name="id"/>
		/// </summary>
		public QuantumMapConfig GetConfig(string id)
		{
			return _dictionary[id];
		}
	}
}