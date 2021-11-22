using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumConsumableConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "ConsumableConfigs", menuName = "ScriptableObjects/Configs/ConsumableConfigs")]
	public class ConsumableConfigs : QuantumConsumableConfigsAsset, IConfigsContainer<QuantumConsumableConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumConsumableConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}