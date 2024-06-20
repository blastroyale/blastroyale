using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="BaseEquipmentStatConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BaseEquipmentStatConfigs",
	                 menuName = "ScriptableObjects/Configs/BaseEquipmentStatConfigs")]
	public class BaseEquipmentStatConfigs : QuantumBaseEquipmentStatConfigsAsset,
	                                         IConfigsContainer<QuantumBaseEquipmentStatConfig>
	{
		public List<QuantumBaseEquipmentStatConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}