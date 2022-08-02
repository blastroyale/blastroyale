using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumEquipmentMaterialStatsConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "EquipmentMaterialStatsConfigs", menuName = "ScriptableObjects/Configs/EquipmentMaterialStatsConfigs")]
	public class EquipmentMaterialStatsConfigs : QuantumEquipmentMaterialStatsConfigsAsset,
	                                     IConfigsContainer<QuantumEquipmentMaterialStatsConfig>
	{
		public List<QuantumEquipmentMaterialStatsConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentMaterialStatsConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public virtual QuantumEquipmentMaterialStatsConfig GetConfig(Equipment equipment)
		{
			return Settings.GetConfig(equipment);
		}
	}
}