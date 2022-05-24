using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumEquipmentStatsConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "EquipmentStatsConfigs", menuName = "ScriptableObjects/Configs/EquipmentStatsConfigs")]
	public class EquipmentStatsConfigs : QuantumEquipmentStatsConfigsAsset,
	                                     IConfigsContainer<QuantumEquipmentStatsConfig>
	{
		public List<QuantumEquipmentStatsConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}


		// TODO mihak: Duplicated code with QuantumEquipmentStatsConfigs
		private readonly Dictionary<EquipmentStatsKey, QuantumEquipmentStatsConfig> _dictionary = new();
		private readonly HashSet<GameIdGroup> _validGroups = new();

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentStatsConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public QuantumEquipmentStatsConfig GetConfig(Equipment equipment)
		{
			if (_dictionary.Count == 0)
			{
				foreach (var statsConfig in Configs)
				{
					_dictionary
						.Add(new EquipmentStatsKey(statsConfig.Category, statsConfig.Adjective, statsConfig.Faction),
						     statsConfig);

					_validGroups.Add(statsConfig.Category);
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
	}
}