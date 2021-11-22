using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumFrontshotConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "FrontshotConfigs", menuName = "ScriptableObjects/Configs/FrontshotConfigs")]
	public class FrontshotConfigs : QuantumFrontshotConfigsAsset, IConfigsContainer<QuantumFrontshotConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumFrontshotConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}