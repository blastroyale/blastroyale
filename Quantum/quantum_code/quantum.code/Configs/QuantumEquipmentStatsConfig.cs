using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumEquipmentStatsConfig
	{
		public GameIdGroup Group;
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

		private Dictionary<EquipmentStatsKey, QuantumEquipmentStatsConfig> _dictionary =
			new Dictionary<EquipmentStatsKey, QuantumEquipmentStatsConfig>();

		private HashSet<GameIdGroup> _validGroups = new HashSet<GameIdGroup>();

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
						.Add(new EquipmentStatsKey(statsConfig.Group, statsConfig.Adjective, statsConfig.Faction),
						     statsConfig);

					_validGroups.Add(statsConfig.Group);
				}
			}

			return _dictionary
				[new EquipmentStatsKey(GetEquipmentGroup(equipment), equipment.Adjective, equipment.Faction)];
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

		private struct EquipmentStatsKey : IEquatable<EquipmentStatsKey>
		{
			public readonly GameIdGroup Group;
			public readonly EquipmentAdjective Adjective;
			public readonly EquipmentFaction Faction;

			public EquipmentStatsKey(GameIdGroup group, EquipmentAdjective adjective, EquipmentFaction faction)
			{
				Faction = faction;
				Group = group;
				Adjective = adjective;
			}

			public bool Equals(EquipmentStatsKey other)
			{
				return Group == other.Group && Adjective == other.Adjective && Faction == other.Faction;
			}

			public override bool Equals(object obj)
			{
				return obj is EquipmentStatsKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = (int) Group;
					hashCode = (hashCode * 397) ^ (int) Adjective;
					hashCode = (hashCode * 397) ^ (int) Faction;
					return hashCode;
				}
			}
		}
	}
}