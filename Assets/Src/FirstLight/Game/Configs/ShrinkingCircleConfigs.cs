using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumShrinkingCircleConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "ShrinkingCircleConfigs", menuName = "ScriptableObjects/Configs/ShrinkingCircleConfigs")]
	public class ShrinkingCircleConfigs : QuantumShrinkingCircleConfigsAsset, IConfigsContainer<QuantumShrinkingCircleConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumShrinkingCircleConfig> Configs
		{
			get => Settings.QuantumConfigs;
			set => Settings.QuantumConfigs = value;
		}
	}
}