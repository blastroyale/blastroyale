using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumBaseEquipmentStatsConfig
	{
		public GameId Id;

		public EquipmentManufacturer Manufacturer;
		public FP HpRatioToBase;
		public FP ArmorRatioToBase;
		public FP SpeedRatioToBase;
		public FP PowerRatioToBase;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumChestConfigs"/>
	/// TODO: Would be nice if we could somehow merge this with QuantumEquipmentStatsConfigs
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumBaseEquipmentStatsConfigs
	{
		public List<QuantumBaseEquipmentStatsConfig> QuantumConfigs = new List<QuantumBaseEquipmentStatsConfig>();

		private IDictionary<GameId, QuantumBaseEquipmentStatsConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumBaseEquipmentStatsConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumBaseEquipmentStatsConfig GetConfig(GameId id)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<GameId, QuantumBaseEquipmentStatsConfig>();

				for (var i = 0; i < QuantumConfigs.Count; i++)
				{
					_dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
				}
			}

			return _dictionary[id];
		}
	}
}