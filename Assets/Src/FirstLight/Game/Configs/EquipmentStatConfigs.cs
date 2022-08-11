using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumEquipmentStatConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "EquipmentStatConfigs", menuName = "ScriptableObjects/Configs/EquipmentStatConfigs")]
	public class EquipmentStatConfigs : QuantumEquipmentStatConfigsAsset,
	                                     IConfigsContainer<QuantumEquipmentStatConfig>
	{
		public List<QuantumEquipmentStatConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentStatConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public virtual QuantumEquipmentStatConfig GetConfig(Equipment equipment)
		{
			return Settings.GetConfig(equipment);
		}
	}
}