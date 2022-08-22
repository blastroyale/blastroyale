using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumStatConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "StatConfigs", menuName = "ScriptableObjects/Configs/StatConfigs")]
	public class StatConfigs : QuantumStatConfigsAsset, IConfigsContainer<QuantumStatConfig>
	{
		public List<QuantumStatConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}