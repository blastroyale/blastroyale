using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public struct QuantumEquipmentMaterialStatConfig
	{
		public GameIdGroup Category;
		public EquipmentMaterial Material;
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
	/// This is the quantum's asset config container for <see cref="QuantumEquipmentMaterialStatConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumEquipmentMaterialStatConfigs
	{
		public List<QuantumEquipmentMaterialStatConfig> QuantumConfigs = new List<QuantumEquipmentMaterialStatConfig>();

		private Dictionary<EquipmentMaterialStatsKey, QuantumEquipmentMaterialStatConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentMaterialStatConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public QuantumEquipmentMaterialStatConfig GetConfig(Equipment equipment)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<EquipmentMaterialStatsKey, QuantumEquipmentMaterialStatConfig>();
				
				foreach (var statsConfig in QuantumConfigs)
				{
					_dictionary
						.Add(new EquipmentMaterialStatsKey(statsConfig.Category, statsConfig.Material), statsConfig);
				}
			}

			return _dictionary[equipment.GetMaterialStatsKey()];
		}
	}

	/// <summary>
	/// A "unique" key that represents a set of <see cref="GameIdGroup"/> and <see cref="EquipmentMaterial"/>.
	/// </summary>
	public readonly struct EquipmentMaterialStatsKey : IEquatable<EquipmentMaterialStatsKey>
	{
		public readonly GameIdGroup Category;
		public readonly EquipmentMaterial Material;

		public EquipmentMaterialStatsKey(GameIdGroup category, EquipmentMaterial material)
		{
			Material = material;
			Category = category;
		}

		public static implicit operator int(EquipmentMaterialStatsKey key) => key.GetHashCode();

		public bool Equals(EquipmentMaterialStatsKey other)
		{
			return Category == other.Category && Material == other.Material;
		}

		public override bool Equals(object obj)
		{
			return obj is EquipmentMaterialStatsKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (int) Category;
				hashCode = (hashCode * 397) ^ (int) Material;
				return hashCode;
			}
		}
	}
	
	/// <summary>
	/// Helper methods to work with <see cref="QuantumEquipmentMaterialStatConfig"/> and <see cref="EquipmentMaterialStatsKey"/>.
	/// </summary>
	public static class EquipmentMaterialStatsHelpers
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
		/// Gets a unique <see cref="EquipmentMaterialStatsKey"/> for this config.
		/// </summary>
		public static EquipmentMaterialStatsKey GetKey(this QuantumEquipmentMaterialStatConfig config)
		{
			return new EquipmentMaterialStatsKey(config.Category, config.Material);
		}

		/// <summary>
		/// Gets a unique <see cref="EquipmentMaterialStatsKey"/> for this <see cref="Equipment"/>.
		/// </summary>
		public static EquipmentMaterialStatsKey GetMaterialStatsKey(this Equipment equipment)
		{
			return new EquipmentMaterialStatsKey(GetEquipmentGroup(equipment), equipment.Material);
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