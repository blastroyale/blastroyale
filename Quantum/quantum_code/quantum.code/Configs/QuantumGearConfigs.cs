using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumGearConfig
	{
		public GameId Id;
		public EquipmentRarity StartingRarity;
		public FP HpRatioToBase;
		public FP SpeedRatioToBase;
		public FP ArmorRatioToBase;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumGearConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumGearConfigs
	{
		public List<QuantumGearConfig> QuantumConfigs = new List<QuantumGearConfig>();
		
		private IDictionary<GameId, QuantumGearConfig> _dictionary = new Dictionary<GameId, QuantumGearConfig>();

		/// <summary>
		/// Requests the <see cref="QuantumGearConfig"/> of the given enemy <paramref name="gameId"/>
		/// </summary>
		public QuantumGearConfig GetConfig(GameId gameId)
		{
			if (_dictionary.Count == 0)
			{
				foreach (var config in QuantumConfigs)
				{
					_dictionary.Add(config.Id, config);
				}
			}

			return _dictionary[gameId];
		}
	}
}