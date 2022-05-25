using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="BaseEquipmentStatsConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BaseEquipmentStatsConfigs",
	                 menuName = "ScriptableObjects/Configs/BaseEquipmentStatsConfigs")]
	public class BaseEquipmentStatsConfigs : QuantumBaseEquipmentStatsConfigsAsset,
	                                         IConfigsContainer<QuantumBaseEquipmentStatsConfig>
	{
		public List<QuantumBaseEquipmentStatsConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}