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

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentStatsConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public QuantumEquipmentStatsConfig GetConfig(Equipment equipment)
		{
			return Settings.GetConfig(equipment);
		}
	}
}