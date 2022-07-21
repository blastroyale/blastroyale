using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumChestConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "ChestConfigs", menuName = "ScriptableObjects/Configs/ChestConfigs")]
	public class ChestConfigs : QuantumChestConfigsAsset, IConfigsContainer<QuantumChestConfig>
	{
		public List<QuantumChestConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}