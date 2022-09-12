using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public struct QuantumEquipmentStatConfig
	{
		public GameIdGroup Category;
		public EquipmentAdjective Adjective;
		public EquipmentFaction Faction;
		public FP HpRatioToBaseK;
		public FP ArmorRatioToBaseK;
		public FP SpeedRatioToBaseK;
		public FP PowerRatioToBaseK;
		public FP AttackRangeRatioToBaseK;
		public FP PickupSpeedRatioToBaseK;

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
					return HpRatioToBaseK;
				case StatType.Power:
					return PowerRatioToBaseK;
				case StatType.Speed:
					return SpeedRatioToBaseK;
				case StatType.Armour:
					return ArmorRatioToBaseK;
				case StatType.AttackRange:
					return AttackRangeRatioToBaseK;
				case StatType.PickupSpeed:
					return PickupSpeedRatioToBaseK;
				default:
					throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
			}
		}
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumEquipmentStatConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumEquipmentStatConfigs
	{
		public List<QuantumEquipmentStatConfig> QuantumConfigs = new List<QuantumEquipmentStatConfig>();

		private Dictionary<EquipmentStatsKey, QuantumEquipmentStatConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentStatConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public QuantumEquipmentStatConfig GetConfig(Equipment equipment)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<EquipmentStatsKey, QuantumEquipmentStatConfig>();
			
				foreach (var statsConfig in QuantumConfigs)
				{
					_dictionary
						.Add(new EquipmentStatsKey(statsConfig.Category, statsConfig.Adjective, statsConfig.Faction),
						     statsConfig);
				}
			}

			return _dictionary[equipment.GetStatsKey()];
		}
	}

	/// <summary>
	/// A "unique" key that represents a set of <see cref="GameIdGroup"/>, <see cref="EquipmentAdjective"/>,
	/// and <see cref="EquipmentFaction"/>.
	/// </summary>
	public readonly struct EquipmentStatsKey : IEquatable<EquipmentStatsKey>
	{
		public readonly GameIdGroup Category;
		public readonly EquipmentAdjective Adjective;
		public readonly EquipmentFaction Faction;

		public EquipmentStatsKey(GameIdGroup category, EquipmentAdjective adjective, EquipmentFaction faction)
		{
			Faction = faction;
			Category = category;
			Adjective = adjective;
		}

		public static implicit operator int(EquipmentStatsKey key) => key.GetHashCode();

		public bool Equals(EquipmentStatsKey other)
		{
			return Category == other.Category && Adjective == other.Adjective && Faction == other.Faction;
		}

		public override bool Equals(object obj)
		{
			return obj is EquipmentStatsKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (int) Category;
				hashCode = (hashCode * 397) ^ (int) Adjective;
				hashCode = (hashCode * 397) ^ (int) Faction;
				return hashCode;
			}
		}
	}

	/// <summary>
	/// Helper methods to work with <see cref="QuantumEquipmentStatConfig"/> and <see cref="EquipmentStatsKey"/>.
	/// </summary>
	public static class EquipmentStatsHelpers
	{
		private static readonly HashSet<GameIdGroup> _validGroups = new HashSet<GameIdGroup>
		{
			GameIdGroup.Weapon,
			GameIdGroup.Helmet,
			GameIdGroup.Armor,
			GameIdGroup.Amulet,
			GameIdGroup.Shield
		};

		/// <summary>
		/// Gets a unique <see cref="EquipmentStatsKey"/> for this config.
		/// </summary>
		public static EquipmentStatsKey GetKey(this QuantumEquipmentStatConfig config)
		{
			return new EquipmentStatsKey(config.Category, config.Adjective, config.Faction);
		}

		/// <summary>
		/// Gets a unique <see cref="EquipmentStatsKey"/> for this <see cref="Equipment"/>.
		/// </summary>
		public static EquipmentStatsKey GetStatsKey(this Equipment equipment)
		{
			return new EquipmentStatsKey(GetEquipmentGroup(equipment), equipment.Adjective, equipment.Faction);
		}

		private static GameIdGroup GetEquipmentGroup(Equipment equipment)
		{
			foreach (var group in _validGroups)
			{
				if (equipment.GameId.IsInGroup(group))
				{
					return group;
				}
			}

			throw new NotSupportedException($"GameIdGroup for Equipment with GameId({equipment.GameId}) not found.");
		}
	}
}