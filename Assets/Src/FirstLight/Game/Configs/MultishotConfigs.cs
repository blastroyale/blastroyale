using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumMultishotConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MultishotConfigs", menuName = "ScriptableObjects/Configs/MultishotConfigs")]
	public class MultishotConfigs : QuantumMultishotConfigsAsset, IConfigsContainer<QuantumMultishotConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumMultishotConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}