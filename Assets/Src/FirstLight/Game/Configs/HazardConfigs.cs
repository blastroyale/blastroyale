using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumHazardConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "HazardConfigs", menuName = "ScriptableObjects/Configs/HazardConfigs")]
	public class HazardConfigs : QuantumHazardConfigsAsset, IConfigsContainer<QuantumHazardConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumHazardConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}