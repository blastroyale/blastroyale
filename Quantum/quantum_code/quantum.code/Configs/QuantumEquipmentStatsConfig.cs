using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public struct QuantumEquipmentStatsConfig
	{
		public GameIdGroup Category;
		public EquipmentAdjective Adjective;
		public EquipmentFaction Faction;
		public FP HpRatioToBaseK;
		public FP ArmorRatioToBaseK;
		public FP SpeedRatioToBaseK;
		public FP PowerRatioToBaseK;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumEquipmentStatsConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumEquipmentStatsConfigs
	{
		public List<QuantumEquipmentStatsConfig> QuantumConfigs = new List<QuantumEquipmentStatsConfig>();

		private readonly Dictionary<EquipmentStatsKey, QuantumEquipmentStatsConfig> _dictionary =
			new Dictionary<EquipmentStatsKey, QuantumEquipmentStatsConfig>();

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentStatsConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public QuantumEquipmentStatsConfig GetConfig(Equipment equipment)
		{
			if (_dictionary.Count == 0)
			{
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
	/// Helper methods to work with <see cref="QuantumEquipmentStatsConfig"/> and <see cref="EquipmentStatsKey"/>.
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
		public static EquipmentStatsKey GetKey(this QuantumEquipmentStatsConfig config)
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