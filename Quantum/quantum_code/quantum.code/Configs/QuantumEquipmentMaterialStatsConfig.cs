using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumEquipmentMaterialStatsConfig
	{
		public GameIdGroup Category;
		public EquipmentMaterial Material;
		public FP HpRatioToBaseK;
		public FP ArmorRatioToBaseK;
		public FP SpeedRatioToBaseK;
		public FP PowerRatioToBaseK;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumEquipmentMaterialStatsConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumEquipmentMaterialStatsConfigs
	{
		public List<QuantumEquipmentMaterialStatsConfig> QuantumConfigs = new List<QuantumEquipmentMaterialStatsConfig>();

		private Dictionary<EquipmentMaterialStatsKey, QuantumEquipmentMaterialStatsConfig> _dictionary =
			new Dictionary<EquipmentMaterialStatsKey, QuantumEquipmentMaterialStatsConfig>();

		private HashSet<GameIdGroup> _validGroups = new HashSet<GameIdGroup>();

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentMaterialStatsConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public QuantumEquipmentMaterialStatsConfig GetConfig(Equipment equipment)
		{
			if (_dictionary.Count == 0)
			{
				foreach (var statsConfig in QuantumConfigs)
				{
					_dictionary
						.Add(new EquipmentMaterialStatsKey(statsConfig.Category, statsConfig.Material),
						     statsConfig);

					_validGroups.Add(statsConfig.Category);
				}
			}

			return _dictionary
				[new EquipmentMaterialStatsKey(GetEquipmentGroup(equipment), equipment.Material)];
		}

		private GameIdGroup GetEquipmentGroup(Equipment equipment)
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

	public readonly struct EquipmentMaterialStatsKey : IEquatable<EquipmentMaterialStatsKey>
	{
		public readonly GameIdGroup Category;
		public readonly EquipmentMaterial Material;

		public EquipmentMaterialStatsKey(GameIdGroup category, EquipmentMaterial material)
		{
			Material = material;
			Category = category;
		}

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
}