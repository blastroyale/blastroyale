using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumSpecialConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "SpecialConfigs", menuName = "ScriptableObjects/Configs/SpecialConfigs")]
	public class SpecialConfigs : QuantumSpecialConfigsAsset, IConfigsContainer<QuantumSpecialConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumSpecialConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}