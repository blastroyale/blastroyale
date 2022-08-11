using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumEquipmentMaterialStatConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "EquipmentMaterialStatConfigs", menuName = "ScriptableObjects/Configs/EquipmentMaterialStatConfigs")]
	public class EquipmentMaterialStatConfigs : QuantumEquipmentMaterialStatConfigsAsset,
	                                     IConfigsContainer<QuantumEquipmentMaterialStatConfig>
	{
		public List<QuantumEquipmentMaterialStatConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentMaterialStatConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public virtual QuantumEquipmentMaterialStatConfig GetConfig(Equipment equipment)
		{
			return Settings.GetConfig(equipment);
		}
	}
}