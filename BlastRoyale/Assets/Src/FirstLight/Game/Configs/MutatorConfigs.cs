using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumMutatorConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MutatorConfigs", menuName = "ScriptableObjects/Configs/MutatorConfigs")]
	public class MutatorConfigs : QuantumMutatorConfigsAsset, IConfigsContainer<QuantumMutatorConfig>
	{

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<QuantumMutatorConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}