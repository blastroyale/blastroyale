using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumBaseEquipmentStatConfig
	{
		public GameId Id;
		public EquipmentManufacturer Manufacturer;
		public FP HpRatioToBase;
		public FP ArmorRatioToBase;
		public FP SpeedRatioToBase;
		public FP PowerRatioToBase;
		public FP AttackRangeRatioToBase;
		public FP PickupSpeedRatioToBase;
		public FP AmmoCapacityRatioToBase;
		public FP ShieldCapacityRatioToBase;

		/// <summary>
		/// Requests the stat value for the given <paramref name="statType"/>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Throws when the given <paramref name="statType"/> is not defined
		/// as part of <see cref="StatType"/> group</exception>
		public FP GetValue(StatType statType)
		{
			switch (statType)
			{
				case StatType.Health:
					return HpRatioToBase;
				case StatType.Power:
					return PowerRatioToBase;
				case StatType.Speed:
					return SpeedRatioToBase;
				case StatType.Armour:
					return ArmorRatioToBase;
				case StatType.AttackRange:
					return AttackRangeRatioToBase;
				case StatType.PickupSpeed:
					return PickupSpeedRatioToBase;
				case StatType.AmmoCapacity:
					return AmmoCapacityRatioToBase;
				case StatType.Shield:
					return ShieldCapacityRatioToBase;
				default:
					throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
			}
		}
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumChestConfigs"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumBaseEquipmentStatConfigs
	{
		public List<QuantumBaseEquipmentStatConfig> QuantumConfigs = new List<QuantumBaseEquipmentStatConfig>();

		private IDictionary<GameId, QuantumBaseEquipmentStatConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumBaseEquipmentStatConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumBaseEquipmentStatConfig GetConfig(GameId id)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<GameId, QuantumBaseEquipmentStatConfig>();

				for (var i = 0; i < QuantumConfigs.Count; i++)
				{
					_dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
				}
			}

			return _dictionary[id];
		}
	}
}