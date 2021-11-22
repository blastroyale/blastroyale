using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumGearConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "GearConfigs", menuName = "ScriptableObjects/Configs/GearConfigs")]
	public class GearConfigs : QuantumGearConfigsAsset, IConfigsContainer<QuantumGearConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumGearConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}